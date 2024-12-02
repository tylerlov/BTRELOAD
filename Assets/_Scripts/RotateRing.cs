using System.Collections;
using System.Collections.Generic;
using Chronos;
using UnityEngine;

public class RotateRing : MonoBehaviour
{
    public Vector3 speed;
    public int runningSpeed;
    private Clock clock;
    Transform trs;

    private const float ClockCheckInterval = 0.5f;
    private float lastClockCheckTime;

    // Start is called before the first frame update
    void Start()
    {
        trs = transform;
        clock = GetComponent<Clock>();
    }

    // Update is called once per frame
    void Update()
    {
        if (clock == null || !clock.enabled)
        {
            if (Time.time - lastClockCheckTime > ClockCheckInterval)
            {
                TryEnableClock();
                lastClockCheckTime = Time.time;
            }
            return;
        }

        float scaledRotationSpeedX = speed.x * clock.deltaTime;
        float scaledRotationSpeedY = speed.y * clock.deltaTime;
        float scaledRotationSpeedZ = speed.z * clock.deltaTime;

        trs.Rotate(scaledRotationSpeedX, scaledRotationSpeedY, scaledRotationSpeedZ, Space.Self);
    }

    private void TryEnableClock()
    {
        if (clock == null)
        {
            clock = GetComponent<Clock>();
        }

        if (clock != null && !clock.enabled)
        {
            clock.enabled = true;
            ConditionalDebug.Log("Re-enabled clock on " + gameObject.name);
        }
    }

    public void randomSpeeds()
    {
        speed = new Vector3(Random.Range(-10, 10), Random.Range(-10, 10), Random.Range(-10, 10));
    }

    public void setRunningSpeed()
    {
        speed = new Vector3(0, 0, runningSpeed);
    }
}
