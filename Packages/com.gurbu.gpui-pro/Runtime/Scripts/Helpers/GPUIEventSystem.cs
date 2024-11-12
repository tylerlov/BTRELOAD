// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GPUInstancerPro
{
    [HelpURL("https://wiki.gurbu.com/index.php?title=GPU_Instancer_Pro:GettingStarted#GPUI_Event_System")]
    public class GPUIEventSystem : MonoBehaviour
    {
        public static GPUIEventSystem Instance { get; private set; }

        [SerializeField]
        public GPUICameraEvent OnPreCull;
        [SerializeField]
        public GPUICameraEvent OnPreRender;
        [SerializeField]
        public GPUICameraEvent OnPostRender;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogError("Duplicate GPUI Event System detected. Destroying second event system.", Instance);
                Destroy(gameObject);
                return;
            }
            else if (Instance == null)
                Instance = this;
        }

        private void OnEnable()
        {
            GPUIRenderingSystem.InitializeRenderingSystem();
            GPUIRenderingSystem.Instance.OnPreCull.AddListener(OnPreCull.Invoke);
            GPUIRenderingSystem.Instance.OnPreRender.AddListener(OnPreRender.Invoke);
            GPUIRenderingSystem.Instance.OnPostRender.AddListener(OnPostRender.Invoke);
        }

        private void OnDisable()
        {
            if (GPUIRenderingSystem.IsActive)
            {
                GPUIRenderingSystem.Instance.OnPreCull.RemoveListener(OnPreCull.Invoke);
                GPUIRenderingSystem.Instance.OnPreRender.RemoveListener(OnPreRender.Invoke);
                GPUIRenderingSystem.Instance.OnPostRender.RemoveListener(OnPostRender.Invoke);
            }
        }
    }
}