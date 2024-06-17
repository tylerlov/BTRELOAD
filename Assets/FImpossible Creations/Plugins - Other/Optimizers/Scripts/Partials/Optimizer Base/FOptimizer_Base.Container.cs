using UnityEngine;

namespace FIMSpace.FOptimizing
{
    public abstract partial class FOptimizer_Base
    {
        public int ContainerGeneratedID { get; private set; }

        /// <summary> To what culling container belongs this optimizer </summary>
        public FOptimizers_CullingContainer OwnerContainer { get; private set; }

        /// <summary> Id of optimizer inside it's container </summary>
        public int ContainerSphereId { get; private set; }

        [Tooltip("Adding optimizer to culling container - when used a lot of objects with same distance levels and LOD levels count it can boost performance a lot.")]
        public bool AddToContainer = true;

        internal void AssignToContainer(FOptimizers_CullingContainer container, int sphereId, ref BoundingSphere sphere)
        {
            OwnerContainer = container;
            ContainerSphereId = sphereId;
            mainVisibilitySphere = sphere;
        }

    }
}
