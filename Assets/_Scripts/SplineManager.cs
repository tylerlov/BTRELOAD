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

    [SerializeField] SplineController splineController;
    [SerializeField] GameObject Splines;
    public GameObject Shooting;
    public List<SplineData> splineDatas;
    int currSpline;

    [SerializeField] private float speedMultiplier = 1.0f; // Add this field to adjust speed globally
    private bool isSplineNeeded => splineDatas.Count > 0; // Dynamically determine if spline is needed

    void Awake()
    {
        splineController = gameObject.GetComponent<SplineController>();
        currSpline = 0;

        if (isSplineNeeded) // Check if spline is needed
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

    public void IncrementSpline()
    {
        if (isSplineNeeded && currSpline < splineDatas.Count - 1) 
        {
            currSpline++;
            SetSplineDataAttributes(currSpline);
            StartCoroutine(lockShooter(2f));

            ConditionalDebug.Log("Spline Incremented");
        }
        else
        {
            ConditionalDebug.Log("Next Spline is not available, maintaining current Spline");
        }
    }

    IEnumerator lockShooter(float waitTime)
    {
        float timer = 0f;
        while (timer < waitTime)
        {
            Shooting.transform.localPosition = new Vector3(0, 0, Shooting.transform.localPosition.z);
            timer += Time.deltaTime;
            yield return null;
        }
    }
}