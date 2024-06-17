using System.Collections;
using System.Collections.Generic;
using System.IO;

using UnityEngine;
using UnityEditor;
using Unity.EditorCoroutines.Editor;

using Raymarcher.Toolkit;
using Raymarcher.Utilities;

namespace Raymarcher.UEditor
{
    using static RMVolumeUtils;

    [CanEditMultipleObjects]
    [CustomEditor(typeof(RMVolumeMeshSliceRenderer))]
    public sealed class RMVolumeMeshSliceRendererEditor : RMEditorUtilities
    {
        private RMVolumeMeshSliceRenderer tex3DRenderer;

        private EditorCoroutine renderingTex3DProcess;

        private Texture2D tempGeneratedSlicedTexture;
        private RenderTexture tempSlicePackedRendererRT;
        private readonly List<Texture2D> tempListSlicedTextures = new List<Texture2D>();

        private void OnEnable()
        {
            tex3DRenderer = (RMVolumeMeshSliceRenderer)target;
        }
         
        private void OnDisable()
        {
            CleanUp();
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            RMs();

            if(tex3DRenderer.targetMaterial == null)
            {
                RMhelpbox("Target material is missing. Please assign the target volume-renderer material.", MessageType.Warning);
                return;
            }

            if(RMb("Auto Assign Material To Children", 220))
            {
                foreach(var renderer in tex3DRenderer.GetComponentsInChildren<Renderer>())
                {
                    if(renderer.sharedMaterials != null)
                    {
                        var mats = renderer.sharedMaterials;
                        for (int i = 0; i < mats.Length; i++)
                            mats[i] = tex3DRenderer.targetMaterial;
                        renderer.sharedMaterials = mats;
                    }
                }    
            }

            if (renderingTex3DProcess == null && RMb("Render To Texture 3D") && EditorUtility.DisplayDialog("Question", "Are you sure to render the current volume to the Texture3D? This may take a while...", "Yes", "No"))
            {
                EditorUtility.DisplayDialog("Standby!", "Please do not deselect the current volume renderer and wait until the Tex3D renderer is finished!", "OK");
                renderingTex3DProcess = EditorCoroutineUtility.StartCoroutine(IERenderSlices(), this);
            }

            RMimage(tex3DRenderer.RenderCameraOutput);
        }

        private void CleanUp()
        {
            tex3DRenderer.IsRenderingSlices = false;

            if (tempListSlicedTextures.Count > 0)
                tempListSlicedTextures.Clear();
            if(tempSlicePackedRendererRT != null)
            {
                tempSlicePackedRendererRT.Release();
                tempSlicePackedRendererRT = null;
            }
            if(tempGeneratedSlicedTexture != null)
            {
                DestroyImmediate(tempGeneratedSlicedTexture);
                tempGeneratedSlicedTexture = null;
            }

            if (renderingTex3DProcess != null)
            {
                EditorCoroutineUtility.StopCoroutine(renderingTex3DProcess);
                renderingTex3DProcess = null;
                EditorUtility.ClearProgressBar();
            }
        }


        private IEnumerator IERenderSlices()
        {
            tex3DRenderer.IsRenderingSlices = true;

            int commonRes = GetCommonVolumeResolution(tex3DRenderer.volumeResolution);
            int countOfSlices = tex3DRenderer.useCommonVolumeResolution ? commonRes : tex3DRenderer.countOfSlices;
            int resolution = tex3DRenderer.useCommonVolumeResolution ? commonRes : tex3DRenderer.sliceResolution;
            Material targetMat = tex3DRenderer.targetMaterial;

            targetMat.SetFloat(RMVolumeMeshSliceRenderer.PROPERTY_PREVIEW, 1);

            RenderTexture tempRenderingRT = new RenderTexture(tex3DRenderer.RenderCameraOutput.descriptor);
            tempRenderingRT.width = resolution;
            tempRenderingRT.height = resolution;
            tempRenderingRT.Create();
            tex3DRenderer.RenderCamera.targetTexture = tempRenderingRT;

            tempListSlicedTextures.Clear();

            yield return null;

            for (int i = 0; i < countOfSlices; i++)
            {
                float t = (float)i / countOfSlices;

                EditorUtility.DisplayProgressBar("Loading", $"Rendering slices [{i}/{countOfSlices}]", t);

                targetMat.SetFloat(RMVolumeMeshSliceRenderer.PROPERTY_SLICE, t);
                tex3DRenderer.RenderCamera.Render();

                yield return null;

                Texture2D texNew = new Texture2D(resolution, resolution, TextureFormat.RGB24, false);
                RenderTexture.active = tempRenderingRT;
                texNew.ReadPixels(new Rect(0, 0, tempRenderingRT.width, tempRenderingRT.height), 0, 0);
                texNew.Apply();
                tempListSlicedTextures.Add(texNew);
            }

            targetMat.SetFloat(RMVolumeMeshSliceRenderer.PROPERTY_PREVIEW, 0);

            EditorUtility.ClearProgressBar();

            tempRenderingRT.Release();
            tex3DRenderer.RenderCamera.targetTexture = tex3DRenderer.RenderCameraOutput;

            renderingTex3DProcess = EditorCoroutineUtility.StartCoroutine(IEPackSlices(), this);
        }

