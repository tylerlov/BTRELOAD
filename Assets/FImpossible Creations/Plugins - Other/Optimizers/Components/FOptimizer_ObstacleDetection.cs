using System.Collections.Generic;
using UnityEngine;

namespace FIMSpace.FOptimizing
{
    /// <summary>
    /// FM: Class for optimizing objects with obstacle detection raycasting
    /// </summary>
    [AddComponentMenu("FImpossible Creations/Optimizers/Optimizer Wall Detection")]
    public class FOptimizer_ObstacleDetection : FOptimizer_Base, UnityEngine.EventSystems.IDropHandler, IFHierarchyIcon
    {
        #region Hierarchy icons

        public string EditorIconPath { get { return "FIMSpace/FOptimizing/Optimizers Wall Icon"; } }
        public void OnDrop(UnityEngine.EventSystems.PointerEventData data) { }
        #endregion

        [HideInInspector]
        [Range(-1, 5)]
        [Tooltip("Allowing component to do more raycasts to detect obstacles covering it")]
        public int CoveragePrecision = 1;
        [HideInInspector]
        [Range(0f, 1.5f)]
        [Tooltip("If you want to avoid casting some raycasts from below ground")]
        public float CoverageScale = 1;

        [HideInInspector]
        [Tooltip("Layer mask for raycasts checking obstacles in front of object in direction to camera")]
        public LayerMask CoverageMask = 1 << 0;

        [HideInInspector]
        [Tooltip("Draw menu for customized raycasting points")]
        public bool CustomCoveragePoints = false;

        //[HideInInspector]
        //[Tooltip("Adjusting raycasts placement")]
        //public Vector3 CoverageOffset = Vector3.zero;

        //[HideInInspector]
        //[Range(0f, 1f)]
        //[Tooltip("When using raycast memory (Optimizers Manager), we can define how unprecise can be checking for sake of better performance - lower value -> higher precision and cpu usage")]
        //public float MemoryTolerance = 0f;

        [HideInInspector]
        public List<Vector3> CoverageOffsets;
        private int currentCoveragePrecision = -1;

        protected override void Start()
        {
            RefreshCoverageOffsets();
            base.Start();
        }

        public override void DynamicLODUpdate(FEOptimizingDistance category, float distance)
        {
            base.DynamicLODUpdate(category, distance);

            if (CoveragePrecision == -1) return;

            if (!OutOfCameraView && !OutOfDistance)
                ObstacleCheck();
        }


        private void ObstacleCheck()
        {
            Vector3[] coveragePoints = GetCoverageDetectionPoints(CoverageOffsets, PreviousPosition);

            for (int i = 0; i < coveragePoints.Length; i++)
            {
                RaycastHit hit;
                //Physics.Linecast(TargetCamera.position, coveragePoints[i], out hit, CoverageMask, QueryTriggerInteraction.Ignore);
                Physics.Linecast(TargetCamera.position, coveragePoints[i], out hit, CoverageMask, QueryTriggerInteraction.Ignore);
#if UNITY_EDITOR
                FOptimizers_Manager.RaycastsInThisFrame++;
#endif
                if (!hit.transform)
                {
                    SetHidden(false);
                    return;
                }
            }

            SetHidden(true);
        }


        public override void CullingGroupStateChanged(CullingGroupEvent cullingEvent)
        {
            base.CullingGroupStateChanged(cullingEvent);

            if (CullIfNotSee)
                if (!OutOfCameraView && !OutOfDistance) if (CoveragePrecision > -1) ObstacleCheck();
        }


