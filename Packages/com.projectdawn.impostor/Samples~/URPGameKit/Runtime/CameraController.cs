using UnityEngine;

namespace ProjectDawn.Impostor.Samples.GameKit
{
    [ExecuteInEditMode]
    public class CameraController : MonoBehaviour
    {
        public Transform target; // The object to look at and rotate around
        public float distance = 5.0f; // The initial distance from the target
        public float minDistance = 2.0f; // The minimum distance from the target
        public float maxDistance = 20.0f; // The maximum distance from the target
        public float xSpeed = 120.0f; // The speed of horizontal rotation
        public float ySpeed = 120.0f; // The speed of vertical rotation
        public float yMinLimit = -20.0f; // The minimum vertical angle
        public float yMaxLimit = 80.0f; // The maximum vertical angle
        public float zoomSpeed = 5.0f; // The speed of zooming in and out

        private float x = 0.0f; // The current horizontal angle
        private float y = 0.0f; // The current vertical angle

        private void Start()
        {
            Vector3 angles = transform.eulerAngles;
            x = angles.y;
            y = angles.x;
        }

        private void LateUpdate()
        {
            var targetPosition = target ? target.position : Vector3.zero;

            // Check for right mouse button down and drag
            if (Input.GetMouseButton(1))
            {
                x += Input.GetAxis("Mouse X") * xSpeed * distance * 0.02f;
                y -= Input.GetAxis("Mouse Y") * ySpeed * 0.02f;

                // Clamp the vertical angle to the limits
                y = ClampAngle(y, yMinLimit, yMaxLimit);
            }

            // Check for mouse wheel input
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0.0f)
            {
                distance -= scroll * zoomSpeed;
                distance = Mathf.Clamp(distance, minDistance, maxDistance);
            }

            Quaternion rotation = Quaternion.Euler(y, x, 0.0f);
            Vector3 position = rotation * new Vector3(0.0f, 0.0f, -distance) + targetPosition;

            transform.rotation = rotation;
            transform.position = position;
        }

        private static float ClampAngle(float angle, float min, float max)
        {
            if (angle < -360.0f)
            {
                angle += 360.0f;
            }

            if (angle > 360.0f)
            {
                angle -= 360.0f;
            }

            return Mathf.Clamp(angle, min, max);
        }
    }
}