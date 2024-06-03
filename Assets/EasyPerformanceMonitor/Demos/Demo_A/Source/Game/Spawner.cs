// Microsoft
using System;
using System.Collections.Generic;

// Unity
using UnityEngine;

namespace GUPS.EasyPerformanceMonitor.Demos.A
{
    [Serializable]
    public class Spawner : MonoBehaviour
    {
        [SerializeField]
        public Vector3 MinSpawnPosition = new Vector3(0, 0, 0);

        [SerializeField]
        public Vector3 MaxSpawnPosition = new Vector3(10, 10, 10);

        /// <summary>
        /// Spawn intervall.
        /// </summary>
        [SerializeField]
        public float SpawnIntervall = 0.5f;

        /// <summary>
        /// The GameObjects to spawn.
        /// </summary>
        [SerializeField]
        public List<GameObject> ObjectsToSpawn;

        /// <summary>
        /// Start a repeating spawn method.
        /// </summary>
        private void Start()
        {
            this.InvokeRepeating("Spawn", 0, this.SpawnIntervall);
        }

        /// <summary>
        /// Spawn every x second at a random location.
        /// </summary>
        /// <returns></returns>
        private void Spawn()
        {
            // Find positon.
            Vector3 var_Position = Vector3.zero;
            
            // Set position in spawn area.
            var_Position.x = UnityEngine.Random.Range(this.MinSpawnPosition.x, this.MaxSpawnPosition.x);
            var_Position.y = UnityEngine.Random.Range(this.MinSpawnPosition.y, this.MaxSpawnPosition.y);
            var_Position.z = UnityEngine.Random.Range(this.MinSpawnPosition.z, this.MaxSpawnPosition.z);

            // Set rotation.
            Quaternion var_Rotation = Quaternion.Euler(UnityEngine.Random.Range(0f, 180f), UnityEngine.Random.Range(0f, 180f), UnityEngine.Random.Range(0f, 180f));

            // Roll random GameObject.
            int var_SpawnObjectIndex = UnityEngine.Random.Range(0, this.ObjectsToSpawn.Count);

            // Spawn a new GameObject.
            GameObject var_GameObject = Instantiate(this.ObjectsToSpawn[var_SpawnObjectIndex], var_Position, var_Rotation);
        }
    }
}