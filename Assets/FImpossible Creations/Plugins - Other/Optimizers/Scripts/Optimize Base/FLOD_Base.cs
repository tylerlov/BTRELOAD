using System;
using UnityEngine;

namespace FIMSpace.FOptimizing
{
    /// <summary>
    /// FM: Base class for LOD settings for single LOD Level
    /// </summary>
    public abstract class FLOD_Base : ScriptableObject
    {
        /// <summary> Just for identification and debugging </summary>
        [HideInInspector]
        public bool CustomEditor = false;

        [Tooltip("If target component should be disabled (not game object) at 'Culled' LOD state.\n\nSometimes you want some of optimized components to be deactivated at certain LOD level and some only at Culled level.")]
        public bool Disable = false;
        [HideInInspector]
        public bool DrawDisableOption = true;

        [HideInInspector]
        public bool SupportingTransitions = false;

        [HideInInspector]
        public bool DrawLowererSlider = false;
        [HideInInspector]
        [Range(0f, 1f)]
        public float QualityLowerer = 1f;

        [HideInInspector]
        public string HeaderText = "";

        [HideInInspector]
        /// <summary> When using transitions, time to wait after transition time then cull it completely (for example time for particles to disappear after emmission) </summary>
        public float ToCullDelay = 0f;

        /// <summary> Can be used for custom scripting (see FLOD_MonoBehaviour) </summary>
        internal int Version = 0;

        #region Trivial Initialization Stuff

        public void CopyBase(FLOD_Base copyFrom)
        {
#if UNITY_EDITOR
            name = copyFrom.name + " Copy";
#endif
            Disable = copyFrom.Disable;
            CustomEditor = copyFrom.CustomEditor;
            QualityLowerer = copyFrom.QualityLowerer;
            DrawLowererSlider = copyFrom.DrawLowererSlider;
            DrawDisableOption = copyFrom.DrawDisableOption;
        }

        #endregion


        /// <summary>
        /// Interpolates LOD values between two LOD settings
        /// </summary>
        /// <param name="transitionToB"> Transition/Interpolation value from 0f to 1f </param>
        public virtual void InterpolateBetween(FLOD_Base lodA, FLOD_Base lodB, float transitionToB)
        {
            #region Base disable bool toggle operation and example code
            // Always do this by: base.InterpolateBetween(lodA, lodB, transitionToB);

            Disable = BoolTransition(Disable, lodA.Disable, lodB.Disable, transitionToB);

            //if (lodB.Disable == false && lodA.Disable == true) // Transitioning from culling to new LOD
            //{
            //    Disable = false;
            //}
            //else
            //{
            //    if (transitionToB >= 1f) Disable = lodB.Disable;
            //    else
            //    if (transitionToB <= 0f) Disable = lodA.Disable;
            //}

            //  Example code for interpolation  \\

            //base.InterpolateBetween(lodA, lodB, transitionToB);

            // Variables which can be interpolated:
            // variable = Mathf.Lerp(lodA.variable, lodB.variable, transitionToB);

            // Variables which can't be interpolated:
            //if ( transitionToB >= 1)
            //{
            //    boolOrEnum = transitionToB.boolOrEnum;
            //}
            //else if ( transitionToB <= 0)
            //{
            //    boolOrEnum = transitionToA.boolOrEnum;
            //}

            #endregion
        }


        /// <summary>
        /// Doing shallow copy of component's parameters as new instance
        /// </summary>
        public virtual FLOD_Base CreateNewCopy()
        {
            return (FLOD_Base)MemberwiseClone();
        }


        /// <summary>
        /// LOD = 0 is not nearest but one after nearest
        /// Try to auto configure universal LOD settings like from 1 (index) to 4 (lodCount) progressively lower values
        /// </summary>
        public virtual void SetAutoSettingsAsForLODLevel(int lodIndex, int lodCount, Component source)
        {
            #region Example Code

            //Light l = source as Light;
            //if (l == null) Debug.LogError("Given component for reference values is null or is not Light Component!");

            //// REMEMBER: LOD = 0 is not nearest but one after nearest
            //// Trying to auto configure universal LOD settings
            //FLOD_Light lodLevel = new FLOD_Light();

            //float mul = GetValueForLODLevel(1f, 0f, lodIndex - 2, lodCount); // Getting like percentage value / 100 for index / maxIndex

            //lodLevel.name = "LOD" + (lodIndex + 2); // + 2 to view it in more responsive way for user inside inspector window

            //if (lodIndex > 2 && lodCount > 2) // Changing intensity etc. when our LOD is far one (sometimes it will not even trigger)
            //{
            //    lodLevel.IntensityMul = mul;
            //    lodLevel.RangeMul = mul;
            //    lodLevel.ShadowsStrength = mul;
            //}

            //lodLevel.ShadowsMode = l.shadows; // before changes, here same settings like component's
            //lodLevel.RenderMode = l.renderMode;

            //if (lodCount > 2) if (l.shadows == LightShadows.Soft && l.shadows != LightShadows.None) lodLevel.ShadowsMode = LightShadows.Hard;
            //if (lodIndex >= lodCount - 2 && lodCount > 2) { lodLevel.ShadowsMode = LightShadows.None; lodLevel.ShadowsStrength = 0f; }

            #endregion
        }

