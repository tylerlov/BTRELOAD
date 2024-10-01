using UnityEngine;

public class CustomParentConstraint : MonoBehaviour
{
    [SerializeField] private string targetTag = "ParentTarget";
    [Range(0f, 1f)]
    [SerializeField] private float weight = 1f;

    private Transform target;
    private Vector3 initialLocalPosition;
    private Quaternion initialLocalRotation;
    private Vector3 initialScale;

    private void Start()
    {
        FindTargetByTag();
        StoreInitialTransform();
    }

    private void FindTargetByTag()
    {
        GameObject targetObject = GameObject.FindGameObjectWithTag(targetTag);
        if (targetObject != null)
        {
            target = targetObject.transform;
        }
        else
        {
            Debug.LogWarning($"No object with tag '{targetTag}' found for CustomParentConstraint on {gameObject.name}");
        }
    }

    private void StoreInitialTransform()
    {
        initialLocalPosition = transform.localPosition;
        initialLocalRotation = transform.localRotation;
        initialScale = transform.localScale;
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            FindTargetByTag();
            if (target == null) return;
        }

        // Calculate the weighted position
        Vector3 targetPosition = target.TransformPoint(initialLocalPosition);
        Vector3 weightedPosition = Vector3.Lerp(transform.position, targetPosition, weight);

        // Calculate the weighted rotation
        Quaternion targetRotation = target.rotation * initialLocalRotation;
        Quaternion weightedRotation = Quaternion.Slerp(transform.rotation, targetRotation, weight);

        // Ensure the up vector is aligned with the world up
        Vector3 upVector = Vector3.Slerp(Vector3.up, target.up, weight);
        weightedRotation = Quaternion.FromToRotation(weightedRotation * Vector3.up, upVector) * weightedRotation;

        // Calculate the weighted scale
        Vector3 targetScale = Vector3.Scale(target.lossyScale, initialScale);
        Vector3 weightedScale = Vector3.Lerp(transform.lossyScale, targetScale, weight);

        // Apply the weighted transform
        transform.SetPositionAndRotation(weightedPosition, weightedRotation);
        transform.localScale = weightedScale;
    }

    public void SetTargetTag(string newTag)
    {
        targetTag = newTag;
        FindTargetByTag();
    }

    public void SetWeight(float newWeight)
    {
        weight = Mathf.Clamp01(newWeight);
    }
}