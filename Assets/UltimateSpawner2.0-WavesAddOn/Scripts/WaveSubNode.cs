using System;
using System.Collections;
using System.Collections.Generic;
using UltimateSpawner.Spawning;
using UnityEngine;
using Random = UnityEngine.Random;

namespace UltimateSpawner.Waves
{
    [Serializable]
    [NodeTint(200, 180, 200)]
    [CreateNodeMenu("Waves/Sub-Wave")]
    public class WaveSubNode : WaveSpawnNode
    {
        // Private
        private WaveState previousState = null;
        private List<SpawnedItem> spawnedItems = null;
        private bool waitForItemSpawn = true;
        private int masterSpawnCount = 0;

        // Public   
        [Input(ShowBackingValue.Never)]
        public WaveSubNode In;

        [Tooltip("Amount of time to wait before starting this sub-wave")]
        public float SpawnDelay = 0f;

        [Tooltip("Wait for the specified number of items to be spawned by the main wave before starting this sub-wave")]
        public int SpawnCountDelay = 0;

        // Properties
        public override string NodeDisplayName
        {
            get { return "Sub-Wave"; }
        }

        // Methods
        public void UpdateState(WaveState previousState, List<SpawnedItem> spawnedItems, bool waitForItemSpawn)
        {
            this.previousState = previousState;
            this.spawnedItems = spawnedItems;
            this.waitForItemSpawn = waitForItemSpawn;
            this.masterSpawnCount = 0;
        }

        public void MasterSpawnedItem()
        {
            masterSpawnCount++;
        }

        public override IEnumerator Evaluate(WaveSpawnController controller)
        {
            // Wait for delay
            if (SpawnDelay > 0f)
                yield return WaitForSecondsNonAlloc.WaitFor(SpawnDelay);

            // Wait for required number of items spawned
            if(SpawnCountDelay > 0)
            {
                // Wait until condition is met
                while (masterSpawnCount < SpawnCountDelay)
                    yield return null;
            }

            // Try to find spawner
            Spawner targetSpawner = ResolveTargetSpawner(controller);

            // Try to find the spawnable item
            SpawnableItemRef targetItem = ResolveTargetSpawnableItemMultiple(controller, targetSpawner);

            if (targetItem != null)
                UltimateSpawning.Log("using spawnable (sub wave): " + targetItem.Name);

            // Get spawn count
            targetSpawnCount = GetInputValue(spawnCountField, spawnCount);

            // Check for multiplier mode
            if (waveMode == WaveSetupMode.Multiplier && previousState != null)
                targetSpawnCount = (int)(previousState.WaveSpawnCount * spawnCountMultiplier);

            targetSpawnFrequency = GetInputValue(spawnFrequencyField, spawnFrequency) * spawnFrequencyMultiplier;
            targetSpawnRandomness = GetInputValue(spawnRandomnessField, spawnRandomness) * spawnRandomnessMultiplier;

            // Spawn items
            for(int i = 0; i < targetSpawnCount; i++)
            {
                // Wait for random amount of time
                float delay = targetSpawnFrequency + Random.Range(0, targetSpawnRandomness);

                // Wait for time to padd
                yield return WaitForSecondsNonAlloc.WaitFor(delay);

                // Try to spawn an item
                IEnumerator itemSpawnRoutine = controller.ItemSpawnRoutine(targetSpawner, targetItem, spawnedItems);

                if(waitForItemSpawn == true)
                {
                    // Wait for spawn to complete
                    yield return controller.StartCoroutine(itemSpawnRoutine);
                }
                else
                {
                    // Fire and forget
                    controller.StartCoroutine(itemSpawnRoutine);
                }

                // Always wait a frame
                yield return null;
            }
        }
    }
}
