// Perfect Culling (C) 2023 Patrick König
//

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Koenigz.PerfectCulling
{
    public class PerfectCullingBakeGroupWindow : EditorWindow
    {
        private PerfectCullingBakingBehaviour m_attachedBakingBehaviour;

        private Vector2 m_scroll;

        private bool m_showOnlyInvalid = false;
        private bool m_hideEmptyGroups = true;

        private readonly Dictionary<int, int> m_rendererPages = new Dictionary<int, int>();

        public void InitializeAndShow(PerfectCullingBakingBehaviour behaviour)
        {
            m_attachedBakingBehaviour = behaviour;
            
            Show();
        }

        private void OnGUI()
        {
            GUILayout.Label(
                "This tool allows to manage the bake groups referenced by this volume.",
                EditorStyles.boldLabel);

            if (m_attachedBakingBehaviour == null)
            {
                EditorGUILayout.HelpBox("Invalid baking behaviour!", MessageType.Error);
                
                return;
            }
            
            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                GUILayout.Label("Target:");
                m_attachedBakingBehaviour =
                    (PerfectCullingBakingBehaviour)EditorGUILayout.ObjectField(m_attachedBakingBehaviour,
                        typeof(PerfectCullingBakingBehaviour), true);

                GUILayout.Space(10);

                EditorGUI.BeginChangeCheck();
                m_showOnlyInvalid = EditorGUILayout.ToggleLeft("Show groups containing invalid renderers only", m_showOnlyInvalid);
                m_hideEmptyGroups = EditorGUILayout.ToggleLeft("Hide empty groups", m_hideEmptyGroups);

                if (EditorGUI.EndChangeCheck())
                {
                    m_scroll = Vector2.zero;
                }
                
                GUILayout.Space(10);

                EditorGUILayout.LabelField("Groups", EditorStyles.boldLabel);

                const float ELEM_HEIGHT = 220.0f;

                Vector2 newScroll  = EditorGUILayout.BeginScrollView(m_scroll);

                if (Event.current.type != EventType.Repaint)
                {
                    m_scroll = newScroll;
                }

                List<PerfectCullingBakeGroup> filteredGroups = new List<PerfectCullingBakeGroup>();
                
                filteredGroups.AddRange(m_attachedBakingBehaviour.bakeGroups);
                filteredGroups.RemoveAll((g) =>
                {
                    Renderer[] renderers = g.renderers;

                    int nullRendererCount = 0;
                    
                    foreach (Renderer r in renderers)
                    {
                        nullRendererCount += (r == null) ? 1 : 0;
                    }

                    if (m_showOnlyInvalid)
                    {
                        return nullRendererCount == 0;
                    }

                    if (m_hideEmptyGroups)
                    {
                        return renderers.Length == 0;
                    }

                    return false;
                });
                
                int maxPerPage = Mathf.CeilToInt(4000.0f / ELEM_HEIGHT);

                int startIndex = Mathf.Max(0, Mathf.RoundToInt((m_scroll.y - ELEM_HEIGHT) / ELEM_HEIGHT));
                int endIndex = Mathf.Clamp(startIndex + maxPerPage + 1, 0, filteredGroups.Count);
                
                GUILayout.Space((startIndex) * ELEM_HEIGHT);
                
                for (int indexFilteredBakeGroup = startIndex; indexFilteredBakeGroup < endIndex; ++indexFilteredBakeGroup)
                {
                    PerfectCullingBakeGroup g = filteredGroups[indexFilteredBakeGroup];

                    Renderer[] renderers = g.renderers;

                    int nullRendererCount = 0;
                    
                    foreach (Renderer r in renderers)
                    {
                        nullRendererCount += (r == null) ? 1 : 0;
                    }

                    Color oldColor = GUI.color;
                    if (nullRendererCount != 0)
                    {
                        GUI.color = Color.red;
                    }

                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Height(ELEM_HEIGHT)))
                    {
                        GUI.color = oldColor;

                        GUILayout.Label($"[{System.Array.IndexOf(m_attachedBakingBehaviour.bakeGroups, g)}] - {g.groupType.ToString()}", EditorStyles.boldLabel);

                        EditorGUILayout.Space();
                        
                        const int MAX_PER_PAGE = 8;
                        
                        if (!m_rendererPages.TryGetValue(indexFilteredBakeGroup, out int currentPage))
                        {
                            currentPage = 1;
                            
                            m_rendererPages.Add(indexFilteredBakeGroup, currentPage);
                        }
                        
                        int maxPages = Mathf.CeilToInt(renderers.Length / (float)MAX_PER_PAGE);
                        
                        int startRendererIndex = (currentPage - 1) * MAX_PER_PAGE;
                        int endRendererIndex = Mathf.Clamp(currentPage * MAX_PER_PAGE, 0, renderers.Length);

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            GUILayout.Label($"Page {currentPage}/{maxPages}");
                            GUI.enabled = currentPage > 1;
                            if (GUILayout.Button("<<", GUILayout.Width(50)))
                            {
                                --m_rendererPages[indexFilteredBakeGroup];
                            }
                            GUI.enabled = currentPage < maxPages;
                            if (GUILayout.Button(">>", GUILayout.Width(50)))
                            {
                                ++m_rendererPages[indexFilteredBakeGroup];
                            }

                            GUI.enabled = true;
                        }
                        
                        EditorGUILayout.Space();
                        
                        for (var indexRenderer = startRendererIndex; indexRenderer < endRendererIndex; ++indexRenderer)
                        {
                            Renderer r = renderers[indexRenderer];
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                if (r == null)
                                {
                                    oldColor = GUI.contentColor;
                                    GUI.contentColor = Color.red;
                                    GUILayout.Label($"[{indexRenderer}] INVALID RENDERER");
                                    GUI.contentColor = oldColor;
                                }
                                else
                                {
                                    GUILayout.Label($"[{indexRenderer}] " + r.name);
                                }

                                GUI.enabled = (r != null);
                                if (GUILayout.Button("Select", GUILayout.Width(75)))
                                {
                                    UnityEditor.Selection.activeObject = r;
                                }

                                GUI.enabled = true;

                                if (GUILayout.Button("Remove", GUILayout.Width(75)))
                                {
                                    g.renderers = g.renderers
                                        .Except(new Renderer[] { r }).ToArray();
                                    
                                    EditorUtility.SetDirty(m_attachedBakingBehaviour);

                                    return;
                                }
                            }
                        }
                    }
                }
                
                GUILayout.Space((filteredGroups.Count - endIndex) * ELEM_HEIGHT);
                
                EditorGUILayout.EndScrollView();
            }
        }
    }
}
#endif