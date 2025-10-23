using System;
using UnityEngine;
using TMPro;
using ConfigEnemyType = AI.Enemy.Configuration.EnemyType;

[DefaultExecutionOrder(-100)] // Make sure Instance is ready early
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("UI (TextMeshPro)")]
    [SerializeField] private TMP_Text scoreText;          // Assign your TextMeshProUGUI here
    [SerializeField] private string scorePrefix = "Score: ";
    [SerializeField] private bool useThousandsSeparator = true;

    [Header("Base Points Per Enemy Type")]
    [SerializeField] private int basicScore = 100;
    [SerializeField] private int fastScore = 200;
    [SerializeField] private int toughScore = 500;

    [Header("Score Over Time")]
    [Tooltip("Award points automatically over time.")]
    [SerializeField] private bool awardOverTime = true;
    [Tooltip("Base points per second before multipliers.")]
    [SerializeField] private float pointsPerSecond = 1f;
    [Tooltip("Apply streak and temp multipliers to time-based score too.")]
    [SerializeField] private bool applyMultipliersToOvertime = true;

    [Header("Kill Streak Multiplier (resets when damaged)")]
    [Tooltip("Each kill increases multiplier by this amount. Example: 0.1 -> 1.0, 1.1, 1.2 ...")]
    [SerializeField] private float killStreakMultiplierPerKill = 0.1f;
    [SerializeField] private float maxKillStreakMultiplier = 2.0f;

    [Header("Multi-Kill Temporary Boosts")]
    [Tooltip("Time window between kills to count as a multi-kill (seconds).")]
    [SerializeField] private float multiKillWindow = 2.5f;
    [Tooltip("Temporary multiplier when a Double Kill happens.")]
    [SerializeField] private float doubleKillMultiplier = 1.5f;
    [Tooltip("Temporary multiplier when a Triple (or more) Kill happens.")]
    [SerializeField] private float tripleKillMultiplier = 2.0f;
    [Tooltip("How long the temporary boost lasts (seconds).")]
    [SerializeField] private float multiKillBoostDuration = 5f;
    [Tooltip("If true, taking damage also clears the temporary multi-kill boost.")]
    [SerializeField] private bool resetTempBoostOnDamage = false;

    [Header("Misc")]
    [SerializeField] private bool dontDestroyOnLoad = true;

    // Public read-only state
    public long Score { get; private set; }
    public int KillStreakCount => killStreakCount;
    public int CurrentMultiKillChain => currentMultiKillChain;
    public float CurrentTempBoostMultiplier => currentTempBoostMultiplier;

    // Optional event if other systems need to react
    public event Action<long> OnScoreChanged;

    // Internals
    private int killStreakCount = 0;
    private int currentMultiKillChain = 0;
    private float lastKillTime = -999f;

    private float currentTempBoostMultiplier = 1f;
    private float tempBoostTimer = 0f;

    private float overTimeAccumulator = 0f;

    private void Reset()
    {
        // Convenience: auto-find a TMP_Text on this object or its children if not assigned
        if (!scoreText) scoreText = GetComponentInChildren<TMP_Text>(true);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        if (dontDestroyOnLoad) DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        UpdateScoreUI();              // Push initial UI
        OnScoreChanged?.Invoke(Score);
    }

    private void Update()
    {
        TickTempBoostTimer();
        TickOverTime();
    }

    // ========= Public API =========

    // Call this when an enemy dies, pass its AI.Enemy.Configuration.EnemyType
    public void RegisterKill(ConfigEnemyType type)
    {
        int basePoints = GetBasePoints(type);

        // Kill streak (resets only when player takes damage)
        killStreakCount = Mathf.Max(0, killStreakCount + 1);

        // Multi-kill chain logic
        if (Time.time - lastKillTime <= multiKillWindow)
        {
            currentMultiKillChain++;
        }
        else
        {
            currentMultiKillChain = 1;
        }
        lastKillTime = Time.time;

        // Trigger temporary boost on double/triple
        if (currentMultiKillChain == 2)
        {
            ActivateTempBoost(doubleKillMultiplier, multiKillBoostDuration);
        }
        else if (currentMultiKillChain >= 3)
        {
            ActivateTempBoost(tripleKillMultiplier, multiKillBoostDuration);
        }

        // Award points with multipliers
        float multiplier = GetCurrentMultiplierForKills();
        long pointsToAdd = Mathf.RoundToInt(basePoints * multiplier);
        AddScore(pointsToAdd);
    }

    // Call this when the player is damaged to reset the streak
    public void RegisterPlayerDamaged()
    {
        killStreakCount = 0;
        if (resetTempBoostOnDamage)
        {
            currentTempBoostMultiplier = 1f;
            tempBoostTimer = 0f;
        }
    }

    // Optional: add arbitrary points (e.g., pickups), choose whether multipliers apply
    public void AddPoints(int basePoints, bool affectedByMultipliers = true)
    {
        float mul = affectedByMultipliers ? GetCurrentMultiplierForKills() : 1f;
        long add = Mathf.RoundToInt(basePoints * mul);
        AddScore(add);
    }

    public void ResetAll()
    {
        Score = 0;
        killStreakCount = 0;
        currentMultiKillChain = 0;
        currentTempBoostMultiplier = 1f;
        tempBoostTimer = 0f;
        overTimeAccumulator = 0f;

        UpdateScoreUI();
        OnScoreChanged?.Invoke(Score);
    }

    // Allow assigning TMP at runtime if needed
    public void SetScoreText(TMP_Text text)
    {
        scoreText = text;
        UpdateScoreUI();
    }

    // ========= Internals =========

    private void TickOverTime()
    {
        if (!awardOverTime || pointsPerSecond <= 0f) return;

        float mul = applyMultipliersToOvertime ? GetCurrentMultiplierForKills() : 1f;
        overTimeAccumulator += pointsPerSecond * mul * Time.deltaTime;

        int wholePoints = Mathf.FloorToInt(overTimeAccumulator);
        if (wholePoints > 0)
        {
            AddScore(wholePoints);
            overTimeAccumulator -= wholePoints;
        }
    }

    private void TickTempBoostTimer()
    {
        if (tempBoostTimer > 0f)
        {
            tempBoostTimer -= Time.deltaTime;
            if (tempBoostTimer <= 0f)
            {
                currentTempBoostMultiplier = 1f;
                tempBoostTimer = 0f;
            }
        }
    }

    private int GetBasePoints(ConfigEnemyType type)
    {
        switch (type)
        {
            case ConfigEnemyType.Basic: return basicScore;
            case ConfigEnemyType.Fast:  return fastScore;
            case ConfigEnemyType.Tough: return toughScore;
            default: return 0;
        }
    }

    private float GetCurrentMultiplierForKills()
    {
        // Kill-streak multiplier
        float streakMult = 1f + Mathf.Max(0, killStreakCount - 1) * killStreakMultiplierPerKill;
        streakMult = Mathf.Min(streakMult, maxKillStreakMultiplier);

        // Combine with temporary multi-kill boost
        return streakMult * currentTempBoostMultiplier;
    }

    private void ActivateTempBoost(float multiplier, float duration)
    {
        // Use the higher multiplier if one is already active, and refresh duration
        currentTempBoostMultiplier = Mathf.Max(currentTempBoostMultiplier, multiplier);
        tempBoostTimer = Mathf.Max(tempBoostTimer, duration);
    }

    private void AddScore(long amount)
    {
        if (amount <= 0) return;
        Score += amount;
        UpdateScoreUI();
        OnScoreChanged?.Invoke(Score);
    }

    private void UpdateScoreUI()
    {
        if (!scoreText) return;

        // Build display string
        string number = useThousandsSeparator ? Score.ToString("N0") : Score.ToString();
        scoreText.text = string.IsNullOrEmpty(scorePrefix) ? number : (scorePrefix + number);
    }
}