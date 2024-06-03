using System;
using System.Collections;

// Unity
using UnityEngine;

namespace GUPS.EasyPerformanceMonitor.Persistent
{
    /// <summary>
    /// Static class to capture and resize a screenshot, based on a passed width.
    /// </summary>
    /// <remarks>
    /// The <see cref="ScreenshotCapturer"/> class provides a set of static methods to capture a screenshot, resize it based on
    /// the specified width, and save it to the provided file path. The capturing and resizing operations are performed
    /// using an IEnumerator. The captured screenshot is first read into a <see cref="Texture2D"/>, resized, encoded
    /// into JPG format, and then saved to the specified file path.
    /// </remarks>
    internal static class ScreenshotCapturer
    {
        /// <summary>
        /// Take a screenshot and save it to the passed <paramref name="_FilePath"/>.
        /// </summary>
        /// <param name="_FilePath">The file path to save the screenshot in.</param>
        /// <param name="_Width">The target width for the screenshot.</param>
        /// <returns>An IEnumerator taking and saving the screenshot.</returns>
        public static IEnumerator TakeScreenshot(String _FilePath, int _Width)
        { 
            // Read the screen buffer after rendering is complete.
            yield return new WaitForEndOfFrame();

            // Create a texture in RGB24 format the size of the screen.
            int width = Screen.width;
            int height = Screen.height;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);

            // Read the screen contents into the texture.
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            // Resize the texture.
            tex = Resize(tex, _Width, (int)(_Width / (float)tex.width * (float)tex.height));

            // Encode the texture in JPG format.
            byte[] bytes = ImageConversion.EncodeToJPG(tex);

            // Destroy the texture.
            UnityEngine.Object.Destroy(tex);

            // Write the returned byte array to a file in the passed folder.
            System.IO.File.WriteAllBytes(_FilePath, bytes);
        }

        /// <summary>
        /// Resize the texture to the target size.
        /// </summary>
        /// <param name="_Texture">The texture to resize.</param>
        /// <param name="_TargetWidth">The target width.</param>
        /// <param name="_TargetHeight">The target height.</param>
        /// <returns>The resized texture.</returns>
        private static Texture2D Resize(Texture2D _Texture, int _TargetWidth, int _TargetHeight)
        {
            RenderTexture var_RenderTexture = new RenderTexture(_TargetWidth, _TargetHeight, 24);
            RenderTexture.active = var_RenderTexture;
            Graphics.Blit(_Texture, var_RenderTexture);
            Texture2D result = new Texture2D(_TargetWidth, _TargetHeight);
            result.ReadPixels(new Rect(0, 0, _TargetWidth, _TargetHeight), 0, 0);
            result.Apply();
            return result;
        }
    }
}
