using System;

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

using Raymarcher.Constants;
using Raymarcher.Attributes;

namespace Raymarcher.RendererData
{
    using static RMConstants.CommonRendererProperties;

    [Serializable]
    public sealed class RMCoreRenderMasterRenderingData : IRMRenderMasterDependency
    {
        [SerializeField] private RMRenderMaster renderMaster;
        [SerializeField, RMAttributes.ReadOnly] private Material rendererSessionMaterialSource;

        /// <summary>
        /// Current Raymarcher session material source
        /// </summary>
        public Material RendererSessionMaterialSource => rendererSessionMaterialSource;

        // Rendering Core

        public enum RenderTypeOptions { Quality, Standard, Performant};
        [SerializeField] private RenderTypeOptions compiledRenderType = RenderTypeOptions.Standard;
        public enum RenderIterationOptions { x16, x32, x64, x128, x256, x512};
        [SerializeField] private RenderIterationOptions renderIterations = RenderIterationOptions.x128;
        public float maximumRenderDistance = 32;
        public bool simpleRenderPrecisionSettings = true;
        [Range(0.5f, 1.0e-6f)] public float renderPrecision = 0.01f;
        public enum RenderPrecisionOptions { VeryLow, Low, Medium, High, VeryHigh, Extreme, Maximum };
        public RenderPrecisionOptions renderPrecisionOption = RenderPrecisionOptions.VeryHigh;

        // Essential Filters

        public Color rendererColorTint = Color.white;
        [Range(0f, 5f)] public float rendererExposure = 1.0f;
        [Range(0, 360)] public float rendererGlobalHueSpectrumOffset = 0;
        [Range(0f, 1f)] public float rendererGlobalHueSaturation = 1;

        // Renderer Features

        [SerializeField] private bool useSceneDepth = true;
        [Range(0f, 1f)] public float sceneDepthSmoothness = 0.0f;

        [SerializeField] private bool reactWithSceneGeometry = false;
        public float sceneGeometrySmoothness = 0.25f;

        [SerializeField] private bool useDistanceFog = false;
        public float distanceFogDistance = 10f;
        public float distanceFogSmoothness = 2f;
        public Color distanceFogColor = Color.gray;

        [SerializeField] private bool usePixelation = false;
        [Range(0.01f, 1f)] public float pixelSize = 0.01f;

        // SDFs Global

        [SerializeField] private bool useSdfSmoothBlend = true;
        public float globalSdfObjectSmoothness = 0.5f;

        // Properties

        public RMRenderMaster RenderMaster => renderMaster;

        public RenderTypeOptions CompiledRenderType => compiledRenderType;
        public RenderIterationOptions RenderIterations => renderIterations;

        public bool IsReactingWithSceneGeometry => reactWithSceneGeometry;
        public bool UseSdfSmoothBlend => useSdfSmoothBlend;
        public bool UseDistanceFog => useDistanceFog;
        public bool UseSceneDepth => useSceneDepth;
        public bool UsePixelation => usePixelation;

#if UNITY_EDITOR
        public void SetRendererMaterialSource(Material rendererMaterialSource)
        {
            rendererSessionMaterialSource = rendererMaterialSource;
        }

        public void SetRenderType(RenderTypeOptions renderType, bool recompile = false)
        {
            compiledRenderType = renderType;

            if(recompile)
            {
                Convertor.RMConvertorCore.RefreshExistingRaymarcherInstance(renderMaster, renderMaster.RegisteredSessionName);
                RenderMaster.RecompileTarget(true);
                RenderMaster.RecompileTarget(false);
            }
        }


        public void DisposeDependency()
        {
            if (RendererSessionMaterialSource != null)
            {
                string asset = AssetDatabase.GetAssetPath(RendererSessionMaterialSource);
                if (!string.IsNullOrEmpty(asset))
                    AssetDatabase.DeleteAsset(asset);
                UnityEngine.Object.DestroyImmediate(RendererSessionMaterialSource, true);
            }
        }

        public void SetupDependency(in RMRenderMaster renderMaster)
        {
            this.renderMaster = renderMaster;
        }

#endif

        public void UpdateDependency(in Material raymarcherSceneMaterial)
        {
            raymarcherSceneMaterial.SetFloat(MaxRenderDistance, maximumRenderDistance);

            if (simpleRenderPrecisionSettings)
            {
                switch (renderPrecisionOption)
                {
                    case RenderPrecisionOptions.VeryLow:
                        renderPrecision = 0.1f;
                        break;
                    case RenderPrecisionOptions.Low:
                        renderPrecision = 0.01f;
                        break;
                    case RenderPrecisionOptions.Medium:
                        renderPrecision = 0.001f;
                        break;
                    case RenderPrecisionOptions.High:
                        renderPrecision = 5.0e-4f;
                        break;
                    case RenderPrecisionOptions.VeryHigh:
                        renderPrecision = 2.0e-4f;
                        break;
                    case RenderPrecisionOptions.Extreme:
                        renderPrecision = 1.0e-4f;
                        break;
                    case RenderPrecisionOptions.Maximum:
                        renderPrecision = 1.0e-5f;
                        break;
                }
            }
            else
                renderPrecision = Mathf.Max(renderPrecision, 1.0e-5f);

            raymarcherSceneMaterial.SetFloat(RenderPrecision, renderPrecision);

            if(UseSceneDepth)
                raymarcherSceneMaterial.SetFloat(SceneDepthSmoothness, Mathf.Abs(sceneDepthSmoothness));

            if(IsReactingWithSceneGeometry)
                raymarcherSceneMaterial.SetFloat(SceneGeometrySmoothness, Mathf.Abs(sceneGeometrySmoothness));

            raymarcherSceneMaterial.SetColor(RendererColorTint, rendererColorTint);
            raymarcherSceneMaterial.SetFloat(RendererExposure, Mathf.Abs(rendererExposure));

            if (CompiledRenderType != RenderTypeOptions.Quality)
            {
                raymarcherSceneMaterial.SetFloat(GlobalHueSpectrumOffset, rendererGlobalHueSpectrumOffset);
                raymarcherSceneMaterial.SetFloat(GlobalHueSaturation, rendererGlobalHueSaturation);
            }

            if (useDistanceFog)
            {
                raymarcherSceneMaterial.SetFloat(DistanceFogSmoothness, Mathf.Abs(distanceFogSmoothness));
                raymarcherSceneMaterial.SetFloat(DistanceFogDistance, Mathf.Abs(distanceFogDistance));
                raymarcherSceneMaterial.SetColor(DistanceFogColor, distanceFogColor);
            }

            if (usePixelation)
                raymarcherSceneMaterial.SetFloat(PixelationSize, pixelSize);

            if(UseSdfSmoothBlend)
                raymarcherSceneMaterial.SetFloat(GlobalSdfObjectSmoothness, Mathf.Abs(globalSdfObjectSmoothness));
        }
    }
}