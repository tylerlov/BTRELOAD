using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Linq;
using BehaviorDesigner.Runtime.Tactical;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class DebugControls : MonoBehaviour, IPointerClickHandler
{
    private GameManager gameManager;
    private GameObject uiElement;
    private InputAction debugNextSceneAction;
    private bool isSceneTransitioning = false; // Flag to prevent multiple scene transitions

    private void Awake()
    {
        // Initialize the InputAction for the 'N' key
        debugNextSceneAction = new InputAction(type: InputActionType.Button, binding: "<Keyboard>/n");
        debugNextSceneAction.performed += ctx => OnDebugNextScene();
        debugNextSceneAction.Enable();
    }

    private void Start()
    {
        InitializeComponents();
    }

    private void OnDestroy()
    {
        // Disable the InputActions when the object is destroyed
        debugNextSceneAction.Disable();
        SceneManager.sceneLoaded -= OnSceneLoaded; // Unsubscribe from the event
    }

    private void OnDebugNextScene()
    {
        if (isSceneTransitioning) return; // Prevent multiple scene transitions

        isSceneTransitioning = true; // Set the flag to true to indicate a scene transition is in progress

        if (gameManager != null)
        {
            gameManager.DebugMoveToNextScene();
        }

        if (gameManager != null)
        {
            gameManager.HandleDebugSceneTransition();

        }

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

    public void OnPointerClick(PointerEventData eventData)
    {
        KillAllEnemies();
        // Projectile is layer 3
        CallDeathOnLayerObjects(3);
    }

    public void KillAllEnemies()
    {
        // Find all game objects that implement the IDamageable interface
        var damageables = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>().OfType<IDamageable>();

        // Apply damage to each damageable object
        foreach (var damageable in damageables)
        {
            if (damageable.IsAlive())
            {
                damageable.Damage(100); // Assuming you want to apply a damage of 100
            }
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
        uiElement = GameObject.FindObjectsOfType<GameObject>().FirstOrDefault(go => go.name == "Score" && go.layer == LayerMask.NameToLayer("UI"));
        if (uiElement == null)
        {
            Debug.LogError("UI element named 'Score' not found on the 'UI' layer.");
        }
    }

    private void AddEventTriggerToUIElement()
    {
        if (uiElement != null)
        {
            EventTrigger eventTrigger = uiElement.AddComponent<EventTrigger>();
            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.PointerClick;
            entry.callback.AddListener((data) => { OnPointerClick((PointerEventData)data); });
            eventTrigger.triggers.Add(entry);
        }
        else
        {
            Debug.LogError("UI element named 'Score' not found.");
        }
    }

    private void OnEnable()
    {
        // Re-find the UI element when the scene is loaded
        InitializeComponents();
    }

    private void InitializeComponents()
    {
        // Get the GameManager component on the same GameObject
        gameManager = GetComponent<GameManager>();
        if (gameManager == null)
        {
            Debug.LogError("GameManager component not found on the same GameObject.");
        }

        gameManager = GameManager.instance;
        if (gameManager == null)
        {
            Debug.LogError("Gmae Manager component not found in the scene.");
        }

        // Find the UI element named "Score"
        FindUIElement();

        // Add Event Trigger to the UI element
        AddEventTriggerToUIElement();
    }
}
