using Chronos;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateRing : MonoBehaviour
{
    public Vector3 speed;
    public int runningSpeed;
    private Clock clock;
    Transform trs;

    // Start is called before the first frame update
    void Start()
    {
        trs = transform;
        clock = GetComponent<Clock>();

    }

    // Update is called once per frame
    void Update()
    {
        float scaledRotationSpeedX = speed.x * clock.deltaTime;
        float scaledRotationSpeedY = speed.y * clock.deltaTime;
        float scaledRotationSpeedZ = speed.z * clock.deltaTime;

        transform.Rotate(scaledRotationSpeedX, scaledRotationSpeedY, scaledRotationSpeedZ, Space.Self);

    }

    public void randomSpeeds()
    {
        speed = new Vector3(Random.Range(-10,10), Random.Range(-10, 10), Random.Range(-10, 10));
    }

    public void setRunningSpeed()
    {
        speed = new Vector3(0, 0, runningSpeed);
    }
}

