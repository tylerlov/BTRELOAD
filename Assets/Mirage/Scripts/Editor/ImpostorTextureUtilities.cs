using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Mirage.Impostors.Elements
{
    public class ImpostorTextureUtilities
    {
        /// <summary>
        /// Helper method to change a Texture2D's import settings
        /// </summary>
        public static void SetTextureReadable(Texture2D texture, bool mipmapEnabled = true, bool normal = false)
        {
            if (null == texture) return;

            string assetPath = AssetDatabase.GetAssetPath(texture);
            var tImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (tImporter != null)
            {
                if (normal)
                    tImporter.textureType = TextureImporterType.NormalMap;
                else
                    tImporter.textureType = TextureImporterType.Default;
                tImporter.textureCompression = TextureImporterCompression.Compressed;
                tImporter.npotScale = TextureImporterNPOTScale.ToNearest;
                tImporter.isReadable = true;
                tImporter.mipmapEnabled = mipmapEnabled;
                tImporter.SaveAndReimport();
                AssetDatabase.ImportAsset(assetPath);
                AssetDatabase.Refresh();
            }
        }

        public static Texture2D ResizeSquared(Texture2D texture2D, int targetSide)
        {
            RenderTexture tmp = new RenderTexture(targetSide, targetSide, 24);
            RenderTexture.active = tmp;
            Graphics.Blit(texture2D, tmp);
            Texture2D result = new Texture2D(targetSide, targetSide, texture2D.format, false);
            result.ReadPixels(new Rect(0, 0, targetSide, targetSide), 0, 0);
            result.Apply();
            return result;
        }
    }
}
