using UnityEngine;

public class FollowExactPosition : MonoBehaviour
{
    [Tooltip("The GameObject to follow")]
    public GameObject targetToFollow;

    [Tooltip("If true, will also match the rotation of the target")]
    public bool matchRotation = false;

    [Tooltip("Offset from the target's position")]
    public Vector3 positionOffset = Vector3.zero;

    private Transform thisTransform;
    private Transform targetTransform;

    private void Awake()
    {
        thisTransform = transform;
        
        if (targetToFollow != null)
        {
            targetTransform = targetToFollow.transform;
        }
        else
        {
            Debug.LogWarning("No target set for FollowExactPosition on " + gameObject.name);
        }
    }

    private void LateUpdate()
    {
        if (targetTransform != null)
        {
            // Directly set the position with offset
            thisTransform.position = targetTransform.position + targetTransform.TransformDirection(positionOffset);

            if (matchRotation)
            {
                thisTransform.rotation = targetTransform.rotation;
            }
        }
    }

    public void SetNewTarget(GameObject newTarget)
    {
        targetToFollow = newTarget;
        targetTransform = newTarget != null ? newTarget.transform : null;
    }

    private void OnDrawGizmosSelected()
    {
        if (targetToFollow != null)
        {
            Gizmos.color = Color.yellow;
            Vector3 offsetPosition = targetToFollow.transform.position + targetToFollow.transform.TransformDirection(positionOffset);
            Gizmos.DrawLine(targetToFollow.transform.position, offsetPosition);
            Gizmos.DrawWireSphere(offsetPosition, 0.1f);
        }
    }
}