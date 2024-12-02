using System;
using System.Collections;
using FMOD.Studio;
using FMODUnity;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class QuickTimeEventManager : MonoBehaviour
{
    public static QuickTimeEventManager Instance { get; private set; }

    [SerializeField]
    private GameObject qtePanel;

    [SerializeField]
    private TextMeshProUGUI qteText;

    [SerializeField]
    private float qteDisplayTime = 0.5f; // Increased to 0.5 seconds

    [SerializeField]
    private EventReference successSoundEvent;

    [SerializeField]
    private EventReference failureSoundEvent;

    // Add these new variables
    [SerializeField]
    private int minSequenceLength = 3;
    [SerializeField]
    private int maxSequenceLength = 6;
    [SerializeField]
    private Image progressBar;
    [SerializeField]
    private Color correctInputColor = Color.green;
    [SerializeField]
    private Color incorrectInputColor = Color.red;
    [SerializeField]
    private float colorChangeTime = 0.1f;

    private string[] directions = { "↑", "→", "↓", "←" };
    private char[] currentSequence;
    private int currentIndex = 0;
    private bool isQteActive = false;
    private float qteStartTime;

    public event Action<bool> OnQteComplete;

    public bool IsQTEActive => isQteActive;

    private DefaultControls playerInputActions;

    private Coroutine qteCoroutine;

    private const float INPUT_BUFFER_TIME = 0.1f;
    private float lastInputTime;

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
        StartQTE(duration, minSequenceLength);
    }

    public void StartQTE(float duration, int difficulty)
    {
        if (isQteActive)
            return;

        ConditionalDebug.Log($"QTE Started. Duration: {duration} seconds, Difficulty: {difficulty}");
        isQteActive = true;
        qteStartTime = Time.time;
        currentSequence = GenerateSequence(difficulty).ToCharArray();
        currentIndex = 0;
        qtePanel.SetActive(true);
        UpdateProgressBar();
        qteCoroutine = StartCoroutine(DisplayQTE(duration));
    }

    private string GenerateSequence(int difficulty)
    {
        int length = Mathf.Clamp(difficulty, minSequenceLength, maxSequenceLength);
        string sequence = "";
        for (int i = 0; i < length; i++)
        {
            sequence += directions[UnityEngine.Random.Range(0, directions.Length)];
        }
        return sequence;
    }

    private IEnumerator DisplayQTE(float duration)
    {
        ConditionalDebug.Log($"QTE Sequence: {new string(currentSequence)}");

        while (Time.time - qteStartTime < duration && currentIndex < currentSequence.Length)
        {
            qteText.text = currentSequence[currentIndex].ToString();

            if (CheckInput())
            {
                currentIndex++;
                UpdateProgressBar();
                if (currentIndex >= currentSequence.Length)
                {
                    ConditionalDebug.Log("QTE Completed Successfully");
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
        float currentTime = Time.time;

        if (currentTime - lastInputTime < INPUT_BUFFER_TIME)
        {
            return false;
        }

        bool wrongInputDetected = false;

        if (
            playerInputActions.Player.Up.WasPressedThisFrame()
            && currentSequence[currentIndex] == '↑'
        )
        {
            inputCorrect = true;
        }
        else if (
            playerInputActions.Player.Right.WasPressedThisFrame()
            && currentSequence[currentIndex] == '→'
        )
        {
            inputCorrect = true;
        }
        else if (
            playerInputActions.Player.Down.WasPressedThisFrame()
            && currentSequence[currentIndex] == '↓'
        )
        {
            inputCorrect = true;
        }
        else if (
            playerInputActions.Player.Left.WasPressedThisFrame()
            && currentSequence[currentIndex] == '←'
        )
        {
            inputCorrect = true;
        }

        if (inputCorrect || wrongInputDetected)
        {
            lastInputTime = currentTime;
        }

        if (inputCorrect)
        {
            RuntimeManager.PlayOneShot(successSoundEvent);
            StartCoroutine(ChangeTextColor(correctInputColor));
        }
        else if (
            playerInputActions.Player.Up.WasPressedThisFrame()
            || playerInputActions.Player.Right.WasPressedThisFrame()
            || playerInputActions.Player.Down.WasPressedThisFrame()
            || playerInputActions.Player.Left.WasPressedThisFrame()
        )
        {
            RuntimeManager.PlayOneShot(failureSoundEvent);
            StartCoroutine(ChangeTextColor(incorrectInputColor));
            EndQTE(false);
        }

        return inputCorrect;
    }

    private IEnumerator ChangeTextColor(Color targetColor)
    {
        Color originalColor = qteText.color;
        qteText.color = targetColor;
        yield return new WaitForSeconds(colorChangeTime);
        qteText.color = originalColor;
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
            ConditionalDebug.Log(success ? "QTE Completed Successfully" : "QTE Failed");
            isQteActive = false;
            qtePanel.SetActive(false);
            OnQteComplete?.Invoke(success);
        }
    }

    public void CancelQTE()
    {
        if (isQteActive)
        {
            ConditionalDebug.Log("QTE Cancelled");
            EndQTE(false);
        }
    }

    private void UpdateProgressBar()
    {
        if (progressBar != null)
        {
            progressBar.fillAmount = (float)currentIndex / currentSequence.Length;
        }
    }
}
