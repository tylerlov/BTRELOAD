using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlanetScript : MonoBehaviour
{

    public float gravity = -12;
    public void Attract(Transform reticle)
    {
        Vector3 gravityUp = (reticle.position - transform.position).normalized;
        Vector3 localUp = reticle.up;

        reticle.GetComponent<Rigidbody>().AddForce(gravityUp * gravity);

        Quaternion targetRotation = Quaternion.FromToRotation(localUp, gravityUp) * reticle.rotation;
        reticle.rotation = Quaternion.Slerp(reticle.rotation, targetRotation, 50f * Time.deltaTime);
    }
}