using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FluffyUnderware.Curvy;
using FluffyUnderware.Curvy.Controllers;
using Dreamteck.Splines;
using UnityEngine.Serialization;

public class SplineManager : MonoBehaviour
{
    public enum ClampingType
    {
        Clamp,
        Loop,
        PingPong
    }

    [System.Serializable]
    public struct SplineData
    {
        public GameObject Spline;
        public float BaseSpeed; // Renamed to BaseSpeed for clarity
        public CurvyClamping Clamping; // added new field
    }

    private SplineController splineController;
    [SerializeField] GameObject Splines;
    private GameObject shooting;
    public List<SplineData> splineDatas;
    int currSpline;

    [SerializeField] private float speedMultiplier = 1.0f; // Add this field to adjust speed globally
    private bool isSplineNeeded => splineDatas.Count > 0; // Dynamically determine if spline is needed

    private bool canIncrement = true;
    private float incrementCooldown = 0.5f; // Cooldown duration between spline increments

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
            splineController.Spline = splineDatas[splineIndex].Spline.GetComponent<CurvySpline>();
            splineController.Speed = splineDatas[splineIndex].BaseSpeed * speedMultiplier; // Apply multiplier
            splineController.Clamping = splineDatas[splineIndex].Clamping;
        }
    }

    private void PerformSplineIncrement()
    {
        if (isSplineNeeded && currSpline < splineDatas.Count - 1)
        {
            currSpline++;
            SetSplineDataAttributes(currSpline);
            Debug.Log("Spline Incremented");
        }
        else
        {
            Debug.Log("Next Spline is not available, maintaining current Spline");
        }
    }

    public void IncrementSpline()
    {
        if (!canIncrement) 
        {
            Debug.Log("Increment attempted during cooldown, ignoring.");
            return;
        }

        PerformSplineIncrement();
        StartCoroutine(lockShooter(2f));
        StartCoroutine(IncrementCooldown());
    }

    private IEnumerator IncrementCooldown()
    {
        canIncrement = false;
        yield return new WaitForSeconds(incrementCooldown);
        canIncrement = true;
    }

    IEnumerator lockShooter(float waitTime)
    {
        if (shooting != null)
        {
            float timer = 0f;
            while (timer < waitTime)
            {
                shooting.transform.localPosition = new Vector3(0, 0, shooting.transform.localPosition.z);
                timer += Time.deltaTime;
                yield return null;
            }
            
            // Perform the second increment after the lock period
            PerformSplineIncrement();
        }
        else
        {
            Debug.LogWarning("Shooting object is null, cannot lock shooter.");
        }
    }
}