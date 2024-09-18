using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerGroundAdjuster : MonoBehaviour
{
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float desiredHeight = 0.1f;
    [SerializeField] private float baseAdjustmentSpeed = 5f;
    [SerializeField] private float maxRaycastDistance = 10f;
    [SerializeField] private float largeOffsetThreshold = 0.5f;
    [SerializeField] private float smallOffsetThreshold = 0.01f;
    [SerializeField] private float largeOffsetMultiplier = 2f;

    private float currentOffset;
    private float previousOffset;

    private void Update()
    {
        AdjustHeight();
    }

    private void AdjustHeight()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, maxRaycastDistance, groundLayer))
        {
            Vector3 targetPosition = hit.point + Vector3.up * desiredHeight;
            float offsetDifference = Mathf.Abs(currentOffset - (transform.position.y - hit.point.y));

            float adjustmentSpeed = baseAdjustmentSpeed;

            // Increase speed for large changes
            if (offsetDifference > largeOffsetThreshold)
            {
                adjustmentSpeed *= largeOffsetMultiplier;
            }
            // Minimize adjustment for very small changes
            else if (offsetDifference < smallOffsetThreshold)
            {
                adjustmentSpeed *= 0.1f;
            }

            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * adjustmentSpeed);
            
            previousOffset = currentOffset;
            currentOffset = transform.position.y - hit.point.y;
        }
    }

    // DebugVisualize method removed
}
