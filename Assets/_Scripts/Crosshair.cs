using System;
using System.Collections;
using System.Collections.Generic;
using Chronos;
using DG.Tweening;
using FluffyUnderware.Curvy.Controllers;
using FMODUnity;
using MoreMountains.Feedbacks;
using SonicBloom.Koreo;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.VFX;

public class Crosshair : MonoBehaviour
{
    public static Crosshair Instance { get; private set; }

    #region Core References
    [Header("Core References")]
    [SerializeField] private GameObject Player;
    [SerializeField] private GameObject LineToTarget;
    [SerializeField] private GameObject RaySpawn;
    [SerializeField] private GameObject RaySpawnEnemyLocking;
    [SerializeField] private GameObject Reticle;
    #endregion

    #region Locking and Targeting
    [Header("Locking and Targeting")]
    public int Locks;
    public bool locking;
    public bool shootTag;
    public bool triggeredLockFire;
    public int maxLockedEnemyTargets = 3;
    public int maxTargets = 6;
    [SerializeField] private int range = 300;
    public float bulletLockInterval = 0.1f;
    public float enemyLockInterval = 0.2f;
    #endregion

    #region Raycast Settings
    [Header("Raycast Settings")]
    public Vector3 bulletLockBoxSize = new Vector3(1, 1, 1);
    public bool showRaycastGizmo = true;
    [SerializeField] private LayerMask groundMask;
    #endregion

    #region Lock-On Visuals
    [Header("Lock-On Visuals")]
    [SerializeField] private GameObject lockOnPrefab;
    [SerializeField] private float initialScale = 1f;
    [SerializeField] private float initialTransparency = 0.5f;
    #endregion

    #region Feedback and Effects
    [Header("Feedback and Effects")]
    public MMF_Player lockFeedback;
    public MMF_Player shootFeedback;
    public MMF_Player rewindFeedback;
    public MMF_Player longrewindFeedback;
    public ParticleSystem temporalBlast;
    public ParticleSystem RewindFXScan;
    public VisualEffect RewindFX;
    public VisualEffect slowTime;
    public GameObject BonusDamage;
    #endregion

    #region Time Control
    [Header("Time Control")]
    [SerializeField] private float rewindTimeScale = -2f;
    [SerializeField] private float rewindDuration = 3f;
    private Coroutine currentRewindCoroutine;
    [SerializeField] private float returnToNormalDuration = 0.25f;
    [SerializeField] private float slowTimeScale = 0.5f;
    [SerializeField] private float slowTimeDuration = 5f;
    [SerializeField] private float rewindCooldown = 0.5f;
    [SerializeField] private float maxRewindDuration = 1f;
    #endregion

    #region FMOD Events
    [Header("FMOD Events")]
    [SerializeField] private EventReference firingBlastsEvent;
    [SerializeField] private EventReference randomShootingEvent;
    [SerializeField] private EventReference shootTagEvent;
    #endregion

    #region Projectile Launch
    [Header("Projectile Launch")]
    [SerializeField] private float launchDelay = 0.1f;
    #endregion

    #region Koreography Events
    [Header("Koreography Events")]
    [EventID] public string eventIDShooting;
    [EventID] public string eventIDRewindTime;
    #endregion

    #region Targets
    [Header("Targets")]
    public List<Transform> projectileTargetList = new List<Transform>();
    public Transform enemyTarget;
    public List<Transform> enemyTargetList = new List<Transform>();
    public List<Transform> LockedList = new List<Transform>();
    private List<Transform> qteEnemyLockList = new List<Transform>();
    #endregion

