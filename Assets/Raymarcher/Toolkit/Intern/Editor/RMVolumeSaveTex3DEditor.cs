using UnityEngine;
using UnityEditor;

using Raymarcher.Toolkit;
using Raymarcher.Utilities;

namespace Raymarcher.UEditor
{
    using static RMTextureUtils;

    [CanEditMultipleObjects]
    [CustomEditor(typeof(RMVolumeSaveTex3D))]
    public sealed class RMVolumeSaveTex3DEditor : RMEditorUtilities
    {
        private RMVolumeSaveTex3D saveTex3D;

        private void OnEnable()
        {
            saveTex3D = (RMVolumeSaveTex3D)target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            RMs();

            if (saveTex3D.TargetTex3DVolumeBox == null)
                return;
            if (saveTex3D.TargetTex3DVolumeBox.VolumeTexture == null)
                return;

            if(RMb("Save Volume Texture To Assets"))
                SaveRenderTexture3DToEditorAssets(saveTex3D.TargetTex3DVolumeBox.VolumeTexture as RenderTexture);
        }
    }
}
