using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class LookAtCamera : MonoBehaviour
{
    public Transform LookAtObject;
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
        transform.DOLookAt(2 * transform.position - LookAtObject.position, 0, AxisConstraint.None, null);
        //transform.LookAt(LookAtObject);
        //transform.LookAt(2 * transform.position - LookAtObject.position);
    }
}
