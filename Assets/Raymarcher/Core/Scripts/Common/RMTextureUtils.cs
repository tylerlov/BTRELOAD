using System;
using System.Collections.Generic;

using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Raymarcher.Utilities
{
    public static class RMTextureUtils
    {
        public static Texture2DArray GenerateTextureArray(List<Texture2D> existingTextures)
        {
            if (existingTextures.Count == 0)
                return null;

            int texCount = 0;
            Texture2D referenceTexture = null;
            for (int i = 0; i < existingTextures.Count; i++)
            {
                if (existingTextures[i] == null)
                    continue;
                if (!existingTextures[i].isReadable)
                {
                    RMDebug.Debug(nameof(RMTextureUtils), $"Couldn't create & cache a material texture array collection... Texture '{existingTextures[i].name}' is not readable! Please go to the texture import settings and enable 'Read/Write'", true);
                    return null;
                }
                if (referenceTexture == null)
                    referenceTexture = existingTextures[i];
                if (existingTextures[i].width != referenceTexture.width || existingTextures[i].height != referenceTexture.height)
                {
                    RMDebug.Debug(nameof(RMTextureUtils), $"Couldn't create & cache a material texture array collection... Texture '{existingTextures[i].name}' has different dimensions! Please keep all your textures in the same & equal dimensions (eg. {referenceTexture.width}x{referenceTexture.height})", true);
                    return null;
                }
                texCount++;
            }
            if (referenceTexture == null)
                return null;
            if (texCount == 0)
                return null;

            Texture2DArray newArray = new
                Texture2DArray(referenceTexture.width,
                referenceTexture.height, existingTextures.Count,
                TextureFormat.RGBA32, false, false);

            newArray.filterMode = FilterMode.Bilinear;
            newArray.wrapMode = TextureWrapMode.Repeat;

            for (int i = 0; i < existingTextures.Count; i++)
            {
                if(existingTextures[i])
                    newArray.SetPixels32(existingTextures[i].GetPixels32(0), i, 0);
            }

            newArray.Apply();
            return newArray;
        }

        public static int GetInstanceTextureIndexFromCachedTextureList(Texture2D existingTextureEntry, List<Texture2D> textureList)
        {
            for (int i = 0; i < textureList.Count; i++)
            {
                if (textureList[i] == existingTextureEntry)
                    return i;
            }
            return -1;
        }

        public static RenderTexture CreateDynamic3DRenderTexture(int uniformResolution, string name)
        {
            return CreateDynamic3DRenderTexture(uniformResolution, uniformResolution, uniformResolution, name);
        }

        public static RenderTexture CreateDynamic3DRenderTexture(int width, int height, int depth, string name)
        {
            RenderTexture rt = new RenderTexture(width, height, 0, GraphicsFormat.R8G8B8A8_UNorm, 0);
            rt.name = name;
            rt.autoGenerateMips = false;
            rt.useMipMap = false;
            rt.dimension = TextureDimension.Tex3D;
            rt.enableRandomWrite = true;
            rt.volumeDepth = depth;
            rt.Create();
            return rt;
        }

        public static bool CompareTex3DDimensions(Texture3D tex3D, int targetResolution)
        {
            if (tex3D.width != targetResolution || tex3D.height != targetResolution || tex3D.depth != targetResolution)
            {
                RMDebug.Debug(typeof(RMTextureUtils), $"Dimensions of the input RT3D ({tex3D.width}x{tex3D.height}x{tex3D.depth}) don't match the target resolution ({targetResolution})!", true);
                return false;
            }
            return true;
        }

        public static bool CompareRT3DDimensions(RenderTexture rt3D, int targetResolution)
        {
            if(rt3D.dimension != TextureDimension.Tex3D)
            {
                RMDebug.Debug(typeof(RMTextureUtils), $"Input RT3D ({rt3D.name}-{rt3D.dimension}) is not a type of Tex3D!", true);
                return false;
            }
            if (rt3D.width != targetResolution || rt3D.height != targetResolution || rt3D.volumeDepth != targetResolution)
            {
                RMDebug.Debug(typeof(RMTextureUtils), $"Dimensions of the input RT3D ({rt3D.width}x{rt3D.height}x{rt3D.volumeDepth}) don't match the target resolution ({targetResolution})!", true);
                return false;
            }
            return true;
        }

        private const string COMPUTE_SHADER_RESOURCES = "RMTex3DToRTCompute";
        private const string COMPUTE_SHADER_KERNEL = "RTConvertor";
        private const string COMPUTE_SHADER_IN = "TexInput";
        private const string COMPUTE_SHADER_OUT = "TexOutput";
        private const int COMPUTE_DISPATCH_THREADGROUPS = 8;

        public static RenderTexture ConvertTexture3DToRenderTexture3D(Texture3D source)
        {
            ComputeShader computeConvertor = Resources.Load<ComputeShader>(COMPUTE_SHADER_RESOURCES);
            if(computeConvertor == null)
            {
                RMDebug.Debug(typeof(RMTextureUtils), "Couldn't find a compute shader while converting from Tex3D to RT3D", true);
                return null;
            }

            RenderTexture rt = CreateDynamic3DRenderTexture(source.width, source.height, source.depth, source.name);

            int kernel = computeConvertor.FindKernel(COMPUTE_SHADER_KERNEL);
            computeConvertor.SetTexture(kernel, COMPUTE_SHADER_IN, source);
            computeConvertor.SetTexture(kernel, COMPUTE_SHADER_OUT, rt);
            computeConvertor.Dispatch(kernel, source.width / COMPUTE_DISPATCH_THREADGROUPS, source.height / COMPUTE_DISPATCH_THREADGROUPS, source.depth / COMPUTE_DISPATCH_THREADGROUPS);

            return rt;
        }

        public static void ConvertRenderTexture3DToTexture3D(RenderTexture renderTexture3D, Action<Texture3D> callbackResult)
        {
            if (renderTexture3D == null)
                return;
            if (renderTexture3D.dimension != TextureDimension.Tex3D)
            {
                RMDebug.Debug(typeof(RMTextureUtils), "Couldn't convert a RT3D to Tex3D because the input RT3D is not a Texture3D!");
                return;
            }

            int width = renderTexture3D.width;
            int height = renderTexture3D.height;
            int depth = renderTexture3D.volumeDepth;

            var na = new NativeArray<byte>(width * height * depth * 4, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            AsyncGPUReadback.RequestIntoNativeArray(ref na, renderTexture3D, 0, (_) =>
            {
                Texture3D output = new Texture3D(width, height, depth, renderTexture3D.graphicsFormat, TextureCreationFlags.None);
                output.wrapMode = TextureWrapMode.Clamp;
                output.SetPixelData(na, 0);
                output.Apply(false, true);
                na.Dispose();
                callbackResult?.Invoke(output);
            });
        }

#if UNITY_EDITOR
        public static void SaveRenderTexture3DToEditorAssets(RenderTexture entryRT3D)
        {
            if (entryRT3D == null)
                return;
            if(entryRT3D.dimension != TextureDimension.Tex3D)
            {
                EditorUtility.DisplayDialog("Error!", "Can't save the modified RT3D to assets. The input RT3D is not a Tex3D!", "OK");
                return;
            }
            if (EditorApplication.isPaused)
            {
                EditorUtility.DisplayDialog("Error!", "Can't save the modified RT3D to assets. Application cannot be paused!", "OK");
                return;
            }

            ConvertRenderTexture3DToTexture3D(entryRT3D, ConvertCallback);
        }

        private static void ConvertCallback(Texture3D entry)
        {
            string path = EditorUtility.SaveFilePanelInProject("Save Converted Tex3D as .Asset", entry.name, "asset", "");
            if (string.IsNullOrEmpty(path))
            {
                EditorUtility.DisplayDialog("Unsuccessful!", "The save was unsuccessful. Path was empty!", "OK");
                return;
            }
            
            AssetDatabase.CreateAsset(entry, path);
            AssetDatabase.SaveAssetIfDirty(entry);
            EditorGUIUtility.PingObject(entry);
        }
#endif
    }
}