// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System;
using System.Collections.Generic;

namespace GPUInstancerPro
{
    public class GPUIEditorSettings : ScriptableObject
    {
        [SerializeField]
        private string _version;
        [SerializeField]
        private uint _buildNo;
        [SerializeField]
        internal List<GPUISubModuleVersion> _subModuleVersions;

        [SerializeField]
        public bool isGenerateShaderVariantCollection = true;
        [SerializeField]
        public bool isAutoShaderConversion = true;
        [SerializeField]
        internal bool _requirePackageReload = true;
        [SerializeField]
        public bool stripDOTSInstancingVariants = true;

        [SerializeField]
        public List<string> packageImportList;

        private static GPUIEditorSettings _instance;
        public static GPUIEditorSettings Instance
        {
            get
            {
                if (_instance == null)
                    _instance = GetDefaultEditorSettings();
                return _instance;
            }
            set
            {
                _instance = value;
            }
        }

        public static GPUIEditorSettings GetDefaultEditorSettings()
        {
            string folderPath = GPUIConstants.GetDefaultUserDataPath() + GPUIConstants.PATH_EDITOR;
            GPUIEditorSettings editorSettings = AssetDatabase.LoadAssetAtPath<GPUIEditorSettings>(folderPath + GPUIConstants.FILE_EDITOR_SETTINGS + ".asset");

            if (editorSettings == null)
            {
                editorSettings = ScriptableObject.CreateInstance<GPUIEditorSettings>();
                editorSettings._requirePackageReload = true;
                editorSettings.SaveAsAsset(folderPath, GPUIConstants.FILE_EDITOR_SETTINGS + ".asset");
            }
            return editorSettings;
        }

        internal bool HasValidVersion()
        {
            return !string.IsNullOrEmpty(_version);
        }

        /// <returns>true if version changed</returns>
        internal bool SetVersion(string currentVersion)
        {
            if (string.IsNullOrEmpty(_version) || _version != currentVersion)
            {
                _version = currentVersion;
                EditorUtility.SetDirty(this);
                return true;
            }
            return false;
        }

        internal void SetBuildNo(uint buildNo)
        {
            _buildNo = buildNo;
            EditorUtility.SetDirty(this);
        }

        /// <returns>true if version changed</returns>
        internal bool SetSubModuleVersion(string packageName, string version)
        {
            if (_subModuleVersions == null)
                _subModuleVersions = new List<GPUISubModuleVersion>();
            for (int i = 0; i < _subModuleVersions.Count; i++)
            {
                var subModuleVersion = _subModuleVersions[i];
                if (subModuleVersion.packageName == packageName)
                {
                    if (subModuleVersion.version != version)
                    {
                        subModuleVersion.version = version;
                        _subModuleVersions[i] = subModuleVersion;
                        EditorUtility.SetDirty(this);
                        return true;
                    }
                    return false;
                }
            }
            _subModuleVersions.Add(new GPUISubModuleVersion() { packageName = packageName, version = version, processedVersion = "0.0.0" });
            EditorUtility.SetDirty(this);
            return true;
        }

        public void SetSubModuleProcessedVersion(string packageName, string processedVersion)
        {
            if (_subModuleVersions == null)
                _subModuleVersions = new List<GPUISubModuleVersion>();
            for (int i = 0; i < _subModuleVersions.Count; i++)
            {
                var subModuleVersion = _subModuleVersions[i];
                if (subModuleVersion.packageName == packageName)
                {
                    subModuleVersion.processedVersion = processedVersion;
                    _subModuleVersions[i] = subModuleVersion;
                    EditorUtility.SetDirty(this);
                    return;
                }
            }
        }

        public string GetVersion(string packageName = null)
        {
            if (!string.IsNullOrEmpty(packageName))
            {
                if (_subModuleVersions != null)
                {
                    foreach (var subModuleVersion in _subModuleVersions)
                    {
                        if (subModuleVersion.packageName == packageName)
                        {
                            if (!string.IsNullOrEmpty(subModuleVersion.version))
                                return subModuleVersion.version;
                            break;
                        }
                    }
                }
            }
            else if (!string.IsNullOrEmpty(_version))
                return _version;
            return "0.0.0";
        }

        internal uint GetBuildNo()
        {
            return _buildNo;
        }

        public bool IsSubModuleExists(string packageName)
        {
            if (_subModuleVersions == null)
                return false;
            for (int i = 0; i < _subModuleVersions.Count; i++)
            {
                var subModuleVersion = _subModuleVersions[i];
                if (subModuleVersion.packageName == packageName)
                    return true;
            }
            return false;
        }

