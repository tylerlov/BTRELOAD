using System;
using UnityEngine;

namespace FIMSpace.FOptimizing
{
    /// <summary>
    /// FM: Class which is managing CullingGroup bounding spheres and sending culling groups' events to optimizer components
    /// Objects need to have the same distance values to be able to be saved in container
    /// </summary>
    [Serializable] // DEBUG
    public class FOptimizers_CullingContainer
    {
        public const int MaxSlots = 1000;
        public int ID { get; private set; }
        public bool HaveFreeSlots { get { return highestIndex < MaxSlots - 1; } }

        internal bool Destroying = false;

        public CullingGroup CullingGroup { get; private set; }
        public FOptimizer_Base[] Optimizers { get; private set; }
        public BoundingSphere[] CullingSpheres { get; private set; }
        //public CullingGroupEvent?[] LastEvent { get; private set; }

        public int BoundingCount { get; private set; }
        public float[] DistanceLevels { get; private set; }

        private int highestIndex;
        private int lastRemovedIndex;

        public FOptimizers_CullingContainer()
        {
            Optimizers = new FOptimizer_Base[MaxSlots];
        }


        /// <summary>
        /// Initializing container with distances, defining ID and preparing CullingGroup to work
        /// </summary>
        public void InitializeContainer(int id, float[] distances, Camera targetCamera)
        {
            ID = id;

            DistanceLevels = new float[distances.Length + 2];
            DistanceLevels[0] = 0.001f; // I'm disappointed I have to use additional distance to allow detect first frame culling event catch everything

            for (int i = 1; i < distances.Length + 1; i++)
                DistanceLevels[i] = distances[i - 1];

            // Additional distance level to be able detecting frustum ranges, instead of frustum with distance ranges combined
            DistanceLevels[DistanceLevels.Length - 1] = distances[distances.Length - 1] * 1.5f;

            CullingGroup = new CullingGroup { targetCamera = targetCamera };

            CullingSpheres = new BoundingSphere[MaxSlots];
            //LastEvent = new CullingGroupEvent?[MaxSlots];

            CullingGroup.SetBoundingSpheres(CullingSpheres);
            BoundingCount = 0;
            highestIndex = -1;
            lastRemovedIndex = -1;
            CullingGroup.SetBoundingSphereCount(BoundingCount);

            CullingGroup.onStateChanged = CullingGroupStateChanged;

            CullingGroup.SetBoundingDistances(DistanceLevels);

            if (targetCamera) CullingGroup.SetDistanceReferencePoint(targetCamera.transform);
        }


        /// <summary>
        /// Setting new main camera
        /// </summary>
        public void SetNewCamera(Camera cam)
        {
            if (cam == null) return;

            CullingGroup.targetCamera = cam;
            CullingGroup.SetDistanceReferencePoint(cam.transform);
        }


        /// <summary>
        /// Returns true if list have free slots
        /// </summary>
        public bool AddOptimizer(FOptimizer_Base optimizer)
        {
            if (!HaveFreeSlots) return false;

            int nextId = highestIndex + 1;

            CullingSpheres[nextId].position = optimizer.GetReferencePosition();
            CullingSpheres[nextId].radius = optimizer.DetectionRadius * FOptimizer_Base.GetScaler(optimizer.transform);
            Optimizers[nextId] = optimizer;

            optimizer.AssignToContainer(this, nextId, ref CullingSpheres[nextId]);

            highestIndex++;
            BoundingCount++;
            CullingGroup.SetBoundingSphereCount(BoundingCount);
            ////if ( nextId > 0)
            ////Debug.Log("nexid = " + nextId + " lastevent-1 i = " + LastEvent[nextId-1].index + " lastevent i = " + LastEvent[nextId].index);

            //if (nextId > 0)
            //{
            //    if (LastEvent[nextId] != null)
            //    {
            //        Debug.Log("Using for " + nextId);
            //        // If using same position, rotation (replacing culling sphere) we triggering last culling event on object
            //        Optimizers[nextId].CullingGroupStateChanged(LastEvent[nextId-1]);
            //    }
            //}

            return true;
        }


        /// <summary>
        /// Remove optimizer from container and freeing slot for another optimizer
        /// </summary>
        public void RemoveOptimizer(FOptimizer_Base optimizer)
        {
            if (Optimizers == null) return;

#if UNITY_EDITOR
            if (FOptimizers_Manager.AppIsQuitting) return;
#endif
            lastRemovedIndex = optimizer.ContainerSphereId;

            if (CullingGroup.targetCamera) CullingSpheres[lastRemovedIndex].position = CullingGroup.targetCamera.transform.position + new Vector3(99999, 99999, 99999);
            CullingSpheres[lastRemovedIndex].radius = Mathf.Epsilon;

            Optimizers[lastRemovedIndex] = null;

            MoveStackOptimizerToFreeSlot();
        }


        private void MoveStackOptimizerToFreeSlot()
        {
            FOptimizer_Base optimizerToMove = Optimizers[highestIndex];
            Optimizers[highestIndex] = null;
            highestIndex--;
            BoundingCount--;

            if (optimizerToMove == null) return;
            int freeSlot = lastRemovedIndex;
            lastRemovedIndex = highestIndex + 1;

            CullingSpheres[freeSlot].position = optimizerToMove.GetReferencePosition();
            CullingSpheres[freeSlot].radius = optimizerToMove.DetectionRadius * FOptimizer_Base.GetScaler(optimizerToMove.transform);
            Optimizers[freeSlot] = optimizerToMove;

            optimizerToMove.AssignToContainer(this, freeSlot, ref CullingSpheres[freeSlot]);
        }


        /// <summary>
        /// Culling state changed for one culling sphere from container
        /// </summary>
        private void CullingGroupStateChanged(CullingGroupEvent cullingEvent)
        {
            if (Optimizers[cullingEvent.index] != null)
            {
                //LastEvent[cullingEvent.index] = cullingEvent;
                Optimizers[cullingEvent.index].CullingGroupStateChanged(cullingEvent);
            }
        }


        /// <summary>
        /// Cleaning culling group from memory
        /// </summary>
        public void Dispose()
        {
            CullingGroup.Dispose();
            CullingGroup = null;
            Optimizers = null;
        }


        /// <summary>
        /// Generating id for distance set
        /// </summary>
        public static int GetId(float[] distances)
        {
            int id = distances.Length * 179;
            for (int i = 0; i < distances.Length; i++)
            {
                id += (int)distances[i] / 2;
            }

            return id;
        }
    }
}
