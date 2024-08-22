using System.Collections;
using UnityEngine;

[ExecuteInEditMode] // This will ensure that the script also runs in the editor
public class ButoParent : MonoBehaviour
{
    // This will appear as a drop-down list in the inspector, populated with all available tags.
    public string parentTag;
    public float maxWaitTime = 5f; // Maximum time to wait for parent object
    public float retryInterval = 0.1f; // Time between retry attempts

    void Start()
    {
        if (Application.isPlaying)
        {
            StartCoroutine(TryParentToObjectWithTag());
        }
    }

    private IEnumerator TryParentToObjectWithTag()
    {
        float elapsedTime = 0f;

        while (elapsedTime < maxWaitTime)
        {
            GameObject[] parentObjects = GameObject.FindGameObjectsWithTag(parentTag);

            if (parentObjects.Length > 0)
            {
                transform.SetParent(parentObjects[0].transform, false);
                yield break; // Exit the coroutine once parenting is successful
            }

            elapsedTime += retryInterval;
            yield return new WaitForSeconds(retryInterval);
        }

        Debug.LogError(
            $"No object with tag {parentTag} found after waiting for {maxWaitTime} seconds."
        );
    }
}
