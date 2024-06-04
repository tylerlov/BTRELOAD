using UnityEngine;

public class ParallelPosition : MonoBehaviour
{
    public Transform target;

    void Update()
    {
        if (target != null)
        {
            transform.position = target.position;
        }
    }
}