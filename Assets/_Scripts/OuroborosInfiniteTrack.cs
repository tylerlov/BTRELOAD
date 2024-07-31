// =====================================================================
// Copyright 2013-2022 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FluffyUnderware.Curvy.Generator;
using FluffyUnderware.Curvy.Generator.Modules;
using FluffyUnderware.Curvy.Controllers;
using FluffyUnderware.Curvy.Shapes;
using FluffyUnderware.DevTools;
using OccaSoftware.BOP;
using UnityEngine.UI;
using Chronos; 
using Deform;

namespace FluffyUnderware.Curvy.Controllers
{
    public class OuroborosInfiniteTrack : MonoBehaviour  
    { [Header("Track Settings")]
        public CurvySpline TrackSpline;
        private SplineController Controller;
        public Material RoadMaterial;

        [Header("Curvation Settings")]
        [Positive] public float CurvationX = 10;
        [Positive] public float CurvationY = 10;

        [Header("Control Point Settings")]
        [Positive] public float CPStepSize = 20;
        [Positive] public int HeadCP = 3;
        [Positive] public int TailCP = 2;

        [Header("Section Settings")]
        [DevTools.Min(3)] public int Sections = 6;
        [DevTools.Min(1)] public int SectionCPCount = 2;

        [Header("Offset Settings")]
        public float YOffset = 0;

        [Header("Ellipse Settings")]
        [SerializeField] private float ellipseRadiusX = 20;
        [SerializeField] private float ellipseRadiusY = 10;

        [Header("Pooling Settings")]
        [SerializeField] private OccaSoftware.BOP.ParticleSystemPooler particleSystemPooler;
        [SerializeField] private OccaSoftware.BOP.Pooler prefabPooler;
        [SerializeField] private int prefabsPerSection = 5;
        [SerializeField] private float prefabScale = 1f;
        [SerializeField] private int maxActivePrefabs = 50;
        private List<GameObject> activePrefabs = new List<GameObject>();
        private List<List<GameObject>> prefabsBySection = new List<List<GameObject>>();
        [SerializeField] private List<GameObject> despawnedPrefabs = new List<GameObject>();

        [Header("Prefab Rotation Adjustments")]
        [SerializeField] private float minRandomXRotationSubtract = 80f;
        [SerializeField] private float maxRandomXRotationSubtract = 101f;
        [SerializeField] private float minRandomYRotation = -30f;
        [SerializeField] private float maxRandomYRotation = 31f;
        [SerializeField] private float minRandomZRotation = -30f;
        [SerializeField] private float maxRandomZRotation = 31f;

        [Header("Deformer Settings")]
        [SerializeField] private Deformer deformerPrefab; // Reference to the Deformer component

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

