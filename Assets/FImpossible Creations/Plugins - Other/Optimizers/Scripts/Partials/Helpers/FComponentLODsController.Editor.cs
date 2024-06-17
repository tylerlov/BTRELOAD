#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

using UnityEditor;

namespace FIMSpace.FOptimizing
{
    /// <summary>
    /// FM: Class which is helping holding settings and references for one optimized component.
    /// > Containing reference to target optimized component from scene/prefab
    /// > Handling applying changes to target optimized component in playmode
    /// > Handling drawing editor windows elements for optimization settings etc.
    /// > Containing ref to LOD settings of target component
    /// </summary>
    public partial class FComponentLODsController
    {
        #region Initialization

        /// <summary>
        /// Taking care of creating LOD Set, LOD Parameters with automatic settings and syncing with prefab if using.
        /// </summary>
        public void GenerateLODParameters()
        {
            if (NeedToGenerateNewLODSet(optimizer.LODLevels)) // Generating new LOD params
                GenerateNewLODSet();

            CheckAndGenerateLODParameters();

            if (!RootReference)
            {
                Debug.LogError("[OPTIMIZERS] No Root Reference! Try adding Optimizers Manager again!");
                return;
            }

            RefreshLODParametersSettings();
        }


        /// <summary>
        /// Checking if LOD parameters need to be generated for needed LOD levels count.
        /// If count is invalid to needed one, new LOD parameters are generated.
        /// </summary>
        private void CheckAndGenerateLODParameters()
        {
            if (RootReference == null)
            {
                Debug.LogError("[OPTIMIZERS] CRITICAL ERROR: There is no root reference in Optimizer's LOD Controller!(" + optimizer + ") " + "Try adding Optimizers Manager again to the scene or import newest version from the Asset Store!");
            }

            if (LODSet != null)
            {
                if (LODSet.LevelOfDetailSets.Count == optimizer.LODLevels + 2)
                {
                    // Checking if there are null references inside LOD Set List
                    for (int i = 0; i < LODSet.LevelOfDetailSets.Count; i++)
                        if (LODSet.LevelOfDetailSets[i] == null)
                        {
                            FOptimizers_LODTransport.RemoveLODControllerSubAssets(this, true);
                            break;
                        }
                }


                if (RootReference)
                {
                    // Checking again count in case if it was cleared in previous lines of code
                    if (LODSet.LevelOfDetailSets.Count != optimizer.LODLevels + 2)
                    {
                        for (int i = 0; i < optimizer.LODLevels + 2; i++)
                        {
                            FLOD_Base newParam = RootReference.GetLODInstance();
                            newParam.Version = Version;
                            LODSet.LevelOfDetailSets.Add(newParam);
                        }
                    }
                }


                if (UsingShared) // Saving sub LOD settings inside LODSet - shared settings asset
                    FOptimizers_LODTransport.SaveLODSetInAsset(this, LODSet);
                else
                {
                    UnityEngine.Object prefab = FOptimizers_LODTransport.GetPrefab(optimizer.gameObject);

                    if (prefab) // Saving sub LOD settings inside prefab
                    {
                        FOptimizers_LODTransport.SaveLODSetInAsset(this, prefab);
                    }
                    else // Saving sub LOD settings inside LODSet - scene object without prefab
                    {
                        FOptimizers_LODTransport.SaveLODSetInAsset(this, LODSet);
                    }
                }

                RefreshOptimizerLODCount();
            }
            else
            {
                Debug.LogWarning("[OPTIMIZERS] No LODSet!");
            }
        }

        /// <summary>
        /// Generating LOD Set file which will contain parameters for each LOD Level
        /// </summary>
        private void GenerateNewLODSet()
        {
            FOptimizer_LODSettings lodSet = LODSet;

            if (LODSet != null)
            {
                FOptimizers_LODTransport.RemoveLODControllerSubAssets(this, true);
            }
            else
            {
                lodSet = ScriptableObject.CreateInstance<FOptimizer_LODSettings>();
            }

            if (UsingShared)
                SetSharedLODSettings(lodSet);
            else
                SetUniqueLODSettings(lodSet);
        }

