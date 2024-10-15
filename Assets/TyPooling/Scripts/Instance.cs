using UnityEngine;
using System.Collections;

namespace Typooling
{
    public class Instance : MonoBehaviour
    {
        private Pooler origin;
        public Pooler GetPoolerOrigin() 
        { 
            return origin; 
        }

        private int index = -1;
        public int GetIndex() 
        { 
            return index; 
        }

        private ParticleSystem[] particleSystems;
        private bool particlesPlaying = false;

        internal void Setup(Pooler origin, int index)
        {
            this.origin = origin;
            this.index = index;
            particleSystems = GetComponentsInChildren<ParticleSystem>(true);
        }

        private Coroutine autoDisableCoroutine;

        public void PlayParticles()
        {
            if (particleSystems != null && !particlesPlaying)
            {
                for (int i = 0; i < particleSystems.Length; i++)
                {
                    particleSystems[i].Play();
                }
                particlesPlaying = true;
                StartAutoDisableCoroutine();
            }
        }

        private void StartAutoDisableCoroutine()
        {
            if (autoDisableCoroutine != null)
            {
                StopCoroutine(autoDisableCoroutine);
            }
            autoDisableCoroutine = StartCoroutine(AutoDisableAfterPlay());
        }

        private IEnumerator AutoDisableAfterPlay()
        {
            float longestDuration = 0f;
            foreach (var ps in particleSystems)
            {
                float totalDuration = ps.main.duration + ps.main.startLifetime.constantMax;
                if (totalDuration > longestDuration)
                {
                    longestDuration = totalDuration;
                }
            }

            yield return new WaitForSeconds(longestDuration);

            Despawn();
        }

        public void Despawn()
        {
            if (autoDisableCoroutine != null)
            {
                StopCoroutine(autoDisableCoroutine);
                autoDisableCoroutine = null;
            }

            if (particleSystems != null && particleSystems.Length > 0 && particlesPlaying)
            {
                StopParticles(true);
            }
            origin.ReturnToPool(this);
        }

        public void StopParticles(bool clear = true)
        {
            if (particleSystems != null && particlesPlaying)
            {
                for (int i = 0; i < particleSystems.Length; i++)
                {
                    if (clear)
                    {
                        particleSystems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    }
                    else
                    {
                        particleSystems[i].Stop(true, ParticleSystemStopBehavior.StopEmitting);
                    }
                }
                particlesPlaying = false;
            }
        }
    }
}
