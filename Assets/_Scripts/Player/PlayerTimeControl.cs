using System.Collections;
using System.Collections.Generic;
using FluffyUnderware.Curvy.Controllers;
using FMODUnity;
using MoreMountains.Feedbacks;
using UnityEngine;
using UnityEngine.VFX;

public class PlayerTimeControl : MonoBehaviour
{
    private CrosshairCore crosshairCore;
    private PlayerLocking playerLocking;
    private SplineController splineControl;
    private PlayerMovement pMove;

    [Header("Component References")]
    [SerializeField] private MMF_Player rewindFeedback;
    [SerializeField] private MMF_Player longrewindFeedback;
    [SerializeField] private ParticleSystem temporalBlast;
    [SerializeField] private ParticleSystem RewindFXScan;
    [SerializeField] private VisualEffect RewindFX;
    [SerializeField] private VisualEffect slowTime;

    [Header("Time Control Settings")]
    [Tooltip("Time scale during rewind")]
    [SerializeField] private float rewindTimeScale = -2f;

    [Tooltip("Duration of the rewind effect")]
    [SerializeField] private float rewindDuration = 3f;

    [Tooltip("Duration to return to normal time")]
    [SerializeField] private float returnToNormalDuration = 0.25f;

    [Tooltip("Time scale during slow motion")]
    [SerializeField] private float slowTimeScale = 0.1f;

    [Tooltip("Duration of the slow motion effect")]
    [SerializeField] private float slowTimeDuration = 5f;

    [Tooltip("Cooldown between rewind actions")]
    [SerializeField] private float rewindCooldown = 0.5f;

    [Tooltip("Maximum duration for rewind")]
    [SerializeField] private float maxRewindDuration = 1f;

    [Header("JPG Effect Settings")]
    [Tooltip("JPG effect intensity during rewind")]
    [SerializeField] private float rewindJPGIntensity = 0.2f;

    [Tooltip("JPG effect intensity during slow motion")]
    [SerializeField] private float slowJPGIntensity = 0.4f;

    [Tooltip("Duration to apply JPG effect")]
    [SerializeField] private float jpgEffectDuration = 0.5f;

    private float lastRewindTime = 0f;
    private bool delayLoop = false;
    private float originalSpeed;

    // Add this line near the top of the class, with the other private variables
    private Coroutine currentRewindCoroutine;

    private float baselineJPGIntensity;

    private void Awake()
    {
        crosshairCore = GetComponent<CrosshairCore>();
        playerLocking = GetComponent<PlayerLocking>();
        splineControl = FindObjectOfType<SplineController>();
        pMove = FindObjectOfType<PlayerMovement>();

        // Store the baseline JPG intensity on startup
        if (JPGEffectController.Instance != null)
        {
            baselineJPGIntensity = JPGEffectController.Instance.GetCurrentIntensity();
        }

        if (crosshairCore == null || playerLocking == null || splineControl == null || pMove == null)
        {
            Debug.LogError("Required components not found on the same GameObject or in the scene.");
        }
    }

    public void HandleRewindToBeat()
    {
        if (crosshairCore.CheckRewindToBeat() && Time.time - lastRewindTime > rewindCooldown)
        {
            float timeSinceLastLaunch = Time.time - crosshairCore.lastProjectileLaunchTime;
            Debug.Log(
                $"Time since last projectile launch: {timeSinceLastLaunch}, QTE window: {CrosshairCore.QTE_TRIGGER_WINDOW}, QTE Locked targets: {playerLocking.qteEnemyLockList.Count}"
            );

            lastRewindTime = Time.time;
            if (
                timeSinceLastLaunch <= CrosshairCore.QTE_TRIGGER_WINDOW
                && playerLocking.qteEnemyLockList.Count > 0
            )
            {
                Debug.Log("QTE Initiated for Rewind");
                TriggerQTE(rewindDuration, 3); // 3 is an example difficulty level
            }
            else
            {
                Debug.Log(
                    $"Rewind started without QTE. Time condition met: {timeSinceLastLaunch <= CrosshairCore.QTE_TRIGGER_WINDOW}, Targets condition met: {playerLocking.qteEnemyLockList.Count > 0}"
                );
                StartCoroutine(RewindToBeat());
            }
        }
    }

    public void HandleSlowToBeat()
    {
        if (crosshairCore.CheckSlowToBeat())
        {
            float timeSinceLastLaunch = Time.time - crosshairCore.lastProjectileLaunchTime;
            Debug.Log(
                $"Time since last projectile launch: {timeSinceLastLaunch}, QTE window: {CrosshairCore.QTE_TRIGGER_WINDOW}, QTE Locked targets: {playerLocking.qteEnemyLockList.Count}"
            );

            if (
                timeSinceLastLaunch <= CrosshairCore.QTE_TRIGGER_WINDOW
                && playerLocking.qteEnemyLockList.Count > 0
            )
            {
                Debug.Log("QTE Initiated for Slow");
                TriggerQTE(slowTimeDuration);
            }
            else
            {
                Debug.Log(
                    $"Slow started without QTE. Time condition met: {timeSinceLastLaunch <= CrosshairCore.QTE_TRIGGER_WINDOW}, Targets condition met: {playerLocking.qteEnemyLockList.Count > 0}"
                );
                StartCoroutine(SlowToBeat());
            }
        }
    }

    private void TriggerQTE(float duration, int difficulty = 3)
    {
        if (QuickTimeEventManager.Instance != null)
        {
            QuickTimeEventManager.Instance.StartQTE(duration, difficulty);
            QuickTimeEventManager.Instance.OnQteComplete += HandleQTEComplete;
            currentRewindCoroutine = StartCoroutine(RewindToBeat());
        }
        else
        {
            Debug.LogError("QuickTimeEventManager instance is null");
        }
    }