        /// <summary>
        /// Checking if LOD controller should generate new LOD Settings
        /// </summary>
        private bool NeedToGenerateNewLODSet(int targetCount)
        {
            bool need = false;

            if (LODSet == null)
            {
                if (uniqueLODSet == null && sharedLODSet == null)
                {
                    need = true;
                    GenerateNewLODSet();
                }
                else
                {
                    if (uniqueLODSet != null) LODSet = uniqueLODSet; else LODSet = sharedLODSet;
                }
            }
            else
            {
                if (LODSet.LevelOfDetailSets == null)
                {
                    need = true;
                    LODSet.LevelOfDetailSets = new List<FLOD_Base>();
                }
                else
                if (LODSet.LevelOfDetailSets.Count == 0) need = true;
                else
                {
                    if (LODSet.LevelOfDetailSets[0] != null)
                    {/*Version = LODSet.LevelOfDetailSets[0].Version;*/}
                    else
                        need = true;

                    if (targetCount != LODSet.LevelOfDetailSets.Count + 2)
                    {
                        need = true;
                    }
                    else if (targetCount != 0)
                    {
                        bool nulls = false;
                        // Checking if there are null references inside LOD Set List
                        for (int i = 0; i < LODSet.LevelOfDetailSets.Count; i++)
                            if (LODSet.LevelOfDetailSets[i] == true)
                            {
                                nulls = true;
                                FOptimizers_LODTransport.RemoveLODControllerSubAssets(this, true);
                                break;
                            }

                        if (nulls)
                        {
                            need = true;
                        }
                    }
                }
            }

            return need;
        }


        /// <summary>
        /// Applying auto settings for all optimizer parameters inside LOD set
        /// </summary>
        public void RefreshLODParametersSettings(float lowerer = 1f)
        {
            if (RootReference == null)
                Debug.LogError("[OPTIMIZERS] CRITICAL ERROR: There is no root reference in Optimizer's LOD Controller! (" + optimizer + ") " + "Try adding Optimizers Manager again to the scene or import newest version from the Asset Store!");

            string nameShort = optimizer.name;
            nameShort = nameShort.Replace("PR_", "");
            nameShort = nameShort.Replace("PR.", "");
            nameShort = nameShort.Substring(0, Mathf.Min(5, nameShort.Length)) + "[";

            string type = RootReference.GetType().ToString();
            type = type.Replace("FIMSpace.FOptimizing.", "");
            type = type.Replace("LOD_", "");
            type = type.Replace("FLOD_", "");

            type = type.Substring(0, Mathf.Min(6, type.Length)) + "]";

            string prefix = nameShort + type;

            // Nearest LOD Params
            FLOD_Base nearestLOD = LODSet.LevelOfDetailSets[0];
            nearestLOD.SetSettingsAsForNearest(Component);
            nearestLOD.QualityLowerer = lowerer;
            nearestLOD.name = prefix + "Nearest";

            // All LODs between - NEAREST ...LODs... and CULLED, HIDDEN
            for (int i = 0; i < optimizer.LODLevels - 1; i++)
            {
                FLOD_Base lod = LODSet.LevelOfDetailSets[i + 1];
                lod.QualityLowerer = lowerer;
                lod.SetAutoSettingsAsForLODLevel(i, optimizer.LODLevels, Component);
                lod.name = prefix + "LOD" + (i + 1);
            }

            FLOD_Base culledLOD = LODSet.LevelOfDetailSets[LODSet.LevelOfDetailSets.Count - 2];
            culledLOD.QualityLowerer = lowerer;
            culledLOD.SetSettingsAsForCulled(Component);
            culledLOD.name = prefix + "Culled";

            FLOD_Base hiddenLOD = LODSet.LevelOfDetailSets[LODSet.LevelOfDetailSets.Count - 1];
            hiddenLOD.QualityLowerer = lowerer;
            hiddenLOD.SetAutoSettingsAsForLODLevel(optimizer.LODLevels - 2, optimizer.LODLevels, Component);
            hiddenLOD.SetSettingsAsForHidden(Component);
            hiddenLOD.name = prefix + "Hidden";
        }


        /// <summary>
        /// Applying quality lowerer variable to LOD set parameters
        /// </summary>
        public void AutoQualityLowerer(float lowerer = 1f)
        {
            LODSet.LevelOfDetailSets[0].QualityLowerer = lowerer;
            if (!RootReference) return;

            FLOD_Base lod;
            for (int i = 1; i < optimizer.LODLevels; i++)
            {
                lod = LODSet.LevelOfDetailSets[i];
                lod.QualityLowerer = lowerer;
                lod.SetAutoSettingsAsForLODLevel(i - 1, optimizer.LODLevels, Component);
            }

            lod = LODSet.LevelOfDetailSets[LODSet.LevelOfDetailSets.Count - 1];
            lod.QualityLowerer = lowerer;
            lod.SetSettingsAsForHidden(Component);
            lod.SetAutoSettingsAsForLODLevel(optimizer.LODLevels - 1, optimizer.LODLevels + 1, Component);
        }


