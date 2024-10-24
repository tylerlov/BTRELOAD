using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;

namespace JPG
{
    [ExecuteInEditMode, ImageEffectAllowedInSceneView, AddComponentMenu("JPG Bitcrunching Effect")]
    [RequireComponent(typeof(Camera))]
    public class JPG : MonoBehaviour
    {
        public enum _BlockSize { [InspectorName("4x4 Fast")]_4x4, [InspectorName("8x8 Medium")]_8x8, [InspectorName("16x16 Slow")]_16x16 }

        [HideInInspector] public Shader jpgShader;

        //
        [Tooltip("Gracefully reduces these together: Color Crunch, Downscaling, Block Size, Reproject Percent, Reproject Length Influence." +
            "\n\nFor example this can be useful to lerp out the effect by lerping out this one parameter instead of all the other ones." +
            "\n\nWarning: When scrolling through this parameter there may be numbers that land on extra low performance, because 16x16 block size + a low Downscaling setting is a very demanding combo." +
            "\nQuickly lerp away from these (like animating out the effect), or set block size to 8x8 to avoid this.")] [Range(0f, 1f)] public float EffectIntensity = 0.35f;
        [Tooltip("When enabled effect will be applied only to pixels with a stencil buffer value of '32'. Shaders that set it to this value are provided in /StencilShaders." +
            "\n\nFor example if you want the effect to only be applied to specific objects, enable this and put a +JPG shader on them." +
            "\n\nFor custom shaders it is extremely easy to modify them to become JPG maskers, see README Stencil.txt.")] public bool OnlyStenciled = false;
        
        [Space(6)]
        [Header("Block Encoding")]
        [Tooltip("A bit of color crunching really brings out the noise from the blocks.")] [Range(0f, 1f)] public float ColorCrunch = 1f;
        [Tooltip("Division applied to screen resolution before applying the effect.")] [Range(1, 10)] public int Downscaling = 10;
        [Tooltip("The size of the encoding blocks, works in conjunction with Downscaling." +
            "\n\nWarning: 16x16 will be very slow at a low Downscaling setting, so when you're scrolling through Effect Intensity there may be numbers that land on extra low performance." +
            "\nQuickly lerp away from them (like animating out the effect), or set block size to 8x8 to avoid this.")] public _BlockSize BlockSize = _BlockSize._16x16;
        [Space(6)]
        [Tooltip("Add some oversharpening if you're going for a deep-fried look. Not affected by Effect Intensity.")] [Range(0f, 1f)] public float Oversharpening = 0.2f;
        [Tooltip("Does color crunching try to ignore the skybox? (aka pixels at depth values 0.0 or 1.0).")] public bool DontCrunchSkybox = false;
        
        [Space(6)]
        [Header("Datamoshing Reprojection")]
        [Tooltip("Base chance of each pixel block to be randomly selected for reprojection.\nDatamoshing is disabled when this and Length Influence are 0.\nDatamoshing enables motion vectors generation if wasn't enabled already."), InspectorNameNonEnum("Base Noise")] [Range(0f, 1f)] public float ReprojectBaseNoise = 0f;
        [Tooltip("How many times per second base noise is rerolled."), InspectorNameNonEnum("Base Reroll Speed")] [Range(0f, 20f)] public float ReprojectBaseRerollSpeed = 3f;
        [Tooltip("How much does the length of a motion vector increase it's chance of being selected on top of base noise."), InspectorNameNonEnum("Length Influence")] [Range(0f, 5f)] public float ReprojectLengthInfluence = 0f;
        [Tooltip("Useful for debugging, as an object will only get datamoshed if it's writing to motion vectors.")] public bool PreviewMotionVectors = false;

        float colorCrunch;
        int downscaling;
        _BlockSize blockSize;
        float reprojectPercent;
        float reprojectLengthInfluence;
        //

        void CheckParameters()
        {
            if (GraphicsSettings.defaultRenderPipeline != null)
            {
                Debug.LogWarning("Please follow the SRP usage instructions.");
                enabled = false;
            }

            colorCrunch = ColorCrunch * EffectIntensity;
            downscaling = Mathf.Max(1, Mathf.CeilToInt(Downscaling * EffectIntensity));
            blockSize = (_BlockSize)Mathf.RoundToInt((int)BlockSize * EffectIntensity);
            reprojectPercent = ReprojectBaseNoise * EffectIntensity;
            reprojectLengthInfluence = ReprojectLengthInfluence * EffectIntensity;
        }

