// =====================================================================
// Modified from ToolBuddy
// =====================================================================

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Chronos;
using Deform;
using FluffyUnderware.Curvy.Controllers;
using FluffyUnderware.Curvy.Generator;
using FluffyUnderware.Curvy.Generator.Modules;
using FluffyUnderware.Curvy.Shapes;
using FluffyUnderware.DevTools;
using Typooling;
using UnityEngine;
using UnityEngine.UI;

namespace FluffyUnderware.Curvy.Controllers
{
    public class OuroborosInfiniteTrack : MonoBehaviour
    {
        [Header("Track Settings")]
        public CurvySpline TrackSpline;
        private SplineController Controller;
        public Material RoadMaterial;

        [Header("Curvation Settings")]
        [Positive]
        public float CurvationX = 10;

        [Positive]
        public float CurvationY = 10;

        [Header("Control Point Settings")]
        [Positive]
        public float CPStepSize = 20;

        [Positive]
        public int HeadCP = 3;

        [Positive]
        public int TailCP = 2;

        [Header("Section Settings")]
        [DevTools.Min(3)]
        public int Sections = 6;

        [DevTools.Min(1)]
        public int SectionCPCount = 2;

        [Header("Offset Settings")]
        public float YOffset = 0;

        [Header("Ellipse Settings")]
        [SerializeField]
        private float ellipseRadiusX = 20;

        [SerializeField]
        private float ellipseRadiusY = 10;

        [Header("Prefab Generation")]
        [SerializeField]
        private bool enablePrefabGeneration = true;

        [Header("Pooling Settings")]
        [SerializeField] private Typooling.Pooler prefabPooler;
        [SerializeField] private int prefabsPerSection = 5;
        [SerializeField] private float prefabScale = 1f;
        [SerializeField] private int maxActivePrefabs = 50;
        private List<GameObject> activePrefabs;
        private List<List<GameObject>> prefabsBySection;
        private List<GameObject> despawnedPrefabs;
        private Dictionary<int, Transform> cachedTransforms = new Dictionary<int, Transform>();
        private Transform cachedTransform;
        private Dictionary<int, CurvyGenerator> generatorPool;

        [Header("Prefab Rotation Adjustments")]
        [SerializeField]
        private float minRandomXRotationSubtract = 80f;

        [SerializeField]
        private float maxRandomXRotationSubtract = 101f;

        [SerializeField]
        private float minRandomYRotation = -30f;

        [SerializeField]
        private float maxRandomYRotation = 31f;

        [SerializeField]
        private float minRandomZRotation = -30f;

        [SerializeField]
        private float maxRandomZRotation = 31f;

        [Header("Deformer Settings")]
        [SerializeField]
        private GameObject deformerPrefab;
        private List<Deformer> cachedDeformers;
        private bool useDeformers = false; 

        [Header("Performance Settings")]
        [SerializeField] private int updateInterval = 2; 
        [SerializeField] private bool batchOperations = true;
        private int frameCounter = 0;
        private Queue<System.Action> deferredOperations = new Queue<System.Action>();
        private bool isProcessingOperations = false;

        [Header("Generator Management")]
        [SerializeField] private bool useGeneratorPooling = true;
        [SerializeField] private int preWarmGeneratorCount = 10;
        private Dictionary<CurvyGenerator, GeneratorModules> cachedModules = new Dictionary<CurvyGenerator, GeneratorModules>();
        private Queue<CurvyGenerator> inactiveGenerators = new Queue<CurvyGenerator>();

        private class GeneratorModules
        {
            public InputSplinePath Path;
            public InputSplineShape Shape;
            public BuildShapeExtrusion Extrude;
            public BuildVolumeMesh Volume;
            public CreateMesh Mesh;
            public CSEllipse EllipseShape;
        }

        private int mInitState = 0;
        private bool mUpdateSpline;
        private int mUpdateIn;
        private CurvyGenerator[] mGenerators;
        private int mCurrentGen;
        private float lastSectionEndV;
        private Vector3 mDir;
        private readonly TimeMeasure timeSpline = new TimeMeasure(30);
        private readonly TimeMeasure timeCG = new TimeMeasure(1);

