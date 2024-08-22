using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using SonicBloom.Koreo;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement; // Include the SceneManager namespace

public class KoreographerSpinEvent : MonoBehaviour
{
    [EventID]
    public string koreoEventID = "Perc Half Half Half"; // Default Koreo Event ID

    [SerializeField]
    private RotationAxis rotationAxis = RotationAxis.Z; // Default Rotation Axis

    [SerializeField]
    private RotationAmount rotationAmount = RotationAmount.TwoHundredFiftySixth; // Default Rotation Amount

    [SerializeField]
    private float spinTime = 0.01f; // Default Spin Time

    private Vector3 rotationVector;

    public enum RotationAxis
    {
        X,
        Y,
        Z,
    }

    public enum RotationAmount
    {
        Full,
        Half,
        Quarter,
        Eighth,
        Sixteenth,
        ThirtySecond,
        SixtyFourth,
        OneHundredTwentyEighth,
        TwoHundredFiftySixth,
        FiveHundredTwelfth,
        OneThousandTwentyFourth,
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded; // Subscribe to the sceneLoaded event
        RegisterKoreoEvents();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded; // Unsubscribe from the sceneLoaded event
        if (Koreographer.Instance != null)
        {
            Koreographer.Instance.UnregisterForEvents(koreoEventID, OnKoreoEvent); // Unregister to avoid duplicates
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RegisterKoreoEvents(); // Re-register for Koreo events when a new scene is loaded
    }

    private void RegisterKoreoEvents()
    {
        if (Koreographer.Instance != null) // Check if Koreographer instance exists
        {
            Koreographer.Instance.RegisterForEvents(koreoEventID, OnKoreoEvent);
            SetRotationVector();
        }
    }

    private void OnKoreoEvent(KoreographyEvent koreoEvent)
    {
        if (this == null || gameObject == null)
            return; // Check if the object is destroyed

        float spinAmount = 360f;

        switch (rotationAmount)
        {
            case RotationAmount.Half:
                spinAmount *= 0.5f;
                break;
            case RotationAmount.Quarter:
                spinAmount *= 0.25f;
                break;
            case RotationAmount.Eighth:
                spinAmount *= 0.125f;
                break;
            case RotationAmount.Sixteenth:
                spinAmount *= 0.0625f;
                break;
            case RotationAmount.ThirtySecond:
                spinAmount *= 0.03125f;
                break;
            case RotationAmount.SixtyFourth:
                spinAmount *= 0.015625f;
                break;
            case RotationAmount.OneHundredTwentyEighth:
                spinAmount *= 0.0078125f;
                break;
            case RotationAmount.TwoHundredFiftySixth:
                spinAmount *= 0.00390625f;
                break;
            case RotationAmount.FiveHundredTwelfth:
                spinAmount *= 0.001953125f;
                break;
            case RotationAmount.OneThousandTwentyFourth:
                spinAmount *= 0.0009765625f;
                break;
        }

        transform
            .DOLocalRotate(rotationVector * spinAmount, spinTime, RotateMode.LocalAxisAdd)
            .SetEase(Ease.OutSine);
    }

    private void SetRotationVector()
    {
        rotationVector = Vector3.zero;

        switch (rotationAxis)
        {
            case RotationAxis.X:
                rotationVector = Vector3.right;
                break;
            case RotationAxis.Y:
                rotationVector = Vector3.up;
                break;
            case RotationAxis.Z:
                rotationVector = Vector3.forward;
                break;
        }
    }
}
