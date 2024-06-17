using System.Collections.Generic;
using UnityEngine;

namespace FIMSpace.FOptimizing
{
    /// <summary>
    /// FM: Class for optimizing objects with complex shape using Culling Groups API.
    /// </summary>
    [AddComponentMenu("FImpossible Creations/Optimizers/Optimizer Complex Shape")]
    public class FOptimizer_ComplexShape : FOptimizer_Base, UnityEngine.EventSystems.IDropHandler, IFHierarchyIcon
    {
        #region Hierarchy icons

        public string EditorIconPath { get { return "FIMSpace/FOptimizing/Optimizers Icon Complex"; } }
        public void OnDrop(UnityEngine.EventSystems.PointerEventData data) { }
        #endregion


        #region Multi Shape Setup Variables

        [HideInInspector]
        [Range(0f, 1f)]
        [Tooltip("How many spheres should be created in auto detection process")]
        public float AutoPrecision = 0.25f;
        [HideInInspector]
        [Tooltip("[Optional] Mesh to create detection spheres on it's structure")]
        public Mesh AutoReferenceMesh;
        [HideInInspector]
        public bool DrawPositionHandles = true;
        [HideInInspector]
        public bool ScalingHandles = true;

        [HideInInspector]
        public List<FOptComplex_DetectionSphere> Shapes;

        // Back compatibility
        [HideInInspector]
        public List<Vector3> ShapePos;
        [HideInInspector]
        public List<float> ShapeRadius;

        #endregion


        protected override void RefreshInitialSettingsForOptimized()
        {
            base.RefreshInitialSettingsForOptimized();
            AddToContainer = false;
        }


        #region Multi spheres detection variables

        private int nearestDistanceLevel = 0;
        private int preNearestDistanceLevel = 0;
        private int[] sphereState;
        private int spheresVisible = 0;
        //private int spheresInvisible = 0;
        private int[] spheresWithLOD;
        //private float nearestDistance = 0f;

        #endregion


        #region Culling Overrides etc.

        protected override void InitCullingGroups(float[] distances, float detectionSphereRadius = 2.5F, Camera targetCamera = null)
        {
            if (Shapes == null || Shapes.Count == 0) return;

            InitBaseCullingVariables(targetCamera);

            DistanceLevels = new float[distances.Length + 2];
            DistanceLevels[0] = 0.001f; // I'm disappointed I have to use additional distance to allow detect initial culling event catch everything

            for (int i = 1; i < distances.Length + 1; i++) DistanceLevels[i] = distances[i - 1];

            // Additional distance level to be able detecting frustum ranges, instead of frustum with distance ranges combined
            DistanceLevels[DistanceLevels.Length - 1] = distances[distances.Length - 1] * 2;

            distancePoint = transform.position;
            CullingGroup = new CullingGroup { targetCamera = targetCamera };


            visibilitySpheres = GetBoundingSpheres();
            sphereState = new int[visibilitySpheres.Length];
            mainVisibilitySphere = visibilitySpheres[0];

            for (int i = 0; i < sphereState.Length; i++) sphereState[i] = 0;
            spheresWithLOD = new int[LODLevels + 2];
            spheresWithLOD[1] = visibilitySpheres.Length;

            CullingGroup.SetBoundingSpheres(visibilitySpheres);
            CullingGroup.SetBoundingSphereCount(visibilitySpheres.Length);

            CullingGroup.onStateChanged = CullingGroupStateChanged;

            CullingGroup.SetBoundingDistances(DistanceLevels);
            CullingGroup.SetDistanceReferencePoint(targetCamera.transform);

            spheresVisible = 0;
            //spheresInvisible = visibilitySpheres.Length;

            float[] elements = GetCenterPosAndFarthest();
            distancePoint = new Vector3(elements[0], elements[1], elements[2]);
        }