        [Header("Controller Placement Settings")]
        [Tooltip("Determines the offset for the player controller placement point.")]
        public int ControllerPlacementOffset = 6; 

        private bool isSplineReady = false;

        [Header("Player Movement")]
        [SerializeField]
        private MonoBehaviour playerMovement; 

        public void OnSplineReady()
        {
            isSplineReady = true;
            EnablePlayerMovement();
        }

        private void EnablePlayerMovement()
        {
            if (playerMovement != null && isSplineReady)
            {
                playerMovement.enabled = true;
            }
        }

        private void Awake()
        {
            // Cache transform component
            cachedTransform = transform;
            
            // Pre-allocate lists with capacity to avoid resizing
            activePrefabs = new List<GameObject>(maxActivePrefabs);
            prefabsBySection = new List<List<GameObject>>(Sections);
            despawnedPrefabs = new List<GameObject>(maxActivePrefabs);
            cachedTransforms = new Dictionary<int, Transform>(maxActivePrefabs);
            generatorPool = new Dictionary<int, CurvyGenerator>(Sections);

            // Initialize section lists
            for (int i = 0; i < Sections; i++)
            {
                prefabsBySection.Add(new List<GameObject>(prefabsPerSection));
            }

            SetupGeneratorPool();
        }

        private void CacheTransformIfNeeded(GameObject obj)
        {
            if (obj != null)
            {
                int instanceId = obj.GetInstanceID();
                if (!cachedTransforms.ContainsKey(instanceId))
                {
                    cachedTransforms[instanceId] = obj.transform;
                }
            }
        }

        private Transform GetCachedTransform(GameObject obj)
        {
            if (obj == null) return null;
            
            int instanceId = obj.GetInstanceID();
            Transform cachedTransform;
            if (!cachedTransforms.TryGetValue(instanceId, out cachedTransform))
            {
                cachedTransform = obj.transform;
                cachedTransforms[instanceId] = cachedTransform;
            }
            return cachedTransform;
        }

        void Start()
        {
            StartCoroutine(InitializeAsync());
        }

        private IEnumerator InitializeAsync()
        {
            if (useDeformers)
            {
                yield return StartCoroutine(CacheDeformersAsync());
            }

            FindAndAssignController();
            yield return StartCoroutine(ProgressiveSetup());
            
            OnSplineReady();
        }

        private IEnumerator ProgressiveSetup()
        {
            mInitState = 1;
            
            if (Controller == null)
            {
                FindAndAssignController();
                if (Controller == null)
                {
                    ConditionalDebug.LogError("Failed to find and assign the SplineController. Setup cannot continue.");
                    yield break;
                }
            }

            mGenerators = new CurvyGenerator[Sections];
            
            TrackSpline.InsertAfter(null, Vector3.zero, true);
            mDir = Vector3.forward;
            
            int initialAlignedPoints = 5;
            for (int i = 0; i < initialAlignedPoints; i++)
            {
                addAlignedTrackCP();
            }
            
            int remainingPoints = TailCP + HeadCP + Sections * SectionCPCount - initialAlignedPoints;
            for (int i = 0; i < remainingPoints; i++)
            {
                addTrackCP();
            }
            
            TrackSpline.Refresh();
            yield return null;
            
            for (int i = 0; i < Sections; i++)
            {
                prefabsBySection.Add(new List<GameObject>());
            }
            
            yield return StartCoroutine(BuildGeneratorsProgressivelyAsync());
            
            if (useDeformers)
            {
                AddDeformableComponents();
            }

            AlignSplineWithWorldUp();

            mInitState = 2;
            mUpdateIn = SectionCPCount;
            
            Controller.AbsolutePosition = TrackSpline.ControlPointsList[TailCP + ControllerPlacementOffset].Distance;
        }

        private IEnumerator BuildGeneratorsProgressivelyAsync()
        {
            for (int i = 0; i < Sections; i++)
            {
                mGenerators[i] = buildGenerator();
                mGenerators[i].name = "Generator " + i;
            }
            
            yield return new WaitUntil(() => {
                for (int i = 0; i < Sections; i++)
                {
                    if (!mGenerators[i].IsInitialized) return false;
                }
                return true;
            });
            
            for (int i = 0; i < Sections; i++)
            {
                StartCoroutine(
                    UpdateGeneratorCoroutine(
                        mGenerators[i],
                        i * SectionCPCount + TailCP,
                        (i + 1) * SectionCPCount + TailCP
                    )
                );
            }
            
            yield return null;
        }

