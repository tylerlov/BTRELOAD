using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
public class PlacePrefabsOnMesh : MonoBehaviour
{
    public GameObject[] prefabs;
    public int numberOfPrefabs = 10;
    public float localYOffset = 0f;

    private MeshFilter meshFilter;
    private Mesh mesh;

    void OnEnable()
    {
        meshFilter = GetComponent<MeshFilter>();
        if (meshFilter != null)
        {
            mesh = meshFilter.sharedMesh;
        }
    }

    void OnValidate()
    {
        // Ensure the number of prefabs is non-negative
        if (numberOfPrefabs < 0) numberOfPrefabs = 0;
    }

    [ContextMenu("Generate")]
    void PlacePrefabs()
    {
        if (mesh == null)
        {
            Debug.LogError("No mesh found on this object. Please add a MeshFilter component.");
            return;
        }

        ClearPrefabs();

        for (int i = 0; i < numberOfPrefabs; i++)
        {
            Vector3 randomPosition = GetRandomPositionOnMesh();
            GameObject prefab = prefabs[Random.Range(0, prefabs.Length)];
            
            // Replace PrefabUtility.InstantiatePrefab with Instantiate
            GameObject instance = Instantiate(prefab, transform);
            
            instance.transform.position = transform.TransformPoint(randomPosition);
            
            // Calculate normal at the random position
            Vector3 normal = CalculateNormalAtPosition(randomPosition);
            
            // Rotate the instance to face outward
            instance.transform.rotation = Quaternion.LookRotation(normal) * Quaternion.Euler(90f, 0f, 0f);
            
            // Apply local Y offset
            instance.transform.position += instance.transform.up * localYOffset;
        }
    }

    [ContextMenu("Clear")]
    void ClearPrefabs()
    {
        foreach (Transform child in transform)
        {
            DestroyImmediate(child.gameObject); // Remove all child prefabs
        }
    }

    Vector3 GetRandomPositionOnMesh()
    {
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;

        // Choose a random triangle
        int triangleIndex = Random.Range(0, triangles.Length / 3) * 3;
        Vector3 v1 = vertices[triangles[triangleIndex]];
        Vector3 v2 = vertices[triangles[triangleIndex + 1]];
        Vector3 v3 = vertices[triangles[triangleIndex + 2]];

        // Generate random barycentric coordinates
        float r1 = Random.value;
        float r2 = Random.value;
        if (r1 + r2 > 1)
        {
            r1 = 1 - r1;
            r2 = 1 - r2;
        }

        // Calculate the random point on the triangle
        return v1 + r1 * (v2 - v1) + r2 * (v3 - v1);
    }

    Vector3 CalculateNormalAtPosition(Vector3 position)
    {
        // Find the closest triangle and calculate its normal
        int[] triangles = mesh.triangles;
        Vector3[] vertices = mesh.vertices;
        float minDistance = float.MaxValue;
        Vector3 closestNormal = Vector3.up;

        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 v1 = vertices[triangles[i]];
            Vector3 v2 = vertices[triangles[i + 1]];
            Vector3 v3 = vertices[triangles[i + 2]];

            Vector3 center = (v1 + v2 + v3) / 3f;
            float distance = Vector3.Distance(position, center);

            if (distance < minDistance)
            {
                minDistance = distance;
                closestNormal = Vector3.Cross(v2 - v1, v3 - v1).normalized;
            }
        }

        return closestNormal;
    }
}