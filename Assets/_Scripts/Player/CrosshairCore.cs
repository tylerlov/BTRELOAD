using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using FMODUnity;
using SonicBloom.Koreo;
using UnityEngine.SceneManagement;

public class CrosshairCore : MonoBehaviour
{
    public static CrosshairCore Instance { get; private set; }

    #region Core References
    [Header("Core References")]
    [SerializeField] private GameObject Player;
    [SerializeField] private GameObject LineToTarget;
    [SerializeField] public GameObject RaySpawn;
    [SerializeField] public GameObject RaySpawnEnemyLocking;
    [SerializeField] public GameObject Reticle;
    [SerializeField] public GameObject BonusDamage;
    #endregion

    #region Lock-On Visuals
    [Header("Lock-On Visuals")]
    [SerializeField] public GameObject lockOnPrefab;
    [SerializeField] public float initialScale = 1f;
    [SerializeField] public float initialTransparency = 0.5f;
    #endregion

    #region Input and Time
    private DefaultControls playerInputActions;
    public float lastProjectileLaunchTime;
    public const float QTE_TRIGGER_WINDOW = 1f;
    #endregion

    #region State Variables
    public bool isQuickTap;
    public bool collectHealthMode;
    public RaycastHit hitEnemy;
    #endregion

    #region Events
    public event Action<float> OnRewindStart;
    public event Action OnRewindEnd;
    #endregion

    #region Components
    [HideInInspector] public StudioEventEmitter musicPlayback;
    #endregion

    #region Koreography Events
    [Header("Koreography Events")]
    [EventID] public string eventIDShooting;
    [EventID] public string eventIDRewindTime;
    #endregion

    [SerializeField] private LayerMask groundMask;
    public bool showRaycastGizmo = true;
    private float tapStartTime;
    private const float tapThreshold = 0.1f;

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

        playerInputActions = new DefaultControls();
        playerInputActions.Player.Enable();
        SceneManager.sceneLoaded += GetComponent<PlayerLocking>().OnSceneLoaded;
    }

    private void OnEnable()
    {
        RegisterKoreographyEvents();
    }

    private void OnDisable()
    {
        UnregisterKoreographyEvents();
    }
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= GetComponent<PlayerLocking>().OnSceneLoaded;
    }

    private void Start()
    {
        InitializeComponents();
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

        collectHealthMode = false;
    }

    private void Update()
    {
        HandleInput();
        Debug.DrawRay(RaySpawn.transform.position, RaySpawn.transform.forward, Color.green);
        GetComponent<PlayerLocking>().OnLock();
        GetComponent<PlayerLocking>().OnLockEnemy();
        GetComponent<PlayerTimeControl>().HandleRewindTime();
        GetComponent<PlayerTimeControl>().HandleRewindToBeat();
        GetComponent<PlayerTimeControl>().HandleSlowToBeat();

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

    public bool CheckLockProjectilesButtonDown() => playerInputActions.Player.LockProjectiles.triggered;

    public bool CheckLockProjectilesButtonUp()
    {
        return !playerInputActions.Player.LockProjectiles.IsPressed();
    }

    public bool CheckLockProjectiles() => playerInputActions.Player.LockProjectiles.ReadValue<float>() > 0;

    public bool CheckLockEnemies() => playerInputActions.Player.LockEnemies.ReadValue<float>() > 0;

    public bool CheckRewindToBeat() => playerInputActions.Player.RewindTime.ReadValue<float>() > 0;

    public bool CheckSlowToBeat() => playerInputActions.Player.SlowTime.ReadValue<float>() > 0;

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
            Koreographer.Instance.UnregisterForEvents(eventIDShooting, OnMusicalLock);
            Koreographer.Instance.UnregisterForEvents(eventIDRewindTime, UpdateTime);
        }
    }

    private void OnMusicalShoot(KoreographyEvent evt)
    {
        PlayerShooting playerShooting = GetComponent<PlayerShooting>();
        PlayerLocking playerLocking = GetComponent<PlayerLocking>();

        if ((!CheckLockProjectiles() || playerLocking.triggeredLockFire) && playerLocking.LockedList.Count > 0 && Time.timeScale != 0f)
        {
            List<PlayerLockedState> projectilesToLaunch = playerLocking.PrepareProjectilesToLaunch();

            if (projectilesToLaunch.Count > 0)
                StartCoroutine(playerShooting.LaunchProjectilesWithDelay(projectilesToLaunch));
            else
                playerLocking.ClearLockedTargets();

            playerShooting.HandleShootingEffects();
            musicPlayback.EventInstance.setParameterByName("Lock State", 0);
        }
    }

    private void OnMusicalLock(KoreographyEvent evt)
    {
        PlayerLocking playerLocking = GetComponent<PlayerLocking>();

        if (CheckLockProjectiles() && playerLocking.projectileTargetList.Count > 0 && Time.timeScale != 0f)
        {
            var target = playerLocking.projectileTargetList[0];
            target.transform.GetChild(0).gameObject.SetActive(true);
            target.GetComponent<ProjectileStateBased>().ChangeState(new PlayerLockedState(target.GetComponent<ProjectileStateBased>()));
            playerLocking.LockedList.Add(target);
            StartCoroutine(playerLocking.LockVibrate());
            playerLocking.lockFeedback.PlayFeedbacks();
            playerLocking.PlayRandomLocking();
            playerLocking.Locks++;
            playerLocking.projectileTargetList.RemoveAt(0);

            GetComponent<PlayerShooting>().AnimateLockOnEffect();
        }
    }

    private void UpdateTime(KoreographyEvent evt)
    {
        GetComponent<PlayerTimeControl>().HandleRewindToBeat();
        GetComponent<PlayerLocking>().HandleLockFire();
    }

    public void TriggerRewindStart(float timeScale)
    {
        OnRewindStart?.Invoke(timeScale);
    }

    public void TriggerRewindEnd()
    {
        OnRewindEnd?.Invoke();
    }
}