using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FIMSpace.FOptimizing
{
    /// <summary>
    /// FM: Helper class for single LOD level settings on Unity's Collider Component
    /// </summary>
    [System.Serializable]
    public sealed class LODI_Collider : ILODInstance
    {
        #region Main Settings : Interface Properties

        public int Index { get { return index; } set { index = value; } }
        internal int index = -1;
        public string Name { get { return LODName; } set { LODName = value; } }
        internal string LODName = "";
        public bool CustomEditor { get { return false; } }
        public bool Disable { get { return SetDisabled; } set { SetDisabled = value; } }
        [HideInInspector] public bool SetDisabled = false;
        public bool DrawDisableOption { get { return true; } }
        public bool SupportingTransitions { get { return false; } }
        public bool DrawLowererSlider { get { return false; } }
        public float QualityLowerer { get { return 1f; } set { } }
        public string HeaderText { get { return "Collider Switch"; } }
        public bool SupportVersions { get { return false; } }
        public int DrawingVersion { get { return 1; } set { } }
        public float ToCullDelay { get { return 0f; } }
        public bool LockSettings { get { return _Locked; } set { _Locked = value; } }
        [HideInInspector] [SerializeField] private bool _Locked = false;
        public Texture Icon
        {
            get
            {
                return
#if UNITY_EDITOR
            EditorGUIUtility.IconContent("BoxCollider Icon").image;
#else
        null;
#endif
            }
        }

        public Component TargetComponent { get { return cmp; } }
        [SerializeField] [HideInInspector] private Collider cmp;
        
        #endregion


        public void SetSameValuesAsComponent(Component component)
        {
            Collider r = component as Collider;
            if (r == null) return;
            Disable = false;
        }


        public void ApplySettingsToTheComponent(Component component, ILODInstance initialSettings)
        {
            Collider coll = component as Collider;
            if (coll == null) return;
            if (Disable) coll.enabled = false; else coll.enabled = true;
        }


        public void AssignAutoSettingsAsForLODLevel(int lodIndex, int lodCount, Component source)
        {
            Disable = false;
        }


        public void AssignSettingsAsForCulled(Component component)
        {
            FLOD.AssignDefaultCulledParams(this);
        }


        public void AssignSettingsAsForNearest(Component component)
        {
            FLOD.AssignDefaultNearestParams(this);
            SetSameValuesAsComponent(component);
        }

        public ILODInstance GetCopy() { return MemberwiseClone() as ILODInstance; }

        public void AssignSettingsAsForHidden(Component component)
        {
            FLOD.AssignDefaultCulledParams(this);
            Name = "Hidden";
        }

        public void InterpolateBetween(ILODInstance lodA, ILODInstance lodB, float transitionToB)
        { }

        // Custom Inspector Features ---------------------------------------------


#if UNITY_EDITOR
        public void AssignToggler(ILODInstance reference)
        { }

        public void DrawTogglers(SerializedProperty iflodProp)
        { }

        public void CustomEditorWindow(SerializedProperty prop, LODsControllerBase lODsControllerBase)
        { }

        public void DrawVersionSwitch(SerializedProperty iflodProp, LODsControllerBase lODsControllerBase) { }

#endif

    }
}