using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BehaviorDesigner.Runtime.Tactical;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DebugControls : MonoBehaviour, IPointerClickHandler
{
    private GameObject uiElement;
    private InputAction debugNextSceneAction;
    private bool isSceneTransitioning = false; // Flag to prevent multiple scene transitions
    private InputAction debugKillPlayerAction;
    private InputAction debugKillAllEnemiesAction;
    private InputAction debugToggleInvincibilityAction;
    private bool isPlayerInvincible = false;

    private void Awake()
    {
        // Initialize the InputAction for the 'N' key
        debugNextSceneAction = new InputAction(
            type: InputActionType.Button,
            binding: "<Keyboard>/n"
        );
        debugNextSceneAction.performed += ctx => OnDebugNextScene();
        debugNextSceneAction.Enable();

        // Initialize the InputAction for the 'K' key to kill the player
        debugKillPlayerAction = new InputAction(
            name: "DebugKillPlayer",
            type: InputActionType.Button,
            binding: "<Keyboard>/k"
        );
        debugKillPlayerAction.performed += ctx =>
        {
            ConditionalDebug.Log("Debug kill player action performed");
            OnDebugKillPlayer();
        };
        debugKillPlayerAction.Enable();

        // Initialize the InputAction for the '0' key to kill all enemies
        debugKillAllEnemiesAction = new InputAction(
            name: "DebugKillAllEnemies",
            type: InputActionType.Button,
            binding: "<Keyboard>/0"
        );
        debugKillAllEnemiesAction.performed += ctx =>
        {
            ConditionalDebug.Log("Debug kill all enemies action performed");
            KillAllEnemies();
        };
        debugKillAllEnemiesAction.Enable();

        // Initialize the InputAction for the 'I' key to toggle invincibility
        debugToggleInvincibilityAction = new InputAction(
            name: "DebugToggleInvincibility",
            type: InputActionType.Button,
            binding: "<Keyboard>/i"
        );
        debugToggleInvincibilityAction.performed += ctx =>
        {
            ConditionalDebug.Log("Debug toggle invincibility action performed");
            OnDebugToggleInvincibility();
        };
        debugToggleInvincibilityAction.Enable();

        ConditionalDebug.Log("DebugControls Awake completed, actions set up");
    }

    private void Start()
    {
        InitializeComponents();
    }

    private void OnDestroy()
    {
        // Disable the InputActions when the object is destroyed
        debugNextSceneAction.Disable();
        debugKillPlayerAction.Disable();
        debugKillAllEnemiesAction.Disable();
        debugToggleInvincibilityAction.Disable();
        SceneManager.sceneLoaded -= OnSceneLoaded; // Unsubscribe from the event
    }

    private void OnDebugNextScene()
    {
        if (isSceneTransitioning)
            return; // Prevent multiple scene transitions

        isSceneTransitioning = true; // Set the flag to true to indicate a scene transition is in progress

        GameManager.Instance.DebugMoveToNextScene();

        GameManager.Instance.HandleDebugSceneTransition();

        // Subscribe to the sceneLoaded event to reset the flag after the scene is loaded
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Re-initialize components after scene transition
        InitializeComponents();
        isSceneTransitioning = false; // Reset the flag
        SceneManager.sceneLoaded -= OnSceneLoaded; // Unsubscribe from the event
    }

    private void OnDebugKillPlayer()
    {
        if (GameManager.Instance == null)
        {
            ConditionalDebug.LogError("GameManager instance is null. Cannot initiate player death.");
            return;
        }

        PlayerHealth playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth == null)
        {
            ConditionalDebug.LogError("PlayerHealth component not found in the scene.");
            return;
        }

        playerHealth.Damage(9999999);
    }

    private void OnDebugKillAllEnemies()
    {
        // Find all game objects that implement the IDamageable interface
        var damageables = UnityEngine
            .Object.FindObjectsOfType<MonoBehaviour>()
            .OfType<IDamageable>();

        int killedEnemies = 0;

        // Apply damage to each damageable object
        foreach (var damageable in damageables)
        {
            if (damageable.IsAlive())
            {
                damageable.Damage(100); // Assuming you want to apply a damage of 100
                killedEnemies++;
            }
        }

        // Handle projectiles
        var projectiles = FindObjectsOfType<ProjectileStateBased>();
        foreach (var projectile in projectiles)
        {
            projectile.Death();
        }

        // Clear all projectiles from the ProjectileManager
        ProjectileManager.Instance.ClearAllProjectiles();

        // Add 1000 to the score if any enemies were killed
        if (killedEnemies > 0 && ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(1000);
            ConditionalDebug.Log("Debug: Added 1000 to score for killing all enemies");
        }
    }

    private void OnDebugToggleInvincibility()
    {
        isPlayerInvincible = !isPlayerInvincible;
        PlayerHealth playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.SetInvincibleInternal(isPlayerInvincible);
            ConditionalDebug.Log($"Debug: Player invincibility set to {isPlayerInvincible}");
        }
        else
        {
            ConditionalDebug.LogError("PlayerHealth component not found");
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        KillAllEnemies();
        // Projectile is layer 3
        CallDeathOnLayerObjects(3);
    }

    public void KillAllEnemies()
    {
        // Find all game objects that implement the IDamageable interface
        var damageables = UnityEngine
            .Object.FindObjectsOfType<MonoBehaviour>()
            .OfType<IDamageable>();

        int killedEnemies = 0;

        // Apply damage to each damageable object
        foreach (var damageable in damageables)
        {
            if (damageable.IsAlive())
            {
                damageable.Damage(100); // Assuming you want to apply a damage of 100
                killedEnemies++;
            }
        }

        // Handle projectiles
        var projectiles = FindObjectsOfType<ProjectileStateBased>();
        foreach (var projectile in projectiles)
        {
            projectile.Death();
        }

        // Clear all projectiles from the ProjectileManager
        ProjectileManager.Instance.ClearAllProjectiles();

        // Add 1000 to the score if any enemies were killed
        if (killedEnemies > 0 && ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(1000);
            ConditionalDebug.Log("Debug: Added 1000 to score for killing all enemies");
        }
    }

    public void CallDeathOnLayerObjects(int layer)
    {
        var gameObjects = FindObjectsOfType(typeof(GameObject)) as GameObject[];

        foreach (var obj in gameObjects)
        {
            if (obj.layer == layer)
            {
                var projectileState = obj.GetComponent<ProjectileStateBased>();
                if (projectileState != null)
                {
                    projectileState.Death();
                }
            }
        }
    }

    private void FindUIElement()
    {
        // Find the UI element named "Score" on the "UI" layer
        uiElement = GameObject
            .FindObjectsOfType<GameObject>()
            .FirstOrDefault(go => go.name == "Score" && go.layer == LayerMask.NameToLayer("UI"));
        if (uiElement == null)
        {
            ConditionalDebug.LogError("UI element named 'Score' not found on the 'UI' layer.");
        }
    }

    private void AddEventTriggerToUIElement()
    {
        if (uiElement != null)
        {
            EventTrigger eventTrigger = uiElement.AddComponent<EventTrigger>();
            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.PointerClick;
            entry.callback.AddListener(
                (data) =>
                {
                    OnPointerClick((PointerEventData)data);
                }
            );
            eventTrigger.triggers.Add(entry);
        }
        else
        {
            ConditionalDebug.LogError("UI element named 'Score' not found.");
        }
    }

    private void OnEnable()
    {
        // Re-find the UI element when the scene is loaded
        InitializeComponents();
    }

    private void InitializeComponents()
    {
        // Find the UI element named "Score"
        FindUIElement();

        // Add Event Trigger to the UI element
        AddEventTriggerToUIElement();
    }
}
