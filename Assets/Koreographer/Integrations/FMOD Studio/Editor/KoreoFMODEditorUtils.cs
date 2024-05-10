//----------------------------------------------
//            	   Koreographer                 
//    Copyright © 2014-2021 Sonic Bloom, LLC    
//----------------------------------------------

using SonicBloom.Koreo.Players.FMODStudio;
using UnityEditor;
using UnityEngine;


namespace SonicBloom.Koreo.EditorUI.FMODStudioTools
{
    /// <summary>
    /// This class updates Koreographer's settings for the FMOD Studio integration workflow.
    /// </summary>
    [InitializeOnLoad]
    public static class KoreoFMODEditorUtils
    {
        /// <summary>
        /// This static constructor is responsible for triggering settings changes required for the
        /// FMOD Integration when the script domain is reloaded in the Unity Editor.
        /// </summary>
        static KoreoFMODEditorUtils()
        {
            EnableFMODIntegration();
        }

        /// <summary>
        /// Adjusts the settings changes required by the FMOD Integration.
        /// </summary>
        static void EnableFMODIntegration()
        {
            // Enable editing Koreography with Audio Filess instead of Unity AudioClip assets.
            KoreographyEditor.ShowAudioFileImportOption = true;
        }

        /// <summary>
        /// Creates a new KoreographySet asset and places it in the project.
        /// </summary>
        [MenuItem("Assets/Create/FMOD Koreography Set")]
        public static void CreateFMODKoreographySetAsset()
        {
            FMODKoreographySet asset = ScriptableObject.CreateInstance<FMODKoreographySet>();
            ProjectWindowUtil.CreateAsset(asset, "New " + typeof(FMODKoreographySet).Name + ".asset");
        }
    }
}
