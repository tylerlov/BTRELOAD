using UnityEngine;

using Raymarcher.Objects.Volumes;

namespace Raymarcher.Utilities
{
    public static class RMVolumeUtils
    {
        public enum CommonVolumeResolution : int { x8, x16, x32, x64, x128, x256 };

        private static readonly int[] VOLUME_RESOLUTIONS = new int[6] { 8, 16, 32, 64, 128, 256 };

        public static CommonVolumeResolution[] GetAllCommonVolumeResolutions => (CommonVolumeResolution[])System.Enum.GetValues(typeof(CommonVolumeResolution));

        public static int GetCommonVolumeResolution(CommonVolumeResolution commonVolumeResolution)
            => VOLUME_RESOLUTIONS[(int)commonVolumeResolution];

        public static bool GetCommonVolumeResolutionFromRT3D(RenderTexture entryRT3D, out CommonVolumeResolution outCommonVolumeResolution)
        {
            outCommonVolumeResolution = CommonVolumeResolution.x8;

            if (entryRT3D.dimension != UnityEngine.Rendering.TextureDimension.Tex3D)
            {
                RMDebug.Debug(typeof(RMVolumeUtils), $"Input RT3D ({entryRT3D.name}-{entryRT3D.dimension}) is not a type of Tex3D!", true);
                return false;
            }

            string strValue = "x" + entryRT3D.width.ToString();
            if (!System.Enum.IsDefined(typeof(CommonVolumeResolution), strValue))
            {
                RMDebug.Debug(typeof(RMVolumeUtils), $"Dimensions of the input RT3D ({entryRT3D.width}x{entryRT3D.height}x{entryRT3D.volumeDepth}) couldn't be found in the common volume canvas resolutions! " +
                    $"In order to process certain features, the tex3D dimensions must be uniform and common in the Raymarcher", true);
                return false;
            }
            return System.Enum.TryParse(strValue, out outCommonVolumeResolution);
        }

        public static bool GetCommonVolumeResolutionFromTex3D(Texture3D entryTex3D, out CommonVolumeResolution outCommonVolumeResolution)
        {
            outCommonVolumeResolution = CommonVolumeResolution.x8;
            string strValue = "x" + entryTex3D.width.ToString();
            if (!System.Enum.IsDefined(typeof(CommonVolumeResolution), strValue))
            {
                RMDebug.Debug(typeof(RMVolumeUtils), $"Dimensions of the input Tex3D ({entryTex3D.width}x{entryTex3D.height}x{entryTex3D.depth}) couldn't be found in the common volume canvas resolutions! " +
                    $"In order to process certain features, the tex3D dimensions must be uniform and common in the Raymarcher", true);
                return false;
            }
            return System.Enum.TryParse(strValue, out outCommonVolumeResolution);
        }

        public static Vector3 ConvertWorldToVolumeTextureSpace(Vector3 worldPosition, RMSdf_VolumeBoxBase targetVolumeBox, int resolution)
        {
            Transform trans = targetVolumeBox.transform;
            return ConvertWorldToVolumeTextureSpace(worldPosition, resolution, trans.position, trans.rotation, trans.localScale, targetVolumeBox.volumeSize);
        }

        public static Vector3 ConvertWorldToVolumeTextureSpace(Vector3 worldPosition, int resolution, Vector3 volumeWorldPosition, Quaternion volumeWorldRotation, Vector3 volumeLocalScale, float volumeSize)
        {
            Vector3 volumePos = Quaternion.Inverse(volumeWorldRotation) * (worldPosition - volumeWorldPosition);
            Vector3 volumeScale = volumeLocalScale * volumeSize;
            Vector3 volumeScaleShifted = volumeScale + volumeScale;
            Vector3 volumeLocalPos = volumePos + volumeScale;

            Vector3 uvCoords = Vector3.zero;

            uvCoords.x = Mathf.Lerp(0f, resolution, volumeLocalPos.x / volumeScaleShifted.x);
            uvCoords.y = Mathf.Lerp(0f, resolution, volumeLocalPos.y / volumeScaleShifted.y);
            uvCoords.z = Mathf.Lerp(0f, resolution, volumeLocalPos.z / volumeScaleShifted.z);

            return uvCoords;
        }

        public static Vector3 ConvertVolumeTextureSpaceToWorld(Vector3 volumeTextureCoordinates, int resolution, Vector3 volumeWorldPosition, Quaternion volumeWorldRotation, Vector3 volumeLocalScale, float volumeSize)
        {
            Vector3 volumeScale = volumeLocalScale * volumeSize;
            Vector3 volumeLocalPos = Vector3.zero;

            volumeLocalPos.x = Mathf.Lerp(-volumeScale.x, volumeScale.x, volumeTextureCoordinates.x / resolution);
            volumeLocalPos.y = Mathf.Lerp(-volumeScale.y, volumeScale.y, volumeTextureCoordinates.y / resolution);
            volumeLocalPos.z = Mathf.Lerp(-volumeScale.z, volumeScale.z, volumeTextureCoordinates.z / resolution);

            return volumeWorldRotation * volumeLocalPos + volumeWorldPosition;
        }
    }
}