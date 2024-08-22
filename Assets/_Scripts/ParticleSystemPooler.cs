using System.Collections.Generic;
using UnityEngine;

namespace OccaSoftware.BOP
{
    public class ParticleSystemPooler : MonoBehaviour
    {
        [SerializeField]
        private ParticleSystem particleSystemPrefab = null;

        [SerializeField, Min(1)]
        private int initialCount = 10;

        [SerializeField]
        private Vector3 storagePosition = new Vector3(1000, 1000, 1000);

        private List<ParticleSystem> availableParticleSystems;
        private List<ParticleSystem> activeParticleSystems;

        private void Awake()
        {
            availableParticleSystems = new List<ParticleSystem>(initialCount);
            activeParticleSystems = new List<ParticleSystem>(initialCount);

            for (int i = 0; i < initialCount; i++)
            {
                CreateNewInstance();
            }
        }

        private void CreateNewInstance()
        {
            ParticleSystem newInstance = Instantiate(
                particleSystemPrefab,
                storagePosition,
                Quaternion.identity,
                transform
            );
            newInstance.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            newInstance.gameObject.SetActive(false);
            availableParticleSystems.Add(newInstance);
        }

        private void Update()
        {
            // Update active particle systems
            for (int i = activeParticleSystems.Count - 1; i >= 0; i--)
            {
                if (!activeParticleSystems[i].isPlaying)
                {
                    ReturnToPool(activeParticleSystems[i]);
                    activeParticleSystems.RemoveAt(i);
                }
            }
        }

        public ParticleSystem GetFromPool()
        {
            ParticleSystem ps;

            if (availableParticleSystems.Count > 0)
            {
                ps = availableParticleSystems[availableParticleSystems.Count - 1];
                availableParticleSystems.RemoveAt(availableParticleSystems.Count - 1);
            }
            else
            {
                CreateNewInstance();
                ps = availableParticleSystems[0];
                availableParticleSystems.RemoveAt(0);
            }

            ps.gameObject.SetActive(true);
            activeParticleSystems.Add(ps);
            return ps;
        }

        public void ReturnToPool(ParticleSystem ps)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.transform.position = storagePosition;
            ps.gameObject.SetActive(false);
            availableParticleSystems.Add(ps);
        }

        private void OnDestroy()
        {
            availableParticleSystems.Clear();
            activeParticleSystems.Clear();
        }
    }
}
