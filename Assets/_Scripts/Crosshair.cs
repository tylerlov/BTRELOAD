using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using SonicBloom.Koreo;
using MoreMountains.Feedbacks;
using FluffyUnderware.Curvy.Controllers;
using Chronos;
using FMODUnity;
using UnityEngine.VFX;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;

public class Crosshair : MonoBehaviour
{
    public static Crosshair Instance { get; private set; }

    [Header("Core References")]
    [SerializeField] private GameObject Player;
    [SerializeField] private GameObject LineToTarget;
    [SerializeField] private GameObject RaySpawn;
    [SerializeField] private GameObject RaySpawnEnemyLocking;
    [SerializeField] private GameObject Reticle;

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

    [Header("Raycast Settings")]
    public Vector3 bulletLockBoxSize = new Vector3(1, 1, 1);
    public bool showRaycastGizmo = true;
    [SerializeField] private LayerMask groundMask;

    [Header("Lock-On Visuals")]
    [SerializeField] private GameObject lockOnPrefab;
    [SerializeField] private float initialScale = 1f;
    [SerializeField] private float initialTransparency = 0.5f;

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

    [Header("Time Control")]
    [SerializeField] private float rewindTimeScale = -2f;
    [SerializeField] private float rewindDuration = 1f;
    [SerializeField] private float returnToNormalDuration = 0.25f;
    [SerializeField] private float slowTimeScale = 0.5f;
    [SerializeField] private float slowTimeDuration = 5f;
    [SerializeField] private float rewindCooldown = 0.5f;
    [SerializeField] private float maxRewindDuration = 1f;

    [Header("FMOD Events")]
    [SerializeField] private EventReference firingBlastsEvent;
    [SerializeField] private EventReference randomShootingEvent;
    [SerializeField] private EventReference shootTagEvent;

    [Header("Projectile Launch")]
    [SerializeField] private float launchDelay = 0.1f;

    [Header("Koreography Events")]
    [EventID] public string eventIDShooting;
    [EventID] public string eventIDRewindTime;

    [Header("Targets")]
    public List<Transform> projectileTargetList = new List<Transform>();
    public Transform enemyTarget;
    public List<Transform> enemyTargetList = new List<Transform>();
    public List<Transform> LockedList = new List<Transform>();

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

    public event Action<float> OnRewindStart;
    public event Action OnRewindEnd;

    private float lastBulletTime, lastEnemyTime;
    private int enemyTargetListIndex;

    private bool isQuickTap;
    private float tapStartTime;
    private const float tapThreshold = 0.1f;