        /// <summary>
        /// Mostly you will not need to change anything here, but to be sure you can adjust highest quality LOD here
        /// </summary
        public virtual void SetSettingsAsForNearest(Component component)
        {
#if UNITY_EDITOR
            name = "Nearest LOD";
#endif
        }


        /// <summary>
        /// Setting the same LOD fields values as non clamped values of target component
        /// </summary>
        public virtual void SetSameValuesAsComponent(Component target)
        {
            #region Example Code

            //if (target == null) Debug.LogError("Given component is null instead of Light!");

            //Light l = target as Light;

            //if (l != null)
            //{
            //    // Assigning component's true values to this LOD class instance
            //    IntensityMul = l.intensity;
            //    RangeMul = l.range;
            //    ShadowsMode = l.shadows;
            //    ShadowsStrength = l.shadowStrength;
            //    RenderMode = l.renderMode;
            //}

            #endregion
        }

        /// <summary>
        /// Basically setting setted properties on to component, you can reference from initial settings to set multiplied values
        /// </summary>
        public virtual void ApplySettingsToComponent(Component component, FLOD_Base initialSettingsReference)
        {
            Behaviour beh = component as Behaviour;

            if (beh)
            {
                if (Disable) beh.enabled = false; else beh.enabled = true;
            }
        }


        /// <summary>
        /// Generating new instance of LOD settings
        /// </summary>
        public virtual FLOD_Base GetLODInstance()
        {
            return null; // CreateInstance<FLOD_Base>();
        }

        /// <summary>
        /// Assigning LOD settings of culled state (disabled)
        /// </summary>
        public virtual void SetSettingsAsForCulled(Component component)
        {
#if UNITY_EDITOR
            name = "Culled";
#endif
            Disable = true;
        }

        /// <summary>
        /// Assigning LOD settings of hidden state (camera look away or hidden through code)
        /// </summary>
        public virtual void SetSettingsAsForHidden(Component component)
        {
#if UNITY_EDITOR
            name = "Hidden";
#endif
        }


        #region Utilities


        /// <summary>
        /// Helping transitioning not lerpable values
        /// </summary>
        protected static bool BoolTransition(bool defaultV, bool a, bool b, float transition)
        {
            if (b == false && a == true) // Transitioning from culling to new LOD
            {
                return false;
            }
            else
            {
                if (transition >= 1f) return b;
                else
                if (transition <= 0f) return a;
            }

            return defaultV;
        }


        /// <summary>
        /// Helping transitioning not lerpable values
        /// </summary>
        protected static object ObjectTransition(object defaultV, object a, object b, float transition)
        {
            if (transition >= 1f) return b;
            else
            if (transition <= 0f) return a;

            return defaultV;
        }


        /// <summary>
        /// Returning lerp value for lod level
        /// </summary>
        protected float GetValueForLODLevel(float from, float to, float lodLevel, float lodLevels)
        {
            return Mathf.Lerp(from, to, (lodLevel + 1f) / lodLevels);
        }

        /// <summary>
        /// Checking if target component is capable for this LOD class optimization
        /// then creating lod controller for target optimizer
        /// </summary>
        public virtual FComponentLODsController GenerateLODController(Component target, FOptimizer_Base optimizer)
        {
            #region Example code

            //Light light = target as Light;
            //if (!light) light = target.gameObject.GetComponentInChildren<Light>();
            //if (light) if (!optimizer.ContainsComponent(light))
            //    {
            //        return new FComponentLODsController(optimizer, light, "Light Properties", this);
            //    }

            //return null;

            #endregion

            return null;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Used only when replacing files when saving LOD set
        /// </summary>
        internal virtual bool IsTheSame(FLOD_Base lod)
        {
            bool same = true;

            Type t = GetType();
            if (t != lod.GetType()) same = false;

            if (same)
                foreach (var prop in t.GetProperties())
                {
                    same = prop.GetValue(this, null).Equals(prop.GetValue(lod, null));
                    if (!same)
                        break;
                }

            return same;
        }
#endif

        #endregion

        #region Editor Stuff

        /// <summary>
        /// Drawing LOD settings properties to be changed inside inspector window
        /// </summary>
        public virtual void EditorWindow()
        {

#if UNITY_EDITOR
            // Inspector window variables fields
#endif

        }

        /// <summary>
        /// Drawing editor gui enabled in first LOD level window (like enabling changing some variables like intensity in light)
        /// </summary>
        public virtual void DrawTogglers(FComponentLODsController lodsController)
        {
            //UnityEditor.EditorGUILayout.BeginVertical(FEditor.FEditor_StylesIn.Style(new Color(0.95f, 0.95f, 0.95f, 0.2f)));
            // Your GUI
            //UnityEditor.EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Assigning toggler variables at start
        /// </summary>
        public virtual void AssignToggler(FLOD_Base reference)
        {

        }

        #endregion
    }
}
