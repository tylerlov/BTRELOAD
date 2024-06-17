using UnityEngine;
using FIMSpace.Basics;

namespace FIMSpace.FOptimizing
{


    #region Scriptable Container for ILODInstance Instance

    // Create your LOD reference file throught 'Right Mouse button' -> Create -> Custom Optimizers -> ScrLOD_FBasic_RotateSpinSin Reference
    // Then move it to 'Resources/Optimizers/Custom' directory then it will be detected by Scriptable Optimizer
    // ALSO you have to rename this .cs file to 'ScrLOD_FBasic_RotateSpinSin'
    [CreateAssetMenu(menuName = "Custom Optimizers/ScrLOD_FBasic_RotateSpinSin Reference - Move it to Resources - Optimizers - Custom")]
    public sealed class ScrLOD_FBasic_RotateSpinSin : ScrLOD_Base
    {
        [SerializeField]
        private ILODInstance_FBasic_RotateSpinSin settings;
        public override ILODInstance GetLODInstance() { return settings; }
        public ScrLOD_FBasic_RotateSpinSin() { settings = new ILODInstance_FBasic_RotateSpinSin(); }

        public override ScrLOD_Base GetScrLODInstance()
        { return CreateInstance<ScrLOD_FBasic_RotateSpinSin>(); }


        public override ScrLOD_Base CreateNewScrCopy()
        {
            ScrLOD_FBasic_RotateSpinSin newA = CreateInstance<ScrLOD_FBasic_RotateSpinSin>();
            newA.settings = settings.GetCopy() as ILODInstance_FBasic_RotateSpinSin;
            return newA;
        }

        public override ScriptableLODsController GenerateLODController(Component target, ScriptableOptimizer optimizer)
        {
            FBasic_RotateSpinSin a = target as FBasic_RotateSpinSin;
            if (!a) a = target.GetComponentInChildren<FBasic_RotateSpinSin>();
            if (a) if (!optimizer.ContainsComponent(a))
                {
                    return new ScriptableLODsController(optimizer, a, -1, "FBasic_RotateSpinSin", this);
                }

            return null;
        }
    }

    #endregion



    // ILODInstance LOD INSTANCE CODE BELOW -----------------------------------------------


    [System.Serializable]
    public sealed class ILODInstance_FBasic_RotateSpinSin : ILODInstance
    {
        public string HeaderText { get { return "DEMO SpinSin LOD Settings"; } }
        public Texture Icon { get { return null; } }
        public bool SupportingTransitions { get { return true; } }// Will you implement supporting transitions?

        #region Main Settings : Interface Properties

        public int Index { get { return index; } set { index = value; } }
        internal int index = -1;
        public string Name { get { return LODName; } set { LODName = value; } }
        internal string LODName = "";
        public bool CustomEditor { get { return false; } }
        public bool Disable { get { return SetDisabled; } set { SetDisabled = value; } }
        [HideInInspector] public bool SetDisabled = false;
        public bool DrawDisableOption { get { return true; } }
        public bool DrawLowererSlider { get { return false; } }
        public float QualityLowerer { get { return 1f; } set { new System.NotImplementedException(); } }
        public int DrawingVersion { get { return 1; } set { new System.NotImplementedException(); } }
        public float ToCullDelay { get { return 0f; } }
        public Component TargetComponent { get { return cmp; } }
        public bool SupportVersions { get { return false; } }
        public bool LockSettings { get { return _Locked; } set { _Locked = value; } }
        [HideInInspector] [SerializeField] private bool _Locked = false;
        [SerializeField]
        [HideInInspector]
        private FBasic_RotateSpinSin cmp;

        #endregion


        [Range(0f, 1f)]
        public float RotationRange = 1f;


        public void SetSameValuesAsComponent(Component component)
        {
            if (component == null) return;

            FBasic_RotateSpinSin typeComponent = component as FBasic_RotateSpinSin;
            cmp = typeComponent;
            RotationRange = typeComponent.RotationRange;
            // ~YOUR CODE~ \\
        }


        public void InterpolateBetween(ILODInstance a, ILODInstance b, float transitionToB)
        {
            FLOD.DoBaseInterpolation(this, a, b, transitionToB);

            ILODInstance_FBasic_RotateSpinSin aa = a as ILODInstance_FBasic_RotateSpinSin;
            ILODInstance_FBasic_RotateSpinSin bb = b as ILODInstance_FBasic_RotateSpinSin;

            RotationRange = Mathf.Lerp(aa.RotationRange, bb.RotationRange, transitionToB);
            // ~YOUR CODE~ \\
        }


        public void ApplySettingsToTheComponent(Component component, ILODInstance initialSettings)
        {
            FBasic_RotateSpinSin comp = component as FBasic_RotateSpinSin;

            // Reference to initial settings collected when object was starting in playmode
            ILODInstance_FBasic_RotateSpinSin initials = initialSettings as ILODInstance_FBasic_RotateSpinSin;


            // Percentage change basing on initial value
            comp.RotationRange = initials.RotationRange * RotationRange;
            // ~YOUR CODE~ \\


            // Apply disable / enable component
            FLOD.ApplyEnableDisableState(this, component);
        }


        public void AssignAutoSettingsAsForLODLevel(int lodIndex, int lodCount, Component source)
        {
            FBasic_RotateSpinSin comp = source as FBasic_RotateSpinSin;
            if (comp == null) Debug.LogError("[OPTIMIZERS] Given component for reference values is null or is not AudioSource Component!");

            float mul = FLOD.GetValueForLODLevel(1f, 0f, lodIndex - 1, lodCount);

            // Your auto settings depending of LOD count
            // For example LOD count = 3, you want every next LOD go with parameters from 1f, to 0.6f, 0.3f, 0f - when culled
            if (lodIndex > 0) RotationRange = mul;

            // ~YOUR OPTIONAL AUTO CODE~ \\

        }


        public void AssignSettingsAsForCulled(Component component)
        {
            FLOD.AssignDefaultCulledParams(this);
            RotationRange = 0f;
            // ~YOUR OPTIONAL AUTO CODE~ \\
        }


        public void AssignSettingsAsForNearest(Component component)
        {
            FLOD.AssignDefaultNearestParams(this);
            RotationRange = 1f;
            // ~YOUR OPTIONAL AUTO CODE~ \\
        }

        public ILODInstance GetCopy() { return MemberwiseClone() as ILODInstance; }

        public void AssignSettingsAsForHidden(Component component)
        {
            FLOD.AssignDefaultHiddenParams(this);
            // ~YOUR OPTIONAL AUTO CODE~ \\
        }


        // Custom Inspector Features ---------------------------------------------


#if UNITY_EDITOR
        public void AssignToggler(ILODInstance reference)
        { }

        public void DrawTogglers(UnityEditor.SerializedProperty ILODInstanceProp)
        { }

        public void CustomEditorWindow(UnityEditor.SerializedProperty prop)
        { }

        public void DrawVersionSwitch(UnityEditor.SerializedProperty iflodProp, LODsControllerBase lODsControllerBase)
        { }

        public void CustomEditorWindow(UnityEditor.SerializedProperty iflodProp, LODsControllerBase lODsControllerBase)
        { }
#endif

    }


}

