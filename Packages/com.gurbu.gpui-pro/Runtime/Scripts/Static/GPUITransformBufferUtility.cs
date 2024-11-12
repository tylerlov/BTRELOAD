// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections.Generic;
using UnityEngine;

namespace GPUInstancerPro
{
    public static class GPUITransformBufferUtility
    {
        #region RemoveInstances

        public static void RemoveInstancesInsideCollider(GPUIManager gpuiManager, Collider collider, float offset = 0, List<int> prototypeIndexFilter = null)
        {
            if (!Application.isPlaying)
            {
                Debug.LogError("RemoveInstances method can not be used in Edit Mode!");
                return;
            }

            if (collider is BoxCollider boxCollider)
                RemoveInstancesInsideBoxCollider(gpuiManager, boxCollider, offset, prototypeIndexFilter);
            else if (collider is SphereCollider sphereCollider)
                RemoveInstancesInsideSphereCollider(gpuiManager, sphereCollider, offset, prototypeIndexFilter);
            else if (collider is CapsuleCollider capsuleCollider)
                RemoveInstancesInsideCapsuleCollider(gpuiManager, capsuleCollider, offset, prototypeIndexFilter);
            else
                RemoveInstancesInsideBounds(gpuiManager, collider.bounds, offset, prototypeIndexFilter);
        }

        public static void RemoveInstancesInsideBounds(GPUIManager gpuiManager, Bounds bounds, float offset = 0, List<int> prototypeIndexFilter = null)
        {
            int count = gpuiManager.GetPrototypeCount();
            Vector3 center = bounds.center;
            Vector3 extents = bounds.extents + Vector3.one * offset;

            for (int i = 0; i < count; i++)
            {
                if (prototypeIndexFilter != null && !prototypeIndexFilter.Contains(i))
                    continue;

                if (GPUIRenderingSystem.TryGetTransformBuffer(gpuiManager.GetRenderKey(i), out GPUIShaderBuffer transformShaderBuffer, out int bufferStartIndex, out int bufferSize))
                    RemoveInstancesInsideBounds(transformShaderBuffer, bufferStartIndex, bufferSize, center, extents);
            }
        }

        public static void RemoveInstancesInsideBounds(GPUIShaderBuffer shaderBuffer, int bufferStartIndex, int bufferSize, Vector3 center, Vector3 extents)
        {
            if (bufferSize == 0 || shaderBuffer.Buffer == null) return;

            ComputeShader cs = GPUIConstants.CS_TransformModifications;
            int kernelIndex = 2;

            cs.SetBuffer(kernelIndex, GPUIConstants.PROP_gpuiTransformBuffer, shaderBuffer.Buffer);
            cs.SetInt(GPUIConstants.PROP_startIndex, bufferStartIndex);
            cs.SetInt(GPUIConstants.PROP_bufferSize, bufferSize);
            cs.SetVector(GPUIConstants.PROP_boundsCenter, center);
            cs.SetVector(GPUIConstants.PROP_boundsExtents, extents);
            cs.DispatchX(kernelIndex, bufferSize);

            shaderBuffer.OnDataModified();
        }

        private static void RemoveInstancesInsideBoxCollider(GPUIManager gpuiManager, BoxCollider boxCollider, float offset = 0, List<int> prototypeIndexFilter = null)
        {
            int count = gpuiManager.GetPrototypeCount();
            Vector3 center = boxCollider.center;
            Vector3 extents = boxCollider.size / 2 + Vector3.one * offset;
            Matrix4x4 modifierTransform = boxCollider.transform.localToWorldMatrix;

            for (int i = 0; i < count; i++)
            {
                if (prototypeIndexFilter != null && !prototypeIndexFilter.Contains(i))
                    continue;

                if (GPUIRenderingSystem.TryGetTransformBuffer(gpuiManager.GetRenderKey(i), out GPUIShaderBuffer transformShaderBuffer, out int bufferStartIndex, out int bufferSize))
                    RemoveInstancesInsideBoxCollider(transformShaderBuffer, bufferStartIndex, bufferSize, center, extents, modifierTransform);
            }
        }

        private static void RemoveInstancesInsideBoxCollider(GPUIShaderBuffer shaderBuffer, int bufferStartIndex, int bufferSize, Vector3 center, Vector3 extents, Matrix4x4 modifierTransform)
        {
            if (bufferSize == 0 || shaderBuffer.Buffer == null) return;

            ComputeShader cs = GPUIConstants.CS_TransformModifications;
            int kernelIndex = 3;

            cs.SetBuffer(kernelIndex, GPUIConstants.PROP_gpuiTransformBuffer, shaderBuffer.Buffer);
            cs.SetInt(GPUIConstants.PROP_startIndex, bufferStartIndex);
            cs.SetInt(GPUIConstants.PROP_bufferSize, bufferSize);
            cs.SetVector(GPUIConstants.PROP_boundsCenter, center);
            cs.SetVector(GPUIConstants.PROP_boundsExtents, extents);
            cs.SetMatrix(GPUIConstants.PROP_modifierTransform, modifierTransform);
            cs.DispatchX(kernelIndex, bufferSize);

            shaderBuffer.OnDataModified();
        }

        private static void RemoveInstancesInsideSphereCollider(GPUIManager gpuiManager, SphereCollider sphereCollider, float offset = 0, List<int> prototypeIndexFilter = null)
        {
            int count = gpuiManager.GetPrototypeCount();
            Vector3 center = sphereCollider.center + sphereCollider.transform.position;
            Vector3 scale = sphereCollider.transform.localScale;
            float radius = sphereCollider.radius * Mathf.Max(Mathf.Max(scale.x, scale.y), scale.z) + offset;

            for (int i = 0; i < count; i++)
            {
                if (prototypeIndexFilter != null && !prototypeIndexFilter.Contains(i))
                    continue;

                if (GPUIRenderingSystem.TryGetTransformBuffer(gpuiManager.GetRenderKey(i), out GPUIShaderBuffer transformShaderBuffer, out int bufferStartIndex, out int bufferSize))
                    RemoveInstancesInsideSphereCollider(transformShaderBuffer, bufferStartIndex, bufferSize, center, radius);
            }
        }

