using UnityEngine;
using Chronos;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [SerializeField] private float scoreDecayRate = 1f; // Points lost per second

    [Header("Wave Management")]
    [SerializeField] private int _totalWaveCount = 0;
    [SerializeField] private int _currentSceneWaveCount = 0;

    private Timeline timeline;
    private float lastScoreUpdateTime;
    private float accumulatedScoreDecrease = 0f;
    private Score scoreUI;

    public int Score { get; private set; }
    public int ShotTally { get; private set; }



    private const int SECTION_TRANSITION_SCORE_BOOST = 200;


    public int TotalWaveCount
    {
        get => _totalWaveCount;
        private set
        {
            _totalWaveCount = value;
            OnValueChanged();
        }
    }

    public int CurrentSceneWaveCount
    {
        get => _currentSceneWaveCount;
        set
        {
            _currentSceneWaveCount = value;
            OnValueChanged();
        }
    }

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

        InitializeTimeline();
    }

    private void Start()
    {
        scoreUI = FindObjectOfType<Score>();
    }

    private void Update()
    {
        if (timeline != null)
        {
            UpdateScoreOverTime();
        }
        else
        {
            Debug.LogWarning("Timeline is null in ScoreManager Update");
        }
    }

    private void InitializeTimeline()
    {
        timeline = GetComponent<Timeline>();
        if (timeline == null)
        {
            Debug.LogError("Timeline component not found on the ScoreManager object.");
        }
        lastScoreUpdateTime = timeline.time;
    }

    public void AddShotTally(int shotsToAdd)
    {
        ShotTally += shotsToAdd;
    }

    public void SetScore(int newScore)
    {
        Score = newScore;
    }

    public void AddScore(int amount)
    {
        Score += amount;
    }

    public void ResetScore()
    {
        Score = 0;
    }

    public int RetrieveScore()
    {
        return Score;
    }

    public void waveCounterAdd()
    {
        CurrentSceneWaveCount++;
        TotalWaveCount++;
        Debug.Log($"Wave added. Current scene wave: {CurrentSceneWaveCount}, Total waves: {TotalWaveCount}");
    }

    private void UpdateScoreOverTime()
    {
        float currentTime = timeline.time;
        float deltaTime = currentTime - lastScoreUpdateTime;

        if (deltaTime > 0)
        {
            accumulatedScoreDecrease += scoreDecayRate * deltaTime;
            int scoreDecrease = Mathf.FloorToInt(accumulatedScoreDecrease);

            if (scoreDecrease > 0)
            {
                Score = Mathf.Max(0, Score - scoreDecrease);
                accumulatedScoreDecrease -= scoreDecrease;
            }

            lastScoreUpdateTime = currentTime;
        }
    }

    public void ReportDamage(int damage)
    {
        if (scoreUI != null)
        {
            scoreUI.ReportDamage(damage);
        }
    }

    private void OnValueChanged()
    {
        // This method is intentionally left empty or you can add logic here if needed
    }

     public void AddSectionTransitionBoost()
    {
        AddScore(SECTION_TRANSITION_SCORE_BOOST);
        if (scoreUI != null)
        {
            scoreUI.ShowSectionTransitionBoost(SECTION_TRANSITION_SCORE_BOOST);
        }
    }
}