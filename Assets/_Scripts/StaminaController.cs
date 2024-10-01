using UnityEngine;
using System;

public class StaminaController : MonoBehaviour
{
    public static StaminaController Instance { get; private set; }

    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float staminaRegenerationTime = 5f; // Time to regenerate from 0 to full
    [SerializeField] private int maxRicochetsAtFullStamina = 5; // Number of ricochets at full stamina
    [SerializeField] private float staminaCostPerDodge = 20f; // New field for dodge cost

    private float currentStamina;
    private float staminaRegenerationRate;
    private float staminaCostPerRicochet;

    public event Action<float> OnStaminaChanged;

    private GameManager gameManager;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Don't use DontDestroyOnLoad here if it's on the same object as PlayerMovement
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        currentStamina = maxStamina;
        staminaRegenerationRate = maxStamina / staminaRegenerationTime;
        staminaCostPerRicochet = maxStamina / maxRicochetsAtFullStamina;
        gameManager = GameManager.Instance;
    }

    private void Update()
    {
        RegenerateStamina();
    }

    private void RegenerateStamina()
    {
        if (currentStamina < maxStamina)
        {
            currentStamina = Mathf.Min(currentStamina + staminaRegenerationRate * Time.deltaTime, maxStamina);
            OnStaminaChanged?.Invoke(currentStamina / maxStamina);
            gameManager.UpdateStaminaUI(currentStamina / maxStamina);
        }
    }

    public bool TryUseStamina()
    {
        Debug.Log($"Attempting to use stamina. Current: {currentStamina}, Cost: {staminaCostPerRicochet}");
        if (currentStamina >= staminaCostPerRicochet)
        {
            currentStamina -= staminaCostPerRicochet;
            OnStaminaChanged?.Invoke(currentStamina / maxStamina);
            Debug.Log("Stamina used successfully");
            return true;
        }
        Debug.Log("Not enough stamina");
        return false;
    }

    public float GetCurrentStaminaPercentage()
    {
        return currentStamina / maxStamina;
    }

    public bool canRewind { get; private set; } = true;
    public bool locking { get; set; } = false;

    public void SetCanRewind(bool value)
    {
        canRewind = value;
    }

    public bool TryUseDodgeStamina()
    {
        Debug.Log($"Attempting to use dodge stamina. Current: {currentStamina}, Cost: {staminaCostPerDodge}"); // New debug log
        if (currentStamina >= staminaCostPerDodge)
        {
            currentStamina -= staminaCostPerDodge;
            OnStaminaChanged?.Invoke(currentStamina / maxStamina);
            gameManager.UpdateStaminaUI(currentStamina / maxStamina);
            Debug.Log("Dodge stamina used successfully"); // New debug log
            return true;
        }
        Debug.Log("Not enough stamina for dodge"); // New debug log
        return false;
    }
}