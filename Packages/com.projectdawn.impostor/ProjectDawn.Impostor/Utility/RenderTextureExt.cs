using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace ProjectDawn.Impostor
{
    public static class RenderTextureExt
    {
        public static Texture2D ToTexture2D(this RenderTexture renderTexture,
            GraphicsFormat format = GraphicsFormat.B8G8R8A8_SRGB, 
            FilterMode filter = FilterMode.Bilinear,
            bool mipmaps = false)
        {
            int width = renderTexture.width;
            int height = renderTexture.width;

            var texture = new Texture2D(width, height, format, mipmaps ? TextureCreationFlags.MipChain : TextureCreationFlags.None);
            texture.filterMode = filter;

            /*if (async)
            {
                var cmd = new CommandBuffer();
                cmd.RequestAsyncReadback(renderTexture, request =>
                {
                    texture.SetPixelData(request.GetData<byte>(), 0);
                    texture.Apply(mipmaps);
                });
                Graphics.ExecuteCommandBuffer(cmd);
            }
            else*/
            {
                RenderTexture.active = renderTexture;
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0, true);
                texture.Apply();
                RenderTexture.active = null;
            }

            texture.Compress(false);

            return texture;
        }
    }
}