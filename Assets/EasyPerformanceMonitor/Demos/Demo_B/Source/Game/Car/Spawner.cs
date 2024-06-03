// Microsoft
using System.Collections.Generic;

// Unity
using UnityEngine;

namespace GUPS.EasyPerformanceMonitor.Demos.B
{
    public class Spawner : MonoBehaviour
    {
        /// <summary>
        /// The top spawn location.
        /// </summary>
        public Transform SpawnTop;

        /// <summary>
        /// The bottom spawn location.
        /// </summary>
        public Transform SpawnBottom;

        /// <summary>
        /// The left spawn location.
        /// </summary>
        public Transform SpawnLeft;

        /// <summary>
        /// The right spawn location.
        /// </summary>
        public Transform SpawnRight;

        /// <summary>
        /// Spawn intervall.
        /// </summary>
        public float SpawnIntervall = 2f;

        /// <summary>
        /// The GameObjects to spawn.
        /// </summary>
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
            // Roll random location.
            int var_SpawnLocationIndex = UnityEngine.Random.Range(0, 4);

            // Depending on location.
            Vector3 var_Position = Vector3.zero;
            Vector2 var_Direction = Vector2.zero;
            float var_Rotation = 0f;

            if (var_SpawnLocationIndex == 0)
            {
                var_Position = this.SpawnTop.position;
                var_Direction = new Vector2(0, -1);
                var_Rotation = 0f;
            }
            else if (var_SpawnLocationIndex == 1)
            {
                var_Position = this.SpawnBottom.position;
                var_Direction = new Vector2(0, 1);
                var_Rotation = 180f;
            }
            else if (var_SpawnLocationIndex == 2)
            {
                var_Position = this.SpawnLeft.position;
                var_Direction = new Vector2(1, 0);
                var_Rotation = -90f;
            }
            else if (var_SpawnLocationIndex == 3)
            {
                var_Position = this.SpawnRight.position;
                var_Direction = new Vector2(-1, 0);
                var_Rotation = 90f;
            }

            // Adjust position.
            var_Position += new Vector3(UnityEngine.Random.Range(-1f, 1f), 0, UnityEngine.Random.Range(-1f, 1f));

            // Roll random GameObject.
            int var_SpawnObjectIndex = UnityEngine.Random.Range(0, this.ObjectsToSpawn.Count);

            // Spawn a new GameObject.
            GameObject var_GameObject = Instantiate(this.ObjectsToSpawn[var_SpawnObjectIndex], var_Position, Quaternion.Euler(0f, var_Rotation, 0f));

            // Adjust movement.
            var_GameObject.GetComponent<MovingCar>().Direction = var_Direction;
            var_GameObject.GetComponent<MovingCar>().Speed = UnityEngine.Random.Range(1f, 4f);

            // Adjust color.
            var_GameObject.GetComponent<SizeBlockModelProvider>().Color = new Color(UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f));
        }
    }
}