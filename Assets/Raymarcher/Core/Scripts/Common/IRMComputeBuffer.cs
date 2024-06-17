using System;

using UnityEngine;

namespace Raymarcher
{
    public interface IRMComputeBuffer
    {
        public ComputeBuffer RuntimeComputeBuffer { get; set; }

        public void CreateComputeBuffer();

        public void ReleaseComputeBuffer();

        public void PushComputeBuffer(in Material raymarcherMaterial);

        /// <summary>
        /// Specify the correct data container length and compute buffer stride size. Any mistakes will result in error messages
        /// https://developer.nvidia.com/content/understanding-structured-buffer-performance
        /// </summary>
        public abstract (int dataLength, int strideSize) GetComputeBufferLengthAndStride();

        /// <summary>
        /// Pass your custom data container array to the compute buffer for blitting
        /// </summary>
        public abstract Array GetComputeBufferDataContainer { get; }
    }

    public static class IRMComputeBufferExtensions
    {
        public static void CreateComputeBuffer(this IRMComputeBuffer computeBuffer)
        {
            ReleaseComputeBuffer(computeBuffer);
            var (dataLength, strideSize) = computeBuffer.GetComputeBufferLengthAndStride();
            if(dataLength > 0)
                computeBuffer.RuntimeComputeBuffer = new ComputeBuffer(dataLength, strideSize, ComputeBufferType.Default);
        }

        public static void CheckComputeBuffer(this IRMComputeBuffer computeBuffer)
        {
            if (computeBuffer.RuntimeComputeBuffer == null)
                computeBuffer.CreateComputeBuffer();
        }

        public static void SetComputeBuffer(this IRMComputeBuffer computeBuffer, string name, Material targetMaterial)
        {
            computeBuffer.RuntimeComputeBuffer.SetData(computeBuffer.GetComputeBufferDataContainer);
            targetMaterial.SetBuffer(name, computeBuffer.RuntimeComputeBuffer);
        }

        public static void ReleaseComputeBuffer(this IRMComputeBuffer computeBuffer)
        {
            if (computeBuffer.RuntimeComputeBuffer != null)
                computeBuffer.RuntimeComputeBuffer.Release();
            computeBuffer.RuntimeComputeBuffer = null;
        }
    }
}