    private void HandleQTEComplete(bool success)
    {
        QuickTimeEventManager.Instance.OnQteComplete -= HandleQTEComplete;
        if (success)
        {
            if (currentRewindCoroutine != null)
            {
                StopCoroutine(currentRewindCoroutine);
                currentRewindCoroutine = null;
            }
            StopRewindEffect();
            ApplyIncreasedDamage();
        }
        else
        {
            playerLocking.ClearLockedTargets();
        }

        ResetMusicState();

        Debug.Log($"QTE completed with success: {success}");
    }

    public void ResetMusicState()
    {
        if (MusicManager.Instance != null)
        {
            Debug.Log("Resetting music state");
            MusicManager.Instance.SetMusicParameter("Time State", 0f); // 0 represents Default state
        }
        else
        {
            Debug.LogError("MusicManager.Instance is null in ResetMusicState");
        }
    }

    private IEnumerator RewindToBeat()
    {
        if (delayLoop)
            yield break;

        delayLoop = true;
        ActivateRewindEffects(true);
        float startPosition = splineControl.RelativePosition;
        originalSpeed = splineControl.Speed;

        // Set new position
        float rewindSpeed = originalSpeed * 0.25f;
        float rewindDistance = rewindSpeed * rewindDuration;
        float newPosition = Mathf.Clamp01(startPosition - rewindDistance);

        // Apply rewind effects
        splineControl.Speed = -rewindSpeed;
        crosshairCore.TriggerRewindStart(rewindTimeScale);
        rewindFeedback.PlayFeedbacks();

        JPGEffectController.Instance.SetJPGIntensity(rewindJPGIntensity, jpgEffectDuration);

        // Use TimeManager to set the time scale
        TimeManager.Instance.SetTimeScale(-1f);
        MusicManager.Instance.SetMusicParameter("Time State", 1f); // 1 represents Rewind state

        yield return new WaitForSeconds(rewindDuration);

        // Reset time scale
        TimeManager.Instance.SetTimeScale(1f);

        // ResetMusicState() will be called in StopRewindEffect()

        // Ensure the position is set correctly after the rewind
        splineControl.RelativePosition = newPosition;

        StopRewindEffect();
        QuickTimeEventManager.Instance.EndQTE();
    }

    private void StopRewindEffect()
    {
        splineControl.Speed = originalSpeed;
        pMove.UpdateAnimation();
        splineControl.MovementDirection = MovementDirection.Forward;

        JPGEffectController.Instance.SetJPGIntensity(baselineJPGIntensity, 0.5f);
        DeactivateRewindEffects();
        crosshairCore.TriggerRewindEnd();

        ResetMusicState();

        playerLocking.qteEnemyLockList.Clear();
        Debug.Log("QTE Enemy Lock List cleared after Rewind");

        delayLoop = false;
    }

    private IEnumerator SlowToBeat()
    {
        if (delayLoop)
            yield break;
        delayLoop = true;

        slowTime.enabled = true;
        ActivateRewindEffects(true);

        float startPosition = splineControl.RelativePosition;
        float originalSpeed = splineControl.Speed;
        float slowedSpeed = originalSpeed * slowTimeScale;

        // Apply slow motion effects
        splineControl.Speed = slowedSpeed;
        crosshairCore.TriggerRewindStart(slowTimeScale);
        rewindFeedback.PlayFeedbacks();
        JPGEffectController.Instance.SetJPGIntensity(slowJPGIntensity, jpgEffectDuration);

        // Set music state to Slow
        MusicManager.Instance.SetMusicParameter("Time State", 2f); // 2 represents Slow state

        // Use TimeManager to set the time scale
        TimeManager.Instance.SetTimeScale(slowTimeScale);

        yield return new WaitForSeconds(slowTimeDuration);

        // Reset time scale
        TimeManager.Instance.SetTimeScale(1f);

        // Calculate new position based on slowed speed and duration
        float distanceTraveled = slowedSpeed * slowTimeDuration;
        float newPosition = Mathf.Clamp01(startPosition + distanceTraveled);

        // Reset after slow motion
        splineControl.RelativePosition = newPosition;
        splineControl.Speed = originalSpeed;
        pMove.UpdateAnimation();
        splineControl.MovementDirection = MovementDirection.Forward;

        JPGEffectController.Instance.SetJPGIntensity(baselineJPGIntensity, 0.5f);
        DeactivateRewindEffects();
        crosshairCore.TriggerRewindEnd();

        // Reset music state to Default
        MusicManager.Instance.SetMusicParameter("Time State", 0f); // 0 represents Default state

        slowTime.enabled = false;
        playerLocking.qteEnemyLockList.Clear();
        Debug.Log("QTE Enemy Lock List cleared after Slow");

        delayLoop = false;
    }

    private void ActivateRewindEffects(bool activate)
    {
        RewindFX.enabled = activate;
        RewindFXScan.gameObject.SetActive(activate);
        temporalBlast.Play();
    }

    private void DeactivateRewindEffects()
    {
        RewindFX.enabled = false;
        RewindFXScan.gameObject.SetActive(false);
    }

    private void ApplyIncreasedDamage()
    {
        foreach (var target in playerLocking.qteEnemyLockList)
        {
            ProjectileStateBased projectile = target.GetComponent<ProjectileStateBased>();
            if (projectile != null)
            {
                projectile.SetDamageMultiplier(2f); // Double the damage
            }
        }
    }

    // Remove or comment out this method as it's not being used
    /*
    public void HandleRewindTime()
    {
        if (rewindTriggedStillPressed && Time.time - lastRewindTime > rewindCooldown)
        {
            lastRewindTime = Time.time;
            TriggerQTE(rewindDuration);
        }
    }
    */
}
