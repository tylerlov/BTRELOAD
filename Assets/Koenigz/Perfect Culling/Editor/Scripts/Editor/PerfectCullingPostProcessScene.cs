// Perfect Culling (C) 2023 Patrick König
//

#if UNITY_EDITOR
using System;
using System.Text;
using Koenigz.PerfectCulling;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Callbacks;
using Object = UnityEngine.Object;

namespace Koenigz.PerfectCulling
{
    public class PerfectCullingPostProcessScene
    {
        [PostProcessSceneAttribute(99999)]
        public static void OnPostprocessScene()
        {
            System.Text.StringBuilder stringBuilder = new StringBuilder();

            bool anyNullRenderers = false;

            foreach (var pc in Object.FindObjectsOfType<PerfectCullingBakingBehaviour>())
            {
                for (var indexBakeGroup = 0; indexBakeGroup < pc.bakeGroups.Length; indexBakeGroup++)
                {
                    var bakeGroup = pc.bakeGroups[indexBakeGroup];
                    for (var indexRenderer = 0; indexRenderer < bakeGroup.renderers.Length; indexRenderer++)
                    {
                        var r = bakeGroup.renderers[indexRenderer];
                        if (r == null)
                        {
                            anyNullRenderers = true;

                            stringBuilder.AppendLine($"[*] BakeGroup: {indexBakeGroup}, Index: {indexRenderer}");
                        }
                    }
                }
            }

            if (PerfectCullingConstants.ReportInvalidRenderers && anyNullRenderers && BuildPipeline.isBuildingPlayer)
            {
                PerfectCullingLogger.LogError(
                    $"Invalid renderers in scene {UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().name}. This can potentially cause culling artifacts in builds. You can disable this error in {nameof(PerfectCullingConstants)}.cs\nInvalid indices:\n{stringBuilder.ToString()}");
            }
        }
    }
}

#endif