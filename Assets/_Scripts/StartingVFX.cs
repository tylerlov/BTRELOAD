using UnityEngine;

public class StartingVFX : MonoBehaviour
{
    public string triggeringTag = "Player";

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(triggeringTag))
        {
            ActivateChild();
        }
    }

    private void ActivateChild()
    {
        if (transform.childCount > 0)
        {
            transform.GetChild(0).gameObject.SetActive(true);
        }
    }
}
