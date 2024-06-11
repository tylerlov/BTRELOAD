using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using SonicBloom.Koreo;
using UnityEngine.SceneManagement; // Import the Scene Management namespace
using MoreMountains.Feedbacks;
//using XInputDotNetPure;
using Cinemachine;
using FluffyUnderware.Curvy;
using Chronos;
using FMODUnity;
using UnityEngine.VFX;
using DG.Tweening;
using FluffyUnderware.Curvy.Controllers;
using UnityEngine.InputSystem;
using System;
using Micosmo.SensorToolkit;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class Crosshair : MonoBehaviour
{
    public static Crosshair Instance { get; private set; }

    public GameObject Player;
    [EventID]
    public string eventIDShooting;
    [EventID]
    public string eventIDRewindTime;
    
    private StudioEventEmitter musicPlayback;
    private StaminaController staminaController;

    [Space]

    public int Locks;
    public bool locking;
    [SerializeField] private bool shootTag;
    public bool triggeredLockFire;
    
    [Header("Targets")]

    public List<Transform> projectileTargetList = new List<Transform>();
    public Transform enemyTarget;
    public int maxLockedEnemyTargets = 3;
    public List<Transform> enemyTargetList = new List<Transform>();
    private int enemyTargetListIndex;
    public GameObject LineToTarget;
    public List<Transform> LockedList = new List<Transform>();
    //use this for firing off at end of rewind?
    public int maxTargets = 6;
    


    [Space]

    [Header("UI")]

    private Transform canvas;
    private UILockOnEffect uiLockOnEffect;
    public GameObject RaySpawn;
    public GameObject RaySpawnEnemyLocking;
    public GameObject Reticle;

    private Renderer RectRend;
    public int range = 300;

    float lookHeight;

    private RaycastHit hit;
    private RaycastHit hitEnemy;

    private int numOfRewinds;
    private bool rewindTriggedStillPressed;

    [Header("Feedbacks")]
    public MMF_Player lockFeedback;
    public MMF_Player shootFeedback;
    public MMF_Player rewindFeedback;
    public MMF_Player longrewindFeedback;

    [Header("FX")]
    public ParticleSystem temporalBlast;
    public VisualEffect RewindFX;
    public ParticleSystem RewindFXScan;
    public GameObject BonusDamage;
    public VisualEffect slowTime;

    private bool stereoVibrateSwitch;
    
    [Space]

    [SerializeField] private UnityEvent playerLockingOn;
    [SerializeField] private UnityEvent playerLockingOff;

    private bool collectHealthMode;

    [SerializeField] private LayerMask groundMask;


    private RaycastHit[] lockHits = new RaycastHit[10];
    private RaycastHit[] lockEnemyHits = new RaycastHit[10];

    private Camera mainCamera;
    FMOD.Studio.Bus masterBus;


    // New variables to facilitate the rate-limited locking mechanic.
    private float lastBulletTime;
    public float bulletLockInterval = 0.1f;  // Time in seconds between each bullet lock
    private float lastEnemyTime;
    public float enemyLockInterval = 0.2f;   // Time in seconds between each enemy lock

    private DefaultControls playerInputActions;

    [Header("Raycast Settings")]
    public Vector3 bulletLockBoxSize = new Vector3(1, 1, 1); // Adjustable in Inspector
    public bool showRaycastGizmo = true; // Toggle to show/hide the Gizmo
    
    ////
    // TIME CONTROL FEATURES
    ////
    
    public GameObject gameplayPlane;
    private SplineController splineControl;
    private PlayerMovement pMove;
    private bool delayLoop;

    // Variables for quick tap detection
    private bool isQuickTap = false;
    private float tapStartTime;
    private const float tapThreshold = 0.1f; // Adjusted for a shorter quick tap sensitivity
    
    [SerializeField] private float rewindTimeScale = -1f; // Default value set to -1f, adjustable in Inspector
    [SerializeField] private float slowTimeScale = 0.1f; // Default value set to 0.1f, adjustable in Inspector

    [Header("Lock-On Visuals")]
    [SerializeField] private GameObject lockOnPrefab; // Prefab for visual effect on lock-on
    [SerializeField] private float initialScale = 1f; // Initial scale for the prefab
    [SerializeField] private float initialTransparency = 0.5f; // Initial transparency for the prefab

    void Awake()
    {
        playerInputActions = new DefaultControls();
        playerInputActions.Player.Enable();
        
        // Subscribe to the sceneLoaded event
        SceneManager.sceneLoaded += OnSceneLoaded;

        Instance = this;
    }

    void OnDestroy()
    {
        // Unsubscribe to avoid memory leaks
        SceneManager.sceneLoaded -= OnSceneLoaded;
        UnregisterKoreographyEvents();
    }

    // This method will be called every time a scene is loaded
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        GameObject musicGameObject = GameObject.Find("FMOD Music");
        if (musicGameObject != null)
        {
            musicPlayback = musicGameObject.GetComponent<StudioEventEmitter>();
            if (musicPlayback == null)
            {
                Debug.LogError("StudioEventEmitter component not found on 'FMOD Music' GameObject.");
            }
        }
        else
        {
            Debug.LogError("GameObject with name 'FMOD Music' not found in the scene.");
        }

        staminaController = FindObjectOfType<StaminaController>();
        uiLockOnEffect = FindObjectOfType<UILockOnEffect>();
        splineControl = FindObjectOfType<SplineController>();
        pMove = FindObjectOfType<PlayerMovement>();
        mainCamera = Camera.main;

        // Reset or initialize any other scene-dependent settings here
        ResetSceneSpecificSettings();
    }

    // Example method to reset or initialize scene-specific settings
    private void ResetSceneSpecificSettings()
    {
        // Reset or initialize settings that are specific to the scene here
        enemyTargetList.Clear();
        projectileTargetList.Clear();
        LockedList.Clear();
        // Any other scene-specific initialization can go here
    }

    void Start()
    {
        masterBus = FMODUnity.RuntimeManager.GetBus("bus:/");

        RectRend = Reticle.GetComponent<Renderer>();

        locking = true;

        delayLoop = false;

        numOfRewinds = 0;
        rewindTriggedStillPressed = false;
        stereoVibrateSwitch = true;

        //Quick fix to match list index and such
        maxLockedEnemyTargets = maxLockedEnemyTargets - 1;

        collectHealthMode = false;

        RegisterKoreographyEvents(); // Register Koreographer events here
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
        {
            Debug.LogError("Failed to register events: Koreographer instance is null");
        }
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
        if (CheckLockProjectilesButtonDown())
        {
            tapStartTime = Time.time; // Record the time when the button is pressed down
        }

        if (CheckLockProjectilesButtonUp())
        {
            if (Time.time - tapStartTime <= tapThreshold) // Check if the release was quick enough
            {
                Debug.Log("Quick tap detected - this would be a parry");
            }
            isQuickTap = false; // Reset the flag after processing the tap
        }

        Debug.DrawRay(RaySpawn.transform.position, RaySpawn.transform.forward, Color.green);

        OnLock();
        OnLockEnemy();
    }

    private bool CheckLockProjectilesButtonDown()
    {
        // Implementation depends on the input system being used
        return playerInputActions.Player.LockProjectiles.triggered;
    }

    private bool CheckLockProjectilesButtonUp()
    {
        // Implementation depends on the input system being used
        // This is a placeholder for actual input detection logic
        return !playerInputActions.Player.LockProjectiles.IsPressed();
    }

    private bool CheckLockProjectiles()
    {
        var value = playerInputActions.Player.LockProjectiles.ReadValue<float>();
        return value > 0;
    }
    private bool CheckLockEnemies()
    {
        return playerInputActions.Player.LockEnemies.ReadValue<float>() > 0;
    }
    private bool CheckRewind()
    {
        return playerInputActions.Player.RewindTime.ReadValue<float>() > 0;
    }
    private bool SlowTime()
    {
        return playerInputActions.Player.SlowTime.ReadValue<float>() > 0;
    }

