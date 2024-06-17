using UnityEngine;

namespace FIMSpace.FOptimizing
{
    /// <summary>
    /// FM: Helper class for single LOD level settings on Renderer
    /// </summary>
    //[CreateAssetMenu(menuName = "Custom Optimizers/FLOD_Renderer Reference")]
    public sealed class FLOD_Renderer : FLOD_Base
    {
        [Space(4)]
        [Tooltip("If model should cast and receive shadows (receive will be always false if renderer have it marked as false by default)")]
        public bool UseShadows = true;
        internal UnityEngine.Rendering.ShadowCastingMode ShadowsCast = UnityEngine.Rendering.ShadowCastingMode.On;
        internal bool ShadowsReceive;

        public MotionVectorGenerationMode MotionVectors = MotionVectorGenerationMode.Object;

        [Tooltip("If it is skinned mesh renderer we can switch bones weights spread quality")]
        public SkinQuality SkinnedQuality = SkinQuality.Auto;

        [SerializeField]
        [HideInInspector]
        private bool skinned;

        #region Initialization


        public FLOD_Renderer()
        {
            SupportingTransitions = false;
            HeaderText = "Renderer LOD Settings";
        }

        public override FLOD_Base GetLODInstance()
        {
            return CreateInstance<FLOD_Renderer>();
        }

        public override FLOD_Base CreateNewCopy()
        {
            FLOD_Renderer newR = CreateInstance<FLOD_Renderer>();
            newR.CopyBase(this);
            newR.UseShadows = UseShadows;
            newR.ShadowsCast = ShadowsCast;
            newR.ShadowsReceive = ShadowsReceive;
            newR.MotionVectors = MotionVectors;
            newR.SkinnedQuality = SkinnedQuality;
            return newR;
        }

        public override void SetSameValuesAsComponent(Component component)
        {
            if (component == null) Debug.LogError("[OPTIMIZERS] Given component is null instead of Renderer!");

            Renderer comp = component as Renderer;

            if (comp != null)
            {
                UseShadows = true;
                if (comp.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.Off) UseShadows = false;

                ShadowsCast = comp.shadowCastingMode;
                ShadowsReceive = comp.receiveShadows;
                MotionVectors = comp.motionVectorGenerationMode;

                SkinnedMeshRenderer skin = RefreshSkinned(component);
                if (skin) SkinnedQuality = skin.quality;
            }
        }


        private SkinnedMeshRenderer RefreshSkinned(Component comp)
        {
            if (skinned) return null;

            SkinnedMeshRenderer skin = comp as SkinnedMeshRenderer;
            if (skin) skinned = true;
            return skin;
        }

        #endregion


        #region Operations


        public override void ApplySettingsToComponent(Component component, FLOD_Base initialSettingsReference)
        {
            FLOD_Renderer initialSettings = initialSettingsReference as FLOD_Renderer;

            #region Security

            if (component == null) { Debug.Log("[OPTIMIZERS] Target component is null"); return; }
            if (initialSettings == null) { Debug.Log("[OPTIMIZERS] Target LOD is not Renderer LOD or is null"); return; }

            #endregion

            Renderer comp = component as Renderer;

            if (UseShadows)
            {
                comp.shadowCastingMode = initialSettings.ShadowsCast;
                comp.receiveShadows = initialSettings.ShadowsReceive;
            }
            else
            {
                comp.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                comp.receiveShadows = false;
            }

            comp.motionVectorGenerationMode = MotionVectors;


#if UNITY_2019_1_OR_NEWER
                if (QualitySettings.skinWeights != SkinWeights.OneBone)
                if (skinned)
                {
                    if (QualitySettings.skinWeights == SkinWeights.TwoBones)
                    {
                        if (SkinnedQuality == SkinQuality.Bone4) SkinnedQuality = SkinQuality.Bone2;
                    }

                    SkinnedMeshRenderer skin = comp as SkinnedMeshRenderer;
                    skin.quality = SkinnedQuality;
                }
#else
            if (QualitySettings.blendWeights != BlendWeights.OneBone)
                if (skinned)
                {
                    if (QualitySettings.blendWeights == BlendWeights.TwoBones)
                    {
                        if (SkinnedQuality == SkinQuality.Bone4) SkinnedQuality = SkinQuality.Bone2;
                    }

                    SkinnedMeshRenderer skin = comp as SkinnedMeshRenderer;
                    skin.quality = SkinnedQuality;
                }
#endif


            if (Disable) comp.enabled = false; else comp.enabled = true;
        }