    #region Private Fields
    private StudioEventEmitter musicPlayback;
    private StaminaController staminaController;
    public SplineController splineControl;
    private PlayerMovement pMove;
    private Camera mainCamera;
    private DefaultControls playerInputActions;
    private FMOD.Studio.Bus masterBus;
    private Renderer RectRend;
    private float lookHeight;
    private Transform canvas;
    private UILockOnEffect uiLockOnEffect;
    private RaycastHit hit, hitEnemy;
    private RaycastHit[] lockHits = new RaycastHit[10], lockEnemyHits = new RaycastHit[10];
    private bool stereoVibrateSwitch, collectHealthMode;
    // Add this field to the class
    private float originalSpeed;
    private float lastBulletTime, lastEnemyTime;
    private int enemyTargetListIndex;
    private bool isQuickTap;
    private float tapStartTime;
    private const float tapThreshold = 0.1f;
    private float lastRewindTime = 0f;
    private bool delayLoop = false, rewindTriggedStillPressed = false;
    private float lastFireTime;
    private const float QTE_TRIGGER_WINDOW = 1f;
    private float lastProjectileLaunchTime;
    private float lastLockReleaseTime;
    #endregion

    #region Events
    public event Action<float> OnRewindStart;
    public event Action OnRewindEnd;
    #endregion

    #region Unity Methods
    private void Awake()
    {
        Instance = this;
        playerInputActions = new DefaultControls();
        playerInputActions.Player.Enable();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        UnregisterKoreographyEvents();
    }

    private void Start()
    {
        masterBus = FMODUnity.RuntimeManager.GetBus("bus:/");
        RectRend = Reticle.GetComponent<Renderer>();
        locking = true;
        maxLockedEnemyTargets--;
        collectHealthMode = false;
        RegisterKoreographyEvents();
        lastProjectileLaunchTime = -QTE_TRIGGER_WINDOW;
    }

    private void Update()
    {
        HandleInput();
        Debug.DrawRay(RaySpawn.transform.position, RaySpawn.transform.forward, Color.green);
        OnLock();
        OnLockEnemy();
        HandleRewindTime();
        HandleRewindToBeat();
        HandleSlowToBeat();

        // Failsafe for music state reset
        if (!QuickTimeEventManager.Instance.IsQTEActive && Time.timeScale == 1f)
        {
            ResetMusicState();
        }
    }
    #endregion

