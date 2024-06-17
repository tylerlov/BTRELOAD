using UnityEngine;

namespace FIMSpace.FOptimizing
{
    public abstract partial class FOptimizer_Base
    {
        private void InitEffectiveOptimizer()
        {
            if (!AddToContainer) FOptimizers_Manager.Get.RegisterNotContainedEffectiveOptimizer(this, true);

            InitCullingGroups(GetDistanceMeasures(), DetectionRadius, FOptimizers_Manager.MainCamera);
            InitDynamicOptimizer(false);
        }

        private void EffectiveLODUpdate()
        {
            float dist = (PreviousPosition - mainVisibilitySphere.position).magnitude;
            if (dist > moveTreshold) RefreshEffectiveCullingGroups();
        }

        protected virtual void RefreshEffectiveCullingGroups()
        {
            if (OwnerContainer != null)
            {
                OwnerContainer.CullingSpheres[ContainerSphereId].position = GetReferencePosition();
            }
            else
                mainVisibilitySphere.position = GetReferencePosition();
        }
    }
}