        private static void RemoveInstancesInsideSphereCollider(GPUIShaderBuffer shaderBuffer, int bufferStartIndex, int bufferSize, Vector3 center, float radius)
        {
            if (bufferSize == 0 || shaderBuffer.Buffer == null) return;

            ComputeShader cs = GPUIConstants.CS_TransformModifications;
            int kernelIndex = 4;

            cs.SetBuffer(kernelIndex, GPUIConstants.PROP_gpuiTransformBuffer, shaderBuffer.Buffer);
            cs.SetInt(GPUIConstants.PROP_startIndex, bufferStartIndex);
            cs.SetInt(GPUIConstants.PROP_bufferSize, bufferSize);
            cs.SetVector(GPUIConstants.PROP_boundsCenter, center);
            cs.SetFloat(GPUIConstants.PROP_modifierRadius, radius);
            cs.DispatchX(kernelIndex, bufferSize);

            shaderBuffer.OnDataModified();
        }

        private static void RemoveInstancesInsideCapsuleCollider(GPUIManager gpuiManager, CapsuleCollider capsuleCollider, float offset = 0, List<int> prototypeIndexFilter = null)
        {
            int count = gpuiManager.GetPrototypeCount();
            Vector3 center = capsuleCollider.center;
            Vector3 scale = capsuleCollider.transform.localScale;
            float radius = capsuleCollider.radius * Mathf.Max(Mathf.Max(
                capsuleCollider.direction == 0 ? 0 : scale.x,
                capsuleCollider.direction == 1 ? 0 : scale.y),
                capsuleCollider.direction == 2 ? 0 : scale.z) + offset;
            float height = capsuleCollider.height * (
                    capsuleCollider.direction == 0 ? scale.x : 0 +
                    capsuleCollider.direction == 1 ? scale.y : 0 +
                    capsuleCollider.direction == 2 ? scale.z : 0);

            for (int i = 0; i < count; i++)
            {
                if (prototypeIndexFilter != null && !prototypeIndexFilter.Contains(i))
                    continue;

                if (GPUIRenderingSystem.TryGetTransformBuffer(gpuiManager.GetRenderKey(i), out GPUIShaderBuffer transformShaderBuffer, out int bufferStartIndex, out int bufferSize))
                    RemoveInstancesInsideCapsuleCollider(transformShaderBuffer, bufferStartIndex, bufferSize, center, radius, height);
            }
        }

        private static void RemoveInstancesInsideCapsuleCollider(GPUIShaderBuffer shaderBuffer, int bufferStartIndex, int bufferSize, Vector3 center, float radius, float height)
        {
            if (bufferSize == 0 || shaderBuffer.Buffer == null) return;

            ComputeShader cs = GPUIConstants.CS_TransformModifications;
            int kernelIndex = 5;

            cs.SetBuffer(kernelIndex, GPUIConstants.PROP_gpuiTransformBuffer, shaderBuffer.Buffer);
            cs.SetInt(GPUIConstants.PROP_startIndex, bufferStartIndex);
            cs.SetInt(GPUIConstants.PROP_bufferSize, bufferSize);
            cs.SetVector(GPUIConstants.PROP_boundsCenter, center);
            cs.SetFloat(GPUIConstants.PROP_modifierRadius, radius);
            cs.SetFloat(GPUIConstants.PROP_modifierHeight, height);
            cs.DispatchX(kernelIndex, bufferSize);

            shaderBuffer.OnDataModified();
        }

        #endregion RemoveInstances

        #region ApplyMatrixOffset

        public static void ApplyMatrixOffsetToTransforms(GPUIManager manager, Matrix4x4 matrixOffset)
        {
            int prototypeCount = manager.GetPrototypeCount();
            for (int i = 0; i < prototypeCount; i++)
            {
                int renderKey = manager.GetRenderKey(i);
                ApplyMatrixOffsetToTransforms(renderKey, matrixOffset);
            }
        }

        public static void ApplyMatrixOffsetToTransforms(int renderKey, Matrix4x4 matrixOffset)
        {
            if (renderKey == 0)
                return;
            if (GPUIRenderingSystem.TryGetTransformBuffer(renderKey, out GPUIShaderBuffer shaderBuffer, out int bufferStartIndex, out int bufferSize))
                ApplyMatrixOffsetToTransforms(shaderBuffer, bufferStartIndex, bufferSize, matrixOffset);
        }

        public static void ApplyMatrixOffsetToTransforms(GPUIShaderBuffer shaderBuffer, int bufferStartIndex, int bufferSize, Matrix4x4 matrixOffset)
        {
            if (bufferSize == 0 || shaderBuffer.Buffer == null) return;

            ComputeShader cs = GPUIConstants.CS_TransformModifications;

            cs.SetBuffer(1, GPUIConstants.PROP_gpuiTransformBuffer, shaderBuffer.Buffer);
            cs.SetInt(GPUIConstants.PROP_startIndex, bufferStartIndex);
            cs.SetInt(GPUIConstants.PROP_bufferSize, bufferSize);
            cs.SetMatrix(GPUIConstants.PROP_matrix44, matrixOffset);
            cs.DispatchX(1, bufferSize);

            shaderBuffer.OnDataModified();
        }

        #endregion ApplyMatrixOffset
    }
}