        public override void OnValidate()
        {
            CullIfNotSee = true;
            if (OptimizingMethod == FEOptimizingMethod.Static)
            {
                Debug.LogError("[OPTIMIZERS] " + OptimizingMethod + " method is not supported for FOptimizer_ObstacleDetection component!");
                OptimizingMethod = FEOptimizingMethod.Effective;
            }

            base.OnValidate();
        }


#if UNITY_EDITOR
        protected override void OnDrawGizmosSelected()
        {
            if (gameObject.activeInHierarchy == false) return;
            base.OnDrawGizmosSelected();

            if (CoveragePrecision == -1) return;
            if (Camera.main == null) return;

            RefreshCoverageOffsets();
            Vector3[] coveragePoints = GetCoverageDetectionPoints(CoverageOffsets, GetReferencePosition());

            float scale = DetectionRadius / 15f;

            if (OptimizingMethod != FEOptimizingMethod.Effective) scale = DetectionBounds.x / 14f;

            Gizmos.color = new Color(0.22f, 0.88f, 0.22f, 0.2f * GizmosAlpha);

            for (int i = 0; i < CoverageOffsets.Count; i++)
            {
                Vector3 origin = coveragePoints[i]; // new Vector3(CoverageArea.x * coverageOffsets[i].x / 2f, CoverageArea.y * coverageOffsets[i].y / 2f, CoverageAreaOffset.z) + new Vector3(CoverageAreaOffset.x, CoverageAreaOffset.y, 0f);

                Gizmos.DrawLine(origin, Camera.main.transform.position);
                Gizmos.DrawRay(origin, -Vector3.forward * scale * 0.75f);
                Gizmos.DrawWireSphere(origin, scale);
            }
        }
#endif


        #region Utilities


