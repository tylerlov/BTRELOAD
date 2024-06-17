using UnityEngine;

namespace FIMSpace.FOptimizing
{
    /// <summary>
    /// FM: Helper class for single LOD level settings on ParticleSystem
    /// </summary>
    //[CreateAssetMenu(menuName = "Custom Optimizers/FLOD_ParticleSystem Reference")]
    public sealed class FLOD_ParticleSystem : FLOD_Base
    {
        [Space(4)]
        //[Range(0f, 1f)]
        [Tooltip("Percentage value of emmision rate for LOD level (percentage of initial emmission rate)")]
        public float EmmissionAmount = 1f;
        //[Range(0f, 1f)]
        [Tooltip("Percentage value of burst rates for LOD level (percentage of initial burst rates)")]
        public float BurstsAmount = 1f;

        //[Range(0f, 5f)]
        [Tooltip("Multiplier for particles size, if you make emmission smaller, particle size should become bigger to mask lower quality in distance")]
        public float ParticleSizeMul = 1f;

        /// <summary> List of bursts </summary>
        [SerializeField]
        [HideInInspector]
        private ParticleSystem.Burst[] Bursts;

        //[Range(0f, 1f)]
        [Tooltip("Percentage value of 'Max Particles' count for LOD level (percentage of initial 'Max Particles' count)")]
        public float MaxParticlAmount = 1f;

        [Tooltip("Percentage value of emmision rate over distance for LOD level (percentage of initial emmission rate)")]
        //[Range(0f, 1f)]
        public float OverDistanceMul = 1f;

        //[Range(0f, 1f)]
        [Tooltip("Percentage Alpha values of 'ColorOverLifetimeAlpha' for LOD level (percentage of initial 'ColorOverLifetimeAlpha' alpha keys on gradient)")]
        public float LifetimeAlpha = 1f;

        [SerializeField]
        [HideInInspector]
        private ParticleSystem.MinMaxGradient ColorOverLifetime;


        #region Initialization


        public FLOD_ParticleSystem()
        {
            DrawLowererSlider = true;
            SupportingTransitions = true;
            HeaderText = "Particle System LOD Settings";
        }


        public override FLOD_Base GetLODInstance()
        {
            FLOD_ParticleSystem lod = CreateInstance<FLOD_ParticleSystem>();
            lod.CopyBase(this);
            return lod;
        }


        public override FLOD_Base CreateNewCopy()
        {
            FLOD_ParticleSystem newP = CreateInstance<FLOD_ParticleSystem>();
            newP.CopyBase(this);
            newP.EmmissionAmount = EmmissionAmount;
            newP.OverDistanceMul = OverDistanceMul;
            newP.BurstsAmount = BurstsAmount;
            newP.Bursts = Bursts;
            newP.MaxParticlAmount = MaxParticlAmount;
            newP.LifetimeAlpha = LifetimeAlpha;
            newP.ColorOverLifetime = ColorOverLifetime;
            newP.ParticleSizeMul = ParticleSizeMul;
            return newP;
        }


        public override void SetSameValuesAsComponent(Component component)
        {
            if (component == null) Debug.LogError("[OPTIMIZERS] Given component is null instead of ParticleSystem!");

            ParticleSystem comp = component as ParticleSystem;

            if (comp != null)
            {
                EmmissionAmount = comp.emission.rateOverTimeMultiplier;
                OverDistanceMul = comp.emission.rateOverDistanceMultiplier;

                BurstsAmount = 1f;
                ParticleSystem.Burst[] bursts = new ParticleSystem.Burst[comp.emission.burstCount];
                comp.emission.GetBursts(bursts);
                Bursts = bursts;

                MaxParticlAmount = comp.main.maxParticles;

                LifetimeAlpha = 1f;
                ColorOverLifetime = comp.colorOverLifetime.color;

                ParticleSizeMul = comp.main.startSizeMultiplier;
            }
        }


        #endregion


        #region Operations

        public override void InterpolateBetween(FLOD_Base lodA, FLOD_Base lodB, float transitionToB)
        {
            base.InterpolateBetween(lodA, lodB, transitionToB);

            FLOD_ParticleSystem a = lodA as FLOD_ParticleSystem;
            FLOD_ParticleSystem b = lodB as FLOD_ParticleSystem;

            EmmissionAmount = Mathf.Lerp(a.EmmissionAmount, b.EmmissionAmount, transitionToB);
            OverDistanceMul = Mathf.Lerp(a.OverDistanceMul, b.OverDistanceMul, transitionToB);
            BurstsAmount = Mathf.Lerp(a.BurstsAmount, b.BurstsAmount, transitionToB);
            MaxParticlAmount = Mathf.Lerp(a.MaxParticlAmount, b.MaxParticlAmount, transitionToB);
            LifetimeAlpha = Mathf.Lerp(a.LifetimeAlpha, b.LifetimeAlpha, transitionToB);
            ParticleSizeMul = Mathf.Lerp(a.ParticleSizeMul, b.ParticleSizeMul, transitionToB);

        }