        #endregion


        #region Main Settings / Scriptables Related Stuff


        /// <summary>
        /// Setting shared LOD settings reference to be used for optimized component settings
        /// </summary>
        public void SetSharedLODSettings(FOptimizer_LODSettings lodSettings)
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("[OPTIMIZERS] No allowed in playmode!");
                return;
            }

            if (lodSettings == null) // lodSettings null means we remove shared - Creating new unique LOD set with the same settings
            {
                lodSettings = ScriptableObject.CreateInstance<FOptimizer_LODSettings>();

                for (int i = 0; i < LODSet.LevelOfDetailSets.Count; i++)
                    lodSettings.LevelOfDetailSets.Add(LODSet.LevelOfDetailSets[i].CreateNewCopy());

                bool shar = UsingShared;
                SetUniqueLODSettings(lodSettings);

                // (nulling shared settings)
                if (shar) FOptimizers_LODTransport.SaveLODSetInAsset(this, FOptimizers_LODTransport.GetPrefab(optimizer.gameObject));
            }
            else
            {
                if (uniqueLODSet != null)
                {
                    if (uniqueLODSet != lodSettings)
                        FOptimizers_LODTransport.RemoveLODControllerSubAssets(this, false);
                }

                uniqueLODSet = null;
                sharedLODSet = lodSettings;
                LODSet = lodSettings;
                UsingShared = true;
            }
        }


        /// <summary>
        /// Syncing optimizer component to LOD Set
        /// </summary>
        private void RefreshOptimizerLODCount()
        {
            if (LODSet != null)
            {
                if (LODSet.LevelOfDetailSets.Count != 0) optimizer.LODLevels = LODSet.LevelOfDetailSets.Count - 2;
            }
        }


        /// <summary>
        /// Checking if lod set is right Type
        /// </summary>
        public static bool CheckLODSetCorrectness(FOptimizer_LODSettings lodSet, FLOD_Base referenceLOD)
        {
            if (lodSet.LevelOfDetailSets.Count == 0)
            {
                Debug.LogError("[OPTIMIZERS] LOD Set is empty");
                return false;
            }

            if (lodSet.LevelOfDetailSets[0] == null)
            {
                Debug.LogError("[OPTIMIZERS] LOD Set element is null");
                return false;
            }

            Type setType = lodSet.LevelOfDetailSets[0].GetType();

            if (setType == referenceLOD.GetType())
            {
                return true;
            }
            else
            {
                Debug.LogError("[OPTIMIZERS] Type of LODSet is uncorrect! (<color=red><b>" + setType.ToString() + "</b></color>) You need <color=blue><b>" + referenceLOD.GetType().ToString() + "</b></color> type");
                return false;
            }
        }

        /// <summary>
        /// Setting unique/individual LOD Settings reference to be used only for this one Game Object
        /// </summary>
        public void SetUniqueLODSettings(FOptimizer_LODSettings lodSettings)
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("[OPTIMIZERS] No allowed in playmode!");
                return;
            }

            if (lodSettings == null)
            {
                Debug.LogError("[OPTIMIZERS] Target lod settings cannot be null!");
                return;
            }

            sharedLODSet = null;
            uniqueLODSet = lodSettings;

            LODSet = lodSettings;
            LODSet.name = "LOD Set-" + optimizer.name;
            UsingShared = false;
        }


        #endregion


        #region Editor Inspector Window Drawing Stuff

        /// <summary>
        /// Drawing settings to tweak on LODs for the inspector window
        /// </summary>
        public void Editor_DrawValues(int selectedLOD = 0)
        {

#if UNITY_EDITOR

            if (optimizer == null)
            {
                EditorGUILayout.HelpBox("Optimizer Reference Lost! You probably removed component to optimize from main object or something similar! You have to remove than add optimizer again or remove this component in 'To Optimize' list (" + Component + ")", MessageType.Error);
                return;
            }
            else
            if (LODSet == null)
            {
                EditorGUILayout.HelpBox("LOD Set Lost!", MessageType.Warning);
                if (GUILayout.Button(new GUIContent("Retry"), new GUILayoutOption[2] { GUILayout.Width(50), GUILayout.Height(15) }))
                {
                    optimizer.RemoveAllComponentsFromToOptimize();
                    optimizer.AssignComponentsToBeOptimizedFromAllChildren(optimizer.gameObject);
                    if (optimizer.ToOptimize.Count == 0) optimizer.AssignComponentsToBeOptimizedFromAllChildren(optimizer.gameObject, true);
                }

                return;
            }
            else
            if (!RootReference)
            {
                EditorGUILayout.HelpBox("Root Reference Lost!", MessageType.Warning);
                if (GUILayout.Button(new GUIContent("Retry"), new GUILayoutOption[2] { GUILayout.Width(50), GUILayout.Height(15) }))
                {
                    optimizer.RemoveAllComponentsFromToOptimize();
                    optimizer.AssignComponentsToBeOptimizedFromAllChildren(optimizer.gameObject);
                    if (optimizer.ToOptimize.Count == 0) optimizer.AssignComponentsToBeOptimizedFromAllChildren(optimizer.gameObject, true);
                }

                return;
            }
            else if (ReferenceLOD == null)
            {
                EditorGUILayout.HelpBox("Reference LOD Lost!", MessageType.Warning);

                if (GUILayout.Button(new GUIContent("Retry"), new GUILayoutOption[2] { GUILayout.Width(50), GUILayout.Height(15) }))
                {
                    optimizer.RemoveAllComponentsFromToOptimize();
                    optimizer.AssignComponentsToBeOptimizedFromAllChildren(optimizer.gameObject);
                    if (optimizer.ToOptimize.Count == 0) optimizer.AssignComponentsToBeOptimizedFromAllChildren(optimizer.gameObject, true);
                }

                return;
            }

            if (selectedLOD < 0 || selectedLOD > LODSet.LevelOfDetailSets.Count)
            {
                Debug.Log("[OPTIMIZERS DEBUG] selected LOD = " + selectedLOD + " not drawing LODs for " + RootReference.GetType() + ". You can go to 'To Optimize' tab and add components to optimize once again.");
                return;
            }

            string headerText = "Draw Properties";
            if (editorHeader != "")
            {
                if (Component) headerText = Component.name + " (" + editorHeader + ")";
                else headerText = editorHeader;
            }

            //GUI.color = new Color(0.82f, 1f, 0.925f); //FOptimizers_EditorHelperMethods.GetLODColor(selectedLOD, optimizer.LODLevels, 1.75f, 0.4f, 1f, 1.35f) * new Color(1f, 1f, 1f, 0.9f);
            Color preCol = GUI.color;

            if (optimizer.ToOptimize.IndexOf(this) % 2 == 0)
                EditorGUILayout.BeginVertical();
            else
                EditorGUILayout.BeginVertical();

            GUI.color = preCol * new Color(1f, 1f, 1f, 0.825f);

            EditorGUI.indentLevel++;

            FLOD_Base refLod = ReferenceLOD;

            if (refLod != null)
                if (refLod.DrawLowererSlider)
                {
                    EditorGUILayout.BeginHorizontal();
                    drawProperties = EditorGUILayout.Foldout(drawProperties, new GUIContent(headerText, "Level Of Detail settings for the optimizer's component: " + Component.name), true, new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold });

                    GUILayout.FlexibleSpace();
                    float preLow = refLod.QualityLowerer;

                    SerializedObject serializedLODSet = new SerializedObject(refLod);

                    EditorGUIUtility.labelWidth = 65;
                    float newLowerer = EditorGUILayout.FloatField(new GUIContent("Quality", "Changing value of this slider, will change quality of all LODs for this component"), refLod.QualityLowerer);
                    refLod.QualityLowerer = newLowerer;
                    EditorGUIUtility.labelWidth = 0;

                    if (newLowerer > 1f) newLowerer = 1f;
                    if (newLowerer < 0.1f) newLowerer = 0.1f;

                    if (preLow != refLod.QualityLowerer)
                        AutoQualityLowerer(newLowerer);

                    serializedLODSet.ApplyModifiedProperties();

                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    if (Component == null)
                    {
                        CheckComponentsCorrectness();
                        GUI.color = preCol;
                        return;
                    }
                    else
                    {
                        EditorGUILayout.BeginHorizontal();
                        drawProperties = EditorGUILayout.Foldout(drawProperties, new GUIContent(headerText, "Level Of Detail settings for the optimizer's component: " + Component.name), true, new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold });

                        FLOD_MonoBehaviour monoLod = ReferenceLOD as FLOD_MonoBehaviour;
                        if (monoLod != null)
                        {
                            GUILayout.FlexibleSpace();

                            GUIContent buttonText;
                            if (monoLod.Version == 1 || Version == 1) buttonText = new GUIContent("Advanced", "Trying to collect monobehaviour's inspector variables to able to modify them with LOD levels");
                            else buttonText = new GUIContent("Simplify", "Making LOD only for enabling / disabling component in LOD levels");

                            if (GUILayout.Button(buttonText, new GUILayoutOption[2] { GUILayout.Width(90), GUILayout.Height(18) }))
                            {
                                if (monoLod.Version == 1)
                                    monoLod.Version = 0;
                                else
                                    monoLod.Version = 1;

                                Version = monoLod.Version;

                                if (monoLod.Version == 1) monoLod.Parameters.Clear();
                                FOptimizers_LODTransport.RemoveLODControllerSubAssets(this, true);
                                GenerateLODParameters();
                            }
                        }

                        EditorGUILayout.EndHorizontal();
                    }
                }

            EditorGUI.indentLevel--;

            GUI.color = preCol;

            if (drawProperties)
            {
                FLOD_Base lod;

                if (selectedLOD == LODSet.LevelOfDetailSets.Count)
                    lod = GetHiddenLOD();
                else
                    lod = LODSet.LevelOfDetailSets[selectedLOD];

                if (lod != null)
                {
                    if (optimizer.DrawSharedSettingsOptions)
                    {
                        var serializedOptimizer = new SerializedObject(optimizer);
                        //var p1 = serializedOptimizer.FindProperty("ToOptimize");
                        //var p2 = p1.GetArrayElementAtIndex(optimizer.ToOptimize.IndexOf(this));
                        //var p3 = p2.FindPropertyRelative("sharedLODSet");

                        EditorGUILayout.BeginHorizontal();
                        FOptimizer_LODSettings pre = sharedLODSet;
                        FOptimizer_LODSettings tempShared = (FOptimizer_LODSettings)EditorGUILayout.ObjectField("Shared LOD Set", sharedLODSet, typeof(FOptimizer_LODSettings), false);

                        bool button = false;

                        if (!sharedLODSet)
                        {
                            if (GUILayout.Button(new GUIContent("New", "Generate new LOD set file basing on current settings in optimizer component."), new GUILayoutOption[2] { GUILayout.Width(40), GUILayout.Height(15) }))
                            {
                                sharedLODSet = SaveLODSet();
                                button = true;
#if UNITY_EDITOR
                                AssetDatabase.SaveAssets();
#endif
                            }
                        }
                        else
                        {
                            if (GUILayout.Button(new GUIContent("X", "Remove shared LOD so settings for this component will be unique"), new GUILayoutOption[2] { GUILayout.Width(20), GUILayout.Height(15) })) { sharedLODSet = null; button = true; }
                            CheckForMultiAssigning();
                        }

                        // If used button for "New" or "X"
                        if (button)
                        {
                            if (sharedLODSet != null)
                            {
                                if (LODSet != sharedLODSet)
                                {
                                    if (CheckLODSetCorrectness(sharedLODSet, ReferenceLOD))
                                        SetSharedLODSettings(sharedLODSet);
                                    else
                                        SetSharedLODSettings(null);
                                }
                            }
                            else
                                if (sharedLODSet == null)
                                if (uniqueLODSet == null)
                                    SetSharedLODSettings(null);
                        }
                        else // when used object field
                        {
                            if (tempShared != null)
                            {
                                if (pre != tempShared)
                                {
                                    if (CheckLODSetCorrectness(tempShared, ReferenceLOD))
                                    {
                                        optimizer.LODLevels = tempShared.LevelOfDetailSets.Count - 2;
                                        sharedLODSet = tempShared;
                                        SetSharedLODSettings(sharedLODSet);
                                        serializedOptimizer.ApplyModifiedProperties();
                                        new SerializedObject(sharedLODSet).ApplyModifiedProperties();
                                    }
                                    else
                                    {
                                        tempShared = pre; // No change
                                    }
                                }
                            }
                            else
                            {
                                if (sharedLODSet == null)
                                    if (uniqueLODSet == null)
                                    {
                                        SetSharedLODSettings(null);
                                    }
                            }
                        }

                        EditorGUILayout.EndHorizontal();

                        serializedOptimizer.ApplyModifiedProperties();
                    }

                    if (selectedLOD == 0 && lockFirstLOD && !optimizer.UnlockFirstLOD) GUI.enabled = false; else GUI.enabled = true;


                    if (lod.CustomEditor == false)
                    {
                        try
                        {
                            SerializedObject s = new SerializedObject(lod);

                            //FEditor.FEditor_StylesIn.DrawUILine(new Color(0.5f, 0.5f, 0.5f, 0.5f), 2, 4);
                            if (lod.DrawDisableOption)
                            {
                                if (selectedLOD > 0 || optimizer.UnlockFirstLOD)
                                {
                                    EditorGUILayout.BeginHorizontal();
                                    EditorGUILayout.PropertyField(s.FindProperty("Disable"));

                                    // Drawing option to draw disable / enable all button in first component settings
                                    if (optimizer.ToOptimize[0] == this)
                                        if (optimizer.ToOptimize.Count > 3)
                                            if (lod.Disable)
                                            {
                                                if (GUILayout.Button(new GUIContent("Set Enabled All", "Setting all components in optimizer to be enabled in this LOD level"), EditorStyles.miniButton, GUILayout.Width(110)))
                                                    for (int i = 0; i < optimizer.ToOptimize.Count; i++) { optimizer.ToOptimize[i].LODSet.LevelOfDetailSets[selectedLOD].Disable = false; }
                                            }
                                            else
                                            {
                                                if (GUILayout.Button(new GUIContent("Set Disabled All", "Setting all components in optimizer to be disabled in this LOD level"), EditorStyles.miniButton, GUILayout.Width(110)))
                                                    for (int i = 0; i < optimizer.ToOptimize.Count; i++) { optimizer.ToOptimize[i].LODSet.LevelOfDetailSets[selectedLOD].Disable = true; }
                                            }

                                    EditorGUILayout.EndHorizontal();
                                }
                            }

                            if (selectedLOD == 0)
                            {
                                bool pre = GUI.enabled;
                                GUI.enabled = true;
                                lod.DrawTogglers(this);
                                GUI.enabled = pre;
                            }

                            var prop = s.GetIterator();
                            int safeLimit = 0;
                            prop.NextVisible(true); // ignoring "Script" field
                            prop.NextVisible(true); // ignoring "Deactivate" field

                            while (prop.NextVisible(true))
                            {
                                EditorGUILayout.PropertyField(prop);
                                if (++safeLimit > 1000) break;
                            }

                            s.ApplyModifiedProperties();
                            s.Dispose();
                        }
                        catch (Exception e)
                        {
                            Debug.LogWarning("[Optimizers] Probably something went wrong. " + e.ToString());
                        }
                    }
                    else
                    {
                        //FLOD_MonoBehaviour mono = lod as FLOD_MonoBehaviour;
                        //if (mono) mono.SetCustomComponentInfo(SourceBehaviour, this as FLODsController_MonoBehaviour);

                        try
                        {
                            SerializedObject s = new SerializedObject(lod);
                            //FEditor.FEditor_StylesIn.DrawUILine(new Color(0.5f, 0.5f, 0.5f, 0.5f), 2, 4);

                            if (ReferenceLOD.DrawDisableOption)
                                if (selectedLOD > 0 || optimizer.UnlockFirstLOD)
                                {
                                    EditorGUILayout.BeginHorizontal();
                                    EditorGUILayout.PropertyField(s.FindProperty("Disable"));

                                    // Drawing option to draw disable / enable all button in first component settings
                                    if (optimizer.ToOptimize[0] == this)
                                        if (optimizer.ToOptimize.Count > 3)
                                            if (lod.Disable)
                                            {
                                                if (GUILayout.Button(new GUIContent("Set Enabled All", "Setting all components in optimizer to be enabled in this LOD level"), EditorStyles.miniButton, GUILayout.Width(110)))
                                                    for (int i = 0; i < optimizer.ToOptimize.Count; i++) { optimizer.ToOptimize[i].LODSet.LevelOfDetailSets[selectedLOD].Disable = false; }
                                            }
                                            else
                                            {
                                                if (GUILayout.Button(new GUIContent("Set Disabled All", "Setting all components in optimizer to be disabled in this LOD level"), EditorStyles.miniButton, GUILayout.Width(110)))
                                                    for (int i = 0; i < optimizer.ToOptimize.Count; i++) { optimizer.ToOptimize[i].LODSet.LevelOfDetailSets[selectedLOD].Disable = true; }
                                            }


                                    EditorGUILayout.EndHorizontal();
                                }

                            if (selectedLOD == 0)
                            {
                                bool pre = GUI.enabled;
                                GUI.enabled = true;
                                lod.DrawTogglers(this);
                                GUI.enabled = pre;
                            }

                            lod.EditorWindow();

                            s.ApplyModifiedProperties();
                            s.Dispose();
                        }
                        catch (Exception e)
                        {
                            Debug.LogWarning("[Optimizers] Probably something went wrong. " + e.ToString());
                        }
                    }
                }
                else
                {
                    Debug.LogWarning("[Optimizers Editor] LOD is null");
                }

                if (selectedLOD == 0 && lockFirstLOD) GUI.enabled = true;
            }

            GUI.color = preCol;

            EditorGUILayout.EndVertical();
