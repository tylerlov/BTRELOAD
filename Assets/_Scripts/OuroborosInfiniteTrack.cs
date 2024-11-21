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
        [SerializeField]
        private Typooling.Pooler prefabPooler;

        [SerializeField]
        private int prefabsPerSection = 5;

        [SerializeField]
        private float prefabScale = 1f;

        [SerializeField]
        private int maxActivePrefabs = 50;
        private List<GameObject> activePrefabs;
        private List<List<GameObject>> prefabsBySection;
        private List<GameObject> despawnedPrefabs;

        [SerializeField]
        private List<GameObject> despawnedPrefabsList = new List<GameObject>();

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
        private bool useDeformers = false; // New flag to check if deformers should be used

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
        public int ControllerPlacementOffset = 6; // Default value is 6

        private bool isSplineReady = false;

        [Header("Player Movement")]
        [SerializeField]
        private MonoBehaviour playerMovement; // Add this field

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
            // Pre-allocate lists with capacity to avoid resizing
            activePrefabs = new List<GameObject>(maxActivePrefabs);
            prefabsBySection = new List<List<GameObject>>(Sections);
            despawnedPrefabs = new List<GameObject>(maxActivePrefabs);
            cachedDeformers = new List<Deformer>(Sections);

            // Initialize section lists
            for (int i = 0; i < Sections; i++)
            {
                prefabsBySection.Add(new List<GameObject>(prefabsPerSection));
            }
        }

        void Start()
        {
            StartCoroutine(InitializeAsync());
        }

        private IEnumerator InitializeAsync()
        {
            // Split initialization across multiple frames
            if (useDeformers)
            {
                yield return StartCoroutine(CacheDeformersAsync());
            }

            FindAndAssignController();
            yield return StartCoroutine(ProgressiveSetup());
            
            OnSplineReady();
        }

        // Progressive setup broken down into smaller operations
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

            // Initialize generators array
            mGenerators = new CurvyGenerator[Sections];
            
            // 1. Initial spline setup - do this in one batch
            TrackSpline.InsertAfter(null, Vector3.zero, true);
            mDir = Vector3.forward;
            
            // Add initial aligned points
            int initialAlignedPoints = 5;
            for (int i = 0; i < initialAlignedPoints; i++)
            {
                addAlignedTrackCP();
            }
            
            // Add remaining points
            int remainingPoints = TailCP + HeadCP + Sections * SectionCPCount - initialAlignedPoints;
            for (int i = 0; i < remainingPoints; i++)
            {
                addTrackCP();
            }
            
            TrackSpline.Refresh();
            yield return null;
            
            // 2. Initialize section lists
            for (int i = 0; i < Sections; i++)
            {
                prefabsBySection.Add(new List<GameObject>());
            }
            
            // 3. Build and initialize generators
            yield return StartCoroutine(BuildGeneratorsProgressivelyAsync());
            
            // 4. Final setup
            if (useDeformers)
            {
                AddDeformableComponents();
            }

            AlignSplineWithWorldUp();

            mInitState = 2;
            mUpdateIn = SectionCPCount;
            
            // Place controller
            Controller.AbsolutePosition = TrackSpline.ControlPointsList[TailCP + ControllerPlacementOffset].Distance;
        }

        private IEnumerator BuildGeneratorsProgressivelyAsync()
        {
            // Build all generators at once since splitting them causes more overhead
            for (int i = 0; i < Sections; i++)
            {
                mGenerators[i] = buildGenerator();
                mGenerators[i].name = "Generator " + i;
            }
            
            // Wait for all generators to initialize in one batch
            yield return new WaitUntil(() => {
                for (int i = 0; i < Sections; i++)
                {
                    if (!mGenerators[i].IsInitialized) return false;
                }
                return true;
            });
            
            // Update all generators at once
            for (int i = 0; i < Sections; i++)
            {
                StartCoroutine(updateSectionGenerator(
                    mGenerators[i],
                    i * SectionCPCount + TailCP,
                    (i + 1) * SectionCPCount + TailCP
                ));
            }
            
            // Wait one frame for generators to start updating
            yield return null;
        }

        private IEnumerator UpdateSectionGeneratorsProgressivelyAsync()
        {
            // Since we're already handling generator updates in BuildGeneratorsProgressivelyAsync
            yield break;
        }

        private IEnumerator InitializeSplineAsync()
        {
            // Add the start CP to the spline
            TrackSpline.InsertAfter(null, Vector3.zero, true);
            mDir = Vector3.forward;
            
            const int pointsPerFrame = 3;
            int processedPoints = 0;
            
            // Add initial aligned points
            int initialAlignedPoints = 5;
            while (processedPoints < initialAlignedPoints)
            {
                int pointsThisFrame = Mathf.Min(pointsPerFrame, initialAlignedPoints - processedPoints);
                for (int i = 0; i < pointsThisFrame; i++)
                {
                    addAlignedTrackCP();
                    processedPoints++;
                }
                yield return null;
            }
            
            // Add remaining points
            int remainingPoints = TailCP + HeadCP + Sections * SectionCPCount - initialAlignedPoints;
            while (processedPoints < initialAlignedPoints + remainingPoints)
            {
                int pointsThisFrame = Mathf.Min(pointsPerFrame, (initialAlignedPoints + remainingPoints) - processedPoints);
                for (int i = 0; i < pointsThisFrame; i++)
                {
                    addTrackCP();
                    processedPoints++;
                }
                yield return null;
            }
            
            TrackSpline.Refresh();
            yield return null;
        }

        private IEnumerator InitializeSectionListsAsync()
        {
            const int sectionsPerFrame = 5;
            for (int i = 0; i < Sections; i += sectionsPerFrame)
            {
                int count = Mathf.Min(sectionsPerFrame, Sections - i);
                for (int j = 0; j < count; j++)
                {
                    prefabsBySection.Add(new List<GameObject>());
                }
                yield return null;
            }
        }

        private IEnumerator FinalizeSetupAsync()
        {
            if (useDeformers)
            {
                AddDeformableComponents();
                yield return null;
            }

            AlignSplineWithWorldUp();
            yield return null;

            mInitState = 2;
            mUpdateIn = SectionCPCount;
            
            // Place controller
            Controller.AbsolutePosition = TrackSpline.ControlPointsList[TailCP + ControllerPlacementOffset].Distance;
        }

        void FindAndAssignController()
        {
            GameObject playerPlane = GameObject.FindGameObjectWithTag("PlayerPlane");
            if (playerPlane != null)
            {
                Controller = playerPlane.GetComponent<SplineController>();
                if (Controller != null)
                {
                    // Register the OnControlPointReached event
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

            const int batchSize = 5; // Process 5 deformers per frame
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
                yield return null; // Wait for next frame
            }
        }

        void FixedUpdate()
        {
            if (mInitState == 0)
                StartCoroutine(nameof(setup));

            if (mInitState == 2 && mUpdateSpline)
                advanceTrack();

            // Only add Deformable components if deformers are being used
            if (useDeformers)
            {
                AddDeformableComponents();
            }

            // Check if the number of active prefabs exceeds the limit
            if (activePrefabs.Count > maxActivePrefabs)
            {
                DespawnOldestPrefabs();
            }
        }

        // setup everything
        IEnumerator setup()
        {
            mInitState = 1;

            if (Controller == null)
            {
                FindAndAssignController();
                if (Controller == null)
                {
                    ConditionalDebug.LogError(
                        "Failed to find and assign the SplineController. Setup cannot continue."
                    );
                    yield break;
                }
            }

            mGenerators = new CurvyGenerator[Sections];

            // Add the start CP to the spline
            TrackSpline.InsertAfter(null, Vector3.zero, true);
            mDir = Vector3.forward;

            // Ensure the first few control points are aligned with world up
            int initialAlignedPoints = 5; // Adjust this number as needed
            for (int i = 0; i < initialAlignedPoints; i++)
            {
                addAlignedTrackCP();
            }

            // Continue with the rest of the control points
            int remainingPoints = TailCP + HeadCP + Sections * SectionCPCount - initialAlignedPoints;
            for (int i = 0; i < remainingPoints; i++)
                addTrackCP();

            TrackSpline.Refresh();

            for (int i = 0; i < Sections; i++)
            {
                prefabsBySection.Add(new List<GameObject>());
            }

            // build Curvy Generators
            for (int i = 0; i < Sections; i++)
            {
                mGenerators[i] = buildGenerator();
                mGenerators[i].name = "Generator " + i;
            }
            // and wait until they're initialized
            for (int i = 0; i < Sections; i++)
                yield return new WaitUntil(() => mGenerators[i].IsInitialized);

            // let all generators do their extrusion
            for (int i = 0; i < Sections; i++)
                StartCoroutine(
                    updateSectionGenerator(
                        mGenerators[i],
                        i * SectionCPCount + TailCP,
                        (i + 1) * SectionCPCount + TailCP
                    )
                );

            AddDeformableComponents();

            // Align spline with world up after setup
            AlignSplineWithWorldUp();

            mInitState = 2;
            mUpdateIn = SectionCPCount;
            
            // Placement of the controller
            Controller.AbsolutePosition = TrackSpline
                .ControlPointsList[TailCP + ControllerPlacementOffset]
                .Distance;
        }

        // build a generator
        CurvyGenerator buildGenerator()
        {
            // Create the Curvy Generator
            CurvyGenerator gen = CurvyGenerator.Create();
            gen.AutoRefresh = false;

            // Set the parent of the generator to this GameObject
            gen.transform.SetParent(this.transform, false);

            // Create Modules
            InputSplinePath path = gen.AddModule<InputSplinePath>();
            InputSplineShape shape = gen.AddModule<InputSplineShape>();
            BuildShapeExtrusion extrude = gen.AddModule<BuildShapeExtrusion>();
            BuildVolumeMesh vol = gen.AddModule<BuildVolumeMesh>();
            CreateMesh msh = gen.AddModule<CreateMesh>();
            // Create Links between modules
            path.OutputByName["Path"].LinkTo(extrude.InputByName["Path"]);
            shape.OutputByName["Shape"].LinkTo(extrude.InputByName["Cross"]);
            extrude.OutputByName["Volume"].LinkTo(vol.InputByName["Volume"]);
            vol.OutputByName["VMesh"].LinkTo(msh.InputByName["VMesh"]);
            // Set module properties
            path.Spline = TrackSpline;
            path.UseCache = true;
            // Assuming CSEllipse exists and can be used similarly to CSRectangle
            CSEllipse ellipseShape = shape.SetManagedShape<CSEllipse>();
            ellipseShape.RadiusX = ellipseRadiusX; // Use the field value
            ellipseShape.RadiusY = ellipseRadiusY; // Use the field value
            ellipseShape.YOffset = YOffset; // Use the YOffset defined in the inspector
            extrude.Optimize = false;
#pragma warning disable 618
            extrude.CrossHardEdges = true; // You might want to set this to false for a smoother snake body
#pragma warning restore 618
            vol.Split = false;
            vol.SetMaterial(0, RoadMaterial); // Ensure your RoadMaterial has a snake-like texture
            vol.MaterialSettings[0].SwapUV = true;

            msh.Collider = CGColliderEnum.Mesh;
            msh.Layer = LayerMask.NameToLayer("Ground"); // Add this line to set the layer of the generated mesh to 'Ground'
            gen.gameObject.layer = LayerMask.NameToLayer("Ground"); // Set the generator's GameObject layer to 'Ground' as well

            return gen;
        }

        // advance the track
        void advanceTrack()
        {
            timeSpline.Start();

            float pos = Controller.AbsolutePosition;
            // Remove oldest section's CP
            for (int i = 0; i < SectionCPCount; i++)
            {
                pos -= TrackSpline.ControlPointsList[0].Length; // Update controller's position, so the ship won't jump
                TrackSpline.Delete(TrackSpline.ControlPointsList[0], true);
            }
            // Add new section's CP
            for (int i = 0; i < SectionCPCount; i++)
                addTrackCP();
            // Refresh the spline, so orientation will be auto-calculated
            TrackSpline.Refresh();

            // Set the controller to the old position
            Controller.AbsolutePosition = pos;
            mUpdateSpline = false;
            timeSpline.Stop();

            // Despawn prefabs from the first section
            foreach (GameObject prefab in prefabsBySection[0])
            {
                Instance instanceComponent = prefab.GetComponent<Instance>();
                if (instanceComponent != null)
                {
                    instanceComponent.Despawn();
                    despawnedPrefabs.Add(prefab);
                }
                else
                {
                    ConditionalDebug.LogWarning(
                        "Prefab does not have an Instance component, cannot despawn: " + prefab.name
                    );
                }
            }

            prefabsBySection[0].Clear(); // Clear the list after despawning

            advanceSections();
        }

        // update all CGs
        void advanceSections()
        {
            // set oldest CG to render path for new section
            CurvyGenerator cur = mGenerators[mCurrentGen++];
            int num = TrackSpline.ControlPointCount - HeadCP - 1;
            StartCoroutine(updateSectionGenerator(cur, num - SectionCPCount, num));

            if (mCurrentGen == Sections)
                mCurrentGen = 0;
        }

        // set a CG to render only a portion of a spline
        IEnumerator updateSectionGenerator(CurvyGenerator gen, int startCP, int endCP)
        {
            // Set Track segment we want to use
            InputSplinePath path = gen.FindModules<InputSplinePath>(true)[0];

            path.SetRange(
                TrackSpline.ControlPointsList[startCP],
                TrackSpline.ControlPointsList[endCP]
            );

            // Set UV-Offset to match
            BuildVolumeMesh vol = gen.FindModules<BuildVolumeMesh>(false)[0];
            vol.MaterialSettings[0].UVOffset.y = lastSectionEndV % 1;
            timeCG.Start();
            gen.Refresh();
            yield return new WaitUntil(() => gen.IsInitialized); // Wait for the generator to be fully initialized

            // fetch the ending V to be used by next section
            CGVMesh vmesh = vol.OutVMesh.GetData<CGVMesh>();
            lastSectionEndV = vmesh.UVs.Array[vmesh.Count - 1].y;

            // After generator refresh, place prefabs on the surface
            PlacePrefabsOnSectionSurface(TrackSpline, mCurrentGen); // Assuming mCurrentGen is the current section index
            timeCG.Stop();

            // Add this line to add Deformable components after the generator is refreshed
            AddDeformableComponentsToGenerator(gen);
        }

        // while we travel past CP's, we update the track
        public void Track_OnControlPointReached(CurvySplineMoveEventArgs e)
        {
            if (--mUpdateIn == 0)
            {
                mUpdateSpline = true;
                mUpdateIn = SectionCPCount;
            }
        }

        // add more CP's, rotating path by random angles
        void addTrackCP()
        {
            Vector3 p = TrackSpline.ControlPointsList[TrackSpline.ControlPointCount - 1].transform.localPosition;
            Vector3 position = TrackSpline.transform.localToWorldMatrix.MultiplyPoint3x4(p + mDir * CPStepSize);

            float rndX = Random.value * CurvationX * DTUtility.RandomSign();
            float rndY = Random.value * CurvationY * DTUtility.RandomSign();
            
            // Calculate new direction while maintaining world up
            Vector3 newDir = Vector3.ProjectOnPlane(
                Quaternion.Euler(rndX, rndY, 0) * mDir,
                Vector3.up
            ).normalized;
            mDir = newDir;

            CurvySplineSegment newControlPoint = TrackSpline.InsertAfter(null, position, true);
            
            // Set rotation ensuring up vector alignment
            Quaternion targetRotation = Quaternion.LookRotation(mDir, Vector3.up);
            newControlPoint.transform.rotation = targetRotation;

            TrackSpline.Refresh();

            if ((TrackSpline.ControlPointCount - 1 - TailCP) % SectionCPCount == 0)
                newControlPoint.SerializedOrientationAnchor = true;
        }

        void PlacePrefabsOnSectionSurface(CurvySpline spline, int sectionIndex)
        {
            if (!enablePrefabGeneration)
            {
                return;
            }

            if (sectionIndex < 0 || sectionIndex >= prefabsBySection.Count)
            {
                ConditionalDebug.LogError(
                    $"Section index {sectionIndex} is out of range. Prefabs cannot be placed."
                );
                return;
            }

            List<float> placedPositions = new List<float>();
            float minDistanceApart = 3f;
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

                    bool tooClose = placedPositions.Any(p =>
                        Vector3.Distance(spline.Interpolate(p), potentialPosition)
                        < minDistanceApart
                    );

                    if (!tooClose)
                    {
                        Quaternion rotation = spline.GetOrientationFast(t);

                        float randomXRotationSubtract = Random.Range(
                            minRandomXRotationSubtract,
                            maxRandomXRotationSubtract
                        );
                        float randomYRotation = Random.Range(
                            minRandomYRotation,
                            maxRandomYRotation
                        );
                        float randomZRotation = Random.Range(
                            minRandomZRotation,
                            maxRandomZRotation
                        );
                        Quaternion rotationAdjustment = Quaternion.Euler(
                            -randomXRotationSubtract,
                            randomYRotation,
                            randomZRotation
                        );
                        rotation *= rotationAdjustment;

                        Vector3 sideDirection =
                            (Random.value > 0.5f) ? Vector3.right : Vector3.left;
                        Vector3 offsetDirection = rotation * sideDirection;

                        float sideOffset = Random.Range(7f, 12f) * ((Random.value > 0.5f) ? 1 : -1);
                        Vector3 finalPosition = potentialPosition + offsetDirection * sideOffset;

                        GameObject prefabInstance = prefabPooler.GetFromPool();
                        prefabInstance.transform.position = finalPosition;
                        prefabInstance.transform.rotation = rotation;
                        prefabInstance.transform.localScale = Vector3.one * prefabScale;

                        prefabInstance.transform.SetParent(this.transform, true);

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
                    instanceComponent.Despawn(); // Despawn using the Instance component
                    activePrefabs.RemoveAt(0); // Remove the despawned prefab from the list
                }
                else
                {
                    // Fallback in case the prefab doesn't have an Instance component
                    Destroy(oldestPrefab);
                    activePrefabs.RemoveAt(0); // Remove the destroyed prefab from the list
                }
            }
        }

        // Ensure there's a way to clear the list when necessary, for example, at the start of the game or level.
        public void ClearDespawnedPrefabsList()
        {
            despawnedPrefabs.Clear();
        }

        void OnDisable()
        {
            // Unregister the event when the script is disabled or destroyed
            if (Controller != null)
            {
                Controller.OnControlPointReached.RemoveListener(Track_OnControlPointReached);
            }
        }

        private void AddDeformableComponents()
        {
            if (!useDeformers) return;

            foreach (var generator in mGenerators)
            {
                AddDeformableComponentsToGenerator(generator);
            }
        }

        private void AddDeformableComponentsToGenerator(CurvyGenerator generator)
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

                            // Add the cached Deformers to the Deformable component
                            if (deformerPrefab != null)
                            {
                                foreach (var deformer in cachedDeformers)
                                {
                                    deformable.AddDeformer(deformer);
                                }
                            }
                            else
                            {
                                Debug.LogWarning(
                                    "Deformer prefab is not assigned in the inspector."
                                );
                            }
                        }
                    }
                }
            }
        }

        private void CacheDeformers()
        {
            if (deformerPrefab != null)
            {
                cachedDeformers.Clear();
                cachedDeformers.AddRange(deformerPrefab.GetComponents<Deformer>());

                if (cachedDeformers.Count == 0)
                {
                    Debug.LogWarning("No Deformer components found on the assigned prefab.");
                    useDeformers = false;
                }
                else
                {
                    useDeformers = true;
                }
            }
            else
            {
                Debug.LogWarning("Deformer prefab is not assigned in the inspector. Deformers will not be used.");
                useDeformers = false;
            }
        }

        /// <summary>
        /// Ensures that all control points align with the world up direction.
        /// </summary>
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
                Debug.LogWarning("Some control points required up vector alignment");
                TrackSpline.Refresh();
            }
        }

        // New method to add aligned control points
        void addAlignedTrackCP()
        {
            Vector3 p = TrackSpline.ControlPointsList[TrackSpline.ControlPointCount - 1].transform.localPosition;
            Vector3 position = TrackSpline.transform.localToWorldMatrix.MultiplyPoint3x4(p + Vector3.forward * CPStepSize);

            CurvySplineSegment newControlPoint = TrackSpline.InsertAfter(null, position, true);

            // Ensure the control point is aligned with world up
            newControlPoint.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);

            // Force the spline to recalculate
            TrackSpline.Refresh();

            if ((TrackSpline.ControlPointCount - 1 - TailCP) % SectionCPCount == 0)
                newControlPoint.SerializedOrientationAnchor = true;
        }
    }
}