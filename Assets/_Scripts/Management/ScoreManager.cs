using Chronos;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [SerializeField]
    private float scoreDecayRate = 1f; // Points lost per second

    [Header("Wave Management")]
    [SerializeField]
    private int _totalWaveCount = 0;

    [SerializeField]
    private int _currentSceneWaveCount = 0;

    private Timeline timeline;
    private float lastScoreUpdateTime;
    private float accumulatedScoreDecrease = 0f;
    public Score scoreUI; // Change to public

    public int Score { get; private set; }
    public int ShotTally { get; private set; }

    private const int SECTION_TRANSITION_SCORE_BOOST = 200;
    private const int SCORE_CHANGE_THRESHOLD = 2; // Only report changes greater than this

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

    // Add this line near the top of the class
    public delegate void ScoreChangedEventHandler(int change);
    public event ScoreChangedEventHandler OnScoreChanged;

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
            ConditionalDebug.LogWarning("Timeline is null in ScoreManager Update");
        }
    }

    private void InitializeTimeline()
    {
        timeline = GetComponent<Timeline>();
        if (timeline == null)
        {
            ConditionalDebug.LogError("Timeline component not found on the ScoreManager object.");
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
        int previousScore = Score;
        Score += amount;
        // Only invoke OnScoreChanged for significant changes
        if (Mathf.Abs(amount) >= SCORE_CHANGE_THRESHOLD)
        {
            OnScoreChanged?.Invoke(amount);
        }
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
        ConditionalDebug.Log($"Wave added. Current scene wave: {CurrentSceneWaveCount}, Total waves: {TotalWaveCount}");
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
                int previousScore = Score;
                Score = Mathf.Max(0, Score - scoreDecrease);
                accumulatedScoreDecrease -= scoreDecrease;

                // Only invoke OnScoreChanged for significant changes
                int change = Score - previousScore;
                if (Mathf.Abs(change) >= SCORE_CHANGE_THRESHOLD)
                {
                    OnScoreChanged?.Invoke(change);
                }
            }

            lastScoreUpdateTime = currentTime;
        }
    }

    public void ReportDamage(int damage)
    {
        // Only invoke OnScoreChanged for significant changes
        if (damage >= SCORE_CHANGE_THRESHOLD)
        {
            OnScoreChanged?.Invoke(-damage);
        }
    }

    private void OnValueChanged()
    {
        // This method is intentionally left empty or you can add logic here if needed
    }

    public void AddSectionTransitionBoost()
    {
        AddScore(SECTION_TRANSITION_SCORE_BOOST);
    }
}
