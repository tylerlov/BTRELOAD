using UnityEngine;

namespace FIMSpace.FOptimizing
{
    public abstract partial class FOptimizer_Base
    {
        public CullingGroup CullingGroup { get; protected set; }
        public float CullSize { get; protected set; }
        protected BoundingSphere[] visibilitySpheres;
        protected BoundingSphere mainVisibilitySphere;
        protected CullingGroupEvent lastEvent;


        protected void InitStaticOptimizer()
        {
            if (!AddToContainer ) FOptimizers_Manager.Get.RegisterNotContainedStaticOptimizer(this, true);

            InitCullingGroups(GetDistanceMeasures(), DetectionRadius, FOptimizers_Manager.MainCamera);
        }

        /// <summary>
        /// Setting up culling groups
        /// </summary>
        /// <param name="distances"> you can set it like distances = new float[] { 30f, 100f, 200f }; step levels of detection for algorithm </param>
        /// <param name="detectionSphereRadius"> Sphere visibility radius for camera to detect if object is visible in view's range </param>
        protected virtual void InitCullingGroups(float[] distances, float detectionSphereRadius = 2.5f, Camera targetCamera = null)
        {
            InitBaseCullingVariables(targetCamera);

            if (!AddToContainer) // Culling group just in optimizer component
            {
                SetDistanceLevels(distances);
                CullingGroup = new CullingGroup { targetCamera = targetCamera };

                visibilitySpheres = new BoundingSphere[1];
                visibilitySpheres[0] = new BoundingSphere(transform.position + transform.TransformVector(DetectionOffset), detectionSphereRadius * GetScaler(transform) );
                mainVisibilitySphere = visibilitySpheres[0];

                CullingGroup.SetBoundingSpheres(visibilitySpheres);
                CullingGroup.SetBoundingSphereCount(1);

                CullingGroup.onStateChanged = CullingGroupStateChanged;

                CullingGroup.SetBoundingDistances(DistanceLevels);
                if ( targetCamera ) CullingGroup.SetDistanceReferencePoint(targetCamera.transform);
            }
            else // Culling group in container
            {
                SetDistanceLevels(distances);
                FOptimizers_Manager.Get.AddToContainer(this);
            }

            distancePoint = GetReferencePosition();
            PreviousPosition = distancePoint;
        }


        /// <summary>
        /// Called when cullling group's state changes
        /// </summary>
        public virtual void CullingGroupStateChanged(CullingGroupEvent cullingEvent)
        {
            lastEvent = cullingEvent;

            if (enabled == false)
            {
                wasDisabled = true;
                return;
            }

            // Initial helper variables definition
            int distanceInd = cullingEvent.currentDistance;
            if (distanceInd == 0) distanceInd = 1;

            int preDistanceInd = cullingEvent.previousDistance;
            if (preDistanceInd == 0) preDistanceInd = 1;

            // Initial culling event operations
            if (distanceInd > DistanceLevels.Length - 2) // Out of distance detection
            {
                OutOfDistance = true;
                if (distanceInd > DistanceLevels.Length - 1) FarAway = true; else FarAway = false;
            }
            else // Object is in LOD range
            {
                OutOfDistance = false;
                FarAway = false;
            }

            // Camera look away quick cull feature support
            if (CullIfNotSee) // If we want to cull object when camera look away
            {
                bool steppedOutRange = false;

                if (preDistanceInd == DistanceLevels.Length - 2 && distanceInd == DistanceLevels.Length - 1)
                    steppedOutRange = true;

                if (cullingEvent.hasBecomeVisible)
                {
                    OutOfCameraView = false; // We can't define if object became visible in camera frustum, only if reached distance range
                }
                else if (cullingEvent.hasBecomeInvisible)
                {
                    if (!steppedOutRange) OutOfCameraView = true;
                }
            }
            else // When we only want to use distance to cull object
            {
                if (cullingEvent.hasBecomeVisible)
                    OutOfCameraView = false;
                else
                if (cullingEvent.hasBecomeInvisible)
                {
                    OutOfCameraView = true;
                }
            }

            bool changeOccured = false;
            int lod = distanceInd - 1;

            if (lod != CurrentDistanceLODLevel)
                changeOccured = true;
            else
            if (WasOutOfCameraView != OutOfCameraView)
                changeOccured = true;
            else
                if (WasHidden != IsHidden)
                changeOccured = true;

            if (!doFirstCull)
            {
                if (changeOccured) RefreshVisibilityState(lod);
            }
            else
                RefreshVisibilityState(lod);

            distancePoint = GetReferencePosition();
        }


        private void SetDistanceLevels(float[] distances)
        {
            DistanceLevels = new float[distances.Length + 2];
            DistanceLevels[0] = 0.001f; // I'm disappointed I have to use additional distance to allow detect first frame culling event catch everything

            for (int i = 1; i < distances.Length + 1; i++)
                DistanceLevels[i] = distances[i - 1];

            // Additional distance level to be able detecting frustum ranges, instead of frustum with distance ranges combined
            DistanceLevels[DistanceLevels.Length - 1] = distances[distances.Length - 1] * 1.5f;

        }


        /// <summary>
        /// Cleaning culling groups from the memory
        /// </summary>
        protected void CleanCullingGroup()
        {
            if (CullingGroup != null)
            {
                CullingGroup.Dispose();
                CullingGroup = null;
            }

            if (OwnerContainer != null)
            {
                OwnerContainer.RemoveOptimizer(this);
            }
        }


        public static float GetScaler(Transform transform)
        {
            float scaler = 1f;
            if (transform.lossyScale.x > transform.lossyScale.y)
            {
                if (transform.lossyScale.y > transform.lossyScale.z)
                    scaler = transform.lossyScale.y;
                else
                    scaler = transform.lossyScale.z;
            }
            else
                scaler = transform.lossyScale.x;

            return scaler;
        }
    }
}
