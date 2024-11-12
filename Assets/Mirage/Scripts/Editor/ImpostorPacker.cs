using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mirage.Impostors.Elements
{
    using Core;
    using System.IO;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine.Rendering;

    public class ImpostorPacker
    {
        Shader litImpostorShader;
        Shader unlitImpostorShader;

        public ImpostorPacker()
        {
            if(GraphicsSettings.currentRenderPipeline == null)
                litImpostorShader = Shader.Find("Mirage/Impostor");
            else
                litImpostorShader = Shader.Find("Shader Graphs/MirageImpostor");
            
            if (litImpostorShader == null || !litImpostorShader.isSupported)
            {
                Debug.LogError("[Mirage] No supported impostor shader found. Please reimport the Mirage package.");
                return;
            }

            if (GraphicsSettings.currentRenderPipeline == null)
                unlitImpostorShader = Shader.Find("Mirage/ImpostorUnlit");
            else
                unlitImpostorShader = Shader.Find("Shader Graphs/MirageImpostorUnlit");

            if (unlitImpostorShader == null || !unlitImpostorShader.isSupported)
            {
                Debug.LogError("[Mirage] No supported impostor shader found. Please reimport the Mirage package.");
                return;
            }
        }

        public GameObject PackImpostor(GameObject sourceGo, List<MeshFilter> sourceFilters, Texture2D colorMap, Texture2D normalMap, Texture2D maskMap, float orthographicSize, int subdivisions, Vector3 pivotExcentricity, ImpostorPreset settings, ImpostorLODGroupPreset lodGroupSettings, string prefabPath)
        {
            string dirPath = prefabPath;
            if (prefabPath.Contains("/"))
                dirPath = prefabPath.Substring(0, prefabPath.LastIndexOf('/'));
            if (!AssetDatabase.IsValidFolder(dirPath))
                Directory.CreateDirectory(dirPath);
            
            prefabPath = AssetDatabase.GenerateUniqueAssetPath(prefabPath);

            GameObject go = new GameObject();
            go.transform.localScale = Vector3.one;
            go.transform.SetParent(sourceGo.transform);

            Material mat = new Material(settings.lightingMethod == LightingMethod.SurfaceEstimation ? litImpostorShader : unlitImpostorShader);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.EnableKeyword("_ALPHATEST_ON");
            Texture2D texCopy = new Texture2D(colorMap.width, colorMap.height, TextureFormat.ARGB32, true);
            texCopy.SetPixels(colorMap.GetPixels());
            texCopy.Apply(true);
            texCopy.name = "ColorMapAtlas";

            Texture2D texNormalsCopy = null;
            Texture2D texMaskCopy = null;
            if (settings.lightingMethod == LightingMethod.SurfaceEstimation)
            {
                texNormalsCopy = new Texture2D(normalMap.width, normalMap.height, TextureFormat.ARGB32, true);
                texNormalsCopy.SetPixels(normalMap.GetPixels());
                texNormalsCopy.Apply(true);
                texNormalsCopy.name = "NormalMapAtlas";
                ImpostorTextureUtilities.SetTextureReadable(texNormalsCopy, false, true);

                texMaskCopy = new Texture2D(maskMap.width, maskMap.height, TextureFormat.RGB24, true);
                texMaskCopy.SetPixels(maskMap.GetPixels());
                texMaskCopy.Apply(true);
                texMaskCopy.name = "MaskAtlas";
                ImpostorTextureUtilities.SetTextureReadable(texMaskCopy, false, false);
            }


            mat.SetTexture("_MainTex", texCopy);
            if(settings.lightingMethod == LightingMethod.SurfaceEstimation)
            {
                mat.SetTexture("_NormalMap", texNormalsCopy);
                mat.SetTexture("_MaskMap", texMaskCopy);
            }
            mat.SetFloat("_GridSize", subdivisions * subdivisions);
            mat.SetInt("_LongitudeSamples", settings.longitudeSamples);
            mat.SetInt("_LatitudeSamples", settings.latitudeSamples);
            mat.SetInt("_LatitudeOffset", settings.latitudeOffset);
            mat.SetFloat("_LatitudeStep", settings.latitudeAngularStep);
            mat.SetFloat("_SmartSphere", settings.FibonacciSphere ? 1f : 0f);

            Mesh mesh = ImpostorMeshUtility.BuildQuad(orthographicSize * 2f);
            mesh.name = "ImpostorQuad";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            prefab.AddComponent<MeshFilter>().sharedMesh = mesh;
            AssetDatabase.AddObjectToAsset(prefab.GetComponent<MeshFilter>().sharedMesh, prefab);
            texCopy.name = "AlbedoAtlas";
            EditorUtility.CompressTexture(texCopy, TextureFormat.DXT5, TextureCompressionQuality.Best);

            AssetDatabase.AddObjectToAsset(texCopy, prefab);
            if (settings.lightingMethod == LightingMethod.SurfaceEstimation)
            {
                EditorUtility.CompressTexture(texNormalsCopy, TextureFormat.DXT1, TextureCompressionQuality.Best);
                AssetDatabase.AddObjectToAsset(texNormalsCopy, prefab);
                EditorUtility.CompressTexture(texMaskCopy, TextureFormat.DXT1, TextureCompressionQuality.Best);
                AssetDatabase.AddObjectToAsset(texMaskCopy, prefab);
            }
            prefab.AddComponent<MeshRenderer>().material = mat;
            AssetDatabase.AddObjectToAsset(mat, prefab);
            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(prefabPath);
            Object.DestroyImmediate(go);

            if (lodGroupSettings.setupLOD)
            {
                LODGroup lodGroup = MirageEditorUtilities.GetSingleLODGroup(sourceFilters, out bool valid);
                if (!valid)
                {
                    Debug.LogError("Could not setup LODGroup because to avoid conflict with existing LODGroups");
                    return null;
                }

                Undo.RecordObject(sourceGo, "Mirage LODGroup setup");

                bool isPartOfLodGroup = lodGroup != null;
                if (lodGroup == null || !isPartOfLodGroup)
                {
                    LOD[] lods = new LOD[2];
                    lods[0].renderers = new Renderer[sourceFilters.Count];
                    for (int i = 0; i < sourceFilters.Count; ++i)
                        lods[0].renderers[i] = sourceFilters[i].GetComponent<Renderer>();
                    lods[0].screenRelativeTransitionHeight = lodGroupSettings.lodPerformance;
                    lods[0].fadeTransitionWidth = 0.1f;
                    lodGroup = Undo.AddComponent<LODGroup>(sourceGo);
                    lodGroup.fadeMode = LODFadeMode.CrossFade;
                    lodGroup.animateCrossFading = false;
                    GameObject impostor = PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath), sourceGo.transform) as GameObject;
                    Undo.RegisterCreatedObjectUndo(impostor, "Impostor Instance");
                    impostor.transform.position = pivotExcentricity;
                    lods[1].renderers = new MeshRenderer[] { impostor.GetComponent<MeshRenderer>() };
                    lods[1].screenRelativeTransitionHeight = lodGroupSettings.lodSizeCulling;
                    lods[1].fadeTransitionWidth = 0.1f;
                    lodGroup.SetLODs(lods);
                    Undo.AddComponent<ImpostorReference>(sourceGo).impostorObject = impostor;
                }
                else
                {
                    LOD[] lods = lodGroup.GetLODs();
                    
                    ImpostorReference reference = lodGroup.gameObject.GetComponent<ImpostorReference>();
                    if (reference == null || reference.impostorObject == null)
                    {
                        LOD[] newLods = new LOD[lods.Length + 1];
                        lods.CopyTo(newLods, 0);
                        GameObject impostor = PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath), lodGroup.transform) as GameObject;
                        Undo.RegisterCreatedObjectUndo(impostor, "Impostor Instance");
                        impostor.transform.position = pivotExcentricity;
                        newLods[lods.Length - 1].screenRelativeTransitionHeight = lodGroupSettings.lodPerformance;
                        newLods[lods.Length].renderers = new MeshRenderer[] { impostor.GetComponent<MeshRenderer>() };
                        newLods[lods.Length].screenRelativeTransitionHeight = lodGroupSettings.lodSizeCulling;
                        lodGroup.SetLODs(newLods);
                        if (reference == null)
                            Undo.AddComponent<ImpostorReference>(sourceGo).impostorObject = impostor;
                        else
                            reference.impostorObject = impostor;
                    }
                    else
                    {
                        int impostorIndex = 0;
                        int count = 0;

                        GameObject previousImpostorObject = reference.impostorObject;

                        foreach (LOD l in lods)
                        {
                            foreach (Renderer r in l.renderers)
                            {
                                if (r == previousImpostorObject.GetComponent<Renderer>())
                                    impostorIndex = count;
                            }
                            ++count;
                        }
                        if (impostorIndex == 0)
                        {
                            Debug.LogError("[Mirage] Impostor is part of a LODGroup but cannot find its index from the ImpostorReference object.");
                        }
                        GameObject impostor = PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath), lodGroup.transform) as GameObject;
                        Undo.RegisterCreatedObjectUndo(impostor, "Impostor Instance");
                        impostor.transform.position = pivotExcentricity;
                        lods[impostorIndex].renderers = new MeshRenderer[] { impostor.GetComponent<MeshRenderer>() };
                        lods[impostorIndex].screenRelativeTransitionHeight = lodGroupSettings.lodSizeCulling;
                        lodGroup.SetLODs(lods);
                        Undo.DestroyObjectImmediate(previousImpostorObject);
                        reference.impostorObject = impostor;
                    }
                }
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }
            return prefab;
        }
    }
}