#endif



        }


        public void CheckForMultiAssigning()
        {
            bool can = false;
            Type type = Component.GetType();

            for (int i = 0; i < optimizer.ToOptimize.Count; i++)
            {
                if (optimizer.ToOptimize[i] == this) continue;
                if (optimizer.ToOptimize[i].Component.GetType() == type)
                {
                    if (optimizer.ToOptimize[i].sharedLODSet != sharedLODSet)
                    {
                        can = true;
                        break;
                    }
                }
            }

            if (can)
                if (GUILayout.Button(new GUIContent("Set All", "Setting this LOD settings to all components of the same type"), new GUILayoutOption[2] { GUILayout.Width(50), GUILayout.Height(15) }))
                {
                    for (int i = 0; i < optimizer.ToOptimize.Count; i++)
                    {
                        if (optimizer.ToOptimize[i] == this) continue;
                        if (optimizer.ToOptimize[i].Component.GetType() == type)
                        {
                            if (optimizer.ToOptimize[i].sharedLODSet != sharedLODSet)
                                optimizer.ToOptimize[i].SetSharedLODSettings(sharedLODSet);
                        }
                    }
                }
        }


        public void CheckComponentsCorrectness()
        {
            if (Component == null)
                optimizer.ToOptimize.Remove(this);
        }


        public void Editor_HideProperties(bool hide = true)
        {
#if UNITY_EDITOR
            drawProperties = !hide;
#endif
        }

        #region Saving LOD Sets

        public static string pathTo = "";
        public FOptimizer_LODSettings SaveLODSet()
        {
            FOptimizer_LODSettings newLODSet = null;

            string type = "";
            string nameShort = "";

            if (RootReference != null)
            {
                if (Component == null)
                {
                    nameShort = optimizer.name;
                    type = RootReference.GetType().ToString();
                    type = type.Replace("FIMSpace.FOptimizing.", "");
                    type = type.Replace("LOD_", "");
                    type = type.Replace("FLOD_", "");
                    int dotIndex = type.LastIndexOf('.') + 1;
                    type = type.Substring(dotIndex, type.Length - dotIndex);
                }
                else
                {
                    nameShort = Component.name;
                    type = Component.GetType().ToString();
                    int dotIndex = type.LastIndexOf('.') + 1;
                    type = type.Substring(dotIndex, type.Length - dotIndex);
                }

                nameShort = nameShort.Replace("PR_", "");
                nameShort = nameShort.Replace("PR.", "");
                nameShort = nameShort.Substring(0, Mathf.Min(11, nameShort.Length));
            }


#if UNITY_EDITOR

            if (pathTo == "")
            {
                if (PlayerPrefs.HasKey("FOPT_LastLSDir"))
                {
                    pathTo = PlayerPrefs.GetString("FOPT_LastLSDir");
                    if (!System.IO.Directory.Exists(pathTo)) pathTo = Application.dataPath;
                }
                else pathTo = Application.dataPath;
            }

            if (!pathTo.Contains(Application.dataPath))
            {
                pathTo = Application.dataPath;
            }

            string path = EditorUtility.SaveFilePanelInProject("Generate LOD Type Settings File (Can be overwritten)", "LS_" + type + "-" + nameShort + " (" + LODLevelsCount + " LODs)", "asset", "Enter name of file which will contain settings for L.O.D. of different components", pathTo);

            if (!pathTo.Contains(Application.dataPath))
            {
                pathTo = Application.dataPath;
            }

            try
            {
                if (path != "")
                {
                    pathTo = Application.dataPath + "/" + System.IO.Path.GetDirectoryName(path).Replace("Assets/", "").Replace("Assets", "");
                    PlayerPrefs.SetString("FOPT_LastLSDir", pathTo);


                    if (File.Exists(path))
                    {
                        FOptimizer_LODSettings selected = (FOptimizer_LODSettings)AssetDatabase.LoadAssetAtPath(path, typeof(FOptimizer_LODSettings));
                        if (selected != null)
                        {
                            if (selected.IsTheSame(LODSet))
                            {
                                SetSharedLODSettings(selected);
                                return selected;
                            }
                        }
                    }

                    newLODSet = FOptimizer_LODSettings.CreateInstance<FOptimizer_LODSettings>();
                    AssetDatabase.CreateAsset(newLODSet, path);

                    if (newLODSet != null)
                    {
                        for (int i = 0; i < LODSet.LevelOfDetailSets.Count; i++)
                        {
                            FLOD_Base lodCopy = LODSet.LevelOfDetailSets[i].CreateNewCopy();
                            AssetDatabase.AddObjectToAsset(lodCopy, newLODSet);
                            newLODSet.LevelOfDetailSets.Add(lodCopy);
                        }

                        SetSharedLODSettings(newLODSet);
                    }
                }
            }
            catch (System.Exception exc)
            {
                Debug.LogError("[OPTIMIZERS] Something went wrong when creating LOD Set in your project. That's probably because of permissions on your hard drive.\n" + exc.ToString());
            }
#endif
            return newLODSet;
        }


        /// <summary>
        /// Checking if there is shared LOD set and if settings are updated
        /// </summary>
        internal void OnValidate()
        {
            if (!optimizer) return;
            if (!optimizer.enabled) return;
            if (Application.isPlaying)
            {
                return;
            }

            CheckAssetStructureCorrectness();

            if (sharedLODSet)
            {
                if (LODSet.LevelOfDetailSets != sharedLODSet.LevelOfDetailSets)
                    SetSharedLODSettings(sharedLODSet);

                if (optimizer.LODLevels != sharedLODSet.LevelOfDetailSets.Count - 2)
                    optimizer.LODLevels = sharedLODSet.LevelOfDetailSets.Count - 2;
            }

            if (LODSet != null)
                if (LODSet.LevelOfDetailSets != null)
                    if (LODSet.LevelOfDetailSets.Count > 1)
                        for (int i = 1; i < LODSet.LevelOfDetailSets.Count; i++)
                        {
                            if (LODSet.LevelOfDetailSets[i] == null) continue;
                            LODSet.LevelOfDetailSets[i].AssignToggler(LODSet.LevelOfDetailSets[0]);
                        }
        }

        #endregion


        #endregion


        #region Editor Related Methods

        public FOptimizer_LODSettings GetSharedSet()
        {
            return sharedLODSet;
        }

        public FOptimizer_LODSettings GetUniqueSet()
        {
            return uniqueLODSet;
        }

        public bool LostRequiredReferences()
        {
            if (RootReference == null) return true;
            if (uniqueLODSet == null && sharedLODSet == null) return true;
            return false;
        }

        public void CheckAssetStructureCorrectness()
        {
            if (!RootReference)
            {
                Debug.LogError("[OPTIMIZERS] CRITICAL ERROR: There is no root reference in Optimizer's LOD Controller! Try adding Optimizers Manager again to the scene or import newest version from the Asset Store!");
                return;
            }

#if UNITY_EDITOR


#if UNITY_2018_3_OR_NEWER
            if (optimizer.gameObject.scene.rootCount == 0)
            {
                // It's hilarious what I need to do to make Unity cooperate with this saving scriptable objects operations
                // I remembering path to asset inside internal memory so I can get it's reference inside prefab mode

                UnityEngine.Object prefab = FOptimizers_LODTransport.GetPrefab(optimizer.gameObject);
                if (prefab)
                {
                    PlayerPrefs.SetString("optim_lastEditedPrefabPath", AssetDatabase.GetAssetPath(prefab));
                }
            }

            // Using remembered path to apply settings from prefab mode to target prefab
            if (optimizer.gameObject.scene.rootCount == 1)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefs.GetString("optim_lastEditedPrefabPath"));
                if ( prefab )
                {
                    Transform myRoot = optimizer.transform;

                    Transform parentRoot = optimizer.transform.parent;
                    while(parentRoot != null)
                    {
                        myRoot = parentRoot;
                        parentRoot = myRoot.parent;
                    }

                    if ( prefab.name == myRoot.name)
                    {
                        FOptimizers_LODTransport.SaveLODSetInAsset(this, prefab);
                    }
                }
            }
#endif


            // When it's not editor mode or prefab mode - just scene object
            if (optimizer.gameObject.scene.rootCount > 1)
            {
                FOptimizers_LODTransport.SyncSceneOptimizerWithPrefab(optimizer, this);
            }

            // If optimizer lost references to LOD Sets, we will generate new LOD settings
            // Or when we create prefab from scene object
            bool referencesLost = false;
            if (LODSet == null) if (LostRequiredReferences()) referencesLost = true;

            if (referencesLost)
            {
                // If it's optimizer inside prefab asset
                // That means there was sprobably just created prefab from scene object
                if (optimizer.gameObject.scene.rootCount == 0)
                {
                    FOptimizers_LODTransport.SaveLODSettingsFromSceneOptimizer(this);
                }
            }
#endif
        }

        #endregion
    }
}

#endif