        private IEnumerator UpdateGeneratorCoroutine(CurvyGenerator gen, int startCP, int endCP)
        {
            if (gen == null || !cachedModules.TryGetValue(gen, out var modules)) yield break;

            try
            {
                modules.Path.SetRange(
                    TrackSpline.ControlPointsList[startCP],
                    TrackSpline.ControlPointsList[endCP]
                );

                modules.Volume.MaterialSettings[0].UVOffset.y = lastSectionEndV % 1;
                
                gen.Refresh();
                yield return new WaitUntil(() => gen.IsInitialized);

                var vmesh = modules.Volume.OutVMesh.GetData<CGVMesh>();
                if (vmesh != null && vmesh.UVs != null && vmesh.Count > 0)
                {
                    lastSectionEndV = vmesh.UVs.Array[vmesh.Count - 1].y;
                }

                PlacePrefabsOnSectionSurface(TrackSpline, mCurrentGen);
            }
            finally
            {
                if (useDeformers)
                {
                    AddDeformableComponentsToGenerator(gen);
                }
            }
        }

        void FindAndAssignController()
        {
            GameObject playerPlane = GameObject.FindGameObjectWithTag("PlayerPlane");
            if (playerPlane != null)
            {
                Controller = playerPlane.GetComponent<SplineController>();
                if (Controller != null)
                {
                    Controller.OnControlPointReached.AddListener(Track_OnControlPointReached);
                }
                else
                {
                    ConditionalDebug.LogError(
                        "SplineController component not found on the PlayerPlane object."
                    );
                }
            }
            else
            {
                ConditionalDebug.LogError(
                    "GameObject with tag 'PlayerPlane' not found in the scene."
                );
            }
        }

        private IEnumerator CacheDeformersAsync()
        {
            if (deformerPrefab == null) yield break;

            const int batchSize = 5; 
            int processed = 0;

            while (processed < Sections)
            {
                int currentBatch = Mathf.Min(batchSize, Sections - processed);
                for (int i = 0; i < currentBatch; i++)
                {
                    if (deformerPrefab.TryGetComponent<Deformer>(out var deformer))
                    {
                        cachedDeformers.Add(deformer);
                    }
                    processed++;
                }
                yield return null; 
            }
        }

        private void SetupGeneratorPool()
        {
            if (!useGeneratorPooling) return;

            for (int i = 0; i < preWarmGeneratorCount; i++)
            {
                var gen = CreateGenerator();
                if (gen != null)
                {
                    gen.gameObject.SetActive(false);
                    inactiveGenerators.Enqueue(gen);
                }
            }
        }

        private CurvyGenerator GetOrCreateGenerator()
        {
            if (useGeneratorPooling && inactiveGenerators.Count > 0)
            {
                var gen = inactiveGenerators.Dequeue();
                gen.gameObject.SetActive(true);
                return gen;
            }

            return CreateGenerator();
        }

        private void ReturnGeneratorToPool(CurvyGenerator gen)
        {
            if (!useGeneratorPooling || gen == null) return;

            gen.gameObject.SetActive(false);
            inactiveGenerators.Enqueue(gen);
        }

        private CurvyGenerator CreateGenerator()
        {
            var gen = CurvyGenerator.Create();
            gen.AutoRefresh = false;
            gen.transform.SetParent(cachedTransform, false);

            var modules = new GeneratorModules
            {
                Path = gen.AddModule<InputSplinePath>(),
                Shape = gen.AddModule<InputSplineShape>(),
                Extrude = gen.AddModule<BuildShapeExtrusion>(),
                Volume = gen.AddModule<BuildVolumeMesh>(),
                Mesh = gen.AddModule<CreateMesh>()
            };

            // Setup module links
            modules.Path.OutputByName["Path"].LinkTo(modules.Extrude.InputByName["Path"]);
            modules.Shape.OutputByName["Shape"].LinkTo(modules.Extrude.InputByName["Cross"]);
            modules.Extrude.OutputByName["Volume"].LinkTo(modules.Volume.InputByName["Volume"]);
            modules.Volume.OutputByName["VMesh"].LinkTo(modules.Mesh.InputByName["VMesh"]);

            // Configure base settings
            modules.Path.UseCache = true;
            modules.EllipseShape = modules.Shape.SetManagedShape<CSEllipse>();
            modules.Extrude.Optimize = false;
            modules.Volume.Split = false;
            modules.Mesh.Collider = CGColliderEnum.Mesh;
            modules.Mesh.Layer = LayerMask.NameToLayer("Ground");
            gen.gameObject.layer = LayerMask.NameToLayer("Ground");

            cachedModules[gen] = modules;
            return gen;
        }

