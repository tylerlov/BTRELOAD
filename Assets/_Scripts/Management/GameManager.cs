using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Cinemachine;
using Michsky.UI.Reach;
using PrimeTween;
using UnityEngine;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Player Reference")]
    [SerializeField]
    private PlayerMovement playerMovement;
    private PlayerHealth playerHealth;

    [SerializeField]
    private bool isPlayerInvincible = false;

    [Header("Dependencies")]
    public EnemyManager enemyManager;

    [SerializeField]
    private PauseMenuManager pauseMenuManager;

    private CinemachineStateDrivenCamera stateDrivenCamera;
    private DebugSettings debugSettings;
    private bool isPlayerDead = false;

    private List<Transform> spawnedEnemies = new List<Transform>();
    private Dictionary<Transform, bool> lockedEnemies = new Dictionary<Transform, bool>();

    public int totalPlayerProjectilesShot = 0;
    public int playerProjectileHits = 0;

    public ScoreManager ScoreManager { get; private set; }
    public AudioManager AudioManager { get; private set; }
    public TimeManager TimeManager { get; private set; }
    public SceneManagerBTR SceneManagerBTR { get; private set; }

    public static readonly string TransCamOnEvent = EventManager.TransCamOnEvent;
    public static readonly string StartingTransitionEvent = EventManager.StartingTransitionEvent;
    public static readonly string TransCamOffEvent = "TransCamOff";

    private PlayerUI playerUI;

    [Header("Dependencies")]
    [SerializeField] private AudioManager audioManager;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        InitializeManagers();
        PrimeTweenConfig.SetTweensCapacity(1600);
        InitializePlayerHealth();

        debugSettings = Resources.Load<DebugSettings>("DebugSettings");
        if (debugSettings == null)
        {
            ConditionalDebug.LogError("DebugSettings asset not found. Create it in Resources folder.");
        }
        else
        {
            TimeManager.SetDebugSettings(debugSettings);
        }

        playerUI = FindAnyObjectByType<PlayerUI>();
        if (playerUI == null)
        {
            ConditionalDebug.LogError("PlayerUI not found in the scene.");
        }
    }

    private void InitializeManagers()
    {
        ScoreManager = GetComponent<ScoreManager>();
        TimeManager = GetComponent<TimeManager>();
        SceneManagerBTR = GetComponent<SceneManagerBTR>();
        AudioManager = FindAnyObjectByType<AudioManager>();

        // Check for required components after assignment
        if (AudioManager == null)
        {
            ConditionalDebug.LogError("AudioManager not found in scene! Ensure it's attached to the FMOD music object.");
        }
        if (ScoreManager == null || TimeManager == null || SceneManagerBTR == null)
        {
            ConditionalDebug.LogError("One or more required manager components are missing on GameManager object!");
        }
    }

    private void InitializePlayerHealth()
    {
        if (playerHealth == null)
        {
            playerHealth = FindFirstObjectByType<PlayerHealth>();
            if (playerHealth == null)
            {
                ConditionalDebug.LogError("PlayerHealth component not found in the scene.");
            }
        }
    }

    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += InitializeListenersAndComponents;
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= InitializeListenersAndComponents;
    }

    private async void Start()
    {
        //MusicManager.FindActiveFMODInstance();
        TimeManager.InitializeDebugTimeScale();

        // Initialize scenes
        await SceneManagerBTR.InitializeScenes();
    }

    public void InitializeListenersAndComponents(
        UnityEngine.SceneManagement.Scene scene,
        UnityEngine.SceneManagement.LoadSceneMode mode
    )
    {
        UnityMainThreadDispatcher
            .Instance()
            .Enqueue(() =>
            {
                InitializeCameraSwitching();
                InitializeCrosshair();
                InitializeShooterMovement();

                stateDrivenCamera = FindFirstObjectByType<CinemachineStateDrivenCamera>();
                if (stateDrivenCamera == null)
                {
                    ConditionalDebug.LogError("No Cinemachine State Driven Camera found in the scene.");
                }
            });
    }

    private void InitializeCameraSwitching()
    {
        var cameraSwitching = FindFirstObjectByType<CinemachineCameraSwitching>();
        if (cameraSwitching != null)
        {
            EventManager.Instance.AddListener(
                EventManager.TransCamOnEvent,
                cameraSwitching.SwitchToTransitionCamera
            );
            EventManager.Instance.AddListener(
                EventManager.StartingTransitionEvent,
                cameraSwitching.SwitchToTransitionCamera
            );
        }
        else
        {
            ConditionalDebug.LogError("CinemachineCameraSwitching component not found in the scene.");
        }
    }

    private void InitializeCrosshair()
    {
        EventManager.Instance.AddListener(
            EventManager.TransCamOnEvent,
            PlayerLocking.Instance.ReleasePlayerLocks
        );
    }

    private void InitializeShooterMovement()
    {
        var shooterMovement = FindFirstObjectByType<ShooterMovement>();
        if (shooterMovement != null)
        {
            EventManager.Instance.AddListener(TransCamOffEvent, shooterMovement.ResetToCenter);
        }
        else
        {
            ConditionalDebug.LogError("ShooterMovement component not found in the scene for transCamOff.");
        }
    }

    public void HandlePlayerDeath()
    {
        if (isPlayerDead)
            return;

        isPlayerDead = true;
        ConditionalDebug.Log("Player has died. Pausing game and showing menu.");

        StopAllCoroutines();

        //MusicManager.ChangeMusicSectionByName("Death");

        DisableGameplayElements();

        ProjectileManager.Instance.ClearAllProjectiles();

        pauseMenuManager.AnimatePauseMenu();
    }

    public void RestartGame()
    {
        isPlayerDead = false;
        pauseMenuManager.AnimatePauseMenu();

        // Clear any remaining enemies and projectiles
        KillAllEnemies();
        ProjectileManager.Instance.ClearAllProjectiles();
        
        // Reset time scale first
        TimeManager.ResetTimeScale();
        
        // Reset player state
        EnableGameplayElements();
        if (playerHealth != null)
        {
            playerHealth.ResetScore();
        }
        else
        {
            InitializePlayerHealth();
            if (playerHealth != null)
            {
                playerHealth.ResetScore();
            }
        }
        
        // Reset invincibility state
        SetPlayerInvincibility(false);
        
        // Restart scene management
        SceneManagerBTR.RestartGame();
    }

    private void DisableGameplayElements()
    {
        if (playerMovement != null)
            playerMovement.enabled = false;

        KillAllEnemies();
        ProjectileManager.Instance.ClearAllProjectiles();
    }

    private void EnableGameplayElements()
    {
        if (playerMovement != null)
            playerMovement.enabled = true;
    }

    public void SetPlayerInvincibility(bool isInvincible)
    {
        if (isPlayerInvincible != isInvincible)
        {
            isPlayerInvincible = isInvincible;
            if (playerHealth != null)
            {
                playerHealth.SetInvincibleInternal(isInvincible);
            }
            else
            {
                ConditionalDebug.LogError("PlayerHealth is not set in GameManager.");
            }
        }
    }

    public bool IsPlayerInvincible()
    {
        return isPlayerInvincible;
    }

    public void DebugMoveToNextScene()
    {
        SceneManagerBTR.ChangeToNextScene();
    }

    public void HandleDebugSceneTransition()
    {
        int wavesToSimulate = 3;
        for (int i = 0; i < wavesToSimulate; i++)
        {
            ScoreManager.waveCounterAdd();
            SceneManagerBTR.MoveToNextSectionOrScene();
        }
    }

    public void RegisterEnemy(Transform enemy)
    {
        if (!spawnedEnemies.Contains(enemy))
        {
            spawnedEnemies.Add(enemy);
            lockedEnemies[enemy] = false;
            ConditionalDebug.Log($"Enemy registered: {enemy.name}");
        }
        else
        {
            ConditionalDebug.LogWarning($"Enemy already registered: {enemy.name}");
        }
    }

    public void SetEnemyLockState(Transform enemy, bool isLocked)
    {
        if (enemy == null)
        {
            lockedEnemies.Remove(enemy);
            return;
        }

        if (lockedEnemies.ContainsKey(enemy))
        {
            lockedEnemies[enemy] = isLocked;
            ConditionalDebug.Log($"Enemy {enemy.name} lock state set to: {isLocked}");
        }
        else
        {
            ConditionalDebug.LogWarning($"Attempted to set lock state for unregistered enemy: {enemy.name}");
        }
    }

    public void ClearAllEnemyLocks()
    {
        var enemiesToRemove = new List<Transform>();
        var enemyKeys = new List<Transform>(lockedEnemies.Keys);

        foreach (var enemy in enemyKeys)
        {
            if (enemy == null)
            {
                enemiesToRemove.Add(enemy);
                continue;
            }

            SetEnemyLockState(enemy, false);
            EnemyBasics enemySetup = enemy.GetComponent<EnemyBasics>();
            if (enemySetup != null)
            {
                enemySetup.SetLockedOnIndicator(false);
            }
        }

        foreach (var enemy in enemiesToRemove)
        {
            lockedEnemies.Remove(enemy);
        }

        ConditionalDebug.Log("All enemy locks cleared");
    }

    public void ClearSpawnedEnemies()
    {
        spawnedEnemies.Clear();
        lockedEnemies.Clear();
    }

    public void RemoveDestroyedEnemies()
    {
        spawnedEnemies.RemoveAll(enemy => enemy == null);
        var destroyedEnemies = lockedEnemies.Keys.Where(enemy => enemy == null).ToList();
        foreach (var enemy in destroyedEnemies)
        {
            lockedEnemies.Remove(enemy);
        }
    }

    public void KillAllEnemies()
    {
        // Find all enemies and kill them
        var enemies = FindObjectsOfType<EnemyBasics>();
        foreach (var enemy in enemies)
        {
            enemy.TakeDamage(float.MaxValue);
        }
    }

    public void LogProjectileHit(bool isPlayerShot, bool hitEnemy, string additionalInfo = "")
    {
        if (isPlayerShot && hitEnemy)
        {
            playerProjectileHits++;
        }

        string message = isPlayerShot
            ? (hitEnemy ? "Player projectile hit enemy" : "Player projectile missed")
            : (hitEnemy ? "Enemy projectile hit player" : "Enemy projectile missed");

        if (!string.IsNullOrEmpty(additionalInfo))
        {
            message += $" - {additionalInfo}";
        }

        ConditionalDebug.Log($"[ProjectileHit] {message}");
    }

    public void LogProjectileExpired(bool isPlayerShot)
    {
        string message = isPlayerShot ? "Player projectile expired" : "Enemy projectile expired";
        ConditionalDebug.Log($"[ProjectileExpired] {message}");
    }

    private void OnDestroy()
    {
        var cameraSwitching = FindFirstObjectByType<CinemachineCameraSwitching>();
        if (cameraSwitching != null)
        {
            EventManager.Instance.RemoveListener(
                EventManager.TransCamOnEvent,
                cameraSwitching.SwitchToTransitionCamera
            );
            EventManager.Instance.RemoveListener(
                EventManager.StartingTransitionEvent,
                cameraSwitching.SwitchToTransitionCamera
            );
        }

        EventManager.Instance.RemoveListener(
            EventManager.TransCamOnEvent,
            PlayerLocking.Instance.ReleasePlayerLocks
        );

        var shooterMovement = FindFirstObjectByType<ShooterMovement>();
        if (shooterMovement != null)
        {
            EventManager.Instance.RemoveListener("TransCamOff", shooterMovement.ResetToCenter);
        }
    }

    public void ClearAllPlayerLocks()
    {
        if (PlayerLocking.Instance != null)
        {
            PlayerLocking.Instance.ClearLockedTargets();
            ConditionalDebug.Log("All player locks cleared.");
        }
        else
        {
            ConditionalDebug.LogWarning("PlayerLocking instance not found. Unable to clear locks.");
        }
    }

    public void UpdateStaminaUI(float staminaPercentage)
    {
        if (playerUI != null)
        {
            playerUI.UpdateStaminaBar(staminaPercentage);
        }
    }
}
