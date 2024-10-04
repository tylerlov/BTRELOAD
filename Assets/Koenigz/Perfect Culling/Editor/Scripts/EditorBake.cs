using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Koenigz.PerfectCulling;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Koenigz.PerfectCulling
{
    public static class EditorBake
    {


        public static IEnumerator PerformBakeAsync(this PerfectCullingBakingBehaviour behaviour, bool saveScene,
            HashSet<Renderer> additionalOccludersHashset)
        {
#if !UNITY_EDITOR
                yield break;
#else
            string currentScenePath = null;

            try
            {
                // Collect Renderers that should be excluded
                HashSet<Renderer> renderersToExcludeFromBake = new HashSet<Renderer>();
                
                foreach (PerfectCullingRendererTag rendererTag in
                         GameObject.FindObjectsOfType<PerfectCullingRendererTag>())
                {
                    if (rendererTag.ExcludeRendererFromBake)
                    {
                        Renderer r = rendererTag.GetComponent<Renderer>();

                        if (r == null)
                        {
                            continue;
                        }
                        
                        renderersToExcludeFromBake.Add(r);
                    }
                }

                // Strip Renderers that should be excluded
                foreach (var bakeGroup in behaviour.bakeGroups)
                {
                    bakeGroup.renderers = bakeGroup.renderers.Where(r => !renderersToExcludeFromBake.Contains(r)).ToArray();
                }

                // Strip empty groups
                behaviour.bakeGroups = behaviour.bakeGroups.Where(bakeGroup => bakeGroup.renderers.Length > 0).ToArray();
                
                if (behaviour.bakeGroups.Length <= 0)
                {
                    PerfectCullingEditorUtil.DisplayDialog("No renderers", "No renderers. Nothing to bake", "OK");

                    yield return new PerfectCullingBakeNotStartedYieldInstruction();
                }

                if (behaviour.bakeGroups.Length == 1)
                {
                    PerfectCullingLogger.LogError(
                        $"{nameof(behaviour.bakeGroups)} contains only one element thus no occlusion is possible. Each Renderer should go into it's own {nameof(PerfectCullingBakeGroup)}. Consider using {nameof(PerfectCullingEditorUtil.CreateBakeGroupsForRenderers)}!");

                    yield return new PerfectCullingBakeNotStartedYieldInstruction();
                }

                /*
                // Order renderers for improved local coherence
                Renderers = Renderers
                    .OrderBy(m => m.transform.position.x)
                    .OrderBy(m => m.transform.position.z)
                    .OrderBy(m => m.transform.position.y)
                    .ToArray();*/

                UnityEditor.EditorUtility.DisplayProgressBar($"Initializing", "Initializing", 0);

                if (!behaviour.PreBake())
                {
                    yield return new PerfectCullingBakeNotStartedYieldInstruction();
                }

                PerfectCullingMonoGroup[] allMonoGroups =
                    PerfectCullingEditorUtil.FindMonoGroupsForBakingBehaviour(behaviour);
                

                HashSet<Renderer> copyAdditionalOccluders = new HashSet<Renderer>();

                if (additionalOccludersHashset != null)
                {
                    foreach (Renderer r in additionalOccludersHashset)
                    {
                        copyAdditionalOccluders.Add(r);
                    }
                }

                // Strip null references in additional occluders
                if (behaviour.additionalOccluders.RemoveAll((x) => x == null) > 0)
                {
                    Debug.LogWarning($"Stripped some null references in {nameof(behaviour.additionalOccluders)}");

#if UNITY_EDITOR
                    UnityEditor.EditorUtility.SetDirty(behaviour);
#endif
                }

                foreach (Renderer r in behaviour.additionalOccluders)
                {
                    copyAdditionalOccluders.Add(r);
                }

                // Strip additional occluders that are already referenced by this behaviour
                HashSet<Renderer> allReferencedRenderers = new HashSet<Renderer>();

                foreach (PerfectCullingBakeGroup group in behaviour.bakeGroups)
                {
                    foreach (var r in group.renderers)
                    {
                        allReferencedRenderers.Add(r);
                    }
                }

                // Only keep occluders unreferenced
                copyAdditionalOccluders =
                    new HashSet<Renderer>(copyAdditionalOccluders.Where((x) => !allReferencedRenderers.Contains(x)));

                behaviour.CullAdditionalOccluders(ref copyAdditionalOccluders);

                // We cannot perform this in play mode due to static batching.
                // So lets do it here.
                bool cleanupInvalidRenderers = false;

                foreach (PerfectCullingBakeGroup group in behaviour.bakeGroups)
                {
                    if (!group.CollectMeshStats())
                    {
                        if (!PerfectCullingEditorUtil.DisplayDialog("Error: Invalid renderers detected!",
                                "Error: Bake groups contain references to invalid renderers.\n\nExamples:\n- Renderer is null\n- MeshFilter is null\n- Mesh is null",
                                "Remove invalid renderers", "Cancel"))
                        {
                            yield return new PerfectCullingBakeNotStartedYieldInstruction();
                        }

                        cleanupInvalidRenderers = true;
                        break;
                    }
                }

                if (cleanupInvalidRenderers)
                {
                    foreach (PerfectCullingBakeGroup group in behaviour.bakeGroups)
                    {
                        group.RemoveInvalidRenderers();
                        if (!group.CollectMeshStats())
                        {
                            PerfectCullingLogger.LogError("Failed to clean-up invalid renderers.");

                            yield return new PerfectCullingBakeNotStartedYieldInstruction();
                        }
                    }
                }

                foreach (var monoGroup in allMonoGroups)
                {
                    monoGroup.PreSceneSave(behaviour);
                }

                UnityEditor.EditorUtility.SetDirty(behaviour);
                UnityEditor.EditorUtility.SetDirty(behaviour.BakeData);

                if (saveScene && UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path !=
                    PerfectCullingConstants.MultiSceneTempPath)
                {
                    if (!PerfectCullingEditorUtil.SaveModifiedScenesIfUserWantsTo(new Scene[]
                            { UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene() }))
                    {
                        yield return new PerfectCullingBakeNotStartedYieldInstruction();
                    }
                }

                // Needs to happen after saving (path might have changed)!
                Scene currentScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();

                currentScenePath = currentScene.path;

                PerfectCullingBakingManager.VerifyCurrentScenePath(currentScenePath);

                PerfectCullingLogger.Log(currentScenePath);

                Scene newScene =
                    string.IsNullOrEmpty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
                        .name) // Check if untitled scene
                        ? currentScene
                        : UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                            UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
                            UnityEditor.SceneManagement.NewSceneMode.Additive);

                UnityEditor.SceneManagement.EditorSceneManager.MergeScenes(currentScene, newScene);
                UnityEditor.SceneManagement.EditorSceneManager.SetActiveScene(newScene);

                foreach (var monoGroup in allMonoGroups)
                {
                    monoGroup.PreBake(behaviour);
                }

                behaviour.BakeData.bakeCompleted = false;

                behaviour.BakeData.PrepareForBake(behaviour);
                behaviour.InitializeAllSamplingProviders();

                List<Vector3> worldPositions = behaviour.GetSamplingPositions(Space.World);

                List<PerfectCullingBakeSettings.SamplingLocation> samplingLocations =
                    new List<PerfectCullingBakeSettings.SamplingLocation>(worldPositions.Count);

                int activeSamplingPositionsCount = 0;

                for (int i = 0; i < worldPositions.Count; ++i)
                {
                    Vector3 pos = worldPositions[i];
                    bool active = behaviour.SamplingProvidersIsPositionActive(worldPositions[i]);

                    samplingLocations.Add(new PerfectCullingBakeSettings.SamplingLocation(pos, active));

                    activeSamplingPositionsCount += active ? 1 : 0;
                }

                PerfectCullingBakeSettings bakeSettings = new PerfectCullingBakeSettings()
                {
                    Groups = behaviour.bakeGroups,

                    AdditionalOccluders = copyAdditionalOccluders,

                    ActiveSamplingPositionCount = activeSamplingPositionsCount,
                    SamplingLocations = samplingLocations,

                    Width = PerfectCullingSettings.Instance.bakeCameraResolutionWidth,
                    Height = PerfectCullingSettings.Instance.bakeCameraResolutionHeight
                };

                using (PerfectCullingBaker baker = PerfectCullingBakerFactory.CreateBaker(bakeSettings))
                {
                    System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();

                    List<Vector3> localPositions = behaviour.GetSamplingPositions();

                    int totalBatchCounts = activeSamplingPositionsCount / baker.BatchCount;
                    int currentBatchCount = 0;

                    List<PerfectCullingBakeHandle> pending = new List<PerfectCullingBakeHandle>(localPositions.Count);

                    const float SMOOTHING_FACTOR = 0.005f;

                    float lastTime = Time.realtimeSinceStartup;
                    int lastElement = 0;

                    float lastSpeed = -1f;
                    float averageSpeed = PerfectCullingSettings.Instance.bakeAverageSamplingSpeedMs / 1000f;

                    int bakedCellCount = 0;

                    for (int i = 0; i < localPositions.Count; ++i)
                    {
                        // We use bakedCellCount instead of i because we might not bake all cells
                        string strBakingTitle =
                            $"[ETA {PerfectCullingEditorUtil.FormatSeconds((activeSamplingPositionsCount - bakedCellCount) * averageSpeed)}], Avg. speed: {System.Math.Round(averageSpeed * 1000f, 2)} ms | ";

                        if (!samplingLocations[i].Active)
                        {
                            // Don't validate. We don't want warnings for cells that are empty on purpose.
                            behaviour.BakeData.SetRawData(i, System.Array.Empty<ushort>(), false);

                            continue;
                        }

                        ++bakedCellCount;

                        PerfectCullingBakerHandle handle =
                            baker.SamplePosition(behaviour.transform.rotation * localPositions[i] + behaviour.transform.position);

                        pending.Add(new PerfectCullingBakeHandle()
                        {
                            Index = i,
                            Handle = handle
                        });

                        if (pending.Count >= baker.BatchCount)
                        {
                            // We call this here to grant some additional time to the GPU while we clean-up
                            System.GC.Collect();

                            if (UnityEditor.EditorUtility.DisplayCancelableProgressBar(
                                    strBakingTitle + "Performing readback ",
                                    strBakingTitle + "Performing readback",
                                    (currentBatchCount / (float)totalBatchCounts)))
                            {
                                // Just so we properly Dispose() them.
                                CompletePending(behaviour.BakeData, pending);

                                yield return new PerfectCullingBakeAbortedYieldInstruction();
                            }

                            CompletePending(behaviour.BakeData, pending);

                            // Give Unity some breathing room.
                            // This seems important because Unity internally might not de-allocate some resources otherwise.
                            yield return null;

                            ++currentBatchCount;

                            if (UnityEditor.EditorUtility.DisplayCancelableProgressBar(
                                    strBakingTitle + $"Batch: {currentBatchCount}/{totalBatchCounts} ",
                                    "Performing sampling batches...", (currentBatchCount / (float)totalBatchCounts)))
                            {
                                // Just so we properly Dispose() them.
                                CompletePending(behaviour.BakeData, pending);

                                yield return new PerfectCullingBakeAbortedYieldInstruction();
                            }

                            lastSpeed = (Time.realtimeSinceStartup - lastTime) / (currentBatchCount - lastElement) /
                                        (float)baker.BatchCount;

                            averageSpeed = SMOOTHING_FACTOR * lastSpeed + (1 - SMOOTHING_FACTOR) * averageSpeed;

                            lastTime = Time.realtimeSinceStartup;
                            lastElement = currentBatchCount;
                        }
                    }

                    if (UnityEditor.EditorUtility.DisplayCancelableProgressBar($"Finishing pending batches",
                            "Finishing pending batches",
                            (currentBatchCount / (float)totalBatchCounts)))
                    {
                        // Just so we properly Dispose() them.
                        CompletePending(behaviour.BakeData, pending);

                        yield return new PerfectCullingBakeAbortedYieldInstruction();
                    }

                    CompletePending(behaviour.BakeData, pending);

                    sw.Stop();

                    PerfectCullingLogger.Log(
                        $"Bake time: {PerfectCullingEditorUtil.FormatSeconds(sw.ElapsedMilliseconds * 0.001f)} | {(sw.ElapsedMilliseconds / (float)localPositions.Count)} ms per sample");

                    if (PerfectCullingSettings.Instance.autoUpdateBakeAverageSamplingSpeedMs)
                    {
                        PerfectCullingSettings.Instance.bakeAverageSamplingSpeedMs =
                            (sw.ElapsedMilliseconds / (float)localPositions.Count);

                        UnityEditor.EditorUtility.SetDirty(PerfectCullingSettings.Instance);
                    }

                    if (UnityEditor.EditorUtility.DisplayCancelableProgressBar($"Performing post bake steps",
                            "Performing post bake steps",
                            (currentBatchCount / (float)totalBatchCounts)))
                    {
                        // Just so we properly Dispose() them.
                        CompletePending(behaviour.BakeData, pending);

                        yield return new PerfectCullingBakeAbortedYieldInstruction();
                    }

                    behaviour.PostBake();

                    foreach (PerfectCullingMonoGroup monoGroup in allMonoGroups)
                    {
                        monoGroup.PostBake(behaviour);
                    }

                    if (UnityEditor.EditorUtility.DisplayCancelableProgressBar($"Compressing data and finishing bake",
                            "Compressing data and finishing bake",
                            (currentBatchCount / (float)totalBatchCounts)))
                    {
                        // Just so we properly Dispose() them.
                        CompletePending(behaviour.BakeData, pending);

                        yield return new PerfectCullingBakeAbortedYieldInstruction();
                    }

                    behaviour.BakeData.CompleteBake();

                    behaviour.BakeData.strBakeDate = System.DateTime.UtcNow.ToString("o");
                    behaviour.BakeData.strRendererName = baker.RendererName;
                    behaviour.BakeData.bakeDurationMilliseconds = sw.ElapsedMilliseconds;
                }

                // Hash calculation needs to happen here to make sure we Disposed PerfectCullingSceneColor because it might have temporarily modified the BakeGroup
                behaviour.BakeData.bakeHash = behaviour.GetBakeHash();
                behaviour.BakeData.bakeCompleted = true;

                UnityEditor.EditorUtility.SetDirty(behaviour.BakeData);
                UnityEditor.AssetDatabase.SaveAssets();
            }
            finally
            {
                UnityEditor.EditorUtility.ClearProgressBar();
            }
#endif
        }
        
        private static void CompletePending(PerfectCullingBakeData BakeData, List<PerfectCullingBakeHandle> pending)
        {
            for (int k = 0; k < pending.Count; ++k)
            {
                pending[k].Handle.Complete();

                BakeData.SetRawData(pending[k].Index, pending[k].Handle.indices);
            }

            pending.Clear();
        }
    }
}