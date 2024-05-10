using UnityEngine;

namespace OccaSoftware.LocalGI.Runtime
{
    /// <summary>
    /// Represents a script component that makes an object follow a target.
    /// </summary>
    public class FollowTarget : MonoBehaviour
    {
        /// <summary>
        /// A Transform that represents the target to follow.
        /// </summary>
        [SerializeField]
        Transform target;

        private void LateUpdate()
        {
            if (target == null)
                return;

            transform.position = target.position;
        }
    }
}