public void OnLock()
{
    if (!IsTimeToLockBullet()) return;

    RaycastHit[] hits = PerformBulletLockBoxCast();
    foreach (var hit in hits)
    {
        if (!IsValidBulletHit(hit)) continue;

        UpdateLastBulletLockTime();
        if (isQuickTap)
        {
            isQuickTap = false; // Reset quick tap flag
            break; // Exit after parrying the shot
        }
        else if (TryLockOntoBullet(hit))
        {
            break; // Successfully locked onto a bullet, exit the loop
        }
    }
}

private bool IsTimeToLockBullet()
{
    return Time.time >= lastBulletTime + bulletLockInterval;
}

private RaycastHit[] PerformBulletLockBoxCast()
{
    RaycastHit[] hits = new RaycastHit[10]; // Adjust size based on expected maximum hits
    int hitsCount = Physics.BoxCastNonAlloc(RaySpawn.transform.position, bulletLockBoxSize / 2, RaySpawn.transform.forward, hits, RaySpawn.transform.rotation, range);
    Array.Resize(ref hits, hitsCount); // Resize the array to match the actual number of hits
    return hits;
}

private bool IsValidBulletHit(RaycastHit hit)
{
    return hit.collider != null && (hit.collider.CompareTag("Bullet") || hit.collider.CompareTag("LaunchableBullet"));
}

