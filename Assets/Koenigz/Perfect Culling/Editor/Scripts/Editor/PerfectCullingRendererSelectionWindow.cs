// Perfect Culling (C) 2021 Patrick König
//

#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Koenigz.PerfectCulling;
using UnityEditor;
using UnityEngine;
using UnityEditorInternal;

namespace Koenigz.PerfectCulling
{
    public class PerfectCullingRendererSelectionWindow : EditorWindow
    {
        private PerfectCullingBakingBehaviour m_attachedBakingBehaviour;

        private LayerMask m_layerMask = ~0;
        private bool m_onlyEnabled = true;
        private bool m_onlyActiveInHierachy = true;
        private bool m_onlyStatic = false;
        private bool m_includeChildren = true;
        private bool m_ignoreDisabledLodGroups = true;
        
        private HashSet<Renderer> m_selectedRenderers = new HashSet<Renderer>();
        private Vector2 m_rendererScrollView;

        bool RendererFilter(Renderer renderer)
        {
            if (renderer == null)
            {
                return false;
            }

            if (m_onlyEnabled && !renderer.enabled)
            {
                return false;
            }
            
            if (m_onlyActiveInHierachy && !renderer.gameObject.activeInHierarchy)
            {
                return false;
            }
            
            // Skip hidden objects
            if ((renderer.gameObject.hideFlags & HideFlags.HideInHierarchy) == HideFlags.HideInHierarchy
                || (renderer.gameObject.hideFlags & HideFlags.HideInInspector) == HideFlags.HideInInspector)
            {
                return false;
            }
                
            if (m_onlyStatic)
            {
                StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(renderer.gameObject);

                bool isOccluderStatic = ((flags & (StaticEditorFlags.OccluderStatic)) != 0) || (flags & (StaticEditorFlags.OccludeeStatic)) != 0;

                if (!isOccluderStatic)
                {
                    return false;
                }
            }
            
            if (!PerfectCullingConstants.SupportedRendererTypes.Contains(renderer.GetType()))
            {
                return false;
            }

            PerfectCullingRendererTag rendererTag = renderer.GetComponent<PerfectCullingRendererTag>();

            if (rendererTag != null && rendererTag.ExcludeRendererFromBake)
            {
                return false;
            }

            return true;
        }
        
        void OnSelectionChange() 
        {
            m_selectedRenderers.Clear();
            
            foreach (var selectedGameObject in UnityEditor.Selection.gameObjects)
            {
                List<Renderer> renderers = new List<Renderer>();

                if (m_includeChildren)
                {
                    selectedGameObject.GetComponentsInChildren<Renderer>(true, renderers);
                }
                else
                {
                    Renderer renderer = selectedGameObject.GetComponent<Renderer>();

                    if (renderer != null)
                    {
                        renderers.Add(renderer);
                    }
                } 
                
                if (renderers == null || renderers.Count <= 0)
                {
                    continue;
                }

                renderers.RemoveAll((rend) => ((1 << rend.gameObject.layer) & m_layerMask.value) == 0);
                
                renderers.RemoveAll((rend) => !RendererFilter(rend));

                if (m_attachedBakingBehaviour.additionalOccluders != null)
                {
                    HashSet<Renderer> additionalOccluders =
                        new HashSet<Renderer>(m_attachedBakingBehaviour.additionalOccluders);

                    for (int i = renderers.Count - 1; i >= 0; --i)
                    {
                        if (additionalOccluders.Contains(renderers[i]))
                        {
                            renderers.RemoveAt(i);
                        }
                    }
                }

                foreach (Renderer r in renderers)
                {
                    m_selectedRenderers.Add(r);
                }
            }
            
            Repaint();
        }

        public void InitializeAndShow(PerfectCullingBakingBehaviour behaviour)
        {
            m_attachedBakingBehaviour = behaviour;
            
            OnSelectionChange();
            
            Show();
        }

        private void OnGUI()
        {
            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                GUILayout.Label("Target:");
                m_attachedBakingBehaviour =
                    (PerfectCullingBakingBehaviour) EditorGUILayout.ObjectField(m_attachedBakingBehaviour,
                        typeof(PerfectCullingBakingBehaviour), true);

                GUI.enabled = m_attachedBakingBehaviour != null && m_selectedRenderers.Count > 0;
                if (GUILayout.Button($"Add selected renderers ({m_selectedRenderers.Count})"))
                {
                    HashSet<PerfectCullingBakeGroup> result =
                        new HashSet<PerfectCullingBakeGroup>(m_attachedBakingBehaviour.bakeGroups,
                            new PerfectCullingBakeGroupComparer());

                    foreach (PerfectCullingBakeGroup newBakeGroup in PerfectCullingEditorUtil.CreateBakeGroupsForRenderers(
                        m_selectedRenderers.ToList(), RendererFilter, m_attachedBakingBehaviour, m_ignoreDisabledLodGroups))
                    {
                        result.Add(newBakeGroup);
                    }

                    Undo.RecordObject(m_attachedBakingBehaviour, "Added renderers");
                    m_attachedBakingBehaviour.bakeGroups = result.ToArray();

                    UnityEditor.EditorUtility.SetDirty(m_attachedBakingBehaviour);

                    UnityEditor.Selection.activeObject = m_attachedBakingBehaviour;
                }

                GUI.enabled = true;

                GUILayout.Label("Filter:", EditorStyles.boldLabel);
                EditorGUI.BeginChangeCheck();
                
                {
                    GUILayout.Label("Layer");
                    m_layerMask = EditorGUILayout.MaskField(m_layerMask == ~0 ? ~0 : InternalEditorUtility.LayerMaskToConcatenatedLayersMask(m_layerMask), InternalEditorUtility.layers);
                    m_layerMask = m_layerMask == ~0 ? (LayerMask)~0 : InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(m_layerMask);
                }

                GUILayout.Space(10);
                
                m_onlyEnabled = EditorGUILayout.ToggleLeft("Only if Renderer component enabled", m_onlyEnabled);
                m_onlyActiveInHierachy = EditorGUILayout.ToggleLeft("Only if GameObject active in hierarchy", m_onlyActiveInHierachy);
                m_onlyStatic = EditorGUILayout.ToggleLeft("Only Occluder/Occludee Static objects", m_onlyStatic);
                m_ignoreDisabledLodGroups = EditorGUILayout.ToggleLeft("Ignore disabled LODGroups", m_ignoreDisabledLodGroups);
                
                m_includeChildren = EditorGUILayout.ToggleLeft("Include children", m_includeChildren);

                if (EditorGUI.EndChangeCheck())
                {
                    OnSelectionChange();
                }

                GUILayout.Space(5);

                GUILayout.Label($"Selected renderers ({m_selectedRenderers.Count}):", EditorStyles.boldLabel);

                m_rendererScrollView = EditorGUILayout.BeginScrollView(m_rendererScrollView);
                foreach (Renderer r in m_selectedRenderers)
                {
                    GUI.enabled = false;
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.ObjectField(r, typeof(Renderer), true);
                    }
                    GUI.enabled = true;
                }

                EditorGUILayout.EndScrollView();
            }
        }
    }
}
#endif