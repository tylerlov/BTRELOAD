using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;

public class Score : MonoBehaviour
{
    public GameObject playerShooting;
    public GameObject player;
    public TMP_Text scoreText;
    public Text lockText;
    public Text enemyLockText;
    public Text healthText;

    public GameObject projectiles;
    public GameObject enemies;

    private List<Transform> projLockUI = new List<Transform>();
    private List<Transform> enemyLockUI = new List<Transform>();

    private int _currLocks;
    private int _currEnemyLocks;

    public bool enableProjectileLocks = true; // Boolean to control projectile locks
    public bool enableEnemyLocks = true; // Boolean to control enemy locks

    private int currLocks
    {
        get => _currLocks;
        set
        {
            if (!enableProjectileLocks || projectiles == null) return;
            if (_currLocks == value) return;
            if (value > _currLocks && _currLocks < projLockUI.Count)
            {
                projLockUI[_currLocks].gameObject.SetActive(true);
            }
            else if (value < _currLocks && _currLocks > 0)
            {
                projLockUI[_currLocks - 1].gameObject.SetActive(false);
            }
            _currLocks = value;
        }
    }

    private int currEnemyLocks
    {
        get => _currEnemyLocks;
        set
        {
            if (!enableEnemyLocks || enemies == null) return;
            if (_currEnemyLocks == value) return;
            if (value > _currEnemyLocks && _currEnemyLocks < enemyLockUI.Count)
            {
                enemyLockUI[_currEnemyLocks].gameObject.SetActive(true);
            }
            else if (value < _currEnemyLocks && _currEnemyLocks > 0)
            {
                enemyLockUI[_currEnemyLocks - 1].gameObject.SetActive(false);
            }
            _currEnemyLocks = value;
        }
    }

    private PlayerHealth playerHealth;
    private Crosshair playerCrosshair;
    private StringBuilder stringBuilder = new StringBuilder();

    public TMP_Text scoreAddedText;    // Reference to the score added UI element
    public TMP_Text scoreSubtractedText; // Reference to the score subtracted UI element
    private int previousScore = 0;
    private int timerDecrementAccumulator = 0;

    private void Start()
    {
        // Find GameObjects by tag
        playerShooting = GameObject.FindWithTag("Shooting");
        player = GameObject.FindWithTag("Player");

        currLocks = 0;
        currEnemyLocks = 0;

        // Ensure components are found after new assignment
        playerHealth = player.GetComponent<PlayerHealth>();
        playerCrosshair = playerShooting.GetComponent<Crosshair>();

        if (projectiles != null)
        {
            foreach (Transform child in projectiles.transform)
            {
                child.gameObject.SetActive(false);
                projLockUI.Add(child);
            }
        }

        if (enemies != null)
        {
            foreach (Transform child in enemies.transform)
            {
                child.gameObject.SetActive(false);
                enemyLockUI.Add(child);
            }
        }

        previousScore = GameManager.instance.Score;
        
        // Ensure both text elements are initially inactive
        scoreAddedText.gameObject.SetActive(false);
        scoreSubtractedText.gameObject.SetActive(false);
    }

    void Update()
    {
        float health = playerHealth.getCurrentHealth();
        int score = GameManager.instance.Score;
        int playerLocks = Mathf.FloorToInt(playerCrosshair.returnLocks());
        int enemyLocks = Mathf.FloorToInt(playerCrosshair.returnEnemyLocks());

        stringBuilder.Clear();
        stringBuilder.Append(score);
        scoreText.text = stringBuilder.ToString();

        stringBuilder.Clear();
        stringBuilder.Append(playerLocks);
        lockText.text = stringBuilder.ToString();

        stringBuilder.Clear();
        stringBuilder.Append(enemyLocks);
        enemyLockText.text = stringBuilder.ToString();

        if (projectiles != null)
        {
            currLocks = Mathf.Min(playerLocks, projLockUI.Count);
        }

        if (enemies != null)
        {
            currEnemyLocks = Mathf.Min(enemyLocks, enemyLockUI.Count);
        }

        int currentScore = GameManager.instance.Score;
        if (currentScore != previousScore)
        {
            int scoreDifference = currentScore - previousScore;
            
            // Check if it's a timer decrement
            if (scoreDifference == -1)
            {
                timerDecrementAccumulator++;
            }
            else
            {
                // If it's not a timer decrement, show the change and reset the accumulator
                ShowScoreChange(scoreDifference - timerDecrementAccumulator);
                timerDecrementAccumulator = 0;
            }
            
            previousScore = currentScore;
        }
    }

    private void ShowScoreChange(int change)
    {
        if (change == 0) return;

        TMP_Text textToShow = change > 0 ? scoreAddedText : scoreSubtractedText;
        textToShow.text = change > 0 ? $"+{change}" : $"{change}";

        StopAllCoroutines();
        StartCoroutine(FadeScoreChange(textToShow));
    }

    private IEnumerator FadeScoreChange(TMP_Text textElement)
    {
        textElement.gameObject.SetActive(true);
        textElement.color = new Color(textElement.color.r, textElement.color.g, textElement.color.b, 1f);

        float duration = 0.5f;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / duration);
            textElement.color = new Color(textElement.color.r, textElement.color.g, textElement.color.b, alpha);
            yield return null;
        }

        textElement.gameObject.SetActive(false);
    }
}