        /// <summary>
        /// Refreshing target points for coverage check feature
        /// It is creating array of percentage-like values to identify raycast points
        /// </summary>
        private void RefreshCoverageOffsets()
        {
            if (CustomCoveragePoints) return;
            if (currentCoveragePrecision == CoveragePrecision) return;
            if (CoveragePrecision == -1) return;
            currentCoveragePrecision = CoveragePrecision;

            CoverageOffsets = new List<Vector3>();
            Vector3[] coverageOffsets = new Vector3[0];

            if (OptimizingMethod == FEOptimizingMethod.Effective)
            {   // Sphere based coverage offsets
                if (CoveragePrecision == 0)
                {
                    coverageOffsets = new Vector3[1];
                    coverageOffsets[0] = new Vector3(0f, 0f, 1f);
                }
                else
                if (CoveragePrecision == 4)
                {
                    coverageOffsets = new Vector3[13];
                    coverageOffsets[0] = new Vector3(0f, 0f, 1f);
                    coverageOffsets[1] = new Vector3(-1f, 0f, 0f);
                    coverageOffsets[2] = new Vector3(1f, 0f, 0f);
                    coverageOffsets[3] = new Vector3(0f, 1f, 0f);
                    coverageOffsets[4] = new Vector3(0f, -1f, 0f);

                    coverageOffsets[5] = new Vector3(-.5f, .5f, .85f);
                    coverageOffsets[6] = new Vector3(.5f, .5f, .85f);
                    coverageOffsets[7] = new Vector3(.5f, -.5f, .85f);
                    coverageOffsets[8] = new Vector3(-.5f, -.5f, .85f);

                    coverageOffsets[9] = new Vector3(.5f, .5f, 0f);
                    coverageOffsets[11] = new Vector3(-.5f, .5f, 0f);
                    coverageOffsets[10] = new Vector3(-.5f, -.5f, 0f);
                    coverageOffsets[12] = new Vector3(.5f, -.5f, 0f);
                }
                else
                if (CoveragePrecision == 5)
                {
                    //coverageOffsets = new Vector3[13];
                    coverageOffsets = new Vector3[25];
                    coverageOffsets[0] = new Vector3(0f, 0f, 1f);
                    coverageOffsets[1] = new Vector3(-1f, 0f, 0f);
                    coverageOffsets[2] = new Vector3(1f, 0f, 0f);
                    coverageOffsets[3] = new Vector3(0f, 1f, 0f);
                    coverageOffsets[4] = new Vector3(0f, -1f, 0f);

                    coverageOffsets[5] = new Vector3(-.5f, .5f, .85f);
                    coverageOffsets[6] = new Vector3(.5f, .5f, .85f);
                    coverageOffsets[7] = new Vector3(.5f, -.5f, .85f);
                    coverageOffsets[8] = new Vector3(-.5f, -.5f, .85f);

                    coverageOffsets[9] = new Vector3(.5f, .5f, 0f);
                    coverageOffsets[11] = new Vector3(-.5f, .5f, 0f);
                    coverageOffsets[10] = new Vector3(-.5f, -.5f, 0f);
                    coverageOffsets[12] = new Vector3(.5f, -.5f, 0f);

                    for (int i = 13; i < coverageOffsets.Length; i++)
                        coverageOffsets[i] = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f));
                }
                else if (CoveragePrecision == 3)
                {
                    coverageOffsets = new Vector3[9];

                    coverageOffsets[0] = new Vector3(0f, 0f, 1f);
                    coverageOffsets[1] = new Vector3(-1f, 0f, 0f);
                    coverageOffsets[2] = new Vector3(1f, 0f, 0f);
                    coverageOffsets[3] = new Vector3(0f, 1f, 0f);
                    coverageOffsets[4] = new Vector3(0f, -1f, 0f);

                    coverageOffsets[5] = new Vector3(-.7f, .7f, .85f);
                    coverageOffsets[6] = new Vector3(.7f, .7f, .85f);
                    coverageOffsets[7] = new Vector3(.7f, -.7f, .85f);
                    coverageOffsets[8] = new Vector3(-.7f, -.7f, .85f);
                }
                else if (CoveragePrecision == 2)
                {
                    coverageOffsets = new Vector3[5];
                    coverageOffsets[0] = new Vector3(0f, 0f, 1f);
                    coverageOffsets[1] = new Vector3(-1f, 1f, .4f);
                    coverageOffsets[2] = new Vector3(1f, -1f, .4f);
                    coverageOffsets[3] = new Vector3(1f, 1f, .4f);
                    coverageOffsets[4] = new Vector3(-1f, -1f, .4f);
                }
                else if (CoveragePrecision == 1)
                {
                    coverageOffsets = new Vector3[4];
                    coverageOffsets[0] = new Vector3(0f, 0f, 1f);
                    coverageOffsets[1] = new Vector3(0f, 0.4f, .1f);
                    coverageOffsets[2] = new Vector3(-.6f, -0.3f, 0.15f);
                    coverageOffsets[3] = new Vector3(.6f, -0.3f, 0.15f);
                }
            }
            else // Bounds based coverage offsets
            {
                if (CoveragePrecision == 0)
                {
                    coverageOffsets = new Vector3[1];
                    coverageOffsets[0] = new Vector3(0f, 0f, 1f);
                }
                else
                if (CoveragePrecision == 4)
                {
                    coverageOffsets = new Vector3[13];
                    coverageOffsets[0] = new Vector3(0f, 0f, 1f);
                    coverageOffsets[1] = new Vector3(-1f, 1f, .4f);
                    coverageOffsets[2] = new Vector3(1f, -1f, .4f);
                    coverageOffsets[3] = new Vector3(1f, 1f, .4f);
                    coverageOffsets[4] = new Vector3(-1f, -1f, .4f);

                    coverageOffsets[5] = new Vector3(-.7f, .4f, .85f);
                    coverageOffsets[6] = new Vector3(.7f, .4f, .85f);
                    coverageOffsets[7] = new Vector3(.7f, -.4f, .85f);
                    coverageOffsets[8] = new Vector3(-.7f, -.4f, .85f);

                    coverageOffsets[9] = new Vector3(-1f, 0f, .0f);
                    coverageOffsets[10] = new Vector3(1f, 0f, .0f);

                    coverageOffsets[11] = new Vector3(0f, 1f, .0f);
                    coverageOffsets[12] = new Vector3(0f, -1f, .0f);
                }
                else
                if (CoveragePrecision == 5)
                {
                    coverageOffsets = new Vector3[25];
                    coverageOffsets[0] = new Vector3(0f, 0f, 1f);
                    coverageOffsets[1] = new Vector3(-1f, 1f, .4f);
                    coverageOffsets[2] = new Vector3(1f, -1f, .4f);
                    coverageOffsets[3] = new Vector3(1f, 1f, .4f);
                    coverageOffsets[4] = new Vector3(-1f, -1f, .4f);

                    coverageOffsets[5] = new Vector3(-.7f, .4f, .85f);
                    coverageOffsets[6] = new Vector3(.7f, .4f, .85f);
                    coverageOffsets[7] = new Vector3(.7f, -.4f, .85f);
                    coverageOffsets[8] = new Vector3(-.7f, -.4f, .85f);

                    coverageOffsets[9] = new Vector3(-1f, 0f, .0f);
                    coverageOffsets[10] = new Vector3(1f, 0f, .0f);

                    coverageOffsets[11] = new Vector3(0f, 1f, .0f);
                    coverageOffsets[12] = new Vector3(0f, -1f, .0f);

                    for (int i = 13; i < coverageOffsets.Length; i++)
                        coverageOffsets[i] = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(0, 1f));
                }
                else if (CoveragePrecision == 3)
                {
                    coverageOffsets = new Vector3[9];

                    coverageOffsets[0] = new Vector3(0f, 0f, 1f);
                    coverageOffsets[1] = new Vector3(-1f, 1f, .4f);
                    coverageOffsets[2] = new Vector3(1f, -1f, .4f);
                    coverageOffsets[3] = new Vector3(1f, 1f, .4f);
                    coverageOffsets[4] = new Vector3(-1f, -1f, .4f);

                    coverageOffsets[5] = new Vector3(-.7f, .4f, .85f);
                    coverageOffsets[6] = new Vector3(.7f, .4f, .85f);
                    coverageOffsets[7] = new Vector3(.7f, -.4f, .85f);
                    coverageOffsets[8] = new Vector3(-.7f, -.4f, .85f);
                }
                else if (CoveragePrecision == 2)
                {
                    coverageOffsets = new Vector3[5];
                    coverageOffsets[0] = new Vector3(0f, 0f, 1f);
                    coverageOffsets[1] = new Vector3(-1f, 1f, .4f);
                    coverageOffsets[2] = new Vector3(1f, -1f, .4f);
                    coverageOffsets[3] = new Vector3(1f, 1f, .4f);
                    coverageOffsets[4] = new Vector3(-1f, -1f, .4f);
                }
                else if (CoveragePrecision == 1)
                {
                    coverageOffsets = new Vector3[4];
                    coverageOffsets[0] = new Vector3(0f, 0f, 1f);
                    coverageOffsets[1] = new Vector3(0f, 0.8f, .1f);
                    coverageOffsets[2] = new Vector3(-1f, -0.85f, 0.15f);
                    coverageOffsets[3] = new Vector3(1f, -0.85f, 0.15f);
                }
            }

            CoverageOffsets.Clear();
            for (int i = 0; i < coverageOffsets.Length; i++)
                CoverageOffsets.Add(coverageOffsets[i]);
        }



        public Vector3[] GetCoverageDetectionPoints(List<Vector3> coverageOffsets, Vector3 origin)
        {
            Vector3[] result = new Vector3[coverageOffsets.Count];

            float scale = (CoverageScale) * 0.7f;

            if (OptimizingMethod == FEOptimizingMethod.Effective)
            {
                if (CustomCoveragePoints)
                {
                    Quaternion flatDirection = Quaternion.LookRotation(Camera.main.transform.position - origin);
                    //Vector3 offset = flatDirection * CoverageOffset;

                    for (int i = 0; i < coverageOffsets.Count; i++)
                    {
                        result[i] = origin;
                        result[i] += (flatDirection) * Vector3.Scale(coverageOffsets[i] * scale, Vector3.one * DetectionRadius);// + offset;
                    }
                }
                else
                {
                    Quaternion flatDirection = Quaternion.LookRotation(Camera.main.transform.position - origin);
                    //Vector3 offset = flatDirection * CoverageOffset;

                    for (int i = 0; i < coverageOffsets.Count; i++)
                    {
                        result[i] = origin;
                        result[i] += (flatDirection) * coverageOffsets[i].normalized * DetectionRadius * scale;// + offset;
                    }
                }
            }
            else
            {
                //Quaternion flatDirection = Camera.main.transform.rotation;  // flatDirection * -coverag...
                Quaternion flatDirection = Quaternion.LookRotation(Camera.main.transform.position - origin);
                //Vector3 offset = flatDirection * CoverageOffset;

                for (int i = 0; i < coverageOffsets.Count; i++)
                {
                    result[i] = origin;
                    result[i] += (flatDirection) * Vector3.Scale(coverageOffsets[i] * scale, DetectionBounds / 2);// + offset;
                }
            }

            return result;
        }


        #endregion

    }

    #region If you want to create own optimizer component

    //[CanEditMultipleObjects]
    //[CustomEditor(typeof(FOptimizer))]
    //public class FOptimizerEditor : FOptimizer_BaseEditor
    //{ } // If you want to create custom script you need to define editor class, check FOptimizers_EditorWindows.cs

    #endregion
}