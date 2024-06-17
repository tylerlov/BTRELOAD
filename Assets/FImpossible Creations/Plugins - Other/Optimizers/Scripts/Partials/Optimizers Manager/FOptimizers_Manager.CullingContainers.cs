using System.Collections.Generic;
using UnityEngine;

namespace FIMSpace.FOptimizing
{
    public partial class FOptimizers_Manager
    {
        /// <summary> Lists of containers, each key can have multiple containers with limited capacity </summary>
        public Dictionary<int, FOptimizers_CullingContainersList> CullingContainersIDSpecific { get; private set; }
        //public List<FOptimizers_CullingContainer> c; // DEBUG

        /// <summary>
        /// Adding optimizer to culling container for optimal detection.
        /// If there is no contianer for target parameters there is created new.
        /// If there is no slots in containers new one is generated.
        /// </summary>
        internal void AddToContainer(FOptimizer_Base optimizer)
        {
            if (optimizer == null) return;

            FOptimizers_CullingContainersList containersIdSpecific;
            FOptimizers_CullingContainer container = null;

            // Getting containers list for optimizer's id (from optimizer's distance settings and LOD count)
            if (CullingContainersIDSpecific.TryGetValue(optimizer.ContainerGeneratedID, out containersIdSpecific))
            {
                // Searching for container with free slots
                for (int i = 0; i < containersIdSpecific.Count; i++)
                {
                    if (containersIdSpecific[i].HaveFreeSlots)
                    {
                        container = containersIdSpecific[i];
                        break;
                    }
                }

                // There is no containers with free slots for this settings
                if (container == null)
                {
                    // Let's generate new one and add to containers list for this settings (stored in id)
                    container = GenerateNewContainer(optimizer);
                    containersIdSpecific.Add(container);
                }
            }
            else
            {
                // Generating new container list for specified settings (stored in id)
                containersIdSpecific = new FOptimizers_CullingContainersList(optimizer.ContainerGeneratedID);

                // Adding container 
                container = GenerateNewContainer(optimizer);
                containersIdSpecific.Add(container);

                // Adding containers list to manager data
                CullingContainersIDSpecific.Add(optimizer.ContainerGeneratedID, containersIdSpecific);
            }

            //if (c == null) c = new List<FOptimizers_CullingContainer>();
            //if (!c.Contains(container)) c.Add(container); // DEBUG

            // Adding optimizer for target container
            container.AddOptimizer(optimizer);
        }


        /// <summary>
        /// Generating container for container list basing on optimizer's parameters
        /// </summary>
        private FOptimizers_CullingContainer GenerateNewContainer(FOptimizer_Base optimizer)
        {
            FOptimizers_CullingContainer container = new FOptimizers_CullingContainer();
            container.InitializeContainer(optimizer.ContainerGeneratedID, optimizer.GetDistanceMeasures(), TargetCamera);
            return container;
        }


        /// <summary>
        /// Removing optimizer from culling container and clearing it's existence
        /// </summary>
        internal void RemoveFromContainer(FOptimizer_Base optimizer)
        {
            if (optimizer == null) return;
            optimizer.OwnerContainer.RemoveOptimizer(optimizer);
        }


        void OnDestroy()
        {
            ClearCullingContainers();
        }

        /// <summary>
        /// Removing culling containers from memory
        /// </summary>
        internal void ClearCullingContainers()
        {
            if (CullingContainersIDSpecific != null)
            {
                foreach (var item in CullingContainersIDSpecific)
                {
                    item.Value.Dispose();
                }

                CullingContainersIDSpecific.Clear();
            }
        }
    }
}
