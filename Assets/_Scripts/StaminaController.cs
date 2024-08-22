using System.Collections;
using System.Collections.Generic;
using SonicBloom.Koreo;
using UnityEngine;
using UnityEngine.SceneManagement; // Import the Scene Management namespace
using UnityEngine.UI;

public class StaminaController : MonoBehaviour
{
    [Header("Stamina Main Parameters")]
    [EventID]
    public string eventIDLocking;

    public float playerStamina = 100.0f;

    [SerializeField]
    private float maxStamina = 100.0f;

    [SerializeField]
    private float rewindCost = 25;
    public bool hasRegenerated = true;
    public bool canRewind = true;

    [Header("Stamina Regen Parameters")]
    [Range(0, 50)]
    [SerializeField]
    private float staminaDrain = 0.5f;

    [Range(0, 50)]
    [SerializeField]
    private float staminaRegen = 0.5f;

    [Header("Stamina UI Elements")]
    [SerializeField]
    private Image staminaProcessUI = null;

    [SerializeField]
    private CanvasGroup sliderCanvasGroup = null;

    public bool locking;

    void Awake()
    {
        // Subscribe to the sceneLoaded event
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        // Unsubscribe from the sceneLoaded event to avoid memory leaks
        SceneManager.sceneLoaded -= OnSceneLoaded;
        // Ensure to unregister from Koreographer events to clean up
        if (Koreographer.Instance != null)
        {
            Koreographer.Instance.UnregisterForEvents(eventIDLocking, OnMusicalLocking);
        }
    }

    // This method will be called every time a new scene is loaded
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Register for Koreography events with the new scene's Koreographer instance
        RegisterKoreographyEvents();
    }

    private void RegisterKoreographyEvents()
    {
        // Ensure the Koreographer instance is ready and then register for events
        if (Koreographer.Instance != null)
        {
            Koreographer.Instance.RegisterForEvents(eventIDLocking, OnMusicalLocking);
        }
    }

    private void Update()
    {
        if (playerStamina <= maxStamina - 0.10)
        {
            playerStamina += staminaRegen * Time.deltaTime;
            UpdateStamina(1);

            if (playerStamina >= maxStamina)
            {
                sliderCanvasGroup.alpha = 0;
                hasRegenerated = true;
            }
        }

        if (playerStamina < 5)
        {
            canRewind = false;
        }
        else
        {
            canRewind = true;
        }
    }

    public void StaminaRewind()
    {
        if (playerStamina >= (maxStamina * rewindCost / maxStamina))
        {
            playerStamina -= rewindCost;
            UpdateStamina(1);
        }
    }

    void OnMusicalLocking(KoreographyEvent evt)
    {
        if (locking && Time.timeScale != 0f)
        {
            if (playerStamina >= (maxStamina * rewindCost / maxStamina))
            {
                playerStamina -= rewindCost;
                UpdateStamina(1);
            }
        }
    }

    void UpdateStamina(int value)
    {
        staminaProcessUI.fillAmount = playerStamina / maxStamina;

        if (value == 0)
        {
            sliderCanvasGroup.alpha = 0;
        }
        else
        {
            sliderCanvasGroup.alpha = 1;
        }
    }

    public void Reset()
    {
        locking = false;
        canRewind = true;
    }
}
