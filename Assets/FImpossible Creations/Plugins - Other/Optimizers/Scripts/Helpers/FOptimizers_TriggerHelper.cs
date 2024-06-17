using UnityEngine;

namespace FIMSpace.FOptimizing
{
    [AddComponentMenu("FImpossible Creations/Hidden/Trigger Helper")]
    public class FOptimizers_TriggerHelper : MonoBehaviour
    {
        public FOptimizer_Base Optimizer;
        public int TriggerIndex = -1;
        //Transform collided;

        public FOptimizers_TriggerHelper Initialize(FOptimizer_Base optimizer, int index)
        {
            Optimizer = optimizer;
            TriggerIndex = index;
            return this;
        }

        void OnTriggerEnter(Collider other)
        {
            if (Optimizer == null) { Destroy(gameObject); return; }

            if (other.transform != Optimizer.TargetCamera) return;

            //collided = other.transform;
            Optimizer.OnTriggerChange(this, false);
        }


        void OnTriggerExit(Collider other)
        {
            if (Optimizer == null) { Destroy(gameObject); return; }
            if (other.transform != Optimizer.TargetCamera) return;

            //collided = other.transform;
            Optimizer.OnTriggerChange(this, true);
        }

    }
}