private void UpdateLastBulletLockTime()
{
    lastBulletTime = Time.time;
}

private bool TryLockOntoBullet(RaycastHit hit)
{
    ProjectileStateBased hitPSB = hit.transform.GetComponent<ProjectileStateBased>();
    if (collectHealthMode && hitPSB && hitPSB.GetCurrentStateType() == typeof(EnemyShotState))
    {
        HandleCollectHealthMode(hit);
        return true; // Indicates that locking has started
    }
    else if (!collectHealthMode && staminaController.canRewind && CheckLockProjectiles())
    {
        return TryAddBulletToLockList(hit, hitPSB);
    }
    return false; // Indicates that no locking occurred
}

private void HandleCollectHealthMode(RaycastHit hit)
{
    // Additional game logic for collectHealthMode.
    GameManager.instance.AddScore(100);
    StartCoroutine(LockVibrate());
    lockFeedback.PlayFeedbacks();
    PlayRandomLocking();
    hit.transform.GetComponent<ProjectileStateBased>().Death();
    UpdateLastBulletLockTime(); // Update lastBulletTime here for all successful locks
}

private bool TryAddBulletToLockList(RaycastHit hit, ProjectileStateBased hitPSB)
{
    if (!projectileTargetList.Contains(hit.transform) && hitPSB && hitPSB.GetCurrentStateType() == typeof(EnemyShotState))
    {
        if (LockedList.Count < maxTargets && projectileTargetList.Count < maxTargets)
        {
            hit.transform.GetComponent<ProjectileStateBased>().ChangeState(new PlayerLockedState(hit.transform.GetComponent<ProjectileStateBased>()));
            projectileTargetList.Add(hit.transform);
            musicPlayback.EventInstance.setParameterByName("Lock State", 1);
            UpdateLastBulletLockTime(); // Update lastBulletTime here for all successful locks
            if (LockedList.Count == 0) playerLockingOn.Invoke(); // Trigger events and feedback for the first lock
            HandleMaxTargetsReached();
            return true; // Indicates that locking has started
        }
    }
    return false; // Indicates that no locking occurred
}

