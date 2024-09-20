using System.Collections;
using System.Collections.Generic;
using PrimeTween; // Import PrimeTween namespace
using UnityEngine;

public class LookAtGameObject : MonoBehaviour
{
    public string playerTag = "Player"; // Define playerTag as a public string
    private GameObject player; // Reference to the player GameObject
    public float rotationSpeed = 1f; // Define rotationSpeed as a public float
    public Vector3 offset = Vector3.zero; // Define offset as a public Vector3, default to (0, 0, 0)

    private Tween lookAtTween;

    void Start()
    {
        // Find the player GameObject using its tag
        player = GameObject.FindGameObjectWithTag(playerTag);

        if (player == null)
        {
            Debug.LogError("No GameObject with tag '" + playerTag + "' found!");
        }
    }

    void Update()
    {
        if (player != null)
        {
            // Stop the previous tween if it's still running
            lookAtTween.Stop();

            // Rotate this GameObject to look at the player with an offset using PrimeTween
            Vector3 targetPosition = player.transform.position + offset;
            Quaternion targetRotation = Quaternion.LookRotation(targetPosition - transform.position);
            lookAtTween = Tween.Rotation(transform, targetRotation, rotationSpeed);
        }
    }

    void OnDisable()
    {
        // Stop the tween when the object is disabled
        lookAtTween.Stop();
    }
}
