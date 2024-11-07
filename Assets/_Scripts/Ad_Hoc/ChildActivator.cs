using System.Collections;
using SensorToolkit;
using UnityEngine;

public class ChildActivator : MonoBehaviour
{
    public bool isActive = true; // Public boolean to control activation

    private void Awake()
    {
        if (isActive)
        {
            SetChildrenActive(false);
        }
    }

    public void SetChildrenActive(bool isActive)
    {
        if (!this.isActive)
            return; // Check if the script is active

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
            ConditionalDebug.Log($"Re-registering {child.name}'s StaticEnemyShooting script.");
            shootingScript.OnEnable();
        }
    }
}
