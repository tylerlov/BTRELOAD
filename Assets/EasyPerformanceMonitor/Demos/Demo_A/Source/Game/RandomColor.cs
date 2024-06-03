// Microsoft
using System;

// Unity
using UnityEngine;

namespace GUPS.EasyPerformanceMonitor.Demos.A
{
    [Serializable]
    public class RandomColor : MonoBehaviour
    {
        private void Awake()
        {
            // Assign random color to the MeshRenderer.
            this.GetComponent<Renderer>().material.color = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
        }
    }
}