        private IEnumerator IEPackSlices()
        {
            int commonRes = GetCommonVolumeResolution(tex3DRenderer.volumeResolution);
            Vector2Int dimensions = FindDimensions(tex3DRenderer.useCommonVolumeResolution ? commonRes : tex3DRenderer.countOfSlices);
            int resolution = tex3DRenderer.useCommonVolumeResolution ? commonRes : tex3DRenderer.sliceResolution;

            tempSlicePackedRendererRT = new RenderTexture(resolution, resolution, 0, UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm);

            yield return null;

            tempGeneratedSlicedTexture = new Texture2D(resolution * dimensions.x, resolution * dimensions.y, TextureFormat.RGB24, false);

            for (int y = 0; y < dimensions.y; y++)
            {
                for (int x = 0; x < dimensions.x; x++)
                {
                    int indx = tempListSlicedTextures.Count - 1 - (y * dimensions.x + x);

                    EditorUtility.DisplayProgressBar("Loading", $"Packing slices [{tempListSlicedTextures.Count - indx}/{tempListSlicedTextures.Count}]", 1 - (float)indx / tempListSlicedTextures.Count);

                    Texture2D textureSlice = tempListSlicedTextures[indx];
                    Graphics.Blit(textureSlice, tempSlicePackedRendererRT);

                    yield return null;

                    RenderTexture.active = tempSlicePackedRendererRT;

                    tempGeneratedSlicedTexture.ReadPixels(new Rect(0, 0, tempSlicePackedRendererRT.width, tempSlicePackedRendererRT.height), 
                        (resolution * (dimensions.x - 1)) - (resolution * x),
                        resolution * y);

                    tempGeneratedSlicedTexture.Apply();
                    RenderTexture.active = null;
                }
            }

            tempSlicePackedRendererRT.Release();
            tempSlicePackedRendererRT = null;

            string path = EditorUtility.SaveFilePanel("Save Rendered 3D Texture", Application.dataPath, "Texture3D", "png");
            SaveTextureToFile(tempGeneratedSlicedTexture, path, dimensions);

            if (tempGeneratedSlicedTexture != null)
                DestroyImmediate(tempGeneratedSlicedTexture);

            CleanUp();
        }

        private Vector2Int FindDimensions(int count)
        {
            int bestWidth = 1;
            int bestHeight = count;

            for (int width = 2; width <= count / 2; width++)
            {
                if (count % width == 0)
                {
                    int height = count / width;
                    if (Mathf.Abs(width - height) < Mathf.Abs(bestWidth - bestHeight))
                    {
                        bestWidth = width;
                        bestHeight = height;
                    }
                }
            }

            return new Vector2Int(bestWidth, bestHeight);
        }

        private void SaveTextureToFile(Texture2D src, string existingFilePath, Vector2Int dimensions)
        {
            if (src == null)
            {
                Debug.LogError("Texture to save is missing!");
                return;
            }

            if (string.IsNullOrWhiteSpace(existingFilePath))
                return;

            byte[] texData = src.EncodeToPNG();

            try
            {
                File.WriteAllBytes(existingFilePath, texData);
                AssetDatabase.Refresh();

                const string assets = "Assets/";
                int assetLocalPath = existingFilePath.IndexOf(assets);
                existingFilePath = existingFilePath.Substring(assetLocalPath, existingFilePath.Length - assetLocalPath);

                TextureImporter importer = (TextureImporter)TextureImporter.GetAtPath(existingFilePath);
                TextureImporterSettings settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);

                settings.flipbookColumns = dimensions.x;
                settings.flipbookRows = dimensions.y;
                settings.textureShape = TextureImporterShape.Texture3D;
                settings.ApplyTextureType(TextureImporterType.Default);
                importer.SetTextureSettings(settings);

                importer.isReadable = false;
                importer.textureShape = TextureImporterShape.Texture3D;
                importer.alphaSource = TextureImporterAlphaSource.None;
                importer.mipmapEnabled = false;
                importer.streamingMipmaps = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = src.filterMode;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.npotScale = TextureImporterNPOTScale.None;

                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
            }
            catch (IOException e)
            {
                Debug.LogError("An error occured while saving the 3D texture... Exception: " + e.Message);
            }
        }
    }
}