using UnityEngine;

public class MeshPrefabPlacer : MonoBehaviour
{
    public GameObject prefabToPlace;
    public int numberOfPrefabs = 100;
    public float yOffset = 0.1f; // Slight offset to prevent z-fighting

    [Header("Prefab Variation")]
    public float minScale = 0.8f;
    public float maxScale = 1.2f;

    [Header("Placement Options")]
    public bool placeOnOutside = false;
    [Range(0f, 90f)]
    public float maxGroundAngle = 45f; // Maximum angle from vertical to be considered "ground"

    private Transform prefabsContainer;

    public void GeneratePrefabs()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        
        if (meshFilter == null)
        {
            Debug.LogError("MeshFilter component is missing on this GameObject!");
            return;
        }

        if (meshFilter.sharedMesh == null)
        {
            Debug.LogError("No mesh assigned to the MeshFilter!");
            return;
        }

        if (prefabToPlace == null)
        {
            Debug.LogError("Prefab to place is not assigned!");
            return;
        }

        // Create or clear the Prefabs container
        if (prefabsContainer == null)
        {
            GameObject container = new GameObject("Prefabs");
            prefabsContainer = container.transform;
            prefabsContainer.SetParent(transform);
            prefabsContainer.localPosition = Vector3.zero;
            prefabsContainer.localRotation = Quaternion.identity;
            prefabsContainer.localScale = Vector3.one;
        }
        else
        {
            for (int i = prefabsContainer.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(prefabsContainer.GetChild(i).gameObject);
            }
        }

        Mesh mesh = meshFilter.sharedMesh;
        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;

        int placedPrefabs = 0;
        int attempts = 0;
        int maxAttempts = numberOfPrefabs * 10; // Avoid infinite loop

        while (placedPrefabs < numberOfPrefabs && attempts < maxAttempts)
        {
            int randomIndex = Random.Range(0, vertices.Length);
            Vector3 randomPoint = transform.TransformPoint(vertices[randomIndex]);
            Vector3 normal = transform.TransformDirection(normals[randomIndex]);

            // Flip normal if placing on outside
            if (placeOnOutside)
            {
                normal = -normal;
            }

            // Check if the normal is within the acceptable ground angle
            if (Vector3.Angle(normal, Vector3.up) <= maxGroundAngle)
            {
                GameObject newPrefab = Instantiate(prefabToPlace, randomPoint + normal * yOffset, Quaternion.identity, prefabsContainer);
                
                // Set rotation to grow outward, but align with "up" direction
                Vector3 upDirection = Vector3.Lerp(Vector3.up, normal, 0.3f); // Blend between straight up and normal
                newPrefab.transform.rotation = Quaternion.LookRotation(Vector3.Cross(normal, Random.onUnitSphere), upDirection);

                // Set scale
                float randomScale = Random.Range(minScale, maxScale);
                newPrefab.transform.localScale = Vector3.one * randomScale;

                placedPrefabs++;
            }

            attempts++;
        }

        Debug.Log($"Successfully generated {placedPrefabs} prefabs.");
    }
}