        CurvyGenerator buildGenerator()
        {
            var gen = GetOrCreateGenerator();
            if (gen == null) return null;

            GeneratorModules modules;
            if (!cachedModules.TryGetValue(gen, out modules))
            {
                ConditionalDebug.LogError("Generator modules not found in cache");
                return gen;
            }

            // Configure modules with current settings
            modules.Path.Spline = TrackSpline;
            modules.EllipseShape.RadiusX = ellipseRadiusX;
            modules.EllipseShape.RadiusY = ellipseRadiusY;
            modules.EllipseShape.YOffset = YOffset;

            modules.Volume.SetMaterial(0, RoadMaterial);
            modules.Volume.MaterialSettings[0].SwapUV = true;

            return gen;
        }

        void FixedUpdate()
        {
            if (mInitState == 0)
            {
                StartCoroutine(InitializeAsync());
                return;
            }

            frameCounter++;
            if (frameCounter % updateInterval != 0) return;
            frameCounter = 0;

            if (mInitState == 2 && mUpdateSpline)
            {
                if (batchOperations)
                {
                    advanceTrack();
                }
                else
                {
                    advanceTrack();
                }
            }

            if (useDeformers)
            {
                AddDeformableComponents();
            }

            if (activePrefabs.Count > maxActivePrefabs)
            {
                DespawnOldestPrefabs();
            }

            ProcessDeferredOperations();
        }

        private void ProcessDeferredOperations()
        {
            if (isProcessingOperations || deferredOperations.Count == 0) return;

            isProcessingOperations = true;

            try
            {
                while (deferredOperations.Count > 0)
                {
                    var operation = deferredOperations.Dequeue();
                    operation?.Invoke();
                }
            }
            finally
            {
                isProcessingOperations = false;
            }
        }

        private void EnqueueOperation(System.Action operation)
        {
            if (batchOperations)
            {
                deferredOperations.Enqueue(operation);
            }
            else
            {
                operation?.Invoke();
            }
        }

        void advanceTrack()
        {
            if (!TrackSpline || !Controller) return;

            timeSpline.Start();
            float pos = Controller.AbsolutePosition;

            try
            {
                // Remove oldest section's CP
                for (int i = 0; i < SectionCPCount; i++)
                {
                    if (TrackSpline.ControlPointCount <= 0) break;
                    pos -= TrackSpline.ControlPointsList[0].Length;
                    TrackSpline.Delete(TrackSpline.ControlPointsList[0], false);
                }

                // Add new section's CP
                for (int i = 0; i < SectionCPCount; i++)
                {
                    addTrackCP(false);
                }

                TrackSpline.Refresh();
            }
            finally
            {
                Controller.AbsolutePosition = pos;
                mUpdateSpline = false;
                timeSpline.Stop();
            }

            // Handle prefab cleanup in a deferred manner
            StartCoroutine(DeferredPrefabCleanup());
        }

        private IEnumerator DeferredPrefabCleanup()
        {
            if (prefabsBySection.Count > 0 && prefabsBySection[0] != null)
            {
                var prefabsToClean = new List<GameObject>(prefabsBySection[0]);
                int batchSize = 10;
                
                for (int i = 0; i < prefabsToClean.Count; i += batchSize)
                {
                    int currentBatchSize = Mathf.Min(batchSize, prefabsToClean.Count - i);
                    for (int j = 0; j < currentBatchSize; j++)
                    {
                        var prefab = prefabsToClean[i + j];
                        if (prefab != null)
                        {
                            if (prefab.TryGetComponent<Instance>(out var instanceComponent))
                            {
                                instanceComponent.Despawn();
                                despawnedPrefabs.Add(prefab);
                            }
                        }
                    }
                    yield return null;
                }

                prefabsBySection[0].Clear();
            }

            advanceSections();
        }

