using UnityEngine;
using UnityEditor;

using Raymarcher.Toolkit;

using Unity.EditorCoroutines.Editor;

using Raymarcher.Utilities;

namespace Raymarcher.UEditor
{
    using static RMTextureUtils;

    [CanEditMultipleObjects]
    [CustomEditor(typeof(RMVolumeMeshPrinter))]
    public sealed class RMVolumeMeshPrinterEditor : RMEditorUtilities
    {
        private RMVolumeMeshPrinter modelPrinter;
        private EditorCoroutine printingCoroutine;
        private RMVolumeMeshPrinter.PrintMode previousPrintMode;

        private bool print = false;

        private void OnEnable()
        {
            modelPrinter = (RMVolumeMeshPrinter)target;
            previousPrintMode = modelPrinter.printMode;
        }

        private void OnDisable()
        {
            if (modelPrinter.IsPrinting)
            {
                if (EditorUtility.DisplayDialog("Warning", "The printer is currently in progress. Would you like to cancel the printing or continue?", "Continue", "Cancel") == false)
                    Release();
            }
            else
                Release();
        }

        private void Release()
        {
            modelPrinter.StopAndRelease();

            if (printingCoroutine != null)
            {
                EditorCoroutineUtility.StopCoroutine(printingCoroutine);
                printingCoroutine = null;
            }
        }

        private void ReleaseResources()
        {
            if (modelPrinter.printMode == RMVolumeMeshPrinter.PrintMode.Progressive && modelPrinter.VolumeVoxelPainter != null)
                modelPrinter.VolumeVoxelPainter.Dispose();
            else
                modelPrinter.SetWorkingCanvas();
            if (modelPrinter.initialVolumeCanvas3D != null && modelPrinter.TargetTex3DVolumeBox != null)
                modelPrinter.TargetTex3DVolumeBox.VolumeTexture = modelPrinter.initialVolumeCanvas3D;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            RMs();

            if(previousPrintMode != modelPrinter.printMode)
            {
                Release();
                ReleaseResources();
                previousPrintMode = modelPrinter.printMode;
                return;
            }

            if (RMb("Print Mesh Models To Tex3D Volume"))
                print = !print;

            if (print)
            {
                RMbh();
                if (RMb("Print As New", tooltip:"The target volume Texture3D will be cleared and initialized as new"))
                {
                    if (Application.isPlaying)
                    {
                        EditorUtility.DisplayDialog("Warning", "This feature is available only in Unity editor, not in the play mode", "OK");
                        return;
                    }
                    EditorUtility.DisplayDialog("Warning", "Please do not deselect the current gameObject with Model Printer component. Deselecting will immediatelly stop the process", "OK");
                    modelPrinter.PrintTargetMeshesToTex3DVolume(false);
                    printingCoroutine = EditorCoroutineUtility.StartCoroutine(modelPrinter.IEPrintModel(false), this);
                    print = false;
                }
                bool canPrintAdditive = (modelPrinter.printMode == RMVolumeMeshPrinter.PrintMode.Progressive && (modelPrinter.VolumeVoxelPainter != null && modelPrinter.VolumeVoxelPainter.IsInitialized))
                    || modelPrinter.WorkingVolumeCanvas != null;
                if (canPrintAdditive && RMb("Print As Additive", tooltip: "The target volume Texture3D will remain and the voxel painter will additively paint"))
                {
                    if (Application.isPlaying)
                    {
                        EditorUtility.DisplayDialog("Warning", "This feature is available only in Unity editor, not in the play mode", "OK");
                        return;
                    }
                    EditorUtility.DisplayDialog("Warning", "Please do not deselect the current gameObject with Model Printer component. Deselecting will immediatelly stop the process", "OK");
                    modelPrinter.PrintTargetMeshesToTex3DVolume(true);
                    printingCoroutine = EditorCoroutineUtility.StartCoroutine(modelPrinter.IEPrintModel(true), this);
                    print = false;
                }
                RMbhe();
            }

            if (modelPrinter.IsPrinting)
            {
                EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(), modelPrinter.PrintingProgress, "Print Progress: " + (modelPrinter.PrintingProgress * 100f).ToString("0.0") + "%");
                Repaint();
                if(RMb("Cancel Printing", 150))
                {
                    Release();
                    return;
                }
            }

            if(modelPrinter.printMode == RMVolumeMeshPrinter.PrintMode.Progressive)
            {
                if (modelPrinter.VolumeVoxelPainter == null || !modelPrinter.VolumeVoxelPainter.IsInitialized)
                    return;
            }
            else
            {
                if (modelPrinter.WorkingVolumeCanvas == null)
                    return;

            }

            RMs();
            if (RMb("Save Modified 3D Canvas To Assets"))
                SaveToAssets();
            if (RMb("Release Modified 3D Canvas"))
            {
                Release();
                ReleaseResources();
            }
        }

        private void SaveToAssets()
        {
            if (EditorApplication.isPaused)
            {
                EditorUtility.DisplayDialog("Error!", "Application cannot be paused!", "OK");
                return;
            }

            ConvertRenderTexture3DToTexture3D(modelPrinter.printMode == RMVolumeMeshPrinter.PrintMode.Progressive
                ? modelPrinter.VolumeVoxelPainter.WorkingVolumeCanvas3D
                : modelPrinter.WorkingVolumeCanvas, ConvertCallback);
        }

        private void ConvertCallback(Texture3D entry)
        {
            string path = EditorUtility.SaveFilePanelInProject("Save Canvas Tex 3D as .Asset", entry.name, "asset", "");
            if (string.IsNullOrEmpty(path))
            {
                EditorUtility.DisplayDialog("Unsuccessful!", "The save was unsuccessful. Path was empty!", "OK");
                return;
            }
            AssetDatabase.CreateAsset(entry, path);
            AssetDatabase.SaveAssetIfDirty(entry);
        }
    }
}