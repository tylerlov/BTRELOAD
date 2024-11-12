// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace GPUInstancerPro
{
    public class GPUITreeProxyProvider : GPUIDataProvider<int, MeshRenderer>
    {
        public override void Dispose()
        {
            if (_dataDict != null)
                foreach (MeshRenderer mr in Values)
                    if (mr != null)
                        mr.gameObject.DestroyGeneric();

            base.Dispose();
        }

        public void GetMaterialPropertyBlock(GPUILODGroupData lgd, GPUICameraData cameraData, MaterialPropertyBlock mpb)
        {
            MeshRenderer mr = AddOrGetTreeProxy(lgd.prototype.prefabObject, cameraData);
            if (mr == null)
                return;
            mr.GetPropertyBlock(mpb);
        }

        private MeshRenderer AddOrGetTreeProxy(GameObject treePrefab, GPUICameraData cameraData)
        {
            if (!Application.isPlaying) return null;
            int key = GPUIUtility.GenerateHash(treePrefab.GetInstanceID(), cameraData.ActiveCamera.GetInstanceID());
            if (!_dataDict.ContainsKey(key) || _dataDict[key] == null)
            {
                MeshRenderer mr = AddTreeProxy(treePrefab, cameraData.ActiveCamera.transform);
                if (mr != null)
                    AddOrSet(key, mr);
                return mr;
            }
            return _dataDict[key];
        }

        private static MeshRenderer AddTreeProxy(GameObject treePrefab, Transform parentTransform)
        {
            Shader treeProxyShader = GPUIUtility.FindShader(GPUIConstants.SHADER_GPUI_TREE_PROXY);
            if (treeProxyShader == null)
            {
#if UNITY_EDITOR
                Debug.LogError("Can not find GPUI Pro Tree Proxy shader! Make sure the shader is imported: " + GPUIConstants.SHADER_GPUI_TREE_PROXY);
#else
                Debug.LogError("Can not find GPUI Pro Tree Proxy shader! Make sure the shader is included in build: " + GPUIConstants.SHADER_GPUI_TREE_PROXY);
#endif
                return null;
            }

            Mesh treeProxyMesh = new Mesh();
            treeProxyMesh.name = "TreeProxyMesh";

            Material[] treeProxyMaterials = new Material[1] { new Material(treeProxyShader) };
            LODGroup lodGroup = treePrefab.GetComponent<LODGroup>();
            MeshRenderer treeProxyObjectMR;
            if (lodGroup != null)
                treeProxyObjectMR = InstantiateTreeProxyObject(lodGroup.GetLODs()[0].renderers[0].gameObject, parentTransform, treeProxyMaterials, treeProxyMesh);
            else
                treeProxyObjectMR = InstantiateTreeProxyObject(treePrefab.GetComponent<Tree>().gameObject, parentTransform, treeProxyMaterials, treeProxyMesh);

            return treeProxyObjectMR;
        }

        private static MeshRenderer InstantiateTreeProxyObject(GameObject treePrefab, Transform parentTransform, Material[] proxyMaterials, Mesh proxyMesh)
        {
            GameObject treeProxyObject = UnityEngine.Object.Instantiate(treePrefab, parentTransform);
            treeProxyObject.hideFlags = HideFlags.DontSave;
            treeProxyObject.name = treePrefab.name + "_GPUITreeProxy";

            proxyMesh.bounds = treeProxyObject.GetComponent<MeshFilter>().sharedMesh.bounds;

            // Setup Tree Proxy object mesh renderer.
            MeshRenderer treeProxyObjectMR = treeProxyObject.GetComponent<MeshRenderer>();
            treeProxyObjectMR.shadowCastingMode = ShadowCastingMode.Off;
            treeProxyObjectMR.receiveShadows = false;
            treeProxyObjectMR.lightProbeUsage = LightProbeUsage.Off;

            for (int i = 0; i < proxyMaterials.Length; i++)
            {
                proxyMaterials[i].CopyPropertiesFromMaterial(treeProxyObjectMR.sharedMaterials[i]);
                proxyMaterials[i].enableInstancing = true;
            }

            treeProxyObjectMR.sharedMaterials = proxyMaterials;
            treeProxyObjectMR.GetComponent<MeshFilter>().sharedMesh = proxyMesh;

            StripComponents(treeProxyObject);

            return treeProxyObjectMR;
        }

        private static void StripComponents(GameObject go)
        {
            Component[] allComponents = go.GetComponents(typeof(Component));
            for (int i = 0; i < allComponents.Length; i++)
            {
                Component component = allComponents[i];
                if (component is Transform || component is MeshFilter || component is MeshRenderer || component is Tree)
                    continue;

                component.DestroyGeneric();
            }
        }
    }
}