        public override void CullingGroupStateChanged(CullingGroupEvent cullingEvent)
        {
            int distInd = cullingEvent.currentDistance; if (distInd == 0) distInd = 1; if (distInd >= spheresWithLOD.Length) distInd = spheresWithLOD.Length - 1;
            sphereState[cullingEvent.index] = distInd;

            int preInd = cullingEvent.previousDistance; if (preInd == 0) preInd = 1; if (preInd >= spheresWithLOD.Length) preInd = spheresWithLOD.Length - 1;

            spheresWithLOD[preInd]--;
            spheresWithLOD[distInd]++;

            //int preVisible = spheresVisible;

            if (cullingEvent.hasBecomeInvisible)
            {
                /*if (spheresVisible == 1) steppedOutVisible = true;*/
                //spheresInvisible++; 
                spheresVisible--;
            }

            if (cullingEvent.hasBecomeVisible)
            {
                /*if (spheresInvisible == 1) steppedOutVisible = false;*/
                //spheresInvisible--; 
                spheresVisible++;
            }

            int nearest = 0;
            for (int i = spheresWithLOD.Length - 1; i >= 0; i--)
                if (spheresWithLOD[i] > 0) nearest = i;

            if (nearest == 0) nearest = 1;

            nearestDistanceLevel = nearest;

            if (nearestDistanceLevel > DistanceLevels.Length - 2)
            {
                OutOfDistance = true;
                if (nearestDistanceLevel > DistanceLevels.Length - 1) FarAway = true; else FarAway = false;
            }
            else
            {
                OutOfDistance = false;
                FarAway = false;
            }

#if UNITY_EDITOR
            int nearestI = 0;
            float nearDist = float.MaxValue;

            for (int i = 0; i < sphereState.Length; i++)
            {
                if (sphereState[i] == nearestDistanceLevel)
                {
                    float dist = Vector3.Distance(visibilitySpheres[i].position, TargetCamera.position);
                    if (dist < nearDist)
                    {
                        nearestI = i;
                        nearDist = dist;
                    }
                }
            }

            //nearestDistance = Mathf.Max(0f, nearDist - DetectionRadius);
            distancePoint = visibilitySpheres[nearestI].position;
#endif

            //if (spheresVisible == 0 && preVisible > 0)
            //{
            //    bool steppedOutRange = false;
            //    if (preInd == DistanceLevels.Length - 2 && distInd == DistanceLevels.Length - 1)
            //        steppedOutRange = true;

            //    if (!steppedOutRange) OutOfCameraView = true;
            //}



            if (spheresVisible == 0) OutOfCameraView = true; else OutOfCameraView = false;

            bool changeOccured = false;
            if (preNearestDistanceLevel != nearestDistanceLevel) changeOccured = true;
            else
            {
                if (WasOutOfCameraView != OutOfCameraView) changeOccured = true;
                else
                {
                    if (WasHidden != IsHidden) changeOccured = true;
                }
            }

            if (changeOccured)
            {
                RefreshVisibilityState(Mathf.Max(0, nearestDistanceLevel - 1));
                preNearestDistanceLevel = nearestDistanceLevel;
            }
        }

        protected BoundingSphere[] GetBoundingSpheres()
        {
            BoundingSphere[] spheres = new BoundingSphere[Shapes.Count];
            Transform targetTr = transform;

            Matrix4x4 m = transform.localToWorldMatrix;

            for (int i = 0; i < Shapes.Count; i++)
            {
                if (Shapes[i].transform == null)
                    spheres[i] = new BoundingSphere(m.MultiplyPoint(Shapes[i].position), DetectionRadius * Shapes[i].radius);
                else
                    spheres[i] = new BoundingSphere(Shapes[i].transform.localToWorldMatrix.MultiplyPoint(Shapes[i].position), DetectionRadius * Shapes[i].radius);
            }


            return spheres;
        }

        public override Vector3 GetReferencePosition()
        {
            return distancePoint;
        }

        #endregion


        #region Editor stuff

        public override void OnValidate()
        {
            if (OptimizingMethod == FEOptimizingMethod.Dynamic || OptimizingMethod == FEOptimizingMethod.TriggerBased)
            {
                Debug.LogError("[OPTIMIZERS] Optimization Method " + OptimizingMethod + " is not supported by Complex Shape Component!");
                OptimizingMethod = FEOptimizingMethod.Effective;
            }

            base.OnValidate();
            CullIfNotSee = true;
            Hideable = true;

            // Auto check for reference mesh
            if (!AutoReferenceMesh)
            {
                MeshFilter meshF = GetComponentInChildren<MeshFilter>();
                if (meshF) AutoReferenceMesh = meshF.sharedMesh;
                if (!AutoReferenceMesh)
                {
                    SkinnedMeshRenderer skin = gameObject.GetComponentInChildren<SkinnedMeshRenderer>();
                    if (skin) AutoReferenceMesh = skin.sharedMesh;
                }
            }

            if ( ShapePos.Count > 0 )
            {
                for (int i = 0; i < ShapePos.Count; i++)
                {
                    Shapes.Add(new FOptComplex_DetectionSphere());
                    Shapes[i].position = ShapePos[i];
                    Shapes[i].radius = ShapeRadius[i];
                }

                ShapePos.Clear();
                ShapeRadius.Clear();
            }
        }

        public override void DynamicLODUpdate(FEOptimizingDistance category, float distance)
        {
            PreviousPosition = visibilitySpheres[0].position + Vector3.right * moveTreshold * 2f; // To update positions every frame, not basing only on one detection sphere

            base.DynamicLODUpdate(category, distance);
        }

