using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

namespace Mirage.Impostors.Core
{
    using Elements;
    public class EditorBakingEngine : IBakingEngine
    {
        public PreviewRenderUtility EditorRenderer { get; private set; }
        private Camera camera;

        private Shader _depthShader;
        private Shader depthShader
        {
            get { return _depthShader != null ? _depthShader : (_depthShader = Shader.Find("Mirage/Internal/Depth")); }
        }

        private Shader _mergeShader;
        private Shader mergeShader
        {
            get { return _mergeShader != null ? _mergeShader : (_mergeShader = Shader.Find("Mirage/Internal/MergeChannels")); }
        }

        private Shader _maskEstimatorShader;
        private Shader maskEstimatorShader
        {
            get { return _maskEstimatorShader != null ? _maskEstimatorShader : (_maskEstimatorShader = Shader.Find("Mirage/Internal/MaskEstimator")); }
        }

        private Shader _normalShader;
        private Shader normalShader
        {
            get { return _normalShader != null ? _normalShader : (_normalShader = Shader.Find("Mirage/Internal/Normals")); }
        }

        private Material _depthMaterial;
        private Material depthMaterial
        {
            get
            {
                if (_depthMaterial == null)
                {
                    _depthMaterial = new Material(depthShader);
                    _depthMaterial.hideFlags = HideFlags.HideAndDontSave;
                }
                return _depthMaterial;
            }
        }

        private Material _mergeMaterial;
        private Material mergeMaterial
        {
            get
            {
                if (_mergeMaterial == null)
                {
                    _mergeMaterial = new Material(mergeShader);
                    _mergeMaterial.hideFlags = HideFlags.HideAndDontSave;
                }
                return _mergeMaterial;
            }
        }

        private Material _maskEstimatorMaterial;
        private Material maskEstimatorMaterial
        {
            get
            {
                if (_maskEstimatorMaterial == null)
                {
                    _maskEstimatorMaterial = new Material(maskEstimatorShader);
                    _maskEstimatorMaterial.hideFlags = HideFlags.HideAndDontSave;
                }
                return _maskEstimatorMaterial;
            }
        }

        private Material _normalMaterial;
        private Material normalMaterial
        {
            get
            {
                if (_normalMaterial == null)
                {
                    _normalMaterial = new Material(normalShader);
                    _normalMaterial.hideFlags = HideFlags.HideAndDontSave;
                }
                return _normalMaterial;
            }
        }


        public EditorBakingEngine(List<Vector3> povs, List<Mesh> meshes, List<Material[]> materials, List<Matrix4x4> meshesTransforms, int textureSize, float orthographicSize, LightingMethod lightingMethod = LightingMethod.SurfaceEstimation)
        {
            EditorRenderer = new PreviewRenderUtility(true);
            camera = EditorRenderer.camera;
            camera.orthographic = true;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = orthographicSize * 1.4142f * 2f;
            camera.orthographicSize = orthographicSize;
            foreach (Light l in EditorRenderer.lights)
            {
                l.gameObject.SetActive(false);
            }
            camera.backgroundColor = Color.black;
            camera.clearFlags = CameraClearFlags.SolidColor;

            switch (lightingMethod)
            {
                case LightingMethod.SurfaceEstimation:
                    EditorRenderer.ambientColor = Color.white;
                    break;
                case LightingMethod.UseSunSource:
                    if (RenderSettings.sun != null)
                    {
                        EditorRenderer.ambientColor = RenderSettings.ambientLight;
                        EditorRenderer.lights[0].gameObject.SetActive(true);
                        EditorRenderer.lights[0] = RenderSettings.sun;
                        EditorRenderer.lights[0].transform.rotation = RenderSettings.sun.transform.rotation;
                    }
                    else
                    {
                        EditorRenderer.ambientColor = Color.white;
                        Debug.LogWarning("[Mirage] Lighting settings: Sun Source is null. Output Impostor will appear unlit.");
                    }
                    break;
            }


            Initialize(povs, meshes, materials, meshesTransforms, textureSize);
        }