        bool DoOnlyStenciled => OnlyStenciled && !PreviewMotionVectors;
        bool DoReprojection => (reprojectPercent > 0f || reprojectLengthInfluence > 0f || PreviewMotionVectors) && cam.cameraType != CameraType.SceneView;

        Material mat;
        Camera cam;
        CommandBuffer cmdBuffer;
        int width;
        int height;

        CameraEvent cameraEvent = CameraEvent.BeforeImageEffects;

        bool forceCommandBufferDirty;
        int previousWidth;
        int previousHeight;
        _BlockSize previousBlockSize;
        int previousDownscale;
        bool previousDoOnlyStenciled;
        bool previousDoReprojection;
        bool IsCommandBufferDirty
        {
            get
            {
                if (forceCommandBufferDirty || previousWidth != width || previousHeight != height || previousBlockSize != blockSize || previousDownscale != downscaling || previousDoOnlyStenciled != DoOnlyStenciled || previousDoReprojection != DoReprojection)
                {
                    forceCommandBufferDirty = false;
                    previousWidth = width; previousHeight = height; previousBlockSize = blockSize; previousDownscale = downscaling; previousDoOnlyStenciled = DoOnlyStenciled; previousDoReprojection = DoReprojection;
                    return true;
                }
                return false;
            }
        }

        void OnEnable()
        {
            if (jpgShader == null) jpgShader = Shader.Find("Hidden/JPG");
            if (jpgShader == null)
            {
                Debug.LogError("JPG shader was not found... ");
                return;
            }
            if (!jpgShader.isSupported)
            {
                Debug.LogWarning("JPG shader is not supported on this platform.");
                enabled = false;
                return;
            }

            cam = GetComponent<Camera>();
            cam.forceIntoRenderTexture = true;

            mat = new Material(jpgShader);
            mat.hideFlags = HideFlags.HideAndDontSave;

            cmdBuffer = new CommandBuffer { name = "JPG" };
            
            forceCommandBufferDirty = true;
        }

        void OnDisable()
        {
            ClearCommandBuffer(cmdBuffer);

            if (mat != null)
                DestroyImmediate(mat);
            if (FullscreenTriangle != null)
                DestroyImmediate(FullscreenTriangle);
        }

        void OnValidate()
        {
            if (jpgShader == null || cam == null) return;

            CheckParameters();
        }

        void ClearCommandBuffer(CommandBuffer cmd)
        {
            if (cmd != null)
            {
                if (cam != null) cam.RemoveCommandBuffer(cameraEvent, cmd);
                cmd.Clear();
            }
        }
        
        void OnPreRender()
        {
            if (jpgShader == null || cam == null) return;

            if (EffectIntensity == 0f)
            {
                ClearCommandBuffer(cmdBuffer);
                forceCommandBufferDirty = true;
                return;
            }

            width = cam.pixelWidth;
            height = cam.pixelHeight;

            if (DoReprojection)
                cam.depthTextureMode |= DepthTextureMode.Depth | DepthTextureMode.MotionVectors;
            
            CheckParameters();
            UpdateMaterialProperties();
            UpdateShaderKeywords();

            if (IsCommandBufferDirty)
            {
                ClearCommandBuffer(cmdBuffer);
                BuildCommandBuffer(cmdBuffer);
                cam.AddCommandBuffer(cameraEvent, cmdBuffer);
            }
        }
        
        void UpdateMaterialProperties()
        {
            int widthDownscaled = Mathf.FloorToInt(width / downscaling / 2f) * 2;
            int heightDownscaled = Mathf.FloorToInt(height / downscaling / 2f) * 2;
            
            mat.SetVector("_Screen_TexelSize", new Vector4(1f / width, 1f / height, width, height));
            mat.SetVector("_Downscaled_TexelSize", new Vector4(1f / widthDownscaled, 1f / heightDownscaled, widthDownscaled, heightDownscaled));

            mat.SetFloat("_ColorCrunch", colorCrunch);
            mat.SetFloat("_Sharpening", Oversharpening);
            
            mat.SetFloat("_ReprojectPercent", reprojectPercent);
            mat.SetFloat("_ReprojectSpeed", ReprojectBaseRerollSpeed);
            mat.SetFloat("_ReprojectLengthInfluence", reprojectLengthInfluence);
        }

