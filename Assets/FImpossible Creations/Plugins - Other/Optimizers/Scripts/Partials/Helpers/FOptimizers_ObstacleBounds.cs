using UnityEngine;

namespace FIMSpace.FOptimizing
{
    /// <summary>
    /// Helper class suporrting detecting visibility of objects behind obstacles
    /// </summary>
    public class FOptimizers_ObstacleBounds
    {
        public Bounds Bounds;
        public Vector3 Normal;
        public Vector3 CastDirection;

        public FOptimizers_ObstacleBounds(RaycastHit hit, Vector3 castDirection)
        {
            Bounds = hit.collider.bounds;
            Normal = hit.normal;
            CastDirection = castDirection;
        }
    }
}