        #endregion


        #region Auto Settings


        public override void SetAutoSettingsAsForLODLevel(int lodIndex, int lodCount, Component source)
        {
            Renderer comp = source as Renderer;
            if (comp == null) Debug.LogError("[OPTIMIZERS] Given component for reference values is null or is not Renderer Component!");

            float mul = GetValueForLODLevel(1f, 0f, lodIndex, lodCount);
            UseShadows = !(comp.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.Off);

            if (lodIndex >= 0)
            {
                if (comp.motionVectorGenerationMode != MotionVectorGenerationMode.ForceNoMotion)
                    MotionVectors = MotionVectorGenerationMode.Camera;
            }

            if (lodCount == 2) if (comp.motionVectorGenerationMode == MotionVectorGenerationMode.Object) MotionVectors = MotionVectorGenerationMode.Camera;

            if (mul > 0.43f) SkinnedQuality = SkinQuality.Bone2;

            if (lodIndex == lodCount - 2)
            {
                UseShadows = false;
                if (lodCount != 2) MotionVectors = MotionVectorGenerationMode.ForceNoMotion;
                SkinnedQuality = SkinQuality.Bone1;
            }

            name = "LOD" + (lodIndex + 2); // + 2 to view it in more responsive way for user inside inspector window
        }


        public override void SetSettingsAsForCulled(Component component)
        {
            base.SetSettingsAsForCulled(component);
            UseShadows = false;
            MotionVectors = MotionVectorGenerationMode.ForceNoMotion;
            SkinnedQuality = SkinQuality.Bone1;
        }

        public override void SetSettingsAsForHidden(Component component)
        {
            base.SetSettingsAsForHidden(component);
            Disable = true;
            UseShadows = false;
            MotionVectors = MotionVectorGenerationMode.ForceNoMotion;
            SkinnedQuality = SkinQuality.Bone1;
        }

        #endregion


        /// <summary>
        /// Assign this LOD type to FOptimizers_Manager
        /// </summary>
        public override FComponentLODsController GenerateLODController(Component target, FOptimizer_Base optimizer)
        {
            Renderer rend = target as Renderer;
            if (!rend) rend = target.GetComponent<Renderer>();
            if (rend) if (!optimizer.ContainsComponent(rend))
                {
                    SkinnedMeshRenderer skinned = rend as SkinnedMeshRenderer;
                    if (skinned)
                    {
                        if (optimizer.ToOptimize != null)
                        {
                            bool hadLight = false;
                            for (int i = 0; i < optimizer.ToOptimize.Count; i++)
                                if (optimizer.ToOptimize[i].Component is Light)
                                {
                                    hadLight = true;
                                    break;
                                }

                            if (!hadLight)
                            {
                                optimizer.DetectionRadius = skinned.bounds.extents.magnitude;
                                optimizer.DetectionBounds = skinned.bounds.size * 1.2f;

                                if (optimizer.DetectionOffset == Vector3.zero)
                                    optimizer.DetectionOffset = skinned.transform.InverseTransformPoint(skinned.bounds.center);
                            }
                        }

                        return new FComponentLODsController(optimizer, skinned, "Skinned Renderer", this);
                    }
                    else
                    {
                        MeshRenderer mesh = rend as MeshRenderer;
                        if (mesh)
                        {
                            if (optimizer.ToOptimize != null)
                            {
                                bool hadLight = false;
                                for (int i = 0; i < optimizer.ToOptimize.Count; i++)
                                    if (optimizer.ToOptimize[i].Component is Light)
                                    {
                                        hadLight = true;
                                        break;
                                    }

                                if (!hadLight)
                                {
                                    float scaler = FOptimizer_Base.GetScaler(optimizer.transform); if (scaler == 0f) scaler = 1f;
                                    optimizer.DetectionRadius = mesh.bounds.extents.magnitude / scaler;
                                    optimizer.DetectionBounds = (mesh.bounds.size * 1.05f) / scaler;

                                    if (optimizer.DetectionOffset == Vector3.zero)
                                        optimizer.DetectionOffset = mesh.transform.InverseTransformPoint(mesh.bounds.center);
                                }
                            }

                            return new FComponentLODsController(optimizer, mesh, "MeshRenderer", this);
                        }
                    }
                }

            return null;
        }

        public static void AutoBounds(FOptimizer_Base targetOptimizer, Mesh sourceMesh)
        {

        }
    }
}