        void advanceSections()
        {
            CurvyGenerator cur = mGenerators[mCurrentGen++];
            int num = TrackSpline.ControlPointCount - HeadCP - 1;
            StartCoroutine(UpdateGeneratorCoroutine(cur, num - SectionCPCount, num));

            if (mCurrentGen == Sections)
                mCurrentGen = 0;
        }

        public void Track_OnControlPointReached(CurvySplineMoveEventArgs e)
        {
            if (--mUpdateIn == 0)
            {
                mUpdateSpline = true;
                mUpdateIn = SectionCPCount;
            }
        }

        void PlacePrefabsOnSectionSurface(CurvySpline spline, int sectionIndex)
        {
            if (!enablePrefabGeneration || prefabPooler == null)
            {
                return;
            }

            if (sectionIndex < 0 || sectionIndex >= prefabsBySection.Count)
            {
                ConditionalDebug.LogError($"Section index {sectionIndex} is out of range. Prefabs cannot be placed.");
                return;
            }

            var placedPositions = new List<float>(prefabsPerSection);
            const float minDistanceApart = 3f;
            float sectionStartT = (float)sectionIndex / Sections;
            float sectionEndT = (float)(sectionIndex + 1) / Sections;

            for (int i = 0; i < prefabsPerSection; i++)
            {
                bool placed = false;
                int attempts = 0;
                while (!placed && attempts < 100)
                {
                    attempts++;
                    float t = Mathf.Lerp(sectionStartT, sectionEndT, Random.value);
                    Vector3 potentialPosition = spline.Interpolate(t);

                    bool tooClose = false;
                    for (int j = 0; j < placedPositions.Count; j++)
                    {
                        if (Vector3.Distance(spline.Interpolate(placedPositions[j]), potentialPosition) < minDistanceApart)
                        {
                            tooClose = true;
                            break;
                        }
                    }

                    if (!tooClose)
                    {
                        GameObject prefabInstance = prefabPooler.GetFromPool();
                        if (prefabInstance == null) continue;

                        Transform prefabTransform = GetCachedTransform(prefabInstance);
                        if (prefabTransform == null) continue;

                        Quaternion rotation = spline.GetOrientationFast(t);
                        float randomXRotationSubtract = Random.Range(minRandomXRotationSubtract, maxRandomXRotationSubtract);
                        float randomYRotation = Random.Range(minRandomYRotation, maxRandomYRotation);
                        float randomZRotation = Random.Range(minRandomZRotation, maxRandomZRotation);
                        rotation *= Quaternion.Euler(-randomXRotationSubtract, randomYRotation, randomZRotation);

                        Vector3 sideDirection = (Random.value > 0.5f) ? Vector3.right : Vector3.left;
                        Vector3 offsetDirection = rotation * sideDirection;
                        float sideOffset = Random.Range(7f, 12f) * ((Random.value > 0.5f) ? 1 : -1);
                        
                        prefabTransform.position = potentialPosition + offsetDirection * sideOffset;
                        prefabTransform.rotation = rotation;
                        prefabTransform.localScale = Vector3.one * prefabScale;
                        prefabTransform.SetParent(cachedTransform, true);

                        prefabsBySection[sectionIndex].Add(prefabInstance);
                        activePrefabs.Add(prefabInstance);
                        placedPositions.Add(t);
                        placed = true;
                    }
                }
            }
        }

        void DespawnOldestPrefabs()
        {
            while (activePrefabs.Count > maxActivePrefabs)
            {
                GameObject oldestPrefab = activePrefabs[0];
                Instance instanceComponent = oldestPrefab.GetComponent<Instance>();
                if (instanceComponent != null)
                {
                    instanceComponent.Despawn(); 
                    activePrefabs.RemoveAt(0); 
                }
                else
                {
                    Destroy(oldestPrefab);
                    activePrefabs.RemoveAt(0); 
                }
            }
        }

