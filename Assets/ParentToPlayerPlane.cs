using UnityEngine;

public class ParentToPlayerPlane : MonoBehaviour
{
    private void Start()
    {
        GameObject playerPlane = GameObject.FindGameObjectWithTag("PlayerPlane");
        
        if (playerPlane != null)
        {
            transform.SetParent(playerPlane.transform, false);
            transform.localPosition = Vector3.zero;
            transform.forward = playerPlane.transform.forward;
        }
        else
        {
            Debug.LogWarning("No GameObject with tag 'PlayerPlane' found in the scene.");
        }
    }
}