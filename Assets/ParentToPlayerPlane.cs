using UnityEngine;

public class ParentToPlayerPlane : MonoBehaviour
{
    [SerializeField] private Vector3 positionOffset = Vector3.zero;

    private void Start()
    {
        GameObject playerPlane = GameObject.FindGameObjectWithTag("PlayerPlane");
        
        if (playerPlane != null)
        {
            transform.SetParent(playerPlane.transform, false);
            transform.localPosition = positionOffset;
            transform.forward = playerPlane.transform.forward;
        }
        else
        {
            Debug.LogWarning("No GameObject with tag 'PlayerPlane' found in the scene.");
        }
    }
}