        void Start()
        {
            FindAndAssignController();
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
                    ConditionalDebug.LogError("SplineController component not found on the PlayerPlane object.");
                }
            }
            else
            {
                ConditionalDebug.LogError("GameObject with tag 'PlayerPlane' not found in the scene.");
            }
        }

        void FixedUpdate()
        {
            if (mInitState == 0)
                StartCoroutine(nameof(setup));

            if (mInitState == 2 && mUpdateSpline)
                advanceTrack();

            // Add this line to continuously check and add Deformable components
            AddDeformableComponents();

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
                    ConditionalDebug.LogError("Failed to find and assign the SplineController. Setup cannot continue.");
                    yield break;
                }
            }

            mGenerators = new CurvyGenerator[Sections];

            // Add the start CP to the spline
            TrackSpline.InsertAfter(null, Vector3.zero, true);
            mDir = Vector3.forward;

            int num = TailCP + HeadCP + Sections * SectionCPCount;
            for (int i = 0; i < num; i++)
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
                while (!mGenerators[i].IsInitialized)
                    yield return 0;

            // let all generators do their extrusion
            for (int i = 0; i < Sections; i++)
                StartCoroutine(updateSectionGenerator(mGenerators[i], i * SectionCPCount + TailCP, (i + 1) * SectionCPCount + TailCP));

            AddDeformableComponents();

            mInitState = 2;
            mUpdateIn = SectionCPCount;
            // Placement of the controller
            Controller.AbsolutePosition = TrackSpline.ControlPointsList[TailCP + ControllerPlacementOffset].Distance;
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
            gen.gameObject.layer = LayerMask.NameToLayer("Ground");

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
                    ConditionalDebug.LogWarning("Prefab does not have an Instance component, cannot despawn: " + prefab.name);
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

            path.SetRange(TrackSpline.ControlPointsList[startCP], TrackSpline.ControlPointsList[endCP]);

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
            mDir = Quaternion.Euler(rndX, rndY, 0) * mDir;

            CurvySplineSegment newControlPoint = TrackSpline.InsertAfter(null, position, true);

            //Set the last control point of each section as an Orientation Anchor, to avoid that Control Points added beyond this point modify the dynamic orientation of previous Control Points
            if ((TrackSpline.ControlPointCount - 1 - TailCP) % SectionCPCount == 0)
                newControlPoint.SerializedOrientationAnchor = true;
        }

        void PlacePrefabsOnSectionSurface(CurvySpline spline, int sectionIndex)
        {
            if (sectionIndex < 0 || sectionIndex >= prefabsBySection.Count)
            {
                ConditionalDebug.LogError($"Section index {sectionIndex} is out of range. Prefabs cannot be placed.");
                return;
            }

            List<float> placedPositions = new List<float>(); // Track placed positions along the spline as normalized values (0 to 1)
            float minDistanceApart = 3f; // Minimum distance between prefabs in world units

            for (int i = 0; i < prefabsPerSection; i++)
            {
                bool placed = false;
                int attempts = 0;
                while (!placed && attempts < 100) // Prevent infinite loop
                {
                    attempts++;
                    float t = Random.Range(0f, 1f); // Random position along the spline as a normalized value
                    Vector3 potentialPosition = spline.InterpolateByDistance(t * spline.Length); // Convert to world position

                    // Check if this position is far enough from all previously placed prefabs
                    bool tooClose = placedPositions.Any(p => Vector3.Distance(spline.InterpolateByDistance(p * spline.Length), potentialPosition) < minDistanceApart);

                    if (!tooClose)
                    {
                        Quaternion rotation = spline.GetOrientationFast(t); // Get rotation at position t

                        // Apply random rotation adjustments
                        float randomXRotationSubtract = Random.Range(minRandomXRotationSubtract, maxRandomXRotationSubtract);
                        float randomYRotation = Random.Range(minRandomYRotation, maxRandomYRotation);
                        float randomZRotation = Random.Range(minRandomZRotation, maxRandomZRotation);
                        Quaternion rotationAdjustment = Quaternion.Euler(-randomXRotationSubtract, randomYRotation, randomZRotation);
                        rotation *= rotationAdjustment;

                        Vector3 sideDirection = (Random.value > 0.5f) ? Vector3.right : Vector3.left;
                        Vector3 offsetDirection = rotation * sideDirection;

                        float sideOffset = Random.Range(7f, 12f) * ((Random.value > 0.5f) ? 1 : -1);
                        Vector3 finalPosition = potentialPosition + offsetDirection * sideOffset;

                        GameObject prefabInstance = prefabPooler.GetFromPool();
                        prefabInstance.transform.position = finalPosition;
                        prefabInstance.transform.rotation = rotation;
                        prefabInstance.transform.localScale = new Vector3(prefabScale, prefabScale, prefabScale);

                        // Set the prefab instance as a child of the GameObject this script is attached to
                        prefabInstance.transform.SetParent(this.transform, true);

                        prefabsBySection[sectionIndex].Add(prefabInstance);
                        activePrefabs.Add(prefabInstance);

                        placedPositions.Add(t); // Remember this position as a normalized value
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
            foreach (var generator in mGenerators)
            {
                AddDeformableComponentsToGenerator(generator);
            }
        }

        private void AddDeformableComponentsToGenerator(CurvyGenerator generator)
        {
            List<CreateMesh> createMeshModules = generator.FindModules<CreateMesh>();

            foreach (var createMeshModule in createMeshModules)
            {
                if (createMeshModule != null)
                {
                    foreach (var meshResource in createMeshModule.Meshes.Items)
                    {
                        if (meshResource != null && !meshResource.GetComponent<Deformable>())
                        {
                            Deformable deformable = meshResource.gameObject.AddComponent<Deformable>();
                            deformable.UpdateMode = UpdateMode.Auto;
                            deformable.NormalsRecalculation = NormalsRecalculation.Auto;
                            deformable.BoundsRecalculation = BoundsRecalculation.Auto;

                            // Add the referenced Deformer to the Deformable component
                            if (deformerPrefab != null)
                            {
                                deformable.AddDeformer(deformerPrefab);
                            }
                            else
                            {
                                Debug.LogWarning("Deformer is not assigned in the inspector.");
                            }
                        }
                    }
                }
            }
        }
    }
}

