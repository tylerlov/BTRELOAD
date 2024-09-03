using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MoreMountains.Feedbacks;
using FluffyUnderware.Curvy.Controllers;
using FMODUnity;
using UnityEngine.VFX;

public class PlayerTimeControl : MonoBehaviour
{
    public static PlayerTimeControl Instance { get; private set; }

    #region Time Control Variables
    [Header("Time Control Settings")]
    [SerializeField] private float rewindTimeScale = -2f;
    [SerializeField] private float rewindDuration = 3f;
    private Coroutine currentRewindCoroutine;
    [SerializeField] private float returnToNormalDuration = 0.25f;
    [SerializeField] private float slowTimeScale = 0.5f;
    [SerializeField] private float slowTimeDuration = 5f;
    [SerializeField] private float rewindCooldown = 0.5f;
    [SerializeField] private float maxRewindDuration = 1f;
    #endregion

    #region References
    private CrosshairCore crosshairCore;
    private PlayerLocking playerLocking;
    private SplineController splineControl;
    private PlayerMovement pMove;
    private bool rewindTriggedStillPressed = false;
    #endregion

    #region Feedback and Effects
    public MMF_Player rewindFeedback;
    public MMF_Player longrewindFeedback;
    public ParticleSystem temporalBlast;
    public ParticleSystem RewindFXScan;
    public VisualEffect RewindFX;
    public VisualEffect slowTime;
    #endregion

    private float lastRewindTime = 0f;
    private bool delayLoop = false;
    private float originalSpeed;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        crosshairCore = GetComponent<CrosshairCore>();
        playerLocking = GetComponent<PlayerLocking>();
        splineControl = FindObjectOfType<SplineController>();
        pMove = FindObjectOfType<PlayerMovement>();
    }

    public void HandleRewindToBeat()
    {
        if (crosshairCore.CheckRewindToBeat() && Time.time - lastRewindTime > rewindCooldown)
        {
            float timeSinceLastLaunch = Time.time - crosshairCore.lastProjectileLaunchTime;
            Debug.Log($"Time since last projectile launch: {timeSinceLastLaunch}, QTE window: {CrosshairCore.QTE_TRIGGER_WINDOW}, QTE Locked targets: {playerLocking.qteEnemyLockList.Count}");
            
            lastRewindTime = Time.time;
            if (timeSinceLastLaunch <= CrosshairCore.QTE_TRIGGER_WINDOW && playerLocking.qteEnemyLockList.Count > 0)
            {
                Debug.Log("QTE Initiated for Rewind");
                TriggerQTE(rewindDuration);
            }
            else
            {
                Debug.Log($"Rewind started without QTE. Time condition met: {timeSinceLastLaunch <= CrosshairCore.QTE_TRIGGER_WINDOW}, Targets condition met: {playerLocking.qteEnemyLockList.Count > 0}");
                StartCoroutine(RewindToBeat());
            }
        }
    }

    public void HandleSlowToBeat()
    {
        if (crosshairCore.CheckSlowToBeat())
        {
            float timeSinceLastLaunch = Time.time - crosshairCore.lastProjectileLaunchTime;
            Debug.Log($"Time since last projectile launch: {timeSinceLastLaunch}, QTE window: {CrosshairCore.QTE_TRIGGER_WINDOW}, QTE Locked targets: {playerLocking.qteEnemyLockList.Count}");
            
            if (timeSinceLastLaunch <= CrosshairCore.QTE_TRIGGER_WINDOW && playerLocking.qteEnemyLockList.Count > 0)
            {
                Debug.Log("QTE Initiated for Slow");
                TriggerQTE(slowTimeDuration);
            }
            else
            {
                Debug.Log($"Slow started without QTE. Time condition met: {timeSinceLastLaunch <= CrosshairCore.QTE_TRIGGER_WINDOW}, Targets condition met: {playerLocking.qteEnemyLockList.Count > 0}");
                StartCoroutine(SlowToBeat());
            }
        }
    }

    private void TriggerQTE(float duration)
    {
        if (QuickTimeEventManager.Instance != null)
        {
            QuickTimeEventManager.Instance.StartQTE(duration);
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
        if (GameManager.Instance != null && MusicManager.Instance.musicPlayback != null)
        {
            Debug.Log("Resetting music state");
            MusicManager.Instance.musicPlayback.EventInstance.setParameterByName("Rewind", 0f);
            // Add any other music-related parameters that need to be reset
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
        float rewindDistance = rewindSpeed * Mathf.Abs(rewindTimeScale) * rewindDuration;
        float newPosition = Mathf.Clamp01(startPosition - rewindDistance);

        // Apply rewind effects
        splineControl.Speed = -rewindSpeed;
        crosshairCore.TriggerRewindStart(rewindTimeScale); // Replace direct event invocation
        rewindFeedback.PlayFeedbacks();

        JPGEffectController.Instance.SetJPGIntensity(0.7f, 0.5f);

        // Use a timer to ensure the rewind completes after the specified duration
        float elapsedTime = 0f;
        while (elapsedTime < rewindDuration)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }

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

        JPGEffectController.Instance.SetJPGIntensity(0f, 0.5f);
        DeactivateRewindEffects();
        crosshairCore.TriggerRewindEnd(); // Replace direct event invocation

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
        crosshairCore.TriggerRewindStart(slowTimeScale); // Replace direct event invocation
        rewindFeedback.PlayFeedbacks();
        JPGEffectController.Instance.SetJPGIntensity(0.7f, 0.5f);

        yield return StartCoroutine(
            TimeManager.Instance.RewindTime(slowTimeScale, slowTimeDuration, returnToNormalDuration)
        );

        // Calculate new position based on slowed speed and duration
        float distanceTraveled = slowedSpeed * slowTimeDuration;
        float newPosition = Mathf.Clamp01(startPosition + distanceTraveled);

        // Reset after slow motion
        splineControl.RelativePosition = newPosition;
        splineControl.Speed = originalSpeed;
        pMove.UpdateAnimation();
        splineControl.MovementDirection = MovementDirection.Forward;

        JPGEffectController.Instance.SetJPGIntensity(0f, 0.5f);
        DeactivateRewindEffects();
        crosshairCore.TriggerRewindEnd(); // Replace direct event invocation

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
    public void HandleRewindTime()
{
    if (rewindTriggedStillPressed && Time.time - lastRewindTime > rewindCooldown)
    {
        lastRewindTime = Time.time;
        TriggerQTE(rewindDuration);
    }
}
}