private void HandleMaxTargetsReached()
{
    if (LockedList.Count == maxTargets - 1)
    {
        BonusDamage.SetActive(true);
        foreach (Transform TargetProjectile in LockedList)
        {
            TargetProjectile.GetComponent<ProjectileStateBased>().damageAmount *= 1.5f;
        }
    }
}

    public void OnLockEnemy()
    {
        if (!IsTimeToLockEnemy()) return;

        RaycastHit[] hits = PerformEnemyLockBoxCast();
        foreach (var hit in hits)
        {
            if (!IsValidEnemyHit(hit)) continue;

            UpdateLastEnemyLockTime();
            hitEnemy = hit; // Assuming hitEnemy is used elsewhere in the class

            if (!ShouldLockOntoEnemy()) continue;

            Transform enemyTransform = GetEnemyTransform(hit);
            if (enemyTransform != null && !enemyTargetList.Contains(enemyTransform))
            {
                LockOntoEnemy(enemyTransform);
                return; // Exit after locking onto an enemy
            }
        }
    }

    private bool IsTimeToLockEnemy()
    {
        return Time.time >= lastEnemyTime + enemyLockInterval;
    }

    private RaycastHit[] PerformEnemyLockBoxCast()
    {
        int enemyLayerMask = 1 << LayerMask.NameToLayer("Enemy");
        int groundLayerMask = 1 << LayerMask.NameToLayer("Ground");
        int combinedLayerMask = enemyLayerMask | groundLayerMask;

        RaycastHit[] hits = new RaycastHit[10]; // Adjust size based on expected maximum hits
        Physics.BoxCastNonAlloc(RaySpawn.transform.position, RaySpawnEnemyLocking.transform.lossyScale / 8, RaySpawnEnemyLocking.transform.forward, hits, RaySpawnEnemyLocking.transform.rotation, range, combinedLayerMask);
        return hits;
    }

    private bool IsValidEnemyHit(RaycastHit hit)
    {
        return hit.collider != null && hit.collider.CompareTag("Enemy") && CheckLockProjectiles();
    }

    private bool ShouldLockOntoEnemy()
    {
        //Temporarily changed to allow enemy lock on without projecitles already locked
        return CheckLockEnemies(); //&& LockedList.Count > 0;
    }

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
            colliderCallback.SetLockedStatus(true); // Assuming this method exists
            return hit.collider.transform;
        }

        return null;
    }

    private void LockOntoEnemy(Transform enemyTransform)
    {
        if (enemyTargetList.Count > maxLockedEnemyTargets)
        {
            UnlockOldestEnemy();
        }

        enemyTarget = enemyTransform;
        enemyTargetList.Add(enemyTarget);
        enemyTargetListIndex = enemyTargetList.Count; // Reset index count
        FMODUnity.RuntimeManager.PlayOneShot("event:/Player/LockEnemy");
        uiLockOnEffect.LockOnTarget(enemyTarget);
        lastEnemyTime = Time.time; // Reset the timer
    }

    private void UnlockOldestEnemy()
    {
        Transform oldestEnemy = enemyTargetList[0];
        EnemyBasicSetup setup = oldestEnemy.GetComponent<EnemyBasicSetup>();
        if (setup != null) setup.lockedStatus(false);

        ColliderHitCallback callback = oldestEnemy.GetComponent<ColliderHitCallback>();
        if (callback != null) callback.SetLockedStatus(false); // Assuming this method exists

        enemyTargetList.RemoveAt(0);
    }

    private void UpdateLastEnemyLockTime()
    {
        lastEnemyTime = Time.time;
    }
