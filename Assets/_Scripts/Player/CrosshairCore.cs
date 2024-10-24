using System;
using System.Collections;
using System.Collections.Generic;
using FMODUnity;
using SonicBloom.Koreo;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class CrosshairCore : MonoBehaviour
{
    public static CrosshairCore Instance { get; private set; }

    #region Core References
    [Header("Core References")]
    [SerializeField]
    private GameObject Player;

    [SerializeField]
    private GameObject LineToTarget;

    [SerializeField]
    public GameObject RaySpawn;

    [SerializeField]
    public GameObject RaySpawnEnemyLocking;

    [SerializeField]
    public GameObject Reticle;

    [SerializeField]
    public GameObject BonusDamage;
    #endregion

    #region Lock-On Visuals
    [Header("Lock-On Visuals")]
    [SerializeField]
    public GameObject lockOnPrefab;

    [SerializeField]
    public float initialScale = 1f;

    [SerializeField]
    public float initialTransparency = 0.5f;
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
    [HideInInspector]
    public StudioEventEmitter musicPlayback;
    #endregion

    #region Koreography Events
    [Header("Koreography Events")]
    [EventID]
    public string eventIDShooting;

    [EventID]
    public string eventIDRewindTime;
    #endregion

    [SerializeField]
    private LayerMask groundMask;
    public bool showRaycastGizmo = true;
    private float tapStartTime;
    private const float tapThreshold = 0.1f;

    private PlayerLocking playerLocking;
    private PlayerShooting playerShooting;
    private PlayerTimeControl playerTimeControl;

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

        playerLocking = GetComponent<PlayerLocking>();
        playerShooting = GetComponent<PlayerShooting>();
        playerTimeControl = GetComponent<PlayerTimeControl>();

        if (playerLocking == null || playerShooting == null || playerTimeControl == null)
        {
            Debug.LogError("Required components not found on the same GameObject.");
        }
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
                Debug.LogError(
                    "StudioEventEmitter component not found on 'FMOD Music' GameObject."
                );
        }
        else
            Debug.LogError("GameObject with name 'FMOD Music' not found in the scene.");

        collectHealthMode = false;
    }

    private void Update()
    {
        HandleInput();
        UpdatePlayerComponents();
    }

    private void InitializeSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeInputActions()
    {
        playerInputActions = new DefaultControls();
        playerInputActions.Player.Enable();
        SceneManager.sceneLoaded += GetComponent<PlayerLocking>().OnSceneLoaded;
    }

    private void UpdatePlayerComponents()
    {
        Debug.DrawRay(RaySpawn.transform.position, RaySpawn.transform.forward, Color.green);
        GetComponent<PlayerLocking>().OnLock();
        GetComponent<PlayerLocking>().CheckEnemyLock(); // Make sure this line is present
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
                ConditionalDebug.Log("Quick tap detected - this would be a parry");
            isQuickTap = false;
        }
    }

    public bool CheckLockProjectilesButtonDown() =>
        playerInputActions.Player.LockProjectiles.triggered;

    public bool CheckLockProjectilesButtonUp()
    {
        return !playerInputActions.Player.LockProjectiles.IsPressed();
    }

    public bool CheckLockProjectiles() =>
        playerInputActions.Player.LockProjectiles.ReadValue<float>() > 0;

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

        if (
            (!CheckLockProjectiles() || playerLocking.triggeredLockFire)
            && playerLocking.GetLockedProjectileCount() > 0
            && Time.timeScale != 0f
        )
        {
            StartCoroutine(playerShooting.LaunchProjectilesWithDelay());
            playerShooting.HandleShootingEffects();
            
            // Reset both parameters
            if (musicPlayback != null && musicPlayback.EventInstance.isValid())
            {
                musicPlayback.EventInstance.setParameterByName("Lock State", 0);
                musicPlayback.EventInstance.setParameterByName("Player_Lock_State", 0);
            }
        }
    }

    private void OnMusicalLock(KoreographyEvent koreoEvent)
    {
        ConditionalDebug.Log($"OnMusicalLock called. Locked projectile count: {GetComponent<PlayerLocking>().GetLockedProjectileCount()}, Time scale: {Time.timeScale}");
        PlayerLocking playerLocking = GetComponent<PlayerLocking>();

        if (CheckLockProjectiles() && playerLocking.GetLockedProjectileCount() < playerLocking.maxTargets && Time.timeScale != 0f)
        {
            if (playerLocking.TryLockOntoProjectile())
            {
                ConditionalDebug.Log($"Locked onto projectile. Current locks: {playerLocking.Locks}");
                
                // Set FMOD parameter but don't let it affect enemy shooting
                if (musicPlayback != null && musicPlayback.EventInstance.isValid())
                {
                    musicPlayback.EventInstance.setParameterByName("Lock State", 1);
                    
                    // Add a new parameter specifically for player lock state
                    musicPlayback.EventInstance.setParameterByName("Player_Lock_State", 1);
                }

                GetComponent<PlayerShooting>().AnimateLockOnEffect();
            }
            else
            {
                ConditionalDebug.Log("Failed to lock onto projectile");
            }
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