        public void ClearDespawnedPrefabsList()
        {
            despawnedPrefabs.Clear();
        }

        void OnDisable()
        {
            if (Controller != null)
            {
                Controller.OnControlPointReached.RemoveListener(Track_OnControlPointReached);
            }

            // Clean up generator pool
            foreach (var gen in inactiveGenerators)
            {
                if (gen != null)
                {
                    Destroy(gen.gameObject);
                }
            }
            inactiveGenerators.Clear();
            cachedModules.Clear();
        }

        void AddDeformableComponents()
        {
            if (!useDeformers) return;

            foreach (var generator in mGenerators)
            {
                AddDeformableComponentsToGenerator(generator);
            }
        }

        void AddDeformableComponentsToGenerator(CurvyGenerator generator)
        {
            if (!useDeformers) return;

            List<CreateMesh> createMeshModules = generator.FindModules<CreateMesh>(true);

            foreach (var createMeshModule in createMeshModules)
            {
                if (createMeshModule != null)
                {
                    foreach (var meshResource in createMeshModule.Meshes.Items)
                    {
                        if (meshResource != null && !meshResource.GetComponent<Deformable>())
                        {
                            Deformable deformable =
                                meshResource.gameObject.AddComponent<Deformable>();
                            deformable.UpdateMode = UpdateMode.Auto;
                            deformable.NormalsRecalculation = NormalsRecalculation.Auto;
                            deformable.BoundsRecalculation = BoundsRecalculation.Auto;

                            if (deformerPrefab != null)
                            {
                                foreach (var deformer in cachedDeformers)
                                {
                                    deformable.AddDeformer(deformer);
                                }
                            }
                            else
                            {
                                ConditionalDebug.LogWarning(
                                    "Deformer prefab is not assigned in the inspector."
                                );
                            }
                        }
                    }
                }
            }
        }

        void AlignSplineWithWorldUp()
        {
            bool anyMisaligned = false;
            foreach (var cp in TrackSpline.ControlPointsList)
            {
                if (Vector3.Dot(cp.transform.up, Vector3.up) < 0.99f)
                {
                    cp.transform.rotation = Quaternion.LookRotation(cp.transform.forward, Vector3.up);
                    anyMisaligned = true;
                }
            }
            
            if (anyMisaligned)
            {
                ConditionalDebug.LogWarning("Some control points required up vector alignment");
                TrackSpline.Refresh();
            }
        }

        void addAlignedTrackCP()
        {
            Vector3 p = TrackSpline.ControlPointsList[TrackSpline.ControlPointCount - 1].transform.localPosition;
            Vector3 position = TrackSpline.transform.localToWorldMatrix.MultiplyPoint3x4(p + Vector3.forward * CPStepSize);

            CurvySplineSegment newControlPoint = TrackSpline.InsertAfter(null, position, true);

            newControlPoint.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);

            TrackSpline.Refresh();

            if ((TrackSpline.ControlPointCount - 1 - TailCP) % SectionCPCount == 0)
                newControlPoint.SerializedOrientationAnchor = true;
        }

        void addTrackCP(bool refresh = true)
        {
            Vector3 p = TrackSpline.ControlPointsList[TrackSpline.ControlPointCount - 1].transform.localPosition;
            Vector3 position = TrackSpline.transform.localToWorldMatrix.MultiplyPoint3x4(p + mDir * CPStepSize);

            float rndX = Random.value * CurvationX * DTUtility.RandomSign();
            float rndY = Random.value * CurvationY * DTUtility.RandomSign();
            
            Vector3 newDir = Vector3.ProjectOnPlane(
                Quaternion.Euler(rndX, rndY, 0) * mDir,
                Vector3.up
            ).normalized;
            mDir = newDir;

            CurvySplineSegment newControlPoint = TrackSpline.InsertAfter(null, position, refresh);
            
            Quaternion targetRotation = Quaternion.LookRotation(mDir, Vector3.up);
            newControlPoint.transform.rotation = targetRotation;

            if (refresh)
            {
                TrackSpline.Refresh();
            }

            if ((TrackSpline.ControlPointCount - 1 - TailCP) % SectionCPCount == 0)
            {
                newControlPoint.SerializedOrientationAnchor = true;
            }
        }
    }
}