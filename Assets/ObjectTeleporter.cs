using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening; // Import the DoTween namespace

[Serializable]
public class ChildObjectMaterial
{
    public GameObject ChildObject;
    public Material Material;

    public ChildObjectMaterial(GameObject obj, Material mat)
    {
        ChildObject = obj;
        Material = mat;
    }
}

public class ObjectTeleporter : MonoBehaviour
{
    public float teleportDelay = 2f; // Define the teleport delay
    public float minTime = 1f;
    public float maxTime = 5f;
    public float dissolveDuration = 0.5f; // Duration of the dissolve effect
    public float dissolveStartValue = 0f; // Starting value of DissolveOffset
    public float dissolveEndValue = 1f; // Ending value of DissolveOffset

    public List<ChildObjectMaterial> childObjectsMaterials = new List<ChildObjectMaterial>();

    private Dictionary<GameObject, Vector3> startPositions = new Dictionary<GameObject, Vector3>();

    private void Start()
    {
        PopulateChildObjectsMaterialsList();
        foreach (Transform child in transform)
        {
            SkinnedMeshRenderer renderer = child.GetChild(0).GetComponent<SkinnedMeshRenderer>();
            if (renderer != null)
            {
                startPositions[child.gameObject] = child.position;
                Vector3 teleportLocation = GetRandomTeleportLocation(child.position);
                StartCoroutine(TeleportRoutine(child.gameObject, teleportLocation));
            }
        }
    }

    private void OnValidate()
    {
        PopulateChildObjectsMaterialsList();
    }

    private void PopulateChildObjectsMaterialsList()
    {
        childObjectsMaterials.Clear();
        foreach (Transform child in transform)
        {
            SkinnedMeshRenderer renderer = child.GetChild(0).GetComponent<SkinnedMeshRenderer>();
            if (renderer != null)
            {
                childObjectsMaterials.Add(new ChildObjectMaterial(child.gameObject, renderer.sharedMaterial));
            }
        }
    }

    private IEnumerator TeleportRoutine(GameObject objectToTeleport, Vector3 teleportLocation)
    {
        while (true)
        {
            // Start the dissolve effect 0.5 seconds before the teleport
            yield return new WaitForSeconds(teleportDelay - dissolveDuration);
            StartDissolveEffect(objectToTeleport, dissolveStartValue, dissolveEndValue);

            // Wait for the teleport delay
            yield return new WaitForSeconds(dissolveDuration);

            // Teleport the object
            objectToTeleport.transform.position = teleportLocation;

            // Reverse the dissolve effect after teleportation
            StartDissolveEffect(objectToTeleport, dissolveEndValue, dissolveStartValue);

            // Wait for a random time before teleporting again
            float randomWaitTime = UnityEngine.Random.Range(minTime, maxTime);
            yield return new WaitForSeconds(randomWaitTime);

            // Update the teleport location for the next teleportation
            teleportLocation = GetRandomTeleportLocation(startPositions[objectToTeleport]);
        }
    }

    private void StartDissolveEffect(GameObject objectToDissolve, float fromValue, float toValue)
    {
        Material material = childObjectsMaterials.Find(x => x.ChildObject == objectToDissolve)?.Material;
        if (material != null)
        {
            Vector3 startVector = new Vector3(0, fromValue, 0);
            DOTween.To(() => startVector, x => {
                Vector3 currentVector = new Vector3(0, x.y, 0);
                material.SetVector("_DissolveOffest", currentVector);
            }, new Vector3(0, toValue, 0), dissolveDuration);
        }
    }

    private Vector3 GetRandomTeleportLocation(Vector3 startPosition)
    {
        float offsetX = UnityEngine.Random.Range(-5f, 5f);
        float offsetY = UnityEngine.Random.Range(-5f, 5f);
        float offsetZ = UnityEngine.Random.Range(-5f, 5f);
        return startPosition + new Vector3(offsetX, offsetY, offsetZ);
    }
}