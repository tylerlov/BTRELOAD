using UnityEngine;

namespace UltimateSpawner.Spawning
{
    public class EnemyTeleporter : MonoBehaviour
    {
        private SpawnTriggerVolume spawnTriggerVolume;
        private SpawnPoint[] spawnPoints;

        void Awake()
        {
            // Get the SpawnTriggerVolume component from the same GameObject
            spawnTriggerVolume = GetComponent<SpawnTriggerVolume>();

            // Get all SpawnPoint components from child GameObjects
            spawnPoints = GetComponentsInChildren<SpawnPoint>();
        }

        void OnTriggerExit(Collider other)
        {
            // Check if the exiting collider has the enemy tag
            if (other.CompareTag("Enemy"))
            {
                Debug.Log("Teleportation attempt started for enemy: " + other.name);
                // Teleport the enemy to a random spawn point
                TeleportEnemy(other.transform);
            }
        }

        private void TeleportEnemy(Transform enemyTransform)
        {
            if (spawnPoints.Length > 0)
            {
                // Select a random SpawnPoint
                int randomIndex = Random.Range(0, spawnPoints.Length);
                SpawnPoint randomSpawnPoint = spawnPoints[randomIndex];

                // Get the spawn location from the selected SpawnPoint
                Vector3 spawnLocation = randomSpawnPoint.transform.position;

                // Teleport the enemy
                enemyTransform.position = spawnLocation;
                Debug.Log("Teleportation successful to " + spawnLocation);
            }
            else
            {
                Debug.LogWarning(
                    "Teleportation failed: No SpawnPoints available for teleporting the enemy."
                );
            }
        }
    }
}
