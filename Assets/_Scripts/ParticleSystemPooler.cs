using System.Collections.Generic;
using UnityEngine;

namespace OccaSoftware.BOP
{
    public class ParticleSystemPooler : MonoBehaviour
    {
        [SerializeField] private ParticleSystem particleSystemPrefab = null;
        [SerializeField, Min(1)] private int initialCount = 10;
        [SerializeField] private Vector3 storagePosition = new Vector3(1000, 1000, 1000); // Offscreen or under the map

        private List<ParticleSystem> pool = new List<ParticleSystem>();

        private void Awake()
        {
            for (int i = 0; i < initialCount; i++)
            {
                CreateNewInstance();
            }
        }

        private ParticleSystem CreateNewInstance()
        {
            ParticleSystem newInstance = Instantiate(particleSystemPrefab, storagePosition, Quaternion.identity, transform);
            newInstance.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            newInstance.gameObject.SetActive(false); // Deactivate the GameObject
            pool.Add(newInstance);
            return newInstance;
        }

        public ParticleSystem GetFromPool()
        {
            foreach (var ps in pool)
            {
                if (!ps.isPlaying)
                {
                    ps.gameObject.SetActive(true); // Activate the GameObject when needed
                    ps.transform.position = storagePosition; // Reset position if needed
                    return ps;
                }
            }

            // If all instances are in use, create a new one and activate it
            ParticleSystem newInstance = CreateNewInstance();
            newInstance.gameObject.SetActive(true); // Ensure the new instance is active
            return newInstance;
        }

        public void ReturnToPool(ParticleSystem ps)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.transform.position = storagePosition;
            ps.gameObject.SetActive(false); // Deactivate the GameObject when returned to the pool
        }
    }
}