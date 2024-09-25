using UnityEngine;
using System;

public class StaminaController : MonoBehaviour
{
    public static StaminaController Instance { get; private set; }

    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float staminaRegenerationTime = 5f; // Time to regenerate from 0 to full
    [SerializeField] private int maxRicochetsAtFullStamina = 5; // Number of ricochets at full stamina

    private float currentStamina;
    private float staminaRegenerationRate;
    private float staminaCostPerRicochet;

    public event Action<float> OnStaminaChanged;

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

        currentStamina = maxStamina;
        staminaRegenerationRate = maxStamina / staminaRegenerationTime;
        staminaCostPerRicochet = maxStamina / maxRicochetsAtFullStamina;
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
}