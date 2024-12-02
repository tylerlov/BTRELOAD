using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PrimeTween;

public class LookAtCamera : MonoBehaviour
{
    public Transform LookAtObject;
    private Tween _lookTween;

    // Update is called once per frame

    private void Start()
    {
        if (LookAtObject == null)
        {
            LookAtObject = GameObject.FindGameObjectWithTag("Player").transform;
        }
    }

    void FixedUpdate()
    {
        // Stop any existing look tween
        _lookTween.Stop();

        // Calculate the look-at rotation
        Vector3 targetPosition = 2 * transform.position - LookAtObject.position;
        Quaternion targetRotation = Quaternion.LookRotation(targetPosition - transform.position);

        // Create a new tween for smooth rotation
        _lookTween = Tween.Rotation(transform, targetRotation, 0.1f);
    }

    private void OnDisable()
    {
        // Clean up tween when disabled
        _lookTween.Stop();
    }
}