        public override void ApplySettingsToComponent(Component component, FLOD_Base initialSettingsReference)
        {
            // Casting LOD to correct type
            FLOD_ParticleSystem initialSettings = initialSettingsReference as FLOD_ParticleSystem;

            #region Security

            // Checking if casting is right
            if (initialSettings == null) { Debug.Log("[OPTIMIZERS] Target LOD is not ParticleSystem LOD or is null ("+component.name+")"); return; }

            #endregion

            ParticleSystem comp = component as ParticleSystem;
            ParticleSystemRenderer pr = comp.GetComponent<ParticleSystemRenderer>();

            if (Disable) pr.enabled = false; else pr.enabled = true;

            var emmission = comp.emission;
            var main = comp.main;

            emmission.rateOverTimeMultiplier = initialSettings.EmmissionAmount * EmmissionAmount;
            emmission.rateOverDistanceMultiplier = initialSettings.OverDistanceMul * OverDistanceMul;

            if (initialSettings.Bursts != null)
            {
                ParticleSystem.Burst[] bursts = new ParticleSystem.Burst[initialSettings.Bursts.Length];
                for (int i = 0; i < bursts.Length; i++)
                {
                    bursts[i] = initialSettings.Bursts[i];
                    bursts[i].minCount = (short)(initialSettings.Bursts[i].minCount * BurstsAmount);
                    bursts[i].maxCount = (short)(initialSettings.Bursts[i].maxCount * BurstsAmount);
                }

                emmission.SetBursts(bursts);
            }

            main.maxParticles = (int)(initialSettings.MaxParticlAmount * MaxParticlAmount);

            #region Lifetime color settings

            ParticleSystem.MinMaxGradient newColor = comp.colorOverLifetime.color;

            if (initialSettings.ColorOverLifetime.mode == ParticleSystemGradientMode.Gradient)
            {
                if (initialSettings.ColorOverLifetime.gradient != null)
                {
                    GradientAlphaKey[] keys = new GradientAlphaKey[initialSettings.ColorOverLifetime.gradient.alphaKeys.Length];
                    for (int i = 0; i < keys.Length; i++)
                    {
                        keys[i].alpha = initialSettings.ColorOverLifetime.gradient.alphaKeys[i].alpha * LifetimeAlpha;
                        keys[i].time = initialSettings.ColorOverLifetime.gradient.alphaKeys[i].time;
                    }

                    newColor.gradient.SetKeys(comp.colorOverLifetime.color.gradient.colorKeys, keys);
                }
            }
            else
            {
                if (initialSettings.ColorOverLifetime.gradientMin != null)
                {
                    GradientAlphaKey[] keys = new GradientAlphaKey[initialSettings.ColorOverLifetime.gradientMin.alphaKeys.Length];
                    for (int i = 0; i < keys.Length; i++)
                    {
                        newColor.gradientMin.alphaKeys[i].alpha = initialSettings.ColorOverLifetime.gradientMin.alphaKeys[i].alpha * LifetimeAlpha;
                        newColor.gradientMin.alphaKeys[i].time = initialSettings.ColorOverLifetime.gradientMin.alphaKeys[i].time;
                    }

                    newColor.gradientMin.SetKeys(comp.colorOverLifetime.color.gradient.colorKeys, keys);

                    keys = new GradientAlphaKey[initialSettings.ColorOverLifetime.gradientMax.alphaKeys.Length];
                    for (int i = 0; i < keys.Length; i++)
                    {
                        newColor.gradientMax.alphaKeys[i].alpha = initialSettings.ColorOverLifetime.gradientMax.alphaKeys[i].alpha * LifetimeAlpha;
                        newColor.gradientMax.alphaKeys[i].time = initialSettings.ColorOverLifetime.gradientMax.alphaKeys[i].time;
                    }

                    newColor.gradientMax.SetKeys(comp.colorOverLifetime.color.gradient.colorKeys, keys);
                }
            }

            var col = comp.colorOverLifetime;
            col.color = newColor;

            #endregion

            main.startSizeMultiplier = initialSettings.ParticleSizeMul * ParticleSizeMul;

            //if (Disable) comp.Stop(); else if (!comp.isPlaying) comp.Play();
            //if (Disable == false) if (!comp.isPlaying) comp.Play();

            ToCullDelay = comp.main.startLifetime.constantMax;

            //Debug.Log("Aplying " + EmmissionAmount + " total " + initialSettings.EmmissionAmount * EmmissionAmount + " initref em = " + initialSettings.EmmissionAmount + " to " + comp);
        }

        #endregion


        #region Auto Settings


        public override void SetAutoSettingsAsForLODLevel(int lodIndex, int lodCount, Component source)
        {
            ParticleSystem comp = source as ParticleSystem;
            if (comp == null) Debug.LogError("[OPTIMIZERS] Given component for reference values is null or is not ParticleSystem Component!");

            // REMEMBER: LOD = 0 is not nearest but one after nearest
            // Trying to auto configure universal LOD settings

            // Making multiplier even smaller for particle systems to change quality even lower automatically
            float mulNoLow = GetValueForLODLevel(1f, 0f, lodIndex, lodCount);
            float mul = mulNoLow * QualityLowerer;
            EmmissionAmount = mul;
            OverDistanceMul = mul;
            BurstsAmount = mul;
            MaxParticlAmount = Mathf.Min(1f, mulNoLow * 1.5f);
            ParticleSizeMul = 1.75f - mul * 0.75f;

            name = "LOD" + (lodIndex + 2); // + 2 to view it in more responsive way for user inside inspector window
        }


        public override void SetSettingsAsForCulled(Component component)
        {
            base.SetSettingsAsForCulled(component);
            EmmissionAmount = 0f;
            OverDistanceMul = 0f;
            BurstsAmount = 0f;
            MaxParticlAmount = 0f;
            ParticleSizeMul = 1.5f;
            LifetimeAlpha = 0f;
        }

        public override void SetSettingsAsForHidden(Component component)
        {
            base.SetSettingsAsForHidden(component);
            MaxParticlAmount = 0.1f;
        }

        #endregion


        public override FComponentLODsController GenerateLODController(Component target, FOptimizer_Base optimizer)
        {
            ParticleSystem p = target as ParticleSystem;
            if (!p) p = target.GetComponentInChildren<ParticleSystem>();
            if (p) if (!optimizer.ContainsComponent(p))
                {
                    return new FComponentLODsController(optimizer, p, "Particles", this);
                }

            return null;
        }
    }
}
