using UnityEngine;

[ExecuteInEditMode]
public class PrefabGenerator : MonoBehaviour
{
    public GameObject parentObject;
    public GameObject prefab;
    public Transform newParent;
    public bool placePrefabsOnChildren;
    public Vector3 customRotation;
    public bool useCustomRotation;
    private GameObject[] generatedPrefabs;

    public void GeneratePrefabs()
    {
        if (parentObject == null || prefab == null)
        {
            Debug.LogWarning("Parent Object and Prefab must be set in the inspector");
            return;
        }

        // Keep track of generated prefabs for undo
        generatedPrefabs = new GameObject[parentObject.transform.childCount];

        int i = 0;
        foreach (Transform child in parentObject.transform)
        {
            GameObject newPrefabInstance = Instantiate(prefab, child.position,
                useCustomRotation ? Quaternion.Euler(customRotation) : child.rotation);
            generatedPrefabs[i] = newPrefabInstance;

            if (placePrefabsOnChildren)
            {
                newPrefabInstance.transform.SetParent(child);
            }
            else if (newParent != null)
            {
                newPrefabInstance.transform.SetParent(newParent);
            }

            i++;
        }
    }

    public void UndoGenerate()
    {
        if (generatedPrefabs != null)
        {
            foreach (GameObject prefab in generatedPrefabs)
            {
                if (prefab != null) DestroyImmediate(prefab);
            }
        }
    }
}