void OnMusicalLock(KoreographyEvent evt)
    {
        if (CheckLockProjectiles() && projectileTargetList.Count > 0 && Time.timeScale != 0f)
        {
            // Check if there's a target on Targets that's not on LockedList!!

            // Turning on the target's aim prefab
            projectileTargetList[0].transform.GetChild(0).gameObject.SetActive(true);
            projectileTargetList[0].GetComponent<ProjectileStateBased>().ChangeState(new PlayerLockedState(projectileTargetList[0].GetComponent<ProjectileStateBased>()));
            Debug.Log(projectileTargetList[0].GetComponent<ProjectileStateBased>().GetCurrentState().ToString() + " is the current state");
            LockedList.Add(projectileTargetList[0]);
            StartCoroutine(LockVibrate());
            lockFeedback.PlayFeedbacks();
            PlayRandomLocking();
            Locks++;
            projectileTargetList.Remove(projectileTargetList[0]);

            // Instantiate and animate the lock-on prefab
            if (lockOnPrefab != null)
            {
                GameObject lockOnInstance = Instantiate(lockOnPrefab, Reticle.transform);
                lockOnInstance.SetActive(true);
                lockOnInstance.transform.localPosition = Vector3.zero; // Center it on the Reticle
                lockOnInstance.transform.localScale = Vector3.one * initialScale; // Set the initial scale

                // Assuming the prefab has a SpriteRenderer component
                SpriteRenderer spriteRenderer = lockOnInstance.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    Color initialColor = spriteRenderer.color;
                    initialColor.a = initialTransparency; // Set initial transparency to the specified value
                    spriteRenderer.color = initialColor;

                    // Animate transparency to fully visible
                    spriteRenderer.DOFade(1f, 0.5f);
                }

                // Scale down and destroy
                lockOnInstance.transform.DOScale(Vector3.zero, 1f).OnComplete(() => Destroy(lockOnInstance)); // Slower scaling
            }

            if (!staminaController.canRewind)
            {
                triggeredLockFire = true;
            }
        }
    }
    void OnMusicalShoot(KoreographyEvent evt)
    {
        // Check all conditions for shooting
        if ((!CheckLockProjectiles() /*|| triggeredLockFire == true*/) && LockedList.Count > 0 && Time.timeScale != 0f)
        {
            List<PlayerLockedState> projectilesToLaunch = new List<PlayerLockedState>();

            int comboScore = 0;
            int tempLocks = Locks;

            bool lockingEnding = true;

            // Ensure there are items in LockedList before proceeding
            if ((!CheckLockProjectiles() /*|| triggeredLockFire == true*/) && LockedList.Count > 0 && Time.timeScale != 0f)
            {
                locking = false;

                CleanLockedList();

                for (int i = LockedList.Count - 1; i >= 0; i--)
                {
                    if (LockedList[i] == null) continue; // Skip if the Transform is null

                    ProjectileStateBased projectileStateBased = LockedList[i].GetComponent<ProjectileStateBased>();
                    if (!(projectileStateBased.GetCurrentState() is PlayerLockedState))
                    {
                        projectileStateBased.ChangeState(new PlayerLockedState(projectileStateBased));
                    }

                    PlayerLockedState lockedState = projectileStateBased.GetCurrentState() as PlayerLockedState;
                    if (lockedState != null)
                    {
                        // Check if there are enemies to lock onto
                        if (enemyTargetList.Count > 0)
                        {
                            // Ensure enemyTargetListIndex is within bounds
                            if (enemyTargetListIndex <= 0 || enemyTargetListIndex > enemyTargetList.Count)
                            {
                                enemyTargetListIndex = enemyTargetList.Count; // Adjust index to be within bounds
                            }

                            Transform currEnemyTarg = enemyTargetList[enemyTargetListIndex - 1];

                            if (currEnemyTarg != null && currEnemyTarg.gameObject.activeSelf)
                            {
                                lockedState.LaunchAtEnemy(currEnemyTarg);
                            }
                            else
                            {
                                enemyTargetList.Remove(currEnemyTarg);
                                if (enemyTargetList.Count <= 0)
                                {
                                    projectilesToLaunch.Add(lockedState);
                                }
                            }

                            enemyTargetListIndex--;
                        }
                        else
                        {
                            projectilesToLaunch.Add(lockedState);
                            Debug.Log("Outer LaunchBack() called.");
                        }

                        staminaController.locking = false;
                    }
                    else
                    {
                        Debug.LogError("Failed to cast to PlayerLockedState or change state to PlayerLockedState.");
                    }

                    // Safely remove the current item from LockedList
                    LockedList.RemoveAt(i);

                    // Instantiate and animate the lock-on prefab for each projectile shot
                    if (lockOnPrefab != null)
                    {
                        GameObject lockOnInstance = Instantiate(lockOnPrefab, Reticle.transform);
                        lockOnInstance.SetActive(true);
                        lockOnInstance.transform.localPosition = Vector3.zero; // Center it on the Reticle
                        lockOnInstance.transform.localScale = Vector3.zero; // Set the initial scale to zero

                        // Assuming the prefab has a SpriteRenderer component
                        SpriteRenderer spriteRenderer = lockOnInstance.GetComponent<SpriteRenderer>();
                        if (spriteRenderer != null)
                        {
                            Color initialColor = spriteRenderer.color;
                            initialColor.a = 0f; // Set initial transparency to 0
                            spriteRenderer.color = initialColor;

                            // Animate transparency to fully visible and then back to not visible
                            spriteRenderer.DOFade(1f, 0.25f).OnComplete(() => spriteRenderer.DOFade(0f, 0.25f));
                        }

                        // Scale up and destroy
                        lockOnInstance.transform.DOScale(Vector3.one * initialScale, 0.5f).OnComplete(() => Destroy(lockOnInstance)); // Scale up
                    }
                }

                StartCoroutine(LaunchProjectilesWithDelay(projectilesToLaunch));
                StartCoroutine(ShootVibrate());
                shootFeedback.PlayFeedbacks();

                if (lockingEnding)
                {
                    playerLockingOff.Invoke();
                    lockingEnding = false;
                }
                if (shootTag)
                {
                    PlayRandomShootTag();
                }
                else
                {
                    PlayRandomShooting();
                }

                locking = true;
                GameManager.instance.AddShotTally(1);
                comboScore++;
                Locks = Locks - 1;

                musicPlayback.EventInstance.setParameterByName("Lock State", 0);
                lockFeedback.StopFeedbacks();
                FMODUnity.RuntimeManager.PlayOneShot("event:/Level - 172 Flux/Firing Blasts");

                GameManager.instance.AddScore(10 * (comboScore * tempLocks));
            }
        }
        else
        {
            // Handle the case where the conditions are not met
        }
    }

    private IEnumerator LaunchProjectilesWithDelay(List<PlayerLockedState> projectiles)
    {
        float delayBetweenShots = 0.1f; // Adjust this value to control the delay between each shot

        foreach (var lockedState in projectiles)
        {
            if (lockedState != null)
            {
                lockedState.LaunchBack();
                FMODUnity.RuntimeManager.PlayOneShot("event:/Level - 172 Flux/Firing Blasts");
                StartCoroutine(ShootVibrate());
                yield return new WaitForSeconds(delayBetweenShots);
                Debug.Log("Inner Projectile Launch called.");
            }
        }
    }

    public int returnScore()
    {
        return GameManager.instance.Score;
    }
    public int returnLocks()
    {
        return Locks;
    }
    public int returnEnemyLocks()
    {
        return enemyTargetList.Count;
    }
    public void subtractLocks(int lockNum)
    {
        Locks = Locks - lockNum;
    }
    public bool FindLostLockedEnemy(Transform enemy)
    {
        if (LockedList.Contains(enemy))
            return true;
        else
            return false;
    }
    public void removeLostLockedEnemy(Transform enemy)
    {
        LockedList.Remove(enemy);
    }
    private IEnumerator LockVibrate()
    {
        //GamePad.SetVibration(playerIndex, 1f, 0f);
        yield return new WaitForSeconds(.2f);
        //GamePad.SetVibration(playerIndex, 0f, 0f);
    }
    private IEnumerator ShootVibrate()
    {
        //GamePad.SetVibration(playerIndex, 0f, 0.8f);
        yield return new WaitForSeconds(.1f);
        //GamePad.SetVibration(playerIndex, 0f, 0f);
    }
    void PlayRandomLocking()
    {
        FMODUnity.RuntimeManager.PlayOneShot("event:/Player/Locking");
    }
    void PlayRandomShooting()
    {
        FMODUnity.RuntimeManager.PlayOneShot("event:/Player/Shooting");
    }
    void PlayRandomShootTag()
    {
        FMODUnity.RuntimeManager.PlayOneShot("event:/Player/Shoot Tag");
    }
    void UpdateTime(KoreographyEvent evt)
    {
        HandleRewindTime();
        HandleLockFire();
    }

    void HandleRewindTime()
    {
        if (Time.timeScale != 0f && CheckRewind())
        {
            rewindFeedback.PlayFeedbacks(); // Play rewind feedback
            StartCoroutine(RewindToBeat());
        }
        if (Time.timeScale != 0f && SlowTime())
        {
            StartCoroutine(SlowToBeat());
        }
        if (CheckRewind() && rewindTriggedStillPressed)
        {
            rewindTriggedStillPressed = false;
        }
    }

    void HandleLockFire()
    {
        if (triggeredLockFire && !CheckLockProjectiles())
        {
            triggeredLockFire = false;
        }
    }


    // Return a hit location or return endpoint of the raycast
    public Vector3 RaycastTarget()
    {
        RaycastHit hit;
        if (Physics.Raycast(RaySpawn.transform.position, RaySpawn.transform.forward, out hit, range))
        {
            return hit.point;
        }
        else
        {
            Ray ray = new Ray(RaySpawn.transform.position, RaySpawn.transform.forward);
            return ray.GetPoint(range);
        }
    }
    private IEnumerator RewindToBeat()
    {
        if (delayLoop) yield break;

        float tempSpeed = splineControl.Speed;
        delayLoop = true;
        ActivateRewindEffects(true);
        Clock clock = Timekeeper.instance.Clock("Test");
        float startPosition = SetClockAndGetPosition(clock, rewindTimeScale);
        splineControl.Speed = tempSpeed * rewindTimeScale;;
        yield return new WaitForSeconds(3f);
        splineControl.Speed = tempSpeed;
        DeactivateRewindEffects();
        SetClockAndGetPosition(clock, 1f, startPosition);
        pMove.UpdateAnimation();
        splineControl.MovementDirection = MovementDirection.Forward;


        delayLoop = false;
    }

    //This one rewinds time shorter, switches attack modes
    private IEnumerator SlowToBeat()
    {
        if (delayLoop || !staminaController.canRewind) yield break;

        float tempSpeed = splineControl.Speed;
        delayLoop = true;
        HandleLockedTargets();
        splineControl.Speed = tempSpeed * slowTimeScale;
        Clock clock = Timekeeper.instance.Clock("Test");
        float startPosition = SetClockAndGetPosition(clock, slowTimeScale);
        numOfRewinds++;
        //SwitchAttackModes();
        staminaController.StaminaRewind();
        FMODUnity.RuntimeManager.PlayOneShot("event:/Player/Pulsewave");
        musicPlayback.EventInstance.setParameterByName("Slow", 1);
        slowTime.SetFloat("Rate", 10f);
        yield return new WaitForSeconds(5f);
        slowTime.SetFloat("Rate", 0f);
        musicPlayback.EventInstance.setParameterByName("Slow", 0);
        splineControl.Speed = tempSpeed;
        SetClockAndGetPosition(clock, 1f, startPosition);
        pMove.UpdateAnimation();
        delayLoop = false;
    }

    public IEnumerator RewindToBeatEnemyDeath()
    { 
        if (delayLoop) yield break;
        float tempSpeed = splineControl.Speed;

        delayLoop = true;
        ActivateRewindEffects(true);
        Clock clock = Timekeeper.instance.Clock("Test");
        float startPosition = SetClockAndGetPosition(clock, -1f);
        splineControl.Speed = 0;

        yield return new WaitForSeconds(1f);
        DeactivateRewindEffects();
        SetClockAndGetPosition(clock, 1f, startPosition);
        splineControl.Speed = tempSpeed;

        delayLoop = false;
    }

    public void ActivateRewindEffects(bool activate)
    {
        musicPlayback.EventInstance.setParameterByName("Rewind", activate ? 1 : 0);
        if (activate)
        {
            RewindFXScan.Play();
            longrewindFeedback.PlayFeedbacks();
            //doBlur.SetIntensity(0.3f);
            //isBlurActive = true;
        }
    }
    public void DeactivateRewindEffects()
    {
        ActivateRewindEffects(false);
        //doBlur.SetIntensity(0f);
        //isBlurActive = false;
        RewindFXScan.Stop();
    }

    public void ActivateSlowEffects(bool activate)
    {
        musicPlayback.EventInstance.setParameterByName("Rewind", activate ? 1 : 0);
        if (activate)
        {
            RewindFXScan.Play();
            longrewindFeedback.PlayFeedbacks();
            //doBlur.SetIntensity(0.3f);
            //isBlurActive = true;
        }
    }


    float SetClockAndGetPosition(Clock clock, float localTimeScale, float startPosition = 0f)
    {
        clock.localTimeScale = localTimeScale;
        //float position = splineControl.RelativePosition;
        //if (localTimeScale < 0) return position;
        //float positionDif = position - startPosition;
        //splineControl.RelativePosition = startPosition - positionDif;
        return 0f;
    }

    void HandleLockedTargets()
    {
        if (numOfRewinds == 0 && LockedList.Count > 0)
        {
            Vector3 currPosition = transform.localPosition;
            Vector3 centerPosition = GetCenterPosition();
            transform.position = new Vector3(centerPosition.x, centerPosition.y, transform.position.z);
            ParentLockedList();
            transform.localPosition = currPosition;
        }
    }

    Vector3 GetCenterPosition()
    {
        float totalX = 0f;
        float totalY = 0f;
        foreach (var targetLocked in LockedList)
        {
            if (targetLocked == null) return Vector3.zero;
            totalX += targetLocked.transform.position.x;
            totalY += targetLocked.transform.position.y;
        }
        float centerX = totalX / LockedList.Count;
        float centerY = totalY / LockedList.Count;
        return new Vector3(centerX, centerY, 0f);
    }

    void ParentLockedList()
    {
        for (int i = LockedList.Count - 1; i >= 0; i--)
        {
            if (LockedList[i].gameObject.CompareTag("LaunchableBullet"))
            {
                if (LockedList[i].gameObject.transform.parent != Reticle.transform)
                {
                    LockedList[i].gameObject.transform.SetParent(Reticle.transform);
                }
            }
        }
    }

    void CleanLockedList()
    {
        LockedList.RemoveAll(Transform => Transform == null);
    }

    public void ReleasePlayerLocks()
    {
        for (int i = 0; i < LockedList.Count; i++)
        {
            if (!LockedList[i].gameObject.activeSelf)
            {
                LockedList.RemoveAt(i);
                Locks--;
            }
        }
    }

    void SwitchAttackModes()
    {
        collectHealthMode = !collectHealthMode;
        gameObject.transform.DOScale(collectHealthMode ? 2f : 1f, collectHealthMode ? 0.1f : 0.44f);
        gameObject.transform.DORotate(new Vector3(0, 0, 45), 0.1f, RotateMode.LocalAxisAdd);
    }

    public void RemoveLockedEnemy(Transform enemyTransform)
    {
        if (enemyTargetList.Contains(enemyTransform))
        {
            enemyTransform.GetComponent<EnemyBasicSetup>()?.lockedStatus(false);
            enemyTransform.GetComponent<ColliderHitCallback>()?.SetLockedStatus(false);
            enemyTargetList.Remove(enemyTransform);
        }
    }

    public float CalculateAimAccuracy(Transform enemyTarget)
    {
        Vector3 aimPoint = RaycastTarget(); // Assuming RaycastTarget() returns the point where the crosshair is aiming
        if (enemyTarget == null) return 0f; // No target, no accuracy

        float distanceToTarget = Vector3.Distance(aimPoint, enemyTarget.position);
        float accuracy = Mathf.Clamp01(1 - (distanceToTarget / range)); // Assuming 'range' is the effective range of accuracy calculation
        return accuracy;
    }

    void OnDrawGizmos()
    {
        if (showRaycastGizmo && RaySpawn != null)
        {
            Gizmos.color = Color.red; // Set the color of the Gizmo
            Matrix4x4 rotationMatrix = Matrix4x4.TRS(RaySpawn.transform.position, RaySpawn.transform.rotation, Vector3.one);
            Gizmos.matrix = rotationMatrix;
            Gizmos.DrawWireCube(Vector3.forward * range * 0.5f, new Vector3(bulletLockBoxSize.x, bulletLockBoxSize.y, range));
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        SceneView.RepaintAll();
    }
#endif

    
}
