using UnityEngine;

namespace FIMSpace.FOptimizing
{
    /// <summary>
    /// FM: Helper class for single LOD level settings on AudioSource
    /// </summary>
    //[CreateAssetMenu(menuName = "Custom Optimizers/FLOD_AudioSource Reference")]
    public sealed class FLOD_AudioSource : FLOD_Base
    {
        [Range(0f, 1f)]
        [Tooltip("Setted to zero will result with priority = 256 so marked as NOT important audio source, marked as 100% will result with priority level like audio source had when initialized")]
        public float PriorityFactor = 1f;

        [HideInInspector]
        public float Volume = 1f;

        private bool unPause = false;

        #region Initialization


        public FLOD_AudioSource()
        {
            SupportingTransitions = true;
            HeaderText = "AudioSource LOD Settings";
        }


        public override FLOD_Base GetLODInstance()
        {
            return CreateInstance<FLOD_AudioSource>();
        }


        public override FLOD_Base CreateNewCopy()
        {
            FLOD_AudioSource newA = CreateInstance<FLOD_AudioSource>();
            newA.CopyBase(this);
            newA.PriorityFactor = PriorityFactor;
            return newA;
        }


        public override void SetSameValuesAsComponent(Component component)
        {
            if (component == null) Debug.LogError("[OPTIMIZERS] Given component is null instead of AudioSource!");

            AudioSource comp = component as AudioSource;

            if (comp != null)
            {
                PriorityFactor = comp.priority;
                Volume = comp.volume;
            }
        }


        #endregion


        #region Operations



        public override void InterpolateBetween(FLOD_Base lodA, FLOD_Base lodB, float transitionToB)
        {
            base.InterpolateBetween(lodA, lodB, transitionToB);

            FLOD_AudioSource a = lodA as FLOD_AudioSource;
            FLOD_AudioSource b = lodB as FLOD_AudioSource;

            PriorityFactor = b.PriorityFactor;
            Volume = Mathf.Lerp(a.Volume, b.Volume, transitionToB);
        }


        public override void ApplySettingsToComponent(Component component, FLOD_Base initialSettingsReference)
        {
            // Casting LOD to correct type and checking if it's right
            FLOD_AudioSource initialSettings = initialSettingsReference as FLOD_AudioSource;
            if (initialSettings == null) { Debug.Log("[OPTIMIZERS] Target LOD is not AudioSource LOD or is null"); return; }

            AudioSource comp = component as AudioSource;
            comp.priority = (int)Mathf.Lerp(255, initialSettings.PriorityFactor, PriorityFactor);

            comp.volume = initialSettings.Volume * Volume;

            if (Disable)
            {
                if (comp.isPlaying) if (comp.loop)
                    {
                        comp.Pause();
                        unPause = true;
                    }

                comp.enabled = false;
            }
            else
            {
                if (unPause)
                {
                    unPause = false;
                    comp.UnPause();
                }

                comp.enabled = true;
            }
        }

        #endregion


        #region Auto Settings


        public override void SetAutoSettingsAsForLODLevel(int lodIndex, int lodCount, Component source)
        {
            AudioSource comp = source as AudioSource;
            if (comp == null) Debug.LogError("[OPTIMIZERS] Given component for reference values is null or is not AudioSource Component!");

            float mul = GetValueForLODLevel(1f, 0f, lodIndex - 1, lodCount);
            if (lodIndex > 0) PriorityFactor = mul;
            name = "LOD" + (lodIndex + 2); // + 2 to view it in more responsive way for user inside inspector window
            Volume = 1f;
        }


        /// AUTO SETTING SETTINGS FOR CULLED LOD
        public override void SetSettingsAsForCulled(Component component)
        {
            base.SetSettingsAsForCulled(component);
            PriorityFactor = 0f;
            Volume = 0f;
        }


        /// AUTO SETTING SETTINGS FOR NEAREST (HIGHEST QUALITY) LOD (DONT NEED TO DO THIS IF INITIAL VALUES FOR YOUR VARIABLES ARE ALREADY MAX)
        public override void SetSettingsAsForNearest(Component component)
        {
            base.SetSettingsAsForNearest(component);
            PriorityFactor = 1f;
            Volume = 1f;
        }


        #endregion


        public override FComponentLODsController GenerateLODController(Component target, FOptimizer_Base optimizer)
        {
            AudioSource a = target as AudioSource;
            if (!a) a = target.GetComponentInChildren<AudioSource>();
            if (a) if (!optimizer.ContainsComponent(a))
                {
                    return new FComponentLODsController(optimizer, a, "Audio Source", this);
                }

            return null;
        }
    }
}
