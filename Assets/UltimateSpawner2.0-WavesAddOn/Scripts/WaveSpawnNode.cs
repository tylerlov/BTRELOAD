using System;
using System.Collections.Generic;
using UltimateSpawner.Spawning;
using UnityEngine;

namespace UltimateSpawner.Waves
{
    public enum WaveSetupMode
    {
        Explicit,
        Multiplier,
    }

    [Serializable]
    public abstract class WaveSpawnNode : WaveNode
    {
        // Internal
        internal const string waveModeField = "waveMode";
        internal const string spawnCountField = "spawnCount";
        internal const string spawnFrequencyField = "spawnFrequency";
        internal const string spawnRandomnessField = "spawnRandomness";

        // Private
        private List<WaveSpawnableReferenceNode> spawnableReferenceNodes = new List<WaveSpawnableReferenceNode>();

        // Protected
        protected int targetSpawnCount = 0;
        protected float targetSpawnFrequency = 0;
        protected float targetSpawnRandomness = 0;

        // Public
        [Input(ShowBackingValue.Never)]
        public WaveSpawnerReferenceNode Spawner;

        [Input(ShowBackingValue.Never)]
        public WaveSpawnableReferenceNode Spawnable;

        [DisplayConditionMethod("IsMultiplierModeAllowed", DisplayType.Disabled)]
        public WaveSetupMode waveMode = WaveSetupMode.Explicit;
        
        [Input]
        [DisplayCondition(waveModeField, WaveSetupMode.Explicit)]
        public int spawnCount = 8;
        
        [Input]
        [DisplayCondition(waveModeField, WaveSetupMode.Multiplier)]
        public float spawnCountMultiplier = 1.5f;

        [Input]
        [DisplayCondition(waveModeField, WaveSetupMode.Explicit)]
        public float spawnFrequency = 4;

        [Input]
        [DisplayCondition(waveModeField, WaveSetupMode.Multiplier)]
        public float spawnFrequencyMultiplier = 0.9f;

        [Input]
        [DisplayCondition(waveModeField, WaveSetupMode.Explicit)]
        public float spawnRandomness = 2;
        
        [Input]
        [DisplayCondition(waveModeField, WaveSetupMode.Multiplier)]
        public float spawnRandomnessMultiplier = 0.85f;

        // Methods
        public override void OnGenerateWaveSession()
        {
            // Check for explicit mode
            if (waveMode == WaveSetupMode.Explicit)
            {
                targetSpawnCount = GetInputValue<int>(spawnCountField, spawnCount);
                targetSpawnFrequency = GetInputValue<float>(spawnFrequencyField, spawnFrequency);
                targetSpawnRandomness = GetInputValue<float>(spawnRandomnessField, spawnRandomness);
            }
        }

        public bool IsMultiplierModeAllowed()
        {
            bool result = IsFirstConnectedNode() == false;

            // Revert to explicit mode
            if (result == false)
                waveMode = WaveSetupMode.Explicit;

            return result;
        }

        public bool IsFirstConnectedNode()
        {
            if (InputPort != null)
            {
                WaveNode node = GetWaveNode(InputPort);

                if (node is WaveStartNode)
                    return true;
            }
            return false;
        }

        protected Spawner ResolveTargetSpawner(WaveSpawnController controller)
        {
            // Check for a connected node
            WaveSpawnerReferenceNode reference = GetWaveNode(GetInputPort(WaveSpawnerReferenceNode.spawnerPortName)) as WaveSpawnerReferenceNode;

            // Check for error
            if (reference != null)
            {
                // Try to resolve the spawner
                return reference.ResolveSpawnerReference(controller);
            }
            return null;
        }

        protected SpawnableItemRef ResolveTargetSpawnableItem(WaveSpawnController controller)
        {
            WaveSpawnableReferenceNode reference = GetWaveNode(GetInputPort(WaveSpawnableReferenceNode.spawnablePortName)) as WaveSpawnableReferenceNode;

            // Check for error
            if (reference != null)
            {
                // Try to resolve the spawner
                return reference.ResolveSpawnableReference(controller);
            }

            return null;
        }

        protected SpawnableItemRef ResolveTargetSpawnableItemMultiple(WaveSpawnController controller, Spawner spawner)
        {
            // Check for null spawner
            if (spawner == null)
                spawner = controller.spawner;

            // Check for no spawner
            if (spawner == null)
                return ResolveTargetSpawnableItem(controller);

            // Clear list
            spawnableReferenceNodes.Clear();

            // Get all nodes
            GetWaveNodes(GetInputPort(WaveSpawnableReferenceNode.spawnablePortName), spawnableReferenceNodes);

            // Check for any
            if (spawnableReferenceNodes.Count == 0)
                return null;

            // Check for single
            if (spawnableReferenceNodes.Count == 1)
                return spawnableReferenceNodes[0].ResolveSpawnableReference(controller);

            // Create mask
            SpawnableMask mask = new SpawnableMask();
            mask.SpawnableItems = spawner.SpawnableItems;
            mask.NoMask = false;
            mask.MaskAll();

            // Multiple requires additional selection
            foreach(WaveSpawnableReferenceNode referenceNode in spawnableReferenceNodes)
            {
                // Resolve the item ref
                SpawnableItemRef itemRef = referenceNode.ResolveSpawnableReference(controller);

                // Check for error
                if (itemRef == null)
                    continue;

                // Resolve item
                SpawnableItem foundItem = spawner.SpawnableItems.FindSpawnableItem(itemRef);

                // Check for error
                if (foundItem == null)
                    continue;

                // Mask the item
                mask.UnmaskItem(foundItem.SpawnableID);
            }

            // Select spawnable item
            SpawnableItem selectedItem = spawner.SpawnableItems.SelectSpawnableItem(mask);

            // Check for valid
            if (selectedItem == null)
                return null;

            // Create item reference
            return SpawnableItemRef.WithID(selectedItem.SpawnableID);
        }
    }
}
