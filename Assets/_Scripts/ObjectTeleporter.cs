using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ChildObjectMaterial
{
    public GameObject ChildObject;
    public Material Material;
    public float TeleportTimer;
    public float DissolveTimer;
    public Vector3 StartPosition;
    public Vector3 TeleportLocation;
    public bool IsDissolving;

    public ChildObjectMaterial(GameObject obj, Material mat, Vector3 startPos)
    {
        ChildObject = obj;
        Material = mat;
        StartPosition = startPos;
        TeleportLocation = startPos;
        ResetTimers();
    }

    public void ResetTimers()
    {
        TeleportTimer = 0f;
        DissolveTimer = 0f;
        IsDissolving = false;
    }
}

public class ObjectTeleporter : MonoBehaviour
{
    public float teleportDelay = 2f;
    public float minTime = 1f;
    public float maxTime = 5f;
    public float dissolveDuration = 0.5f;
    public float dissolveStartValue = 0f;
    public float dissolveEndValue = 1f;

    private List<ChildObjectMaterial> childObjectsMaterials = new List<ChildObjectMaterial>();
    private Dictionary<GameObject, ChildObjectMaterial> materialLookup =
        new Dictionary<GameObject, ChildObjectMaterial>();

    private static readonly Vector3 DissolveVectorStart = new Vector3(0, 0, 0);
    private static readonly Vector3 DissolveVectorEnd = new Vector3(0, 1, 0);

    private void Start()
    {
        PopulateChildObjectsMaterialsList();
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;

        for (int i = 0; i < childObjectsMaterials.Count; i++)
        {
            var com = childObjectsMaterials[i];
            com.TeleportTimer += deltaTime;

            if (com.TeleportTimer >= teleportDelay)
            {
                if (!com.IsDissolving)
                {
                    com.IsDissolving = true;
                    com.DissolveTimer = 0f;
                }

                com.DissolveTimer += deltaTime;
                float dissolveProgress = Mathf.Clamp01(com.DissolveTimer / dissolveDuration);

                if (dissolveProgress < 1f)
                {
                    UpdateDissolveEffect(com, dissolveProgress);
                }
                else if (
                    dissolveProgress >= 1f
                    && com.ChildObject.transform.position != com.TeleportLocation
                )
                {
                    com.ChildObject.transform.position = com.TeleportLocation;
                    com.IsDissolving = false;
                    com.DissolveTimer = 0f;
                }
                else if (dissolveProgress >= 2f)
                {
                    com.TeleportLocation = GetRandomTeleportLocation(com.StartPosition);
                    com.TeleportTimer = UnityEngine.Random.Range(minTime, maxTime);
                    com.IsDissolving = false;
                }
            }
        }
    }

    private void PopulateChildObjectsMaterialsList()
    {
        childObjectsMaterials.Clear();
        materialLookup.Clear();

        foreach (Transform child in transform)
        {
            SkinnedMeshRenderer renderer = child.GetChild(0).GetComponent<SkinnedMeshRenderer>();
            if (renderer != null)
            {
                var com = new ChildObjectMaterial(
                    child.gameObject,
                    renderer.sharedMaterial,
                    child.position
                );
                childObjectsMaterials.Add(com);
                materialLookup[child.gameObject] = com;
            }
        }
    }

    private void UpdateDissolveEffect(ChildObjectMaterial com, float progress)
    {
        Vector3 currentVector = Vector3.Lerp(DissolveVectorStart, DissolveVectorEnd, progress);
        com.Material.SetVector("_DissolveOffest", currentVector);
    }

    private Vector3 GetRandomTeleportLocation(Vector3 startPosition)
    {
        return new Vector3(
            startPosition.x + UnityEngine.Random.Range(-5f, 5f),
            startPosition.y + UnityEngine.Random.Range(-5f, 5f),
            startPosition.z + UnityEngine.Random.Range(-5f, 5f)
        );
    }
}
