using UnityEngine;
using SensorToolkit;

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
        }
    }
}