        protected override void RefreshEffectiveCullingGroups()
        {
            Matrix4x4 m = transform.localToWorldMatrix;

            for (int i = 0; i < Shapes.Count; i++)
            {
                if (Shapes[i].transform == null)
                    visibilitySpheres[i].position = m.MultiplyPoint(Shapes[i].position);
                else
                    visibilitySpheres[i].position = Shapes[i].transform.localToWorldMatrix.MultiplyPoint(Shapes[i].position);

            }
        }


#if UNITY_EDITOR
        protected override void OnDrawGizmosSelected()
        {
            if (gameObject.activeInHierarchy == false) return;
            if (Shapes == null || Shapes.Count == 0) return;

            Color preCol = Gizmos.color;

            if (Shapes != null)
            {
                if (!Application.isPlaying)
                    visibilitySpheres = GetBoundingSpheres();

                Gizmos.color = new Color(.9f, .9f, .9f, 0.5f * GizmosAlpha);

                for (int i = 0; i < visibilitySpheres.Length; i++)
                    Gizmos.DrawSphere(visibilitySpheres[i].position, visibilitySpheres[i].radius);

                float[] elements = GetCenterPosAndFarthest();
                DrawLODRangeSpheres(new Vector3(elements[0], elements[1], elements[2]), elements[3] + elements[4]);
            }

            Gizmos.color = preCol;
        }
#endif

        /// <returns> 5 element array of floats, 0-x, 1-y, 2-z, 3-farthest sphere from center distance, 4-biggest radius sphere </returns>
        protected float[] GetCenterPosAndFarthest()
        {
            float[] elements = new float[5];

            Vector3 result = Vector3.zero;

            for (int i = 0; i < visibilitySpheres.Length; i++)
                result += visibilitySpheres[i].position;

            result /= (float)Shapes.Count;

            float dist;
            float farthest = 0f;
            float biggestRadius = 0f;

            for (int i = 0; i < visibilitySpheres.Length; i++)
            {
                dist = Vector3.Distance(visibilitySpheres[i].position, result);
                if (dist > farthest) farthest = dist;
                if (visibilitySpheres[i].radius > biggestRadius) biggestRadius = visibilitySpheres[i].radius;
            }

            elements[0] = result.x;
            elements[1] = result.y;
            elements[2] = result.z;
            elements[3] = farthest;
            elements[4] = biggestRadius;
            return elements;
        }

        /// <summary>
        /// Generating spheres for target mesh structure
        /// </summary>
        public void GenerateAutoShape()
        {
            if (AutoReferenceMesh)
            {
                List<Vector3> positions = GetPointsFromMesh(AutoReferenceMesh, AutoPrecision);
                Shapes = new List<FOptComplex_DetectionSphere>();
                for (int i = 0; i < positions.Count; i++)
                {
                    Shapes.Add(new FOptComplex_DetectionSphere());
                    Shapes[i].position = positions[i];
                }
            }
            else
            {
                Debug.LogError("[OPTIMIZERS] No mesh to reference from");
            }
        }

        /// <summary>
        /// Getting points from mesh in certain separation distances
        /// </summary>
        protected List<Vector3> GetPointsFromMesh(Mesh mesh, float precision)
        {
            try
            {


                List<Vector3> avPoints = new List<Vector3>();
                float radius = mesh.bounds.size.magnitude / Mathf.Lerp(2, 10, precision);
                DetectionRadius = radius;

                avPoints.Add(mesh.vertices[0]);

                for (int i = 0; i < 100; i++)
                {
                    float nearDist = float.MaxValue;
                    int nearestAv = -1;

                    for (int v = 0; v < mesh.vertices.Length; v++)
                    {
#if UNITY_EDITOR
                        if (v % 50 == 0)
                        {
                            UnityEditor.EditorUtility.DisplayProgressBar("Analyzing Vertices (" + (i + 1) + ")", "Checking vertices to create " + (i + 1) + (i == 1 ? "st" : (i == 2 ? "nd" : (i < 4 ? "rd" : "th"))) + " detection sphere... (" + v + "/" + mesh.vertexCount + ")", (float)v / (float)mesh.vertexCount);
                        }
#endif

                        bool can = true;
                        float dist;

                        for (int a = 0; a < avPoints.Count; a++)
                        {
                            dist = Vector3.Distance(mesh.vertices[v], avPoints[a]);
                            if (dist < radius)
                            {
                                can = false;
                                break;
                            }
                        }

                        if (!can) continue;

                        dist = Vector3.Distance(mesh.vertices[v], avPoints[i]);
                        if (dist < nearDist)
                        {
                            nearDist = dist;
                            nearestAv = v;
                        }
                    }

                    if (nearestAv == -1) { break; }

                    avPoints.Add(mesh.vertices[nearestAv]);
                }

#if UNITY_EDITOR
                UnityEditor.EditorUtility.ClearProgressBar();
#endif

                return avPoints;
            }
            catch (System.Exception)
            {
#if UNITY_EDITOR
                UnityEditor.EditorUtility.ClearProgressBar();
#endif
            }

            return null;
        }

        #endregion

        [System.Serializable]
        public class FOptComplex_DetectionSphere
        {
            public Vector3 position;
            public float radius = 1f;
            public Transform transform;
        }

    }
}