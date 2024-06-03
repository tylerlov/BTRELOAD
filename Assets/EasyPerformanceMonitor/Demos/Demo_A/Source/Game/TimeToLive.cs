// Microsoft
using System;

// Unity
using UnityEngine;

namespace GUPS.EasyPerformanceMonitor.Demos.A
{
    [Serializable]
    public class TimeToLive : MonoBehaviour
    {
        public MeshRenderer MeshRenderer;

        public ParticleSystem ParticleSystem;

        [SerializeField]
        public float TimeToLiveInSeconds = 10f;

        private void Awake()
        {
            // Start the destroy coroutine directly.
            this.StartCoroutine(this.DestroyAfterTime());
        }

        private System.Collections.IEnumerator DestroyAfterTime()
        {
            // Wait for time to live.
            yield return new WaitForSeconds(this.TimeToLiveInSeconds);

            // Check if MeshRenderer and ParticleSystem are set.
            if (this.MeshRenderer != null && this.ParticleSystem != null)
            {
                // Disable MeshRenderer.
                this.MeshRenderer.enabled = false;

                // Set ParticleSystem color.
                var main = this.ParticleSystem.main;
                main.startColor = this.MeshRenderer.material.color;

                // Play ParticleSystem.
                this.ParticleSystem.Play();
            }

            // Wait for 5 seconds, so the ParticleSystem can finish.
            yield return new WaitForSeconds(5f);

            // Destroy GameObject finally.
            Destroy(this.gameObject);
        }
    }
}