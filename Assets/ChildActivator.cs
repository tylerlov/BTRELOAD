using UnityEngine;
using SensorToolkit;
using System.Collections;

public class ChildActivator : MonoBehaviour
{
    private void Awake() {
        SetChildrenActive(false);
    }

    public void SetChildrenActive(bool isActive)
    {
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(isActive);
            if (isActive)
            {
                StartCoroutine(EnableShootingScriptWithDelay(child));
            }
        }
    }

    private IEnumerator EnableShootingScriptWithDelay(Transform child)
    {
        yield return null; // Wait for one frame to ensure all dependencies are initialized

        StaticEnemyShooting shootingScript = child.GetComponent<StaticEnemyShooting>();
        if (shootingScript != null)
        {
            Debug.Log($"Re-registering {child.name}'s StaticEnemyShooting script.");
            shootingScript.OnEnable();
        }
    }
}
