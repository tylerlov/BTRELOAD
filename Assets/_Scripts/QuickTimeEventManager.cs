using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using FMODUnity;
using FMOD.Studio;

public class QuickTimeEventManager : MonoBehaviour
{
    public static QuickTimeEventManager Instance { get; private set; }

    [SerializeField] private GameObject qtePanel;
    [SerializeField] private TextMeshProUGUI qteText;
    [SerializeField] private float qteDisplayTime = 0.5f; // Increased to 0.5 seconds
    [SerializeField] private EventReference successSoundEvent;
    [SerializeField] private EventReference failureSoundEvent;

    private string[] directions = { "↑", "→", "↓", "←" };
    private char[] currentSequence;
    private int currentIndex = 0;
    private bool isQteActive = false;
    private float qteStartTime;

    public event Action<bool> OnQteComplete;

    public bool IsQTEActive => isQteActive;

    private DefaultControls playerInputActions;

    private Coroutine qteCoroutine;

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
        }
        playerInputActions = new DefaultControls();
    }

    private void OnEnable()
    {
        playerInputActions.Enable();
    }

    private void OnDisable()
    {
        playerInputActions.Disable();
    }

    public void StartQTE(float duration)
    {
        if (isQteActive) return;

        Debug.Log($"QTE Started. Duration: {duration} seconds");
        isQteActive = true;
        qteStartTime = Time.time;
        currentSequence = GenerateSequence().ToCharArray();
        currentIndex = 0;
        qtePanel.SetActive(true);
        qteCoroutine = StartCoroutine(DisplayQTE(duration));
    }

    private string GenerateSequence()
    {
        string sequence = "";
        for (int i = 0; i < 4; i++)
        {
            sequence += directions[UnityEngine.Random.Range(0, directions.Length)];
        }
        return sequence;
    }

    private IEnumerator DisplayQTE(float duration)
    {
        Debug.Log($"QTE Sequence: {new string(currentSequence)}");

        while (Time.time - qteStartTime < duration && currentIndex < currentSequence.Length)
        {
            qteText.text = currentSequence[currentIndex].ToString();

            if (CheckInput())
            {
                currentIndex++;
                if (currentIndex >= currentSequence.Length)
                {
                    Debug.Log("QTE Completed Successfully");
                    EndQTE(true);
                    break;
                }
            }
            yield return null;
        }

        if (isQteActive)
        {
            EndQTE(false);
        }
    }

    private bool CheckInput()
    {
        bool inputCorrect = false;

        if (playerInputActions.Player.Up.WasPressedThisFrame() && currentSequence[currentIndex] == '↑')
        {
            inputCorrect = true;
        }
        else if (playerInputActions.Player.Right.WasPressedThisFrame() && currentSequence[currentIndex] == '→')
        {
            inputCorrect = true;
        }
        else if (playerInputActions.Player.Down.WasPressedThisFrame() && currentSequence[currentIndex] == '↓')
        {
            inputCorrect = true;
        }
        else if (playerInputActions.Player.Left.WasPressedThisFrame() && currentSequence[currentIndex] == '←')
        {
            inputCorrect = true;
        }

        if (inputCorrect)
        {
            RuntimeManager.PlayOneShot(successSoundEvent);
        }
        else if (playerInputActions.Player.Up.WasPressedThisFrame() ||
                 playerInputActions.Player.Right.WasPressedThisFrame() ||
                 playerInputActions.Player.Down.WasPressedThisFrame() ||
                 playerInputActions.Player.Left.WasPressedThisFrame())
        {
            RuntimeManager.PlayOneShot(failureSoundEvent);
            EndQTE(false);
        }

        return inputCorrect;
    }

    public void EndQTE(bool success = false)
    {
        if (qteCoroutine != null)
        {
            StopCoroutine(qteCoroutine);
            qteCoroutine = null;
        }
        if (isQteActive)
        {
            Debug.Log(success ? "QTE Completed Successfully" : "QTE Failed");
            isQteActive = false;
            qtePanel.SetActive(false);
            OnQteComplete?.Invoke(success);
        }
    }
}