        public bool IsSubModuleVersionChanged(string packageName, out string currentVersion, out string processedVersion)
        {
            currentVersion = "0.0.0";
            processedVersion = "0.0.0";
            if (_subModuleVersions == null)
                return false;
            for (int i = 0; i < _subModuleVersions.Count; i++)
            {
                var subModuleVersion = _subModuleVersions[i];
                if (subModuleVersion.packageName == packageName && subModuleVersion.version != null)
                {
                    currentVersion = subModuleVersion.version;
                    processedVersion = subModuleVersion.processedVersion;
                    return !currentVersion.Equals(processedVersion);
                }
            }
            return false;
        }

        public void RequirePackageReload()
        {
            _requirePackageReload = true;
            EditorUtility.SetDirty(this);
        }


        public void ImportPackageAtPath(string packagePath)
        {
            if (packageImportList == null)
                packageImportList = new List<string>();
            if (!packageImportList.Contains(packagePath))
            {
                packageImportList.Add(packagePath);
                EditorUtility.SetDirty(this);
            }
        }

        [Serializable]
        internal struct GPUISubModuleVersion
        {
            public string packageName;
            /// <summary>
            /// Current package version
            /// </summary>
            public string version;
            /// <summary>
            /// The latest package version that was processed (e.g. ran upgrade code)
            /// </summary>
            public string processedVersion;

            public string GetPackagesPath()
            {
                return "Packages/" + packageName + "/";
            }

            public string GetUIPath()
            {
                return GetPackagesPath() + GPUIConstants.PATH_EDITOR + GPUIEditorConstants.PATH_UI;
            }

            public string GetEditorTextPath()
            {
                return GetUIPath() + GPUIEditorConstants.EDITOR_TEXT;
            }
        }
    }

    [CustomEditor(typeof(GPUIEditorSettings))]
    public class GPUIEditorSettingsEditor : GPUIEditor
    {
        public override void DrawContentGUI(VisualElement contentElement)
        {
            DrawContentGUI(serializedObject, contentElement, _helpBoxes);
        }

        public static void DrawContentGUI(SerializedObject serializedObject, VisualElement contentElement, List<GPUIHelpBox> helpBoxes)
        {
            contentElement.Add(DrawSerializedProperty(serializedObject.FindProperty("isGenerateShaderVariantCollection"), helpBoxes));
            contentElement.Add(DrawSerializedProperty(serializedObject.FindProperty("isAutoShaderConversion"), helpBoxes));
            contentElement.Add(DrawSerializedProperty(serializedObject.FindProperty("stripDOTSInstancingVariants"), helpBoxes));
        }

        public override string GetTitleText()
        {
            return "GPUI Editor Settings";
        }

        [SettingsProvider]
        public static SettingsProvider CreateMyCustomSettingsProvider()
        {
            var provider = new SettingsProvider("Preferences/GPUInstancerPro", SettingsScope.User)
            {
                label = "GPU Instancer Pro",
                activateHandler = (searchContext, rootElement) =>
                {
                    ScrollView contentsVE = new();
                    contentsVE.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
                    rootElement.Add(contentsVE);

                    contentsVE.style.paddingLeft = 10;
                    contentsVE.style.paddingTop = 2;

                    Label title = new("GPU Instancer Pro");
                    title.style.fontSize = 20;
                    title.style.unityFontStyleAndWeight = FontStyle.Bold;
                    contentsVE.Add(title);

                    VisualElement editorSettingsVE = new();
                    editorSettingsVE.style.marginTop = 10;
                    editorSettingsVE.name = "GPUI Editor Settings";
                    //VisualElement runtimeSettingsVE = new();
                    //runtimeSettingsVE.style.marginTop = 30;
                    contentsVE.Add(editorSettingsVE);
                    //contentsVE.Add(runtimeSettingsVE);

                    GPUIEditorSettingsEditor editorSettingsEditor = (GPUIEditorSettingsEditor)CreateEditor(GPUIEditorSettings.Instance);
                    //GPUIRuntimeSettingsEditor runtimeSettingsEditor = (GPUIRuntimeSettingsEditor)CreateEditor(GPUIRuntimeSettings.Instance);

                    editorSettingsEditor.CreateInspectorUI(editorSettingsVE);
                    //runtimeSettingsEditor.CreateInspectorUI(runtimeSettingsVE);
                }
            };

            return provider;
        }
    }
}
