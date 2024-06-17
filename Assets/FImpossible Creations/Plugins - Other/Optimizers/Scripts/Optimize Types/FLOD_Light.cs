using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FIMSpace.FOptimizing
{
    /// <summary>
    /// FM: Helper class for single LOD level settings on Unity's Particle System
    /// </summary>
    //[CreateAssetMenu(menuName = "Custom Optimizers/FLOD_Light Reference")]
    public sealed class FLOD_Light : FLOD_Base
    {
        [Space(4)]
        //[Range(0f, 1f)]
        //[FPD_Percentage(0f,1f)]
        [Tooltip("Percentage value of light intensity for LOD level (percentage of initial light intensity)")]
        public float IntensityMul = 1f;
        //[Range(0f, 1f)]
        //[FPD_Percentage(0f,1f)]
        [Tooltip("Percentage value of light range for LOD level (percentage of initial light range)")]
        public float RangeMul = 1f;

        [Space(3)]
        public LightShadows ShadowsMode = LightShadows.Soft;
        //[Range(0f, 1f)]
        //[FPD_Percentage(0f,1f)]
        [Tooltip("Percentage value of shadows intensity for LOD level (percentage of initial shadow value)")]
        public float ShadowsStrength = 1f;
        public EOptLightMode RenderMode = EOptLightMode.Auto;

        public enum EOptLightMode : int
        {
            Auto = 0,
            Important = 1,
            NotImportant = 2
        }

        [HideInInspector]
        [Tooltip("If component should change intensity and range of light component (disable if you using flickering or something)")]
        public bool ChangeIntensity = true;

        #region Initialization


        public FLOD_Light()
        {
            SupportingTransitions = true;
            HeaderText = "Light LOD Settings";
            CustomEditor = true;
            // If you don't want to use transitions (InterpolateBetween) - then set "SupportingTransitions" to false
        }

        public override FLOD_Base GetLODInstance()
        {
            return CreateInstance<FLOD_Light>();
        }

        public override FLOD_Base CreateNewCopy()
        {
            FLOD_Light lightA = CreateInstance<FLOD_Light>();
            lightA.CopyBase(this);
            lightA.IntensityMul = IntensityMul;
            lightA.RangeMul = RangeMul;
            lightA.ShadowsMode = ShadowsMode;
            lightA.ShadowsStrength = ShadowsStrength;
            lightA.RenderMode = RenderMode;
            lightA.ChangeIntensity = ChangeIntensity;
            return lightA;
        }


        public override void SetSameValuesAsComponent(Component component)
        {
            if (component == null) Debug.LogError("[OPTIMIZERS] Given component is null instead of Light!");

            Light l = component as Light;

            if (l != null)
            {
                // Assigning component's true values to this LOD class instance
                IntensityMul = l.intensity;
                RangeMul = l.range;
                ShadowsMode = l.shadows;
                ShadowsStrength = l.shadowStrength;
                RenderMode = (EOptLightMode)l.renderMode;
            }
        }


        #endregion


        #region Operations


        /// <summary>
        /// Transitioning values between LOD levels
        /// </summary>
        public override void InterpolateBetween(FLOD_Base lodA, FLOD_Base lodB, float transitionToB)
        {
            base.InterpolateBetween(lodA, lodB, transitionToB);

            FLOD_Light a = lodA as FLOD_Light;
            FLOD_Light b = lodB as FLOD_Light;

            if (ChangeIntensity)
            {
                IntensityMul = Mathf.Lerp(a.IntensityMul, b.IntensityMul, transitionToB);
                RangeMul = Mathf.Lerp(a.RangeMul, b.RangeMul, transitionToB);
            }

            if (b.ShadowsMode == LightShadows.None) b.ShadowsStrength = 0f;

            ShadowsStrength = Mathf.Lerp(a.ShadowsStrength, b.ShadowsStrength, transitionToB);

            #region Toggling bools and enums

            if (b.ShadowsStrength > 0)
            {
                if (a.ShadowsMode == LightShadows.None)
                {
                    if (transitionToB >= 1)
                    {
                        RenderMode = b.RenderMode;
                    }
                }

                ShadowsMode = b.ShadowsMode;
            }

            if ((int)RenderMode == (int)LightRenderMode.ForcePixel)
            {
                if (transitionToB >= 1)
                    RenderMode = b.RenderMode;
            }
            else
            if ((int)b.RenderMode == (int)LightRenderMode.ForcePixel || (int)b.RenderMode == (int)LightRenderMode.Auto)
            {
                RenderMode = b.RenderMode;
            }

            if (transitionToB >= 1)
            {
                ShadowsMode = b.ShadowsMode;
                RenderMode = b.RenderMode;
            }
            else if (transitionToB <= 0f)
            {
                ShadowsMode = a.ShadowsMode;
                RenderMode = a.RenderMode;
            }

            #endregion
        }


        public override void ApplySettingsToComponent(Component component, FLOD_Base initialSettingsReference)
        {
            // Casting LOD to correct type and checking if it's right
            FLOD_Light initialSettings = initialSettingsReference as FLOD_Light;
            if (initialSettings == null) { Debug.Log("[OPTIMIZERS] Target LOD is not LightLOD or is null"); return; }

            Light l = component as Light;

            // Setting new settings to optimized component
            if (ChangeIntensity)
            {
                l.intensity = IntensityMul * initialSettings.IntensityMul;
                l.range = RangeMul * initialSettings.RangeMul;
            }

            l.shadowStrength = ShadowsStrength * initialSettings.ShadowsStrength;

            l.shadows = ShadowsMode;
            l.renderMode = (LightRenderMode)RenderMode;

            if (Disable) l.enabled = false; else l.enabled = true;
        }


        #endregion


        #region Auto Settings


        public override void SetAutoSettingsAsForLODLevel(int lodIndex, int lodCount, Component source)
        {
            Light l = source as Light;
            if (l == null) Debug.LogError("[OPTIMIZERS] Given component for reference values is null or is not Light Component!");

            // REMEMBER: LOD = 0 is not nearest but one after nearest
            // Trying to auto configure universal LOD settings

            float mul = GetValueForLODLevel(1f, 0f, lodIndex - 2, lodCount);

            name = "LOD" + (lodIndex + 2); // + 2 to view it in more responsive way for user inside inspector window

            if (lodIndex > 2 && lodCount > 2)
            {
                //IntensityMul = mul;
                RangeMul = mul;
                ShadowsStrength = mul;
            }

            ShadowsMode = l.shadows;
            RenderMode = (EOptLightMode)l.renderMode;

            if (lodCount == 2) if (l.shadows == LightShadows.Soft) ShadowsMode = LightShadows.Hard;
            if (lodCount > 2) if (l.shadows == LightShadows.Soft) ShadowsMode = LightShadows.Hard;

            if (l.renderMode == LightRenderMode.ForcePixel) RenderMode = EOptLightMode.Auto;

            if (lodIndex > 0) if (l.renderMode == LightRenderMode.ForcePixel) RenderMode = EOptLightMode.Auto;


            //if (lodCount > 2) if (lodIndex > 0) RenderMode = LightRenderMode.ForceVertex;
            if (lodIndex >= lodCount - 2 && lodCount > 2) { ShadowsMode = LightShadows.None; /*RenderMode = LightRenderMode.ForceVertex;*/ ShadowsStrength = 0f; }
            if (lodIndex >= 1 && lodCount == 3) RenderMode = EOptLightMode.NotImportant;
            if (lodIndex >= 2) RenderMode = EOptLightMode.NotImportant;

            if (RenderMode == EOptLightMode.NotImportant)
            {
                IntensityMul = 0.4f;
                RangeMul = 0.5f;
            }
        }


        public override void SetSettingsAsForCulled(Component component)
        {
            base.SetSettingsAsForCulled(component);
            IntensityMul = 0f;
            RangeMul = 0f;
            ShadowsStrength = 0f;
            ShadowsMode = LightShadows.None;
            RenderMode = EOptLightMode.NotImportant;
        }

        public override void SetSettingsAsForNearest(Component component)
        {
            base.SetSettingsAsForNearest(component);

            Light l = component as Light;
            ShadowsMode = l.shadows;
            RenderMode = (EOptLightMode)l.renderMode;
        }

        public override void SetSettingsAsForHidden(Component component)
        {
            base.SetSettingsAsForHidden(component);
            Disable = true;
        }


        #endregion


        public override void AssignToggler(FLOD_Base reference)
        {
            FLOD_Light l = reference as FLOD_Light;
            if (l != null) ChangeIntensity = l.ChangeIntensity;
        }


        public override void DrawTogglers(FComponentLODsController lodsController)
        {
#if UNITY_EDITOR
            UnityEditor.EditorGUILayout.BeginVertical();

            SerializedObject s = new SerializedObject(this);
            EditorGUILayout.PropertyField(s.FindProperty("ChangeIntensity"));
            s.ApplyModifiedProperties();
            s.Dispose();

            UnityEditor.EditorGUILayout.EndVertical();
#endif
        }

        public override void EditorWindow()
        {
#if UNITY_EDITOR

            bool pre = GUI.enabled;
            if ( !ChangeIntensity ) GUI.enabled = false;

            SerializedObject s = new SerializedObject(this);

            var prop = s.GetIterator();
            int safeLimit = 0;
            prop.NextVisible(true); // ignoring "Script" field
            prop.NextVisible(true); // ignoring "Deactivate" field

            if (!ChangeIntensity)
            {
                prop.NextVisible(true);
                prop.NextVisible(true);
            }

            GUI.enabled = pre;

            while (prop.NextVisible(true))
            {
                EditorGUILayout.PropertyField(prop);
                if (++safeLimit > 1000) break;
            }

            s.ApplyModifiedProperties();
            s.Dispose();

#endif
        }


        public override FComponentLODsController GenerateLODController(Component target, FOptimizer_Base optimizer)
        {
            Light light = target as Light;
            if (!light) light = target.gameObject.GetComponentInChildren<Light>();

            if (light) if (!optimizer.ContainsComponent(light))
                {
                    //float scaler = optimizer.transform.lossyScale.x;
                    
                    optimizer.DetectionRadius = light.range;
                    if (optimizer.transform.lossyScale.x != 0f) optimizer.DetectionRadius *= 1f / optimizer.transform.lossyScale.x;
                    
                    optimizer.DetectionBounds = Vector3.one * light.range * 1.8f;

                    if ( optimizer.transform != light.transform)
                    {
                        optimizer.DetectionOffset = optimizer.transform.InverseTransformPoint(light.transform.position);
                    }

                    return new FComponentLODsController(optimizer, light, "Light Properties", this);
                }

            return null;
        }

        internal void GetLODsController(object target, FOptimizer_Base fOptimizer_Base)
        {
            throw new NotImplementedException();
        }
    }
}