    private float lastRewindTime = 0f;
    private bool delayLoop = false, rewindTriggedStillPressed = false;

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
    }

    private void Start()
    {
        masterBus = FMODUnity.RuntimeManager.GetBus("bus:/");
        RectRend = Reticle.GetComponent<Renderer>();
        locking = true;
        maxLockedEnemyTargets--;
        collectHealthMode = false;
        RegisterKoreographyEvents();
    }

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

    private void Update()
    {
        HandleInput();
        Debug.DrawRay(RaySpawn.transform.position, RaySpawn.transform.forward, Color.green);
        OnLock();
        OnLockEnemy();
        HandleRewindTime();
        HandleRewindToBeat();
        HandleSlowToBeat();
    }

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
    private bool CheckLockProjectilesButtonUp() => !playerInputActions.Player.LockProjectiles.IsPressed();
    private bool CheckLockProjectiles() => playerInputActions.Player.LockProjectiles.ReadValue<float>() > 0;
    private bool CheckLockEnemies() => playerInputActions.Player.LockEnemies.ReadValue<float>() > 0;
    private bool CheckRewindToBeat() => playerInputActions.Player.RewindTime.ReadValue<float>() > 0;
    private bool CheckSlowToBeat() => playerInputActions.Player.SlowTime.ReadValue<float>() > 0;

    private void HandleRewindToBeat()
    {
        if (CheckRewindToBeat() && Time.time - lastRewindTime > rewindCooldown)
        {
            lastRewindTime = Time.time;
            StartCoroutine(RewindToBeat());
        }
    }

    private IEnumerator RewindToBeat()
    {
        if (delayLoop) yield break;

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

        yield return StartCoroutine(GameManager.instance.RewindTime(rewindTimeScale, rewindDuration, returnToNormalDuration));

        // Reset after rewind
        splineControl.RelativePosition = newPosition; // Set to the new position after rewind
        splineControl.Speed = originalSpeed;
        pMove.UpdateAnimation();
        splineControl.MovementDirection = MovementDirection.Forward;

        JPGEffectController.Instance.SetJPGIntensity(0f, 0.5f);
        DeactivateRewindEffects();
        OnRewindEnd?.Invoke();

        delayLoop = false;
    }

    float SetClockAndGetPosition(Clock clock, float localTimeScale, float startPosition = 0f)
    {
        clock.localTimeScale = localTimeScale;

        return 0f;
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

    private void HandleSlowToBeat()
    {
        if (CheckSlowToBeat())
            StartCoroutine(SlowToBeat());
    }


private IEnumerator SlowToBeat()
{
    if (delayLoop) yield break;
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

    yield return StartCoroutine(GameManager.instance.RewindTime(slowTimeScale, slowTimeDuration, returnToNormalDuration));

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
    delayLoop = false;
}

    public void OnLock()
    {
        if (!IsTimeToLockBullet()) return;

        foreach (var hit in PerformBulletLockBoxCast())
        {
            if (!IsValidBulletHit(hit)) continue;

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
        int hitsCount = Physics.BoxCastNonAlloc(RaySpawn.transform.position, bulletLockBoxSize / 2, RaySpawn.transform.forward, hits, RaySpawn.transform.rotation, range);
        Array.Resize(ref hits, hitsCount);
        return hits;
    }

    private bool IsValidBulletHit(RaycastHit hit) => hit.collider != null && (hit.collider.CompareTag("Bullet") || hit.collider.CompareTag("LaunchableBullet"));

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
            return TryAddBulletToLockList(hit, hitPSB);
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
        if (!IsTimeToLockEnemy()) return;

        foreach (var hit in PerformEnemyLockBoxCast())
        {
            if (!IsValidEnemyHit(hit)) continue;

            UpdateLastEnemyLockTime();
            hitEnemy = hit;

            if (!ShouldLockOntoEnemy()) continue;

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
        Physics.BoxCastNonAlloc(RaySpawn.transform.position, RaySpawnEnemyLocking.transform.lossyScale / 8, RaySpawnEnemyLocking.transform.forward, hits, RaySpawnEnemyLocking.transform.rotation, range, combinedLayerMask);
        return hits;
    }

    private bool IsValidEnemyHit(RaycastHit hit) => hit.collider != null && hit.collider.CompareTag("Enemy") && CheckLockProjectiles();

    private bool ShouldLockOntoEnemy() => CheckLockEnemies();

    private Transform GetEnemyTransform(RaycastHit hit)
    {
        EnemyBasicSetup enemySetup = hit.collider.GetComponentInParent<EnemyBasicSetup>();
        ColliderHitCallback colliderCallback = hit.collider.GetComponent<ColliderHitCallback>();

        if (enemySetup != null)
        {
            enemySetup.lockedStatus(true);
            return hit.collider.transform;
        }
        else if (colliderCallback != null)
        {
            colliderCallback.SetLockedStatus(true);
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

        GameManager.instance.SetEnemyLockState(enemyTarget, true);
    }

    private void UnlockOldestEnemy()
    {
        Transform oldestEnemy = enemyTargetList[0];
        oldestEnemy.GetComponent<EnemyBasicSetup>()?.lockedStatus(false);
        oldestEnemy.GetComponent<ColliderHitCallback>()?.SetLockedStatus(false);
        enemyTargetList.RemoveAt(0);
        GameManager.instance.SetEnemyLockState(oldestEnemy, false);
    }

    private void UpdateLastEnemyLockTime() => lastEnemyTime = Time.time;

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

            lockOnInstance.transform.DOScale(Vector3.zero, 1f).OnComplete(() => Destroy(lockOnInstance));
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
        }
    }

    private List<PlayerLockedState> PrepareProjectilesToLaunch()
    {
        List<PlayerLockedState> projectilesToLaunch = new List<PlayerLockedState>();
        CleanLockedList();

        for (int i = LockedList.Count - 1; i >= 0; i--)
        {
            if (LockedList[i] == null) continue;

            ProjectileStateBased projectile = LockedList[i].GetComponent<ProjectileStateBased>();
            if (projectile == null) continue;

            if (!(projectile.GetCurrentState() is PlayerLockedState))
                projectile.ChangeState(new PlayerLockedState(projectile));

            PlayerLockedState lockedState = projectile.GetCurrentState() as PlayerLockedState;
            if (lockedState != null)
            {
                if (enemyTargetList.Count > 0)
                {
                    enemyTargetListIndex = Mathf.Clamp(enemyTargetListIndex, 1, enemyTargetList.Count);
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

    private void HandleShootingEffects()
    {
        StartCoroutine(ShootVibrate());
        shootFeedback.PlayFeedbacks();
        PlayRandomShooting();
        
        locking = true;
        GameManager.instance.AddShotTally(1);
        Locks--;

        musicPlayback.EventInstance.setParameterByName("Lock State", 0);
        lockFeedback.StopFeedbacks();
        FMODUnity.RuntimeManager.PlayOneShot("event:/Player/Firing Blasts");

        GameManager.instance.AddScore(10 * (LockedList.Count * Locks));
    }

    private IEnumerator LaunchProjectilesWithDelay(List<PlayerLockedState> projectilesToLaunch)
    {
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

    public void ClearLockedTargets()
    {
        enemyTargetList.RemoveAll(enemy => enemy == null);
        foreach (var enemy in enemyTargetList)
            enemy.GetComponent<EnemyBasicSetup>()?.lockedStatus(false);

        enemyTargetList.Clear();
        projectileTargetList.RemoveAll(projectile => projectile == null);
        LockedList.RemoveAll(locked => locked == null);
        enemyTarget = null;
        GameManager.instance.ClearAllEnemyLocks();
    }

    public void OnNewWaveOrAreaTransition() => ClearLockedTargets();

    private void CleanLockedList() => LockedList.RemoveAll(item => item == null);

    private IEnumerator LockVibrate()
    {
        yield return new WaitForSeconds(.2f);
    }

    private IEnumerator ShootVibrate()
    {
        yield return new WaitForSeconds(.1f);
    }

    private void PlayRandomLocking() => FMODUnity.RuntimeManager.PlayOneShot("event:/Player/Locking");
    private void PlayRandomShooting() => RuntimeManager.PlayOneShot(randomShootingEvent);
    private void PlayRandomShootTag() => RuntimeManager.PlayOneShot(shootTagEvent);

    private void UpdateTime(KoreographyEvent evt)
    {
        HandleRewindTime();
        HandleLockFire();
    }

    private void HandleRewindTime()
    {
        if (Time.timeScale != 0f && CheckRewindToBeat())
        {
            rewindFeedback.PlayFeedbacks();
            StartCoroutine(GameManager.instance.RewindTime(rewindTimeScale, rewindDuration, returnToNormalDuration));
        }
        if (Time.timeScale != 0f && CheckSlowToBeat())
        {
            StartCoroutine(GameManager.instance.RewindTime(slowTimeScale, slowTimeDuration, returnToNormalDuration));
        }
        if (CheckRewindToBeat() && rewindTriggedStillPressed)
        {
            rewindTriggedStillPressed = false;
        }
    }

    private void HandleLockFire()
    {
        if (triggeredLockFire && !CheckLockProjectiles())
            triggeredLockFire = false;
    }
    
    public void ReleasePlayerLocks()
    {
        ClearLockedTargets();
        // Add any additional logic needed for releasing player locks
    }

    public void RemoveLockedEnemy(Transform enemy)
    {
        LockedList.Remove(enemy);
        projectileTargetList.Remove(enemy);
        enemyTargetList.Remove(enemy);
    }

    public int returnLocks()
    {
        return Locks;
    }

    public int returnEnemyLocks()
    {
        return enemyTargetList.Count;
    }

    public Vector3 RaycastTarget()
    {
        if (Physics.Raycast(RaySpawn.transform.position, RaySpawn.transform.forward, out RaycastHit hit, range))
            return hit.point;
        else
            return new Ray(RaySpawn.transform.position, RaySpawn.transform.forward).GetPoint(range);
    }

    private void OnDrawGizmos()
    {
        if (showRaycastGizmo && RaySpawn != null)
        {
            Gizmos.color = Color.red;
            Matrix4x4 rotationMatrix = Matrix4x4.TRS(RaySpawn.transform.position, RaySpawn.transform.rotation, Vector3.one);
            Gizmos.matrix = rotationMatrix;
            Gizmos.DrawWireCube(Vector3.forward * range * 0.5f, new Vector3(bulletLockBoxSize.x, bulletLockBoxSize.y, range));
        }
    }

#if UNITY_EDITOR
    private void OnValidate() => UnityEditor.SceneView.RepaintAll();
#endif
}