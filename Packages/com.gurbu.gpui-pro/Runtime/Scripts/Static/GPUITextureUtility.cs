// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;
using Unity.Collections;
using UnityEngine.Jobs;
using UnityEngine.Rendering;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GPUInstancerPro
{
    public static class GPUITextureUtility
    {
        public static void CopyTextureWithComputeShader(Texture source, Texture destination, int offsetX, int sourceMip = 0, int destinationMip = 0)
        {
            int sourceW = source.width;
            int sourceH = source.height;
            for (int i = 0; i < sourceMip; i++)
            {
                sourceW >>= 1;
                sourceH >>= 1;
            }

            ComputeShader cs = GPUIConstants.CS_TextureUtility;
            int kernelIndex = 0;

            cs.SetTexture(kernelIndex, GPUIConstants.PROP_source, source, sourceMip);
            cs.SetTexture(kernelIndex, GPUIConstants.PROP_destination, destination, destinationMip);

            cs.SetInt(GPUIConstants.PROP_offsetX, offsetX);
            cs.SetInt(GPUIConstants.PROP_sourceSizeX, sourceW);
            cs.SetInt(GPUIConstants.PROP_sourceSizeY, sourceH);

            cs.DispatchXY(kernelIndex, sourceW, sourceH);
        }

        public static void CopyHiZTextureWithComputeShader(Texture source, Texture destination, int offsetX, int sourceMip = 0, int destinationMip = 0, bool reverseZ = true)
        {
            int sourceW = source.width;
            int sourceH = source.height;
            for (int i = 0; i < sourceMip; i++)
            {
                sourceW >>= 1;
                sourceH >>= 1;
            }

            ComputeShader cs = GPUIConstants.CS_HiZTextureCopy;
            int kernelIndex = 0;

            cs.SetTexture(kernelIndex, GPUIConstants.PROP_source, source, sourceMip);
            cs.SetTexture(kernelIndex, GPUIConstants.PROP_destination, destination, destinationMip);

            cs.SetInt(GPUIConstants.PROP_offsetX, offsetX);
            cs.SetInt(GPUIConstants.PROP_sourceSizeX, sourceW);
            cs.SetInt(GPUIConstants.PROP_sourceSizeY, sourceH);
            cs.SetInt(GPUIConstants.PROP_reverseZ, reverseZ && GPUIRuntimeSettings.Instance.ReversedZBuffer ? 1 : 0);

            cs.DispatchXY(kernelIndex, sourceW, sourceH);
        }

        public static void CopyHiZTextureWithComputeShader(CommandBuffer commandBuffer, RenderTargetIdentifier sourceIdentifier, RenderTextureSubElement sourceSubElement, int sourceW, int sourceH, RenderTargetIdentifier destinationIdentifier, RenderTextureSubElement destinationSubElement, int offsetX, int sourceMip = 0, int destinationMip = 0, bool reverseZ = true)
        {
            for (int i = 0; i < sourceMip; i++)
            {
                sourceW >>= 1;
                sourceH >>= 1;
            }

            ComputeShader cs = GPUIConstants.CS_HiZTextureCopy;
            int kernelIndex = 0;

            commandBuffer.SetComputeTextureParam(cs, kernelIndex, GPUIConstants.PROP_source, sourceIdentifier, sourceMip, sourceSubElement);
            commandBuffer.SetComputeTextureParam(cs, kernelIndex, GPUIConstants.PROP_destination, destinationIdentifier, destinationMip, destinationSubElement);
            commandBuffer.SetComputeIntParam(cs, GPUIConstants.PROP_offsetX, offsetX);
            commandBuffer.SetComputeIntParam(cs, GPUIConstants.PROP_sourceSizeX, sourceW);
            commandBuffer.SetComputeIntParam(cs, GPUIConstants.PROP_sourceSizeY, sourceH);
            commandBuffer.SetComputeIntParam(cs, GPUIConstants.PROP_reverseZ, reverseZ && GPUIRuntimeSettings.Instance.ReversedZBuffer ? 1 : 0);
            commandBuffer.DispatchCompute(cs, kernelIndex, Mathf.CeilToInt(sourceW / GPUIConstants.CS_THREAD_COUNT_2D), Mathf.CeilToInt(sourceH / GPUIConstants.CS_THREAD_COUNT_2D), 1);
        }

        public static void CopyHiZTextureArrayWithComputeShader(Texture source, Texture destination, int offsetX, int textureArrayIndex, int sourceMip = 0, int destinationMip = 0, bool reverseZ = true)
        {
            int sourceW = source.width;
            int sourceH = source.height;
            for (int i = 0; i < sourceMip; i++)
            {
                sourceW >>= 1;
                sourceH >>= 1;
            }

            ComputeShader cs = GPUIConstants.CS_HiZTextureCopy;
            int kernelIndex = 1;

            cs.SetTexture(kernelIndex, GPUIConstants.PROP_textureArray, source, sourceMip);
            cs.SetTexture(kernelIndex, GPUIConstants.PROP_destination, destination, destinationMip);

            cs.SetInt(GPUIConstants.PROP_offsetX, offsetX);
            cs.SetInt(GPUIConstants.PROP_textureArrayIndex, textureArrayIndex);
            cs.SetInt(GPUIConstants.PROP_sourceSizeX, sourceW);
            cs.SetInt(GPUIConstants.PROP_sourceSizeY, sourceH);
            cs.SetInt(GPUIConstants.PROP_reverseZ, reverseZ && GPUIRuntimeSettings.Instance.ReversedZBuffer ? 1 : 0);

            cs.DispatchXY(kernelIndex, sourceW, sourceH);
        }

        public static void CopyHiZTextureArrayWithComputeShader(CommandBuffer commandBuffer, RenderTargetIdentifier sourceIdentifier, RenderTextureSubElement sourceSubElement, int sourceW, int sourceH, RenderTargetIdentifier destinationIdentifier, RenderTextureSubElement destinationSubElement, int offsetX, int textureArrayIndex, int sourceMip = 0, int destinationMip = 0, bool reverseZ = true)
        {
            for (int i = 0; i < sourceMip; i++)
            {
                sourceW >>= 1;
                sourceH >>= 1;
            }

            ComputeShader cs = GPUIConstants.CS_HiZTextureCopy;
            int kernelIndex = 1;

            commandBuffer.SetComputeTextureParam(cs, kernelIndex, GPUIConstants.PROP_textureArray, sourceIdentifier, sourceMip, sourceSubElement);
            commandBuffer.SetComputeTextureParam(cs, kernelIndex, GPUIConstants.PROP_destination, destinationIdentifier, destinationMip, destinationSubElement);
            commandBuffer.SetComputeIntParam(cs, GPUIConstants.PROP_offsetX, offsetX);
            commandBuffer.SetComputeIntParam(cs, GPUIConstants.PROP_sourceSizeX, sourceW);
            commandBuffer.SetComputeIntParam(cs, GPUIConstants.PROP_sourceSizeY, sourceH);
            commandBuffer.SetComputeIntParam(cs, GPUIConstants.PROP_reverseZ, reverseZ && GPUIRuntimeSettings.Instance.ReversedZBuffer ? 1 : 0);
            commandBuffer.SetComputeIntParam(cs, GPUIConstants.PROP_textureArrayIndex, textureArrayIndex);
            commandBuffer.DispatchCompute(cs, kernelIndex, Mathf.CeilToInt(sourceW / GPUIConstants.CS_THREAD_COUNT_2D), Mathf.CeilToInt(sourceH / GPUIConstants.CS_THREAD_COUNT_2D), 1);
        }

        public static void ReduceTextureWithComputeShader(Texture source, Texture destination, int offsetX, int sourceMip = 0, int destinationMip = 0)
        {
            int sourceW = source.width;
            int sourceH = source.height;
            int destinationW = destination.width;
            int destinationH = destination.height;
            for (int i = 0; i < sourceMip; i++)
            {
                sourceW >>= 1;
                sourceH >>= 1;
            }
            for (int i = 0; i < destinationMip; i++)
            {
                destinationW >>= 1;
                destinationH >>= 1;
            }

            if (destinationW == 0 || destinationH == 0)
                return;

            ComputeShader cs = GPUIConstants.CS_TextureReduce;
            int kernelIndex = 0;

            cs.SetTexture(kernelIndex, GPUIConstants.PROP_source, source, sourceMip);
            cs.SetTexture(kernelIndex, GPUIConstants.PROP_destination, destination, destinationMip);

            cs.SetInt(GPUIConstants.PROP_offsetX, offsetX);
            cs.SetInt(GPUIConstants.PROP_sourceSizeX, sourceW);
            cs.SetInt(GPUIConstants.PROP_sourceSizeY, sourceH);
            cs.SetInt(GPUIConstants.PROP_destinationSizeX, destinationW);
            cs.SetInt(GPUIConstants.PROP_destinationSizeY, destinationH);

            cs.DispatchXY(kernelIndex, destinationW, destinationH);
        }

        public static void ReduceTextureWithComputeShader(CommandBuffer commandBuffer, RenderTargetIdentifier sourceIdentifier, RenderTextureSubElement sourceSubElement, int sourceW, int sourceH, RenderTargetIdentifier destinationIdentifier, RenderTextureSubElement destinationSubElement, int offsetX, int sourceMip = 0, int destinationMip = 0)
        {
            int destinationW = sourceW;
            int destinationH = sourceH;
            for (int i = 0; i < sourceMip; i++)
            {
                sourceW >>= 1;
                sourceH >>= 1;
            }
            for (int i = 0; i < destinationMip; i++)
            {
                destinationW >>= 1;
                destinationH >>= 1;
            }

            if (destinationW == 0 || destinationH == 0)
                return;

            ComputeShader cs = GPUIConstants.CS_TextureReduce;
            int kernelIndex = 0;

            commandBuffer.SetComputeTextureParam(cs, kernelIndex, GPUIConstants.PROP_source, sourceIdentifier, sourceMip, sourceSubElement);
            commandBuffer.SetComputeTextureParam(cs, kernelIndex, GPUIConstants.PROP_destination, destinationIdentifier, destinationMip, destinationSubElement);
            commandBuffer.SetComputeIntParam(cs, GPUIConstants.PROP_offsetX, offsetX);
            commandBuffer.SetComputeIntParam(cs, GPUIConstants.PROP_sourceSizeX, sourceW);
            commandBuffer.SetComputeIntParam(cs, GPUIConstants.PROP_sourceSizeY, sourceH);
            commandBuffer.SetComputeIntParam(cs, GPUIConstants.PROP_destinationSizeX, destinationW);
            commandBuffer.SetComputeIntParam(cs, GPUIConstants.PROP_destinationSizeY, destinationH);
            commandBuffer.DispatchCompute(cs, kernelIndex, Mathf.CeilToInt(destinationW / GPUIConstants.CS_THREAD_COUNT_2D), Mathf.CeilToInt(destinationH / GPUIConstants.CS_THREAD_COUNT_2D), 1);
        }

        public static Texture2D RenderTextureToTexture2D(RenderTexture renderTexture, TextureFormat textureFormat, bool linear)
        {
            Texture2D texture2d = new Texture2D(renderTexture.width, renderTexture.height, textureFormat, false, linear);
            RenderTexture.active = renderTexture;
            texture2d.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            RenderTexture.active = null;

            return texture2d;
        }

#if UNITY_EDITOR
        public static Texture2D SaveRenderTextureToPNG(RenderTexture renderTexture, TextureFormat textureFormat, string folderPath, TextureImporterType textureImporterType = TextureImporterType.Default, int maxTextureSize = 2048, bool linear = false, bool mipmapEnabled = true, bool sRGBTexture = true, bool alphaIsTransparency = false)
        {
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);
            string assetPath = folderPath + (string.IsNullOrEmpty(renderTexture.name) ? "RenderTexture" : renderTexture.name) + ".png";
            File.WriteAllBytes(assetPath, RenderTextureToTexture2D(renderTexture, textureFormat, linear).EncodeToPNG());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
            importer.maxTextureSize = maxTextureSize;
            importer.textureType = textureImporterType;
            importer.mipmapEnabled = mipmapEnabled;
            if (!sRGBTexture)
                importer.sRGBTexture = false;
            importer.mipMapsPreserveCoverage = mipmapEnabled;
            importer.alphaIsTransparency = alphaIsTransparency;
            AssetDatabase.ImportAsset(assetPath);

            Texture2D texture2D = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            return texture2D;
        }
#endif

        public static void DestroyRenderTexture(this RenderTexture rt)
        {
            if (rt != null)
            {
                rt.Release();
                GPUIUtility.DestroyGeneric(rt);
            }
        }
    }
}