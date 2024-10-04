using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Koenigz.PerfectCulling
{
    [RequireComponent(typeof(UnityEngine.Terrain))]
    public class PerfectCullingTerrain : PerfectCullingMonoGroup
    {
        [Range(16, 2048)]
        public int MeshResolutionX = 512;
        
        [Range(16, 2048)]
        public int MeshResolutionZ = 512;

        [Header("Creates double-sided mesh to make it not see-through from the other side.")]
        public bool DoubleSided = true;

        [HideInInspector] public Renderer terrainMeshRenderer;
        [HideInInspector] public MeshFilter terrainMeshFilter;
        
        public override List<Renderer> Renderers
        {
            get
            {
                return new List<Renderer>(1);
            }
        }

        public override List<UnityEngine.Behaviour> UnityBehaviours
        {
            get
            {
                return new List<UnityEngine.Behaviour>()
                {
                    GetComponent<UnityEngine.Terrain>() 
                };
            }
        }
        
        public override void PreBake(PerfectCullingBakingBehaviour bakingBehaviour)
        {
            UpdateRenderer();
            
            UnityEngine.Terrain terrain = GetComponent<UnityEngine.Terrain>();

            Renderer[] terrainRenderer = new Renderer[] {terrainMeshRenderer};
            
            foreach (var group in bakingBehaviour.bakeGroups)
            {
                if (group.unityBehaviours.Contains(terrain))
                {
                    group.renderers = terrainRenderer;
                }
            }
        }

        public override void PreSceneSave(PerfectCullingBakingBehaviour bakingBehaviour)
        {
        }

        public override void PostBake(PerfectCullingBakingBehaviour bakingBehaviour)
        {
            // Technically we don't need to do this because our mesh is never saved into the scene.
            // However the post bake hash calculation would include it and that's why we need to remove it.
            
            UnityEngine.Terrain terrain = GetComponent<UnityEngine.Terrain>();

            foreach (var group in bakingBehaviour.bakeGroups)
            {
                if (group.unityBehaviours.Contains(terrain))
                {
                    group.renderers = System.Array.Empty<Renderer>();
                }
            }
        }

        private void UpdateRenderer()
        {
            UnityEngine.Terrain terrain = GetComponent<Terrain>();

            Mesh terrainMesh = TerrainToMeshUtility.CreateMesh(terrain, MeshResolutionX, MeshResolutionZ, DoubleSided);
            
            if (terrainMeshRenderer == null)
            {
                GameObject go = new GameObject("Terrain Bake Mesh [EditorOnly]");

                terrainMeshRenderer = go.AddComponent<MeshRenderer>();

                terrainMeshRenderer.sharedMaterials = new Material[terrainMesh.subMeshCount];
            }

            if (terrainMeshFilter == null)
            {
                terrainMeshFilter = terrainMeshRenderer.gameObject.AddComponent<MeshFilter>();
            }

            terrainMeshFilter.sharedMesh = terrainMesh;

            terrainMeshRenderer.transform.SetPositionAndRotation(terrain.transform.position, Quaternion.identity);

            // Don't disable this mesh. We need the Unity Renderer to see it!
            terrainMeshRenderer.transform.parent = transform;
            terrainMeshRenderer.gameObject.layer = PerfectCullingConstants.CamBakeLayer;
            terrainMeshRenderer.tag = "EditorOnly";
        }

        public Renderer CreatePreview()
        {
            UpdateRenderer();

            return terrainMeshRenderer;
        }

        public Renderer GetPreview()
        {
            return terrainMeshRenderer;
        }

        public void DestroyPreview()
        {
            Object.DestroyImmediate(terrainMeshRenderer.gameObject);
        }
    }

#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(PerfectCullingTerrain))]
    public class PerfectCullingTerrainEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            PerfectCullingTerrain terrain = target as PerfectCullingTerrain;
            
            DrawDefaultInspector();

            using (new UnityEditor.EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Create/Update preview"))
                {
                    Renderer newPreview = terrain.CreatePreview();

                    newPreview.gameObject.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;
                }

                Renderer currentPreview = terrain.GetPreview();
                
                if (currentPreview != null)
                {
                    if (GUILayout.Button("Delete preview"))
                    {
                        if (currentPreview != null)
                        {
                            terrain.DestroyPreview();
                        }
                    }
                }
            }
        }
    }
#endif
}