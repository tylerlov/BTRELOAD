using UnityEngine;
using UnityEngine.Rendering;

namespace ProjectDawn.Impostor
{
    public static class CustomRenderMode
    {
        //public delegate void 
        public static void Render(RenderTexture target, Surface surface, CapturePoints capturePoints, Material overrideMaterial)
        {
            using (new AllowAsyncCompilationScope(false))
            {
                int resolution = target.width;

                var cmd = CommandBufferPool.Get($"Render Custom");
                cmd.Clear();

                var depth = Shader.PropertyToID("_DepthAttachment");
                cmd.GetTemporaryRT(depth, resolution, resolution, 24, target.filterMode, RenderTextureFormat.Depth);

                cmd.SetRenderTarget(new RenderTargetIdentifier(target), new RenderTargetIdentifier(depth));
                cmd.ClearRenderTarget(true, true, Color.black);

                var projection = Matrix4x4.Ortho(-capturePoints.Radius, capturePoints.Radius, -capturePoints.Radius, capturePoints.Radius, 0, capturePoints.Radius * 2);
                cmd.SetProjectionMatrix(projection);

                // Render all frames
                foreach (var snapshot in capturePoints.Points)
                {
                    var pixelRect = new Rect(snapshot.Uv.x * resolution, snapshot.Uv.y * resolution,
                        snapshot.Uv.width * resolution, snapshot.Uv.height * resolution);

                    var fromPoint = snapshot.From;
                    var toPoint = snapshot.To;
                    var up = Vector3.up;

                    var lookMatrix = Matrix4x4.LookAt(fromPoint, toPoint, up);
                    var viewMatrix = lookMatrix.inverse;

                    // Flip the z-axis of the view matrix to convert from left-handed to right-handed
                    viewMatrix.m20 = -viewMatrix.m20;
                    viewMatrix.m21 = -viewMatrix.m21;
                    viewMatrix.m22 = -viewMatrix.m22;
                    viewMatrix.m23 = -viewMatrix.m23;

                    cmd.SetViewport(pixelRect);

                    cmd.SetViewMatrix(viewMatrix);

                    foreach (var renderer in surface.Renderers)
                    {
                        Render(cmd, renderer, surface.Matrix, overrideMaterial);
                    }
                }

                cmd.ReleaseTemporaryRT(depth);

                Graphics.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }
        }

        static void Render(CommandBuffer cmd, Renderer renderer, Matrix4x4 tranform, Material overrideMaterial)
        {
            if (renderer.TryGetComponent(out MeshFilter meshFilter))
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    var material = materials[i];
                    foreach (var id in material.GetTexturePropertyNameIDs())
                        cmd.SetGlobalTexture(id, material.GetTexture(id));
                    cmd.DrawMesh(meshFilter.sharedMesh, tranform * renderer.transform.localToWorldMatrix, overrideMaterial, i, 0);
                }
            }
        }
    }
}