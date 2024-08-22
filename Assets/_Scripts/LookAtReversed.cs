using UnityEngine;

public class LookAtReversed : MonoBehaviour
{
    private Transform target;
    public float smoothSpeed = 5f;
    private Quaternion targetRotation;

    void Start()
    {
        target = Camera.main.transform;
    }

    void LateUpdate()
    {
        Vector3 directionToCamera = (target.position - transform.position).normalized;
        targetRotation = Quaternion.LookRotation(-directionToCamera);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            smoothSpeed * Time.deltaTime
        );
    }
}
