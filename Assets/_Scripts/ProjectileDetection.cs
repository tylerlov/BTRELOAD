using System.Collections;
using FMODUnity;
using UnityEngine;

public class ProjectileDetection : MonoBehaviour
{
    public GameObject[] directionArrows; // Array of 4 arrow GameObjects (Up, Down, Left, Right)
    public float detectionThreshold = 0.8f;
    public float spriteVisibilityDuration = 0.3f;
    public float maxScaleFactor = 2f;

    public EventReference detectionSoundEvent;

    private SpriteRenderer[] arrowRenderers;
    private Vector3[] originalScales;

    private FMOD.Studio.EventInstance detectionSoundInstance;
    private Coroutine detectionCoroutine;

    private void Start()
    {
        arrowRenderers = new SpriteRenderer[4];
        originalScales = new Vector3[4];

        for (int i = 0; i < 4; i++)
        {
            arrowRenderers[i] = directionArrows[i].GetComponent<SpriteRenderer>();
            originalScales[i] = directionArrows[i].transform.localScale;
            arrowRenderers[i].enabled = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        ProjectileStateBased projectile = other.GetComponent<ProjectileStateBased>();
        if (projectile != null && projectile.accuracy > detectionThreshold)
        {
            if (detectionCoroutine != null)
            {
                StopCoroutine(detectionCoroutine);
            }
            detectionCoroutine = StartCoroutine(DetectionSequence(projectile));
        }
    }

    private IEnumerator DetectionSequence(ProjectileStateBased projectile)
    {
        int directionIndex = DetermineProjectileDirection(projectile.transform.position);
        StartCoroutine(ShowAndScaleArrowTemporarily(directionIndex));
        PlayDetectionSound();

        while (projectile != null && projectile.gameObject.activeInHierarchy)
        {
            if (projectile.projHitPlayer || !projectile.gameObject.activeInHierarchy)
            {
                StopDetectionSound();
                yield break;
            }
            yield return null;
        }

        StopDetectionSound();
    }

    private int DetermineProjectileDirection(Vector3 projectilePosition)
    {
        Vector3 directionToProjectile = (projectilePosition - transform.position).normalized;
        float angle = Vector3.SignedAngle(Vector3.forward, directionToProjectile, Vector3.up);

        if (angle >= -45f && angle < 45f)
            return 0; // Up
        if (angle >= 45f && angle < 135f)
            return 2; // Right
        if (angle >= 135f || angle < -135f)
            return 1; // Down
        return 3; // Left
    }

    private IEnumerator ShowAndScaleArrowTemporarily(int arrowIndex)
    {
        arrowRenderers[arrowIndex].enabled = true;
        directionArrows[arrowIndex].transform.localScale = originalScales[arrowIndex];

        float elapsedTime = 0f;
        while (elapsedTime < spriteVisibilityDuration)
        {
            float t = elapsedTime / spriteVisibilityDuration;
            float currentScale = Mathf.Lerp(1f, maxScaleFactor, t);
            directionArrows[arrowIndex].transform.localScale =
                originalScales[arrowIndex] * currentScale;

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        arrowRenderers[arrowIndex].enabled = false;
        directionArrows[arrowIndex].transform.localScale = originalScales[arrowIndex];
    }

    private void PlayDetectionSound()
    {
        if (!detectionSoundEvent.IsNull)
        {
            detectionSoundInstance = FMODUnity.RuntimeManager.CreateInstance(detectionSoundEvent);
            detectionSoundInstance.set3DAttributes(
                FMODUnity.RuntimeUtils.To3DAttributes(transform.position)
            );
            detectionSoundInstance.start();
        }
        else
        {
            Debug.LogWarning("Detection sound event is not set in the inspector.");
        }
    }

    private void StopDetectionSound()
    {
        if (detectionSoundInstance.isValid())
        {
            detectionSoundInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            detectionSoundInstance.release();
        }
    }

    private void OnDisable()
    {
        if (detectionCoroutine != null)
        {
            StopCoroutine(detectionCoroutine);
        }
        StopDetectionSound();
    }
}
