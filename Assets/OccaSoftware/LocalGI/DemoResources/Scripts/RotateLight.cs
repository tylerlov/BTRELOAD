using UnityEngine;

namespace OccaSoftware.LocalGI.Demo
{
    public class RotateLight : MonoBehaviour
    {
        [SerializeField]
        float rate = 360f;

        void Update()
        {
            transform.Rotate(0, rate * Time.deltaTime, 0);
        }
    }
}
