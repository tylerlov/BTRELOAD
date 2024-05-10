using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening; // Import DoTween namespace

public class LookAtGameObject : MonoBehaviour
{
    public string playerTag = "Player"; // Define playerTag as a public string
    private GameObject player; // Reference to the player GameObject
    public float rotationSpeed = 1f; // Define rotationSpeed as a public float
    public Vector3 offset = Vector3.zero; // Define offset as a public Vector3, default to (0, 0, 0)

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
            // Rotate this GameObject to look at the player with an offset using DoTween
            Vector3 targetPosition = player.transform.position + offset;
            transform.DOLookAt(targetPosition, rotationSpeed);
        }
    }
}
