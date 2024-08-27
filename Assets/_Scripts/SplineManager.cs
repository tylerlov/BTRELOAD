using System.Collections;
using System.Collections.Generic;
using Dreamteck.Splines;
using FluffyUnderware.Curvy;
using FluffyUnderware.Curvy.Controllers;
using UnityEngine;
using UnityEngine.Serialization;

public class SplineManager : MonoBehaviour
{
    public enum ClampingType
    {
        Clamp,
        Loop,
        PingPong,
    }

    [System.Serializable]
    public struct SplineData
    {
        public GameObject Spline;
        public float BaseSpeed; // Renamed to BaseSpeed for clarity
        public CurvyClamping Clamping; // added new field
        public bool Reverse; // New attribute
    }

    private SplineController splineController;

    [SerializeField]
    GameObject SplineContainer; // Renamed from Splines
    private GameObject shooting;
    public List<SplineData> splineDatas;
    int currSpline;

    [SerializeField]
    private float speedMultiplier = 1.0f; // Add this field to adjust speed globally
    private bool isSplineNeeded => splineDatas.Count > 0; // Dynamically determine if spline is needed

    private bool canIncrement = true;
    private float incrementCooldown = 0.5f; // Cooldown duration between spline increments

    private int splineSwitchCounter = 0; // Add this line
    private float lastSwitchTime = 0f; // Add this line
    private const float MIN_SWITCH_INTERVAL = 0.5f; // Add this line

    public delegate void FinalSplineReachedHandler();
    public event FinalSplineReachedHandler OnFinalSplineReached;

    void Awake()
    {
        // Find the PlayerPlane and get its SplineController
        GameObject playerPlane = GameObject.FindGameObjectWithTag("PlayerPlane");
        if (playerPlane != null)
        {
            splineController = playerPlane.GetComponent<SplineController>();
            if (splineController == null)
            {
                Debug.LogError("SplineController not found on PlayerPlane!");
            }
        }
        else
        {
            Debug.LogError("PlayerPlane not found!");
        }

        // Find the Shooting object
        shooting = GameObject.FindGameObjectWithTag("Shooting");
        if (shooting == null)
        {
            Debug.LogError("Shooting object not found!");
        }

        currSpline = 0;

        if (isSplineNeeded && splineController != null)
        {
            SetSplineDataAttributes(currSpline);
        }
    }

    void SetSplineDataAttributes(int splineIndex)
    {
        if (splineIndex >= 0 && splineIndex < splineDatas.Count) // Additional safety check
        {
            SplineData data = splineDatas[splineIndex];
            splineController.Spline = data.Spline.GetComponent<CurvySpline>();
            splineController.Speed = data.BaseSpeed * speedMultiplier; // Apply multiplier
            splineController.Clamping = data.Clamping;

            // Set the movement direction based on the Reverse attribute
            splineController.MovementDirection = data.Reverse
                ? MovementDirection.Backward
                : MovementDirection.Forward;
        }
    }

    private void PerformSplineIncrement()
    {
        if (isSplineNeeded && currSpline < splineDatas.Count - 1)
        {
            float currentTime = Time.time;
            if (currentTime - lastSwitchTime < MIN_SWITCH_INTERVAL)
            {
                Debug.LogWarning(
                    $"Spline switch attempted too soon. Time since last switch: {currentTime - lastSwitchTime}"
                );
                return;
            }

            currSpline++;
            SetSplineDataAttributes(currSpline);
            splineSwitchCounter++;
            lastSwitchTime = currentTime;
            Debug.Log(
                $"Spline Incremented. Current spline: {currSpline}, Total switches: {splineSwitchCounter}, Time: {currentTime}"
            );

            // Check if we've reached the final spline
            if (currSpline == splineDatas.Count - 1)
            {
                OnFinalSplineReached?.Invoke();
            }
        }
        else
        {
            Debug.Log("Next Spline is not available, maintaining current Spline");
        }
    }

    public void IncrementSpline()
    {
        Debug.Log($"IncrementSpline called from:\n{UnityEngine.StackTraceUtility.ExtractStackTrace()}");
        if (!canIncrement)
        {
            Debug.Log($"Increment attempted during cooldown, ignoring. Time since last increment: {Time.time - lastSwitchTime}");
            return;
        }

        PerformSplineIncrement();
        StartCoroutine(IncrementCooldown());
    }

    private IEnumerator IncrementCooldown()
    {
        canIncrement = false;
        yield return new WaitForSeconds(incrementCooldown);
        canIncrement = true;
    }

    // Remove the lockShooter coroutine as it's no longer needed
    // Mayu need to bring this back, but for now, it's not needed

    // ... (keep other existing methods)
}
