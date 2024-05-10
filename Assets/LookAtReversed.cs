using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAtReversed : MonoBehaviour
{
    private Transform target;

    void Start()
    {
        // Cache the reference to the main camera's transform at start
        target = Camera.main.transform;
    }

    void Update()
    {
        // No need to check for null in every frame if the main camera is guaranteed to exist
        Vector3 directionToCamera = (target.position - transform.position).normalized;
        Quaternion rotation = Quaternion.LookRotation(-directionToCamera);
        transform.rotation = rotation;
    }
}
