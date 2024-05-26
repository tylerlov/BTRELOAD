using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileTut : MonoBehaviour
{
    public GameObject impactVFX; 

    private bool collided;

    void Awake()
    {
        // Ensure that all child GameObjects have the same scale as the parent
        foreach (Transform child in transform)
        {
            child.localScale = transform.localScale;
        }
    }

    void OnCollisionEnter (Collision co)
    {
        if(co.gameObject.tag != "Bullet" && co.gameObject.tag != "Player" && !collided)
        {
            collided = true;

            var impact = Instantiate (impactVFX, co.contacts[0].point, Quaternion.identity) as GameObject;

            Destroy (impact, 2);

            Destroy (gameObject);
        }
    }

    void OnDisable()
    {
        // Check if the GameObject is active in the hierarchy, which means it's not just the component being disabled
        if (!gameObject.activeInHierarchy)
        {
            Destroy(gameObject);
        }
    }
}

