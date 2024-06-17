using UnityEditor;

using Raymarcher.Toolkit;

namespace Raymarcher.UEditor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(RMVolumeRendererPD))]
    public sealed class RMVolumeRendererPDEditor : RMEditorUtilities
    {
        private RMVolumeRendererPD volumeRenderer;

        private int volumeResolution;
        private bool volumeFilteringBilinear;

        private void OnEnable()
        {
            volumeRenderer = (RMVolumeRendererPD)target;
            volumeResolution = volumeRenderer.VolumeResolution;
            volumeFilteringBilinear = volumeRenderer.VolumeFilteringBilinear;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            RMs();
            RMbv();
            volumeResolution = EditorGUILayout.IntSlider("Volume Resolution", volumeResolution, 10, 4000);
            if (RMb("Change Resolution"))
            {
                if (volumeResolution > 3000 && EditorUtility.DisplayDialog("Warning", "The volume resolution you have set is over the recommended limit (3k). Would you like to set this volume resolution?", "Yes", "No") == false)
                {
                    volumeResolution = volumeRenderer.VolumeResolution;
                    return;
                }
                volumeRenderer.SetVolumeResolution(volumeResolution);
            }
            if(RMb("Change Filtering To "+(volumeFilteringBilinear ? "Point" : "Bilinear")))
            {
                volumeFilteringBilinear = !volumeFilteringBilinear;
                volumeRenderer.SetVolumeFiltering(volumeFilteringBilinear);
            }
            RMs(5);
            if(RMb("Setup New Volume Renderer") && EditorUtility.DisplayDialog("Double Check", "Are you sure to setup a new PD volume renderer?", "Yes", "No"))
            {
                volumeRenderer.SetupVolumeRenderer();
            }
            RMbve();

            RMl("Top, Front & Right View", true);
            RMbh();
            if (volumeRenderer.RTTop != null)
                RMimage(volumeRenderer.RTTop, 128, 128);
            if (volumeRenderer.RTFront != null)
                RMimage(volumeRenderer.RTFront, 128, 128);
            if (volumeRenderer.RTRight != null)
                RMimage(volumeRenderer.RTRight, 128, 128);
            RMbhe();
        }
    }
}