        ~EditorBakingEngine()
        {
            EditorRenderer?.Cleanup();
        }

        public override Texture2D ComputeColorMaps()
        {
            int subdivisions = Mathf.CeilToInt(Mathf.Sqrt(CameraPositions.Count));
            int impostorSize = Mathf.FloorToInt(TextureSize / (float)subdivisions);

            // PreviewrenderUtility uses the current scene's RenderSettings
            // We back it up and set it back after rendering the color map
            AmbientMode originalAmbientMode = RenderSettings.ambientMode;
            Color originalAmbientColor = RenderSettings.ambientLight;
            DefaultReflectionMode originalDefaultReflectionMode = RenderSettings.defaultReflectionMode;
            float originalReflectionIntensity = RenderSettings.reflectionIntensity;
#if UNITY_2022_1_OR_NEWER
            Texture originalCustomReflection = RenderSettings.customReflectionTexture;
            RenderSettings.customReflectionTexture = Resources.Load<Texture>("MirageWhiteBakingCubeMap");
#else
            Texture originalCustomReflection = RenderSettings.customReflection;
            RenderSettings.customReflection = Resources.Load<Texture>("MirageWhiteBakingCubeMap");
#endif

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = EditorRenderer.ambientColor;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.reflectionIntensity = 0;

            camera.backgroundColor = Color.gray;

            Texture2D colorMapAtlas = new Texture2D(impostorSize * subdivisions, impostorSize * subdivisions, TextureFormat.ARGB32, false);
            Texture2D alphaClippingDiscriminatorAtlas = new Texture2D(impostorSize * subdivisions, impostorSize * subdivisions, TextureFormat.ARGB32, false);

            for (int pov = 0; pov < CameraPositions.Count; ++pov)
            {
                camera.transform.position = CameraPositions[pov];
                camera.transform.LookAt(Vector3.zero);

                for (int i = 0; i < Meshes.Count; ++i)
                {
                    Mesh mesh = Meshes[i];
                    for (int j = 0; j < Materials[i].Length; ++j)
                    {
                        Material currentMaterial = new Material(Materials[i][j]);
                        currentMaterial.DisableKeyword("_GLOSSYREFLECTIONS_OFF");
                        EditorRenderer.DrawMesh(mesh, MeshesTransforms[i], currentMaterial, j < mesh.subMeshCount ? j : mesh.subMeshCount - 1, null, null, false);
                    }
                }

                camera.backgroundColor = Color.gray;
                Rect impostorRect = new Rect(0, 0, impostorSize, impostorSize);
                EditorRenderer.BeginStaticPreview(impostorRect);
                EditorRenderer.Render(true);
                Texture2D cameraRender = EditorRenderer.EndStaticPreview();

                for (int i = 0; i < Meshes.Count; ++i)
                {
                    Mesh mesh = Meshes[i];
                    for (int j = 0; j < Materials[i].Length; ++j)
                    {
                        Material currentMaterial = new Material(Materials[i][j]);
                        currentMaterial.DisableKeyword("_GLOSSYREFLECTIONS_OFF");
                        EditorRenderer.DrawMesh(mesh, MeshesTransforms[i], currentMaterial, j < mesh.subMeshCount ? j : mesh.subMeshCount - 1, null, null, false);
                    }
                }


                camera.backgroundColor = Color.black;
                EditorRenderer.BeginStaticPreview(impostorRect);
                EditorRenderer.Render(true);
                Texture2D alphaClippingDiscriminator = EditorRenderer.EndStaticPreview();

                int col = pov % subdivisions;
                int row = Mathf.FloorToInt(pov / (float)subdivisions);
                colorMapAtlas.SetPixels(col * impostorSize, row * impostorSize, impostorSize, impostorSize, cameraRender.GetPixels());
                alphaClippingDiscriminatorAtlas.SetPixels(col * impostorSize, row * impostorSize, impostorSize, impostorSize, alphaClippingDiscriminator.GetPixels());
            }
            colorMapAtlas.Apply();
            colorMapAtlas = ImpostorTextureUtilities.ResizeSquared(colorMapAtlas, Mathf.ClosestPowerOfTwo(impostorSize * subdivisions));
            alphaClippingDiscriminatorAtlas.Apply();
            alphaClippingDiscriminatorAtlas = ImpostorTextureUtilities.ResizeSquared(alphaClippingDiscriminatorAtlas, Mathf.ClosestPowerOfTwo(impostorSize * subdivisions));

            RenderSettings.ambientMode = originalAmbientMode;
            RenderSettings.ambientLight = originalAmbientColor;
#if UNITY_2022_1_OR_NEWER
            RenderSettings.customReflectionTexture = originalCustomReflection;
#else
            RenderSettings.customReflection = originalCustomReflection;
#endif
            RenderSettings.defaultReflectionMode = originalDefaultReflectionMode;
            RenderSettings.reflectionIntensity = originalReflectionIntensity;


            camera.backgroundColor = Color.black;

            Texture2D depthAtlas = new Texture2D(impostorSize * subdivisions, impostorSize * subdivisions, TextureFormat.ARGB32, false);
            // Render the Depth Map
            for (int pov = 0; pov < CameraPositions.Count; ++pov)
            {
                camera.transform.position = CameraPositions[pov];
                camera.transform.LookAt(Vector3.zero);

                // Draw all the mesh and their submeshes
                for (int i = 0; i < Meshes.Count; ++i)
                {
                    Mesh mesh = Meshes[i];
                    for (int j = 0; j < mesh.subMeshCount; ++j)
                        EditorRenderer.DrawMesh(mesh, MeshesTransforms[i], depthMaterial, j, null, null, false);
                }

                Rect impostorRect = new Rect(0, 0, impostorSize, impostorSize);
                EditorRenderer.BeginStaticPreview(impostorRect);
                EditorRenderer.Render();
                Texture2D cameraRender = EditorRenderer.EndStaticPreview();

                int col = pov % subdivisions;
                int row = Mathf.FloorToInt(pov / (float)subdivisions);
                depthAtlas.SetPixels(col * impostorSize, row * impostorSize, impostorSize, impostorSize, cameraRender.GetPixels());
            }
            depthAtlas.Apply();
            depthAtlas = ImpostorTextureUtilities.ResizeSquared(depthAtlas, Mathf.ClosestPowerOfTwo(impostorSize * subdivisions));

            //applying the alpha channel to the ColorMap
            RenderTexture ColorMapAtlasRT = new RenderTexture(colorMapAtlas.width, colorMapAtlas.height, 24, RenderTextureFormat.ARGB32);
            ColorMapAtlasRT.enableRandomWrite = true;
            ColorMapAtlasRT.Create();

            mergeMaterial.SetTexture("_DepthTex", depthAtlas);
            mergeMaterial.SetTexture("_AlphaClippingDiscriminatorTex", alphaClippingDiscriminatorAtlas);
            Graphics.Blit(colorMapAtlas, ColorMapAtlasRT, mergeMaterial);

            RenderTexture.active = ColorMapAtlasRT;
            colorMapAtlas.ReadPixels(new Rect(0, 0, ColorMapAtlasRT.width, ColorMapAtlasRT.height), 0, 0);
            colorMapAtlas.Apply();
            RenderTexture.active = null;
            ColorMapAtlasRT.Release();
            //colorMapAtlas.Compress(true);
            return colorMapAtlas;
        }
        public override Texture2D ComputeNormalMaps()
        {
            int subdivisions = Mathf.CeilToInt(Mathf.Sqrt(CameraPositions.Count));
            int impostorSize = Mathf.FloorToInt(TextureSize / (float)subdivisions);

            Texture2D normalMapAtlas = new Texture2D(impostorSize * subdivisions, impostorSize * subdivisions, TextureFormat.RGB24, false);

            camera.backgroundColor = new Color(0.5f, 0.5f, 1f); //flat bump map

            // Render the Normal Map
            for (int pov = 0; pov < CameraPositions.Count; ++pov)
            {
                camera.transform.position = CameraPositions[pov];
                camera.transform.LookAt(Vector3.zero);
                // Draw all the mesh and their submeshes
                for (int i = 0; i < Meshes.Count; ++i)
                {
                    Mesh mesh = Meshes[i];
                    for (int j = 0; j < mesh.subMeshCount; ++j)
                        EditorRenderer.DrawMesh(mesh, MeshesTransforms[i], normalMaterial, j, null, null, false);
                }

                Rect impostorRect = new Rect(0, 0, impostorSize, impostorSize);
                EditorRenderer.BeginStaticPreview(impostorRect);
                EditorRenderer.Render();
                Texture2D cameraRender = EditorRenderer.EndStaticPreview();


                int col = pov % subdivisions;
                int row = Mathf.FloorToInt(pov / (float)subdivisions);
                normalMapAtlas.SetPixels(col * impostorSize, row * impostorSize, impostorSize, impostorSize, cameraRender.GetPixels());
            }
            normalMapAtlas.Apply();
            normalMapAtlas = ImpostorTextureUtilities.ResizeSquared(normalMapAtlas, Mathf.ClosestPowerOfTwo(impostorSize * subdivisions));
            //normalMapAtlas.Compress(true);
            return normalMapAtlas;
        }

