using System;

using UnityEngine;

using Raymarcher.Constants;

namespace Raymarcher.CameraFilters
{
    using static RMConstants.CommonReflection;

    public static class RMCamFilterUtils
    {
        public static readonly Rect FRUSTUM_RECT = new Rect(0, 0, 1, 1);

        public const float NEAR_CLIP_OFFSET = 1.0e-3f;

        public static Vector3[] CalculateFrustum(Camera targetCamera, Camera.MonoOrStereoscopicEye eye = Camera.MonoOrStereoscopicEye.Mono)
        {
            Vector3[] corners = new Vector3[4];
            targetCamera.CalculateFrustumCorners(FRUSTUM_RECT, -targetCamera.nearClipPlane - NEAR_CLIP_OFFSET, eye, corners);
            return corners;
        }

        public static void AdjustFrustumToProjector(ref Vector3[] frustumCorners, float projectorSize, bool alterGLVertex = false)
        {
            for (int i = 0; i < frustumCorners.Length; i++)
            {
                Vector3 outCorner = frustumCorners[i]
                    + Vector3.left * (frustumCorners[i].x >= 0 ? projectorSize : -projectorSize)
                    + Vector3.down * (frustumCorners[i].y >= 0 ? projectorSize : -projectorSize);
                frustumCorners[i] = outCorner;
                if (alterGLVertex)
                    GL.Vertex(outCorner);
            }
        }

        public static void InitializeCameraFilter(Camera initialCameraTarget, RMRenderMaster sessionRM)
        {
            Type camType;
            switch (sessionRM.CompiledTargetPipeline)
            {
                case RMRenderMaster.TargetPipeline.BuiltIn:
                    {
                        if (!initialCameraTarget)
                            return;
                        camType = Type.GetType(RP_BUILTIN_CAMFILTER);
                        if (camType == null)
                        {
                            OutputError(sessionRM.CompiledTargetPipeline);
                            return;
                        }
                        var component = initialCameraTarget.GetComponent(camType);
                        if (!component)
                            component = initialCameraTarget.gameObject.AddComponent(camType);
                        component.GetType().GetMethod(METHOD_BUILTIN_SETUP).Invoke(component, new object[] { sessionRM, initialCameraTarget });
                    }
                    break;

                case RMRenderMaster.TargetPipeline.URP:
                    camType = Type.GetType(RP_URP_CAMFILTER);
                    if (camType == null)
                    {
                        OutputError(sessionRM.CompiledTargetPipeline);
                        return;
                    }
                    camType.GetMethod(METHOD_URP_SETUP).Invoke(null, new object[] { sessionRM.RenderingData.RendererSessionMaterialSource });
                    break;
            }
        }
        
        public static void DisposeCameraFilter(Camera initialCameraTarget, RMRenderMaster sessionRM)
        {
            Type camType;
            switch (sessionRM.CompiledTargetPipeline)
            {
                case RMRenderMaster.TargetPipeline.BuiltIn:
                    {
                        if (!initialCameraTarget)
                            return;
                        camType = Type.GetType(RP_BUILTIN_CAMFILTER);
                        if (camType == null)
                        {
                            OutputError(sessionRM.CompiledTargetPipeline);
                            return;
                        }
                        var component = initialCameraTarget.GetComponent(camType);
                        if (component)
                            component.GetType().GetMethod(METHOD_BUILTIN_DISPOSE).Invoke(component, null);
                        else
                            OutputError(sessionRM.CompiledTargetPipeline);
                    }
                    break;

                case RMRenderMaster.TargetPipeline.URP:
                    camType = Type.GetType(RP_URP_CAMFILTER);
                    if (camType == null)
                    {
                        OutputError(sessionRM.CompiledTargetPipeline);
                        return;
                    }
                    camType.GetMethod(METHOD_URP_DISPOSE).Invoke(null, null);
                    break;
            }
        }

        private static void OutputError(RMRenderMaster.TargetPipeline pipeline)
        {
            RMDebug.Debug(typeof(RMCamFilterUtils), $"Couldn't initialize/dispose a camera filter for pipeline '{pipeline}'. The camera filter class doesn't exist or couldn't be found. Please make sure you have chosen a correct pipeline!", true);
        }
    }
}