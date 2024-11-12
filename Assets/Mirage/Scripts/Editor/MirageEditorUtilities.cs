/*
 * Copyright (c) Léo CHAUMARTIN 2024
 * All Rights Reserved
 * 
 * File: MirageEditorUtilities.cs
 */

using System.IO;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;

namespace Mirage.Impostors
{
    using Core;
    using Elements;
    using System.Text.RegularExpressions;

    public class MirageEditorUtilities
    {
        /// <summary>
        /// Helper method to get a valid and unique path for impostor export
        /// </summary>
        public static string ProcessPath(string inputPath, GameObject targetObject)
        {
            inputPath = "Assets/" + inputPath;
            if (inputPath.EndsWith("/") || AssetDatabase.IsValidFolder(inputPath))
                if (targetObject != null)
                    inputPath += "/" + targetObject.name + "_impostor.prefab";
                else
                    inputPath += "/impostor.prefab";
            else
            {
                if (inputPath.Contains("/"))
                {
                    if (AssetDatabase.IsValidFolder(inputPath.Substring(0, inputPath.IndexOf('/'))))
                    {
                        inputPath += ".prefab";

                    }
                }
            }
            inputPath = Regex.Replace(inputPath, @"/+", "/");
            inputPath = Regex.Replace(inputPath, @"\.+", ".");
            inputPath = Regex.Replace(inputPath, @"\./", "/");
            if (inputPath.Contains("/") && AssetDatabase.IsValidFolder(inputPath.Substring(0, inputPath.LastIndexOf("/"))))
            {
                inputPath = AssetDatabase.GenerateUniqueAssetPath(inputPath);
            }
            return inputPath;
        }

        /// <summary>
        /// Helper method to detect if a renderer is part of LODGroup
        /// </summary>
        public static bool IsPartOfLODGroup(Renderer renderer)
        {
            LODGroup lodGroup = renderer.GetComponentInParent<LODGroup>();
            if (lodGroup != null)
            {
                Renderer sourceRenderer = renderer;
                LOD[] lodCandidates = lodGroup.GetLODs();
                foreach (LOD l in lodCandidates)
                    foreach (Renderer r in l.renderers)
                        if (r == sourceRenderer)
                            return true;
            }
            return false;
        }

        public static LODGroup GetSingleLODGroup(List<MeshFilter> meshFilters, out bool valid)
        {
            valid = false;
            if (meshFilters.Count == 0)
                return null;

            LODGroup commonLODGroup = null;
            int? commonLodIndex = null;

            foreach (var meshFilter in meshFilters)
            {
                if (meshFilter == null)
                    continue;

                var renderer = meshFilter.GetComponent<Renderer>();
                if (renderer == null)
                    continue;

                var lodGroup = renderer.GetComponentInParent<LODGroup>();
                if (lodGroup == null)
                    continue;

                int currentLodIndex = GetLODIndex(lodGroup, renderer);
                if (commonLODGroup == null && commonLodIndex == null)
                {
                    commonLODGroup = lodGroup;
                    commonLodIndex = currentLodIndex;
                }
                else if (lodGroup != commonLODGroup || commonLodIndex != currentLodIndex)
                {
                    return commonLODGroup; // Found renderers from different LODs
                }
            }
            valid = true;
            return commonLODGroup;
        }

        private static int GetLODIndex(LODGroup lodGroup, Renderer renderer)
        {
            LOD[] lods = lodGroup.GetLODs();
            for (int i = 0; i < lods.Length; i++)
            {
                for (int j = 0; j < lods[i].renderers.Length; j++)
                    if (lods[i].renderers[j] == renderer)
                        return i;
            }
            return -1; // Renderer not part of any LOD in this LODGroup
        }

        public static List<MeshFilter> GetAllDescendantMeshFilters(GameObject ancestor)
        {
            List<MeshFilter> meshFilters = new List<MeshFilter>();
            GetMeshFiltersRecursive(ancestor.transform, meshFilters);
            return meshFilters;
        }

