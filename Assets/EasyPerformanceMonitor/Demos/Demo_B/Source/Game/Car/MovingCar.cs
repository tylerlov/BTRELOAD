// Unity
using UnityEngine;

namespace GUPS.EasyPerformanceMonitor.Demos.B
{
    /// <summary>
    /// Move the car over the map and destroy it, if it moves out of the map.
    /// </summary>
    public class MovingCar : MonoBehaviour
    {
        /// <summary>
        /// The speed the car is moving.
        /// </summary>
        public float Speed { get; set; } = 1.0f;

        /// <summary>
        /// The direction the car is moving.
        /// </summary>
        public Vector2 Direction { get; set; } = Vector2.down;

        /// <summary>
        /// Update the car movement and destroy if out of map.
        /// </summary>
        private void Update()
        {
            // Movement direction.
            Vector3 var_Movement = new Vector3(this.Direction.x, 0, this.Direction.y) * this.Speed * Time.deltaTime;

            // Move.
            this.transform.position = new Vector3(this.transform.position.x + var_Movement.x, this.transform.position.y + var_Movement.y, this.transform.position.z + var_Movement.z);

            // Delete car if out of map.
            if(this.transform.position.x <= -5 || this.transform.position.x >= 45
                || this.transform.position.z <= -5 || this.transform.position.z >= 35)
            {
                Destroy(this.gameObject);
            }
        }
    }
}