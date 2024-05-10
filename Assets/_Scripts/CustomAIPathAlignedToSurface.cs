
using UnityEngine;
using Pathfinding;
using Chronos;

namespace Pathfinding
{
    public class CustomAIPathAlignedToSurface : AIPathAlignedToSurface
    {
        public Timeline clock; // Chronos local clock

        private void Awake()
        {
            clock = GetComponent<Timeline>();
        }

        void Update()
        {
            base.OnUpdate(clock.deltaTime); // Use the Chronos time instead of Unity time

            // Get the nearest walkable node
            Vector3 nearestPosition = AstarPath.active.GetNearest(transform.position).position;

            if (float.IsNaN(nearestPosition.x) || float.IsNaN(nearestPosition.y) || float.IsNaN(nearestPosition.z) ||
                float.IsInfinity(nearestPosition.x) || float.IsInfinity(nearestPosition.y) || float.IsInfinity(nearestPosition.z))
            {
                // Handle the invalid position, e.g., skip updating the position or assign a default position
                ConditionalDebug.LogWarning("Invalid position detected: " + nearestPosition);
            }
            else
            {
                transform.position = nearestPosition;
            }
        }

    }
}

