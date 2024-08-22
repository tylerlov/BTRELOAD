using UnityEngine;

public class YAxisRotationLimiter : MonoBehaviour
{
    public float minYRotation = -45.0f; // Lower limit of Y rotation
    public float maxYRotation = 45.0f; // Upper limit of Y rotation

    void Update()
    {
        Quaternion currentRotation = transform.rotation;
        float yRotation = NormalizeAngle(transform.eulerAngles.y);
        yRotation = Mathf.Clamp(yRotation, minYRotation, maxYRotation);
        transform.rotation = Quaternion.Euler(
            transform.eulerAngles.x,
            yRotation,
            transform.eulerAngles.z
        );
    }

    float NormalizeAngle(float angle)
    {
        while (angle > 180)
            angle -= 360;
        while (angle < -180)
            angle += 360;
        return angle;
    }
}