        string[] keywords = new string[4];
        void UpdateShaderKeywords()
        {
            keywords[0] = blockSize == _BlockSize._4x4 ? "BLOCK_SIZE_4" : blockSize == _BlockSize._8x8 ? "BLOCK_SIZE_8" : blockSize == _BlockSize._16x16 ? "BLOCK_SIZE_16" : "";
            keywords[1] = !DontCrunchSkybox ? "COLOR_CRUNCH_SKYBOX" : "";
            keywords[2] = DoReprojection ? "REPROJECTION" : "";
            keywords[3] = PreviewMotionVectors ? "VIZ_MOTION_VECTORS" : "";
            mat.shaderKeywords = keywords;
        }

        static class Pass
        {
            public const int Downscale = 0;
            public const int Encode = 1;
            public const int Decode = 2;
            public const int UpscalePull = 3;
            public const int UpscalePullStenciled = 4;
            public const int CopyToPrev = 5;
        }
        RenderTexture prevScreenTex;
        int prevWidth = -1;
        int prevHeight = -1;
        void BuildCommandBuffer(CommandBuffer cmd)
        {
            if (prevWidth != width || prevHeight != height)
            {
                prevWidth = width;
                prevHeight = height;
                if (prevScreenTex != null) RenderTexture.ReleaseTemporary(prevScreenTex);
                prevScreenTex = RenderTexture.GetTemporary(prevWidth, prevHeight, 0, GraphicsFormat.R32G32B32A32_SFloat);
                prevScreenTex.name = "_PrevScreen RT";
            }
            
            int widthDownscaled = Mathf.FloorToInt(width / downscaling / 2f) * 2;
            int heightDownscaled = Mathf.FloorToInt(height / downscaling / 2f) * 2;
            
            var downscaledTex = Shader.PropertyToID("_JpgScreenDownscaled");
            cmd.GetTemporaryRT(downscaledTex, widthDownscaled, heightDownscaled, 0, FilterMode.Bilinear, GraphicsFormat.R32G32B32A32_SFloat);
            
            RenderWith(BuiltinRenderTextureType.CameraTarget, downscaledTex, cmd, mat, Pass.Downscale);

            var blocksTex = Shader.PropertyToID("_JpgBlocks");
            cmd.GetTemporaryRT(blocksTex, widthDownscaled, heightDownscaled, 0, FilterMode.Bilinear, GraphicsFormat.R32G32B32A32_SFloat);
            
            RenderWith(downscaledTex, blocksTex, cmd, mat, Pass.Encode);
            RenderWith(blocksTex, downscaledTex, cmd, mat, Pass.Decode);
            
            cmd.SetGlobalTexture("_PrevScreen", prevScreenTex);
            RenderWith(downscaledTex, BuiltinRenderTextureType.CameraTarget, cmd, mat, DoOnlyStenciled ? Pass.UpscalePullStenciled : Pass.UpscalePull);
            
            if (DoReprojection)
                RenderWith(BuiltinRenderTextureType.CameraTarget, prevScreenTex, cmd, mat, Pass.CopyToPrev);

            cmd.ReleaseTemporaryRT(downscaledTex);
            cmd.ReleaseTemporaryRT(blocksTex);
        }

        Mesh fullscreenTriangle;
        Mesh FullscreenTriangle
        {
            get
            {
                if (fullscreenTriangle != null) return fullscreenTriangle;
                fullscreenTriangle = new Mesh { name = "Fullscreen Triangle" };
                fullscreenTriangle.SetVertices(new List<Vector3> { new Vector3(-1f, -1f, 0f), new Vector3(-1f, 3f, 0f), new Vector3(3f, -1f, 0f) });
                fullscreenTriangle.SetIndices(new[] { 0, 1, 2 }, MeshTopology.Triangles, 0, false);
                fullscreenTriangle.UploadMeshData(false);
                return fullscreenTriangle;
            }
        }
        void RenderWith(RenderTargetIdentifier source, RenderTargetIdentifier destination, CommandBuffer cmd, Material material, int pass)
        {
            cmd.SetGlobalTexture("_Input", source);
            cmd.SetRenderTarget(destination);
            cmd.DrawMesh(FullscreenTriangle, Matrix4x4.identity, material, 0, pass);
        }

    }

    public class InspectorNameNonEnum : PropertyAttribute
    {
        public string label;
        public InspectorNameNonEnum(string label) => this.label = label;
        #if UNITY_EDITOR
        [CustomPropertyDrawer(typeof(InspectorNameNonEnum))]
        public class ThisPropertyDrawer : PropertyDrawer
        {
            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                var propertyAttribute = attribute as InspectorNameNonEnum;
                label.text = propertyAttribute.label;
                EditorGUI.PropertyField(position, property, label);
            }
        }
        #endif
    }
}