    #region Initialization Methods
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        InitializeComponents();
        ResetSceneSpecificSettings();
        ClearLockedTargets();
    }

    private void InitializeComponents()
    {
        GameObject musicGameObject = GameObject.Find("FMOD Music");
        if (musicGameObject != null)
        {
            musicPlayback = musicGameObject.GetComponent<StudioEventEmitter>();
            if (musicPlayback == null)
                Debug.LogError("StudioEventEmitter component not found on 'FMOD Music' GameObject.");
        }
        else
            Debug.LogError("GameObject with name 'FMOD Music' not found in the scene.");

        staminaController = FindObjectOfType<StaminaController>();
        uiLockOnEffect = FindObjectOfType<UILockOnEffect>();
        splineControl = FindObjectOfType<SplineController>();
        pMove = FindObjectOfType<PlayerMovement>();
        mainCamera = Camera.main;
    }

    private void ResetSceneSpecificSettings()
    {
        enemyTargetList.Clear();
        projectileTargetList.Clear();
        LockedList.Clear();
        qteEnemyLockList.Clear();
    }
    #endregion

    #region Input Handling
    private void HandleInput()
    {
        if (CheckLockProjectilesButtonDown())
            tapStartTime = Time.time;

        if (CheckLockProjectilesButtonUp())
        {
            if (Time.time - tapStartTime <= tapThreshold)
                Debug.Log("Quick tap detected - this would be a parry");
            isQuickTap = false;
        }
    }

    private bool CheckLockProjectilesButtonDown() => playerInputActions.Player.LockProjectiles.triggered;

    private bool CheckLockProjectilesButtonUp()
    {
        if (!playerInputActions.Player.LockProjectiles.IsPressed())
        {
            lastLockReleaseTime = Time.time;
            return true;
        }
        return false;
    }

    private bool CheckLockProjectiles() => playerInputActions.Player.LockProjectiles.ReadValue<float>() > 0;

    private bool CheckLockEnemies() => playerInputActions.Player.LockEnemies.ReadValue<float>() > 0;

    private bool CheckRewindToBeat() => playerInputActions.Player.RewindTime.ReadValue<float>() > 0;

    private bool CheckSlowToBeat() => playerInputActions.Player.SlowTime.ReadValue<float>() > 0;
    #endregion

    #region Locking Methods
    public void OnLock()
    {
        if (!IsTimeToLockBullet())
            return;

        foreach (var hit in PerformBulletLockBoxCast())
        {
            if (!IsValidBulletHit(hit))
                continue;

            UpdateLastBulletLockTime();
            if (isQuickTap)
            {
                isQuickTap = false;
                break;
            }
            else if (TryLockOntoBullet(hit))
                break;
        }
    }

    private bool IsTimeToLockBullet() => Time.time >= lastBulletTime + bulletLockInterval;

    private RaycastHit[] PerformBulletLockBoxCast()
    {
        RaycastHit[] hits = new RaycastHit[10];
        int hitsCount = Physics.BoxCastNonAlloc(
            RaySpawn.transform.position,
            bulletLockBoxSize / 2,
            RaySpawn.transform.forward,
            hits,
            RaySpawn.transform.rotation,
            range
        );
        Array.Resize(ref hits, hitsCount);
        return hits;
    }

    private bool IsValidBulletHit(RaycastHit hit) =>
        hit.collider != null && (hit.collider.CompareTag("Bullet") || hit.collider.CompareTag("LaunchableBullet"));

    private void UpdateLastBulletLockTime() => lastBulletTime = Time.time;

    private bool TryLockOntoBullet(RaycastHit hit)
    {
        ProjectileStateBased hitPSB = hit.transform.GetComponent<ProjectileStateBased>();
        if (collectHealthMode && hitPSB && hitPSB.GetCurrentStateType() == typeof(EnemyShotState))
        {
            HandleCollectHealthMode(hit);
            return true;
        }
        else if (!collectHealthMode && staminaController.canRewind && CheckLockProjectiles())
        {
            return TryAddBulletToLockList(hit, hitPSB);
        }
        return false;
    }

    private void HandleCollectHealthMode(RaycastHit hit)
    {
        GameManager.instance.AddScore(100);
        StartCoroutine(LockVibrate());
        lockFeedback.PlayFeedbacks();
        PlayRandomLocking();
        hit.transform.GetComponent<ProjectileStateBased>().Death();
        UpdateLastBulletLockTime();
    }

    private bool TryAddBulletToLockList(RaycastHit hit, ProjectileStateBased hitPSB)
    {
        if (!projectileTargetList.Contains(hit.transform) && hitPSB && hitPSB.GetCurrentStateType() == typeof(EnemyShotState))
        {
            if (LockedList.Count < maxTargets && projectileTargetList.Count < maxTargets)
            {
                hitPSB.ChangeState(new PlayerLockedState(hitPSB));
                projectileTargetList.Add(hit.transform);
                musicPlayback.EventInstance.setParameterByName("Lock State", 1);
                UpdateLastBulletLockTime();
                HandleMaxTargetsReached();
                return true;
            }
        }
        return false;
    }

    private void HandleMaxTargetsReached()
    {
        if (LockedList.Count == maxTargets - 1)
        {
            BonusDamage.SetActive(true);
            foreach (Transform TargetProjectile in LockedList)
                TargetProjectile.GetComponent<ProjectileStateBased>().damageAmount *= 1.5f;
        }
    }

    public void OnLockEnemy()
    {
        if (!IsTimeToLockEnemy())
            return;

        foreach (var hit in PerformEnemyLockBoxCast())
        {
            if (!IsValidEnemyHit(hit))
                continue;

            UpdateLastEnemyLockTime();
            hitEnemy = hit;

            if (!ShouldLockOntoEnemy())
                continue;

            Transform enemyTransform = GetEnemyTransform(hit);
            if (enemyTransform != null && !enemyTargetList.Contains(enemyTransform))
            {
                LockOntoEnemy(enemyTransform);
                return;
            }
        }
    }

    private bool IsTimeToLockEnemy() => Time.time >= lastEnemyTime + enemyLockInterval;

    private RaycastHit[] PerformEnemyLockBoxCast()
    {
        int combinedLayerMask = (1 << LayerMask.NameToLayer("Enemy")) | (1 << LayerMask.NameToLayer("Ground"));
        RaycastHit[] hits = new RaycastHit[10];
        Physics.BoxCastNonAlloc(
            RaySpawn.transform.position,
            RaySpawnEnemyLocking.transform.lossyScale / 8,
            RaySpawnEnemyLocking.transform.forward,
            hits,
            RaySpawnEnemyLocking.transform.rotation,
            range,
            combinedLayerMask
        );
        return hits;
    }

    private bool IsValidEnemyHit(RaycastHit hit) =>
        hit.collider != null && hit.collider.CompareTag("Enemy") && CheckLockProjectiles();

    private bool ShouldLockOntoEnemy() => CheckLockEnemies();

    private Transform GetEnemyTransform(RaycastHit hit)
    {
        EnemyBasicSetup enemySetup = hit.collider.GetComponentInParent<EnemyBasicSetup>();
        EnemyBasicDamagablePart damagablePart = hit.collider.GetComponent<EnemyBasicDamagablePart>();

        if (enemySetup != null)
        {
            enemySetup.SetLockOnStatus(true);
            return hit.collider.transform;
        }
        else if (damagablePart != null)
        {
            damagablePart.SetLockOnStatus(true);
            return hit.collider.transform;
        }

        return null;
    }

    private void LockOntoEnemy(Transform enemyTransform)
    {
        if (enemyTargetList.Count > maxLockedEnemyTargets)
            UnlockOldestEnemy();

        enemyTarget = enemyTransform;
        enemyTargetList.Add(enemyTarget);
        enemyTargetListIndex = enemyTargetList.Count;
        FMODUnity.RuntimeManager.PlayOneShot("event:/Player/LockEnemy");
        uiLockOnEffect.LockOnTarget(enemyTarget);
        lastEnemyTime = Time.time;
    }

    private void UnlockOldestEnemy()
    {
        Transform oldestEnemy = enemyTargetList[0];
        EnemyBasicSetup enemySetup = oldestEnemy.GetComponent<EnemyBasicSetup>();
        EnemyBasicDamagablePart damagablePart = oldestEnemy.GetComponent<EnemyBasicDamagablePart>();

        if (enemySetup != null)
            enemySetup.SetLockOnStatus(false);
        else if (damagablePart != null)
            damagablePart.SetLockOnStatus(false);

        enemyTargetList.RemoveAt(0);
    }

    private void UpdateLastEnemyLockTime() => lastEnemyTime = Time.time;
    #endregion

    // Add this method to the Crosshair class
    private void ApplyIncreasedDamage()
    {
        foreach (var target in qteEnemyLockList)
        {
            ProjectileStateBased projectile = target.GetComponent<ProjectileStateBased>();
            if (projectile != null)
            {
                projectile.SetDamageMultiplier(2f); // Double the damage
            }
        }
    }

    #region Koreography Event Handlers
    private void OnMusicalLock(KoreographyEvent evt)
    {
        if (CheckLockProjectiles() && projectileTargetList.Count > 0 && Time.timeScale != 0f)
        {
            var target = projectileTargetList[0];
            target.transform.GetChild(0).gameObject.SetActive(true);
            target.GetComponent<ProjectileStateBased>().ChangeState(new PlayerLockedState(target.GetComponent<ProjectileStateBased>()));
            LockedList.Add(target);
            StartCoroutine(LockVibrate());
            lockFeedback.PlayFeedbacks();
            PlayRandomLocking();
            Locks++;
            projectileTargetList.RemoveAt(0);

            AnimateLockOnEffect();

            if (!staminaController.canRewind)
                triggeredLockFire = true;
        }
    }

    private void OnMusicalShoot(KoreographyEvent evt)
    {
        if ((!CheckLockProjectiles() || triggeredLockFire) && LockedList.Count > 0 && Time.timeScale != 0f)
        {
            List<PlayerLockedState> projectilesToLaunch = PrepareProjectilesToLaunch();

            if (projectilesToLaunch.Count > 0)
                StartCoroutine(LaunchProjectilesWithDelay(projectilesToLaunch));
            else
                ClearLockedTargets();

            HandleShootingEffects();
            musicPlayback.EventInstance.setParameterByName("Lock State", 0);
        }
    }

    private void UpdateTime(KoreographyEvent evt)
    {
        HandleRewindTime();
        HandleLockFire();
    }
    #endregion

    #region Rewind and Slow Time Methods
    private void HandleRewindToBeat()
    {
        if (CheckRewindToBeat() && Time.time - lastRewindTime > rewindCooldown)
        {
            float timeSinceLastLaunch = Time.time - lastProjectileLaunchTime;
            Debug.Log($"Time since last projectile launch: {timeSinceLastLaunch}, QTE window: {QTE_TRIGGER_WINDOW}, QTE Locked targets: {qteEnemyLockList.Count}");
            
            lastRewindTime = Time.time;
            if (timeSinceLastLaunch <= QTE_TRIGGER_WINDOW && qteEnemyLockList.Count > 0)
            {
                Debug.Log("QTE Initiated for Rewind");
                TriggerQTE(rewindDuration);
            }
            else
            {
                Debug.Log($"Rewind started without QTE. Time condition met: {timeSinceLastLaunch <= QTE_TRIGGER_WINDOW}, Targets condition met: {qteEnemyLockList.Count > 0}");
                StartCoroutine(RewindToBeat());
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
            ClearLockedTargets();
        }
        
        // Reset music state regardless of QTE success
        //ResetMusicState();
        
        Debug.Log($"QTE completed with success: {success}");
    }

    private void ResetMusicState()
    {
        if (GameManager.instance != null && GameManager.instance.musicPlayback != null)
        {
            Debug.Log("Resetting music state");
            GameManager.instance.musicPlayback.EventInstance.setParameterByName("Rewind", 0f);
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
        OnRewindStart?.Invoke(rewindTimeScale);
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
        OnRewindEnd?.Invoke();

        // Reset music state
        ResetMusicState();

        qteEnemyLockList.Clear();
        Debug.Log("QTE Enemy Lock List cleared after Rewind");

        delayLoop = false;
    }

    private void HandleSlowToBeat()
    {
        if (CheckSlowToBeat())
        {
            float timeSinceLastLaunch = Time.time - lastProjectileLaunchTime;
            Debug.Log($"Time since last projectile launch: {timeSinceLastLaunch}, QTE window: {QTE_TRIGGER_WINDOW}, QTE Locked targets: {qteEnemyLockList.Count}");
            
            if (timeSinceLastLaunch <= QTE_TRIGGER_WINDOW && qteEnemyLockList.Count > 0)
            {
                Debug.Log("QTE Initiated for Slow");
                TriggerQTE(slowTimeDuration);
            }
            else
            {
                Debug.Log($"Slow started without QTE. Time condition met: {timeSinceLastLaunch <= QTE_TRIGGER_WINDOW}, Targets condition met: {qteEnemyLockList.Count > 0}");
                StartCoroutine(SlowToBeat());
            }
        }
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
        OnRewindStart?.Invoke(slowTimeScale);
        rewindFeedback.PlayFeedbacks();
        JPGEffectController.Instance.SetJPGIntensity(0.7f, 0.5f);

        yield return StartCoroutine(
            GameManager.instance.RewindTime(slowTimeScale, slowTimeDuration, returnToNormalDuration)
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
        OnRewindEnd?.Invoke();

        slowTime.enabled = false;
        qteEnemyLockList.Clear();
        Debug.Log("QTE Enemy Lock List cleared after Slow");

        delayLoop = false;
    }

    private void HandleRewindTime()
    {
        if (rewindTriggedStillPressed && Time.time - lastRewindTime > rewindCooldown)
        {
            lastRewindTime = Time.time;
            TriggerQTE(rewindDuration);
        }
    }

    private IEnumerator RewindTime()
    {
        if (delayLoop)
            yield break;

        delayLoop = true;
        ActivateRewindEffects(true);
        float startPosition = splineControl.RelativePosition;
        float originalSpeed = splineControl.Speed;

        // Set new position
        float rewindSpeed = originalSpeed * 0.25f; // 1/4 of the original speed
        float rewindDistance = rewindSpeed * Mathf.Abs(rewindTimeScale) * rewindDuration;
        float newPosition = Mathf.Clamp01(startPosition - rewindDistance); // Subtract for reverse movement

        // Apply rewind effects
        splineControl.Speed = -rewindSpeed; // Negative for reverse movement
        OnRewindStart?.Invoke(rewindTimeScale);
        rewindFeedback.PlayFeedbacks();

        JPGEffectController.Instance.SetJPGIntensity(0.7f, 0.5f);

        yield return StartCoroutine(
            GameManager.instance.RewindTime(rewindTimeScale, rewindDuration, returnToNormalDuration)
        );

        // Reset after rewind
        splineControl.RelativePosition = newPosition; // Set to the new position after rewind
        splineControl.Speed = originalSpeed;
        pMove.UpdateAnimation();
        splineControl.MovementDirection = MovementDirection.Forward;

        JPGEffectController.Instance.SetJPGIntensity(0f, 0.5f);
        DeactivateRewindEffects();
        OnRewindEnd?.Invoke();

        qteEnemyLockList.Clear();
        Debug.Log("QTE Enemy Lock List cleared after Rewind");

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
    #endregion

    #region Shooting Methods
    private void HandleShootingEffects()
    {
        PlayRandomShooting();
        PlayRandomShootTag();
        StartCoroutine(ShootVibrate());
        shootFeedback.PlayFeedbacks();
    }

    private void PlayRandomShooting() => RuntimeManager.PlayOneShot(randomShootingEvent);

    private void PlayRandomShootTag() => RuntimeManager.PlayOneShot(shootTagEvent);

    private IEnumerator ShootVibrate()
    {
        // Implement vibration logic here
        yield return new WaitForSeconds(.1f);
    }

    private IEnumerator LaunchProjectilesWithDelay(List<PlayerLockedState> projectilesToLaunch)
    {
        lastProjectileLaunchTime = Time.time;
        Debug.Log($"Projectiles launched at {lastProjectileLaunchTime}");

        // Populate the qteEnemyLockList with current locked targets
        qteEnemyLockList.Clear();
        foreach (var lockedState in projectilesToLaunch)
        {
            if (lockedState.GetProjectile()?.currentTarget != null)
            {
                qteEnemyLockList.Add(lockedState.GetProjectile().currentTarget);
            }
        }
        Debug.Log($"QTE Enemy Lock List populated with {qteEnemyLockList.Count} targets");

        for (int i = 0; i < projectilesToLaunch.Count; i++)
        {
            PlayerLockedState lockedState = projectilesToLaunch[i];
            ProjectileStateBased projectile = lockedState.GetProjectile();

            if (projectile != null)
            {
                if (projectile.currentTarget != null)
                    lockedState.LaunchAtEnemy(projectile.currentTarget);
                else
                    lockedState.LaunchBack();

                AnimateLockOnEffect();
                yield return new WaitForSeconds(launchDelay);
            }
        }

        ClearLockedTargets();
    }

    private List<PlayerLockedState> PrepareProjectilesToLaunch()
    {
        List<PlayerLockedState> projectilesToLaunch = new List<PlayerLockedState>();
        CleanLockedList();

        for (int i = LockedList.Count - 1; i >= 0; i--)
        {
            if (LockedList[i] == null)
                continue;

            ProjectileStateBased projectile = LockedList[i].GetComponent<ProjectileStateBased>();
            if (projectile == null)
                continue;

            if (!(projectile.GetCurrentState() is PlayerLockedState))
                projectile.ChangeState(new PlayerLockedState(projectile));

            PlayerLockedState lockedState = projectile.GetCurrentState() as PlayerLockedState;
            if (lockedState != null)
            {
                if (enemyTargetList.Count > 0)
                {
                    enemyTargetListIndex = Mathf.Clamp(
                        enemyTargetListIndex,
                        1,
                        enemyTargetList.Count
                    );
                    Transform currEnemyTarg = enemyTargetList[enemyTargetListIndex - 1];

                    if (currEnemyTarg != null && currEnemyTarg.gameObject.activeSelf)
                        lockedState.LaunchAtEnemy(currEnemyTarg);
                    else
                        enemyTargetList.Remove(currEnemyTarg);

                    projectilesToLaunch.Add(lockedState);
                    enemyTargetListIndex--;
                }
                else
                    projectilesToLaunch.Add(lockedState);

                staminaController.locking = false;
            }

            LockedList.RemoveAt(i);
        }

        return projectilesToLaunch;
    }

    private void AnimateLockOnEffect()
    {
        if (lockOnPrefab != null)
        {
            GameObject lockOnInstance = Instantiate(lockOnPrefab, Reticle.transform);
            lockOnInstance.SetActive(true);
            lockOnInstance.transform.localPosition = Vector3.zero;
            lockOnInstance.transform.localScale = Vector3.one * initialScale;

            SpriteRenderer spriteRenderer = lockOnInstance.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                Color initialColor = spriteRenderer.color;
                initialColor.a = initialTransparency;
                spriteRenderer.color = initialColor;

                spriteRenderer.DOFade(1f, 0.5f);
            }

            lockOnInstance
                .transform.DOScale(Vector3.zero, 1f)
                .OnComplete(() => Destroy(lockOnInstance));
        }
    }
    #endregion

    #region Utility Methods
    public void ClearLockedTargets()
    {
        enemyTargetList.RemoveAll(enemy => enemy == null);
        foreach (var enemy in enemyTargetList)
        {
            EnemyBasicSetup enemySetup = enemy.GetComponent<EnemyBasicSetup>();
            EnemyBasicDamagablePart damagablePart = enemy.GetComponent<EnemyBasicDamagablePart>();

            if (enemySetup != null)
                enemySetup.SetLockOnStatus(false);
            else if (damagablePart != null)
                damagablePart.SetLockOnStatus(false);
        }

        enemyTargetList.Clear();
        projectileTargetList.RemoveAll(projectile => projectile == null);
        LockedList.RemoveAll(locked => locked == null);
        enemyTarget = null;

        // Clear the QTE enemy lock list as well
        qteEnemyLockList.Clear();
        Debug.Log("All locked targets cleared, including QTE Enemy Lock List");
    }

    public void ReleasePlayerLocks()
    {
        ClearLockedTargets();
        // Add any additional logic needed for releasing player locks
    }

    public void OnNewWaveOrAreaTransition() => ClearLockedTargets();

    public int returnLocks() => Locks;

    public int returnEnemyLocks() => enemyTargetList.Count;

    public Vector3 RaycastTarget()
    {
        if (Physics.Raycast(RaySpawn.transform.position, RaySpawn.transform.forward, out RaycastHit hit, range))
            return hit.point;
        else
            return new Ray(RaySpawn.transform.position, RaySpawn.transform.forward).GetPoint(range);
    }

    private void PlayRandomLocking() => FMODUnity.RuntimeManager.PlayOneShot("event:/Player/Locking");

    private IEnumerator LockVibrate()
    {
        yield return new WaitForSeconds(.2f);
    }

    private void HandleLockFire()
    {
        if (triggeredLockFire && !CheckLockProjectiles())
            triggeredLockFire = false;
    }

    private void CleanLockedList()
    {
        LockedList.RemoveAll(locked => locked == null);
    }

    public void RemoveLockedEnemy(Transform enemy)
    {
        LockedList.Remove(enemy);
        projectileTargetList.Remove(enemy);
        enemyTargetList.Remove(enemy);
    }
    #endregion



    //Extra methods to organize

     private void RegisterKoreographyEvents()
    {
        if (Koreographer.Instance != null)
        {
            Koreographer.Instance.RegisterForEvents(eventIDShooting, OnMusicalShoot);
            Koreographer.Instance.RegisterForEvents(eventIDShooting, OnMusicalLock);
            Koreographer.Instance.RegisterForEvents(eventIDRewindTime, UpdateTime);
            Debug.Log("Events registered successfully");
        }
        else
            Debug.LogError("Failed to register events: Koreographer instance is null");
    }
        private void UnregisterKoreographyEvents()
    {
        if (Koreographer.Instance != null)
        {
            Koreographer.Instance.UnregisterForEvents(eventIDShooting, OnMusicalShoot);
            Koreographer.Instance.UnregisterForEvents(eventIDRewindTime, UpdateTime);
        }
    }
}




       