        /// <summary>
        /// Red: metallic map estimation
        /// Other channels are left black but may be useful for smoothness and AO estimation in a future update.
        /// </summary>
        /// <returns></returns>
        public override Texture2D ComputeMaskMaps()
        {
            int subdivisions = Mathf.CeilToInt(Mathf.Sqrt(CameraPositions.Count));
            int impostorSize = Mathf.FloorToInt(TextureSize / (float)subdivisions);

            // PreviewrenderUtility uses the current scene's RenderSettings
            // We back it up and set it back after rendering the mask map
            AmbientMode originalAmbientMode = RenderSettings.ambientMode;
            Color originalAmbientColor = RenderSettings.ambientLight;
            DefaultReflectionMode originalDefaultReflectionMode = RenderSettings.defaultReflectionMode;

            float originalReflectionIntensity = RenderSettings.reflectionIntensity;

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = Color.white;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            camera.backgroundColor = Color.gray;

            Texture2D colorMapAtlas = new Texture2D(impostorSize * subdivisions, impostorSize * subdivisions, TextureFormat.RGB24, false);

            for (int pov = 0; pov < CameraPositions.Count; ++pov)
            {
                camera.transform.position = CameraPositions[pov];
                camera.transform.LookAt(Vector3.zero);
                Rect impostorRect = new Rect(0, 0, impostorSize, impostorSize);

                for (int i = 0; i < Meshes.Count; ++i)
                {
                    Mesh mesh = Meshes[i];
                    for (int j = 0; j < Materials[i].Length; ++j)
                    {
                        Material currentMaterial = new Material(Materials[i][j]);
                        currentMaterial.DisableKeyword("_GLOSSYREFLECTIONS_OFF");
                        EditorRenderer.DrawMesh(mesh, MeshesTransforms[i], currentMaterial, j < mesh.subMeshCount ? j : mesh.subMeshCount - 1, null, null, false);
                    }
                }

                EditorRenderer.BeginStaticPreview(impostorRect);

#if UNITY_2022_1_OR_NEWER
                Texture originalCustomReflection = RenderSettings.customReflectionTexture;
                RenderSettings.customReflectionTexture = Resources.Load<Texture>("MirageWhiteBakingCubeMap");
#else
                Texture originalCustomReflection = RenderSettings.customReflection;
                RenderSettings.customReflection = Resources.Load<Texture>("MirageWhiteBakingCubeMap");
#endif

                EditorRenderer.Render(true);
                Texture2D cameraRender = EditorRenderer.EndStaticPreview();
#if UNITY_2022_1_OR_NEWER
                RenderSettings.customReflectionTexture = originalCustomReflection;
#else
                RenderSettings.customReflection = originalCustomReflection;
#endif

                int col = pov % subdivisions;
                int row = Mathf.FloorToInt(pov / (float)subdivisions);
                colorMapAtlas.SetPixels(col * impostorSize, row * impostorSize, impostorSize, impostorSize, cameraRender.GetPixels());
            }

            colorMapAtlas.Apply();
            colorMapAtlas = ImpostorTextureUtilities.ResizeSquared(colorMapAtlas, Mathf.ClosestPowerOfTwo(impostorSize * subdivisions));

            // Now we compute the colormap with no ambient color. Pixel will tend to 0 when metallic tend to 1.
            Texture2D colorMapER0Atlas = new Texture2D(impostorSize * subdivisions, impostorSize * subdivisions, TextureFormat.RGB24, false);

            for (int pov = 0; pov < CameraPositions.Count; ++pov)
            {
                camera.transform.position = CameraPositions[pov];
                camera.transform.LookAt(Vector3.zero);
                Rect impostorRect = new Rect(0, 0, impostorSize, impostorSize);

                for (int i = 0; i < Meshes.Count; ++i)
                {
                    Mesh mesh = Meshes[i];
                    for (int j = 0; j < Materials[i].Length; ++j)
                    {
                        Material currentMaterial = new Material(Materials[i][j]);
                        currentMaterial.DisableKeyword("_GLOSSYREFLECTIONS_OFF");
                        EditorRenderer.DrawMesh(mesh, MeshesTransforms[i], currentMaterial, j < mesh.subMeshCount ? j : mesh.subMeshCount - 1, null, null, false);
                    }
                }

                EditorRenderer.BeginStaticPreview(impostorRect);
#if UNITY_2022_1_OR_NEWER
                Texture originalCustomReflection = RenderSettings.customReflectionTexture;
                RenderSettings.customReflectionTexture = Resources.Load<Texture>("MirageBlackBakingCubeMap");
#else
                Texture originalCustomReflection = RenderSettings.customReflection;
                RenderSettings.customReflection = Resources.Load<Texture>("MirageBlackBakingCubeMap");
#endif

                EditorRenderer.Render(true);
                Texture2D cameraRender = EditorRenderer.EndStaticPreview();

#if UNITY_2022_1_OR_NEWER
            RenderSettings.customReflectionTexture = originalCustomReflection;
#else
            RenderSettings.customReflection = originalCustomReflection;
#endif
                int col = pov % subdivisions;
                int row = Mathf.FloorToInt(pov / (float)subdivisions);
                colorMapER0Atlas.SetPixels(col * impostorSize, row * impostorSize, impostorSize, impostorSize, cameraRender.GetPixels());
            }

            colorMapER0Atlas.Apply();
            colorMapER0Atlas = ImpostorTextureUtilities.ResizeSquared(colorMapER0Atlas, Mathf.ClosestPowerOfTwo(impostorSize * subdivisions));

            RenderSettings.ambientMode = originalAmbientMode;
            RenderSettings.ambientLight = originalAmbientColor;
            RenderSettings.defaultReflectionMode = originalDefaultReflectionMode;
            RenderSettings.reflectionIntensity = originalReflectionIntensity;
            EditorRenderer.ambientColor = Color.white;

            //Estimating the MaskMap from two different ColorMap
            RenderTexture maskMapAtlasRT = new RenderTexture(colorMapAtlas.width, colorMapAtlas.height, 24, RenderTextureFormat.ARGB32);
            maskMapAtlasRT.enableRandomWrite = true;
            maskMapAtlasRT.Create();

            maskEstimatorMaterial.SetTexture("_TexER0", colorMapER0Atlas);
            Graphics.Blit(colorMapAtlas, maskMapAtlasRT, maskEstimatorMaterial);
            RenderTexture.active = maskMapAtlasRT;
            Texture2D maskMap = new Texture2D(maskMapAtlasRT.width, maskMapAtlasRT.height, TextureFormat.RGB24, false);
            maskMap.ReadPixels(new Rect(0, 0, maskMapAtlasRT.width, maskMapAtlasRT.height), 0, 0);
            maskMap.Apply();
            RenderTexture.active = null;
            maskMapAtlasRT.Release();
            return maskMap;
        }

        public override void Cleanup()
        {
            EditorRenderer.Cleanup();
        }
    }
}