        private static void GetMeshFiltersRecursive(Transform current, List<MeshFilter> meshFilters)
        {
            {
                MeshFilter meshFilter = current.GetComponent<MeshFilter>();
                if (meshFilter != null)
                {
                    ValidateCandidate(meshFilter, out bool valid);
                    if(valid)
                        meshFilters.Add(meshFilter);
                }
            }
            foreach (Transform child in current)
            {

                // Recursively get MeshFilters from children
                GetMeshFiltersRecursive(child, meshFilters);
            }
        }

        public static GameObject GetCommonAncestor(List<MeshFilter> meshFilters)
        {
            if (meshFilters.Count == 0)
                return null;
            Transform currentAncestor = null;
            int i = 0;
            while (currentAncestor == null && i < meshFilters.Count)
            {
                currentAncestor = meshFilters[i] != null ? meshFilters[i].gameObject.transform : null;
                i++;
            }
            if (currentAncestor == null)
                return null;

            while (currentAncestor != null)
            {
                bool isCommonAncestor = true;

                foreach (var meshFilter in meshFilters)
                {
                    if (meshFilter is null)
                        continue;
                    Transform currentParent = meshFilter.gameObject.transform;

                    // Check if currentAncestor is an ancestor of currentParent
                    bool foundAncestor = false;
                    while (currentParent != null)
                    {
                        if (currentParent == currentAncestor)
                        {
                            foundAncestor = true;
                            break;
                        }
                        currentParent = currentParent.parent;
                    }

                    // If currentAncestor is not an ancestor of the current meshFilter, break and move up the hierarchy
                    if (!foundAncestor)
                    {
                        isCommonAncestor = false;
                        break;
                    }
                }

                if (isCommonAncestor)
                {
                    return currentAncestor.gameObject;
                }

                currentAncestor = currentAncestor.parent;
            }

            // No common ancestor found
            return null;
        }

        public static bool IsEmpty(List<MeshFilter> filters)
        {
            foreach(MeshFilter filter in filters)
            {
                if (filter != null)
                    return false;
            }
            return true;
        }

        public static string ValidateCandidate(MeshFilter filter, out bool valid)
        {
            valid = false;
            if (filter != null)
            {
                MeshRenderer renderer = filter.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    if (renderer.sharedMaterial != null && renderer.sharedMaterial.shader != null)
                    {
                        string shaderName = renderer.sharedMaterial.shader.name;
                        if (!(shaderName.Contains("Mirage/") && shaderName.Contains("Impostor")))
                        {
                            valid = true;
                            return "";
                        }
                        return "The attached renderer is already an impostor";
                    }
                    return "The mesh renderer has no valid material";
                }
                return "No mesh renderer attached";
            }
            return "Mesh filter is null";
        }

        private static Dictionary<string, bool> sectionStates = new Dictionary<string, bool>();
        private static GUIStyle sectionStyle = new GUIStyle(EditorStyles.toolbarButton) { alignment = TextAnchor.MiddleLeft, fontSize = 14, fontStyle = FontStyle.Bold, fixedHeight = 32, fixedWidth = 150 };
        private static GUIStyle sectionStyleExpanded = new GUIStyle(EditorStyles.toolbarButton) { alignment = TextAnchor.MiddleLeft, fontSize = 14, fontStyle = FontStyle.Bold, fixedHeight = 32};

        public static bool BeginSection(string name, bool defaultExpandedState = true, Action customGuiAction = null, bool customGuiActionWhenExpanded = false)
        {
            if (!sectionStates.TryGetValue(name, out bool isExpanded))
            {
                sectionStates[name] = defaultExpandedState; // Default state
                isExpanded = defaultExpandedState;
            }
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUIContent foldoutContent = new GUIContent(name);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(foldoutContent, isExpanded ? sectionStyleExpanded : sectionStyle))
            {
                sectionStates[name] = !isExpanded;

            }
            EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link); // Change cursor to pointer
            
            if(customGuiActionWhenExpanded || !isExpanded)
                customGuiAction?.Invoke();
            EditorGUILayout.EndHorizontal();
            if (isExpanded)
                EditorGUILayout.Separator();

            return isExpanded;
        }

        public static void EndSection()
        {
            EditorGUILayout.EndVertical();
        }
    }
}
