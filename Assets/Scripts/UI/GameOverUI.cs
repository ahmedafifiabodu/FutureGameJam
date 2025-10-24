using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Manages the Game Over UI screen
/// Shows player stats and provides options to restart or quit
/// </summary>
public class GameOverUI : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject gameOverPanel;

    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Text Elements")]
    [SerializeField] private TextMeshProUGUI gameOverTitle;

    [SerializeField] private TextMeshProUGUI hostsConsumedText;
    [SerializeField] private TextMeshProUGUI survivalTimeText;
    [SerializeField] private TextMeshProUGUI finalScoreText;

    [Header("Buttons")]
    [SerializeField] private Button restartButton;

    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button quitButton;

    [Header("Animation Settings")]
    [SerializeField] private bool useAnimation = true;

    [SerializeField] private float fadeInDuration = 1f;
    [SerializeField] private float delayBeforeShow = 0.5f;

    [Header("Audio")]
    [SerializeField] private AudioClip gameOverSound;

    [SerializeField] private AudioSource audioSource;

    [Header("Effects")]
    [SerializeField] private bool enableScreenEffect = true;

    [SerializeField] private Image screenOverlay;
    [SerializeField] private Color overlayColor = new(0, 0, 0, 0.7f);

    private bool isShowing = false;
    private float fadeTimer = 0f;

    private GameStateMachine.GameStateMachineManager _gameStateMachineManager;
    private CursorManager _cursorManager;

    private void Awake()
    {
        // Ensure panel is hidden at start
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        // Setup canvas group for fade effects
        if (canvasGroup == null)
        {
            canvasGroup = gameOverPanel?.GetComponent<CanvasGroup>();
            if (canvasGroup == null && gameOverPanel != null)
            {
                canvasGroup = gameOverPanel.AddComponent<CanvasGroup>();
            }
        }

        // Setup audio source
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
        }

        // Setup button listeners
        SetupButtons();
    }

    private void Start()
    {
        _cursorManager = ServiceLocator.Instance.GetService<CursorManager>();
        ServiceLocator.Instance.TryGetService(out _gameStateMachineManager);
    }

    private void SetupButtons()
    {
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(OnRestartClicked);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(OnQuitClicked);
        }
    }

    private void Update()
    {
        // Handle fade-in animation
        if (isShowing && useAnimation && canvasGroup != null)
        {
            fadeTimer += Time.unscaledDeltaTime;
            float alpha = Mathf.Clamp01(fadeTimer / fadeInDuration);
            canvasGroup.alpha = alpha;

            // Enable interactivity when fully visible
            if (alpha >= 1f)
            {
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
                isShowing = false; // Animation complete
            }
        }
    }

    /// <summary>
    /// Show the game over screen with player stats
    /// </summary>
    public void ShowGameOver(int hostsConsumed, float survivalTime)
    {
        if (gameOverPanel == null)
        {
            Debug.LogError("[GameOverUI] Game Over Panel is not assigned!");
            return;
        }

        // Prevent multiple calls from overwriting stats
        if (gameOverPanel.activeSelf)
        {
            Debug.LogWarning($"[GameOverUI] Game Over screen already showing. Ignoring duplicate call.");
            return;
        }

        // Calculate final score (simple formula - can be adjusted)
        int finalScore = CalculateScore(hostsConsumed, survivalTime);

        Debug.Log("[GameOverUI] Preparing to show game over screen...");

        // Start showing after delay using realtime so it works when timeScale == 0
        StartCoroutine(ShowGameOverRealtime(delayBeforeShow));

        // Store values to show after delay
        PlayerPrefs.SetInt("TempHostsConsumed", hostsConsumed);
        PlayerPrefs.SetFloat("TempSurvivalTime", survivalTime);
        PlayerPrefs.SetInt("TempFinalScore", finalScore);
    }

    private IEnumerator ShowGameOverRealtime(float delay)
    {
        // Use realtime wait so the show is not blocked by Time.timeScale == 0
        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);

        ShowGameOverDelayed();
    }

    private void ShowGameOverDelayed()
    {
        int hostsConsumed = PlayerPrefs.GetInt("TempHostsConsumed", 0);
        float survivalTime = PlayerPrefs.GetFloat("TempSurvivalTime", 0f);
        int finalScore = PlayerPrefs.GetInt("TempFinalScore", 0);

        // Clean up temp values
        PlayerPrefs.DeleteKey("TempHostsConsumed");
        PlayerPrefs.DeleteKey("TempSurvivalTime");
        PlayerPrefs.DeleteKey("TempFinalScore");

        Debug.Log("[GameOverUI] Showing game over screen now...");

        // Track if panel was already active to avoid double-notification
        bool wasAlreadyActive = gameOverPanel != null && gameOverPanel.activeSelf;

        // Show panel
        gameOverPanel.SetActive(true);

        // Notify CursorManager that UI panel opened (only if it wasn't already active)
        if (!wasAlreadyActive)
        {
            _cursorManager.OnUIPanelOpened();
            Debug.Log("[GameOverUI] Notified CursorManager - UI panel opened");
        }

        // Setup canvas group for animation
        if (canvasGroup != null && useAnimation)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            isShowing = true;
            fadeTimer = 0f;
        }
        else if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        // Update text elements
        UpdateTexts(hostsConsumed, survivalTime, finalScore);

        // Apply screen overlay effect
        if (enableScreenEffect && screenOverlay != null)
        {
            screenOverlay.color = overlayColor;
            screenOverlay.gameObject.SetActive(true);
        }

        // Play sound
        if (gameOverSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(gameOverSound);
        }

        Debug.Log($"[GameOverUI] Showing game over screen - Hosts: {hostsConsumed}, Time: {survivalTime:F1}s, Score: {finalScore}");
    }

    private void UpdateTexts(int hostsConsumed, float survivalTime, int finalScore)
    {
        // Update title
        if (gameOverTitle != null)
        {
            gameOverTitle.text = "GAME OVER";
        }

        // Update hosts consumed
        if (hostsConsumedText != null)
        {
            hostsConsumedText.text = $"Hosts Consumed: {hostsConsumed}";
        }

        // Update survival time
        if (survivalTimeText != null)
        {
            int minutes = Mathf.FloorToInt(survivalTime / 60f);
            int seconds = Mathf.FloorToInt(survivalTime % 60f);
            survivalTimeText.text = $"Survival Time: {minutes:00}:{seconds:00}";
        }

        // Update final score
        if (finalScoreText != null)
        {
            finalScoreText.text = $"Final Score: {finalScore}";
        }
    }

    /// <summary>
    /// Calculate final score based on stats
    /// </summary>
    private int CalculateScore(int hostsConsumed, float survivalTime)
    {
        // Simple scoring formula:
        // - 100 points per host consumed
        // - 10 points per second survived
        int hostPoints = hostsConsumed * 100;
        int timePoints = Mathf.FloorToInt(survivalTime) * 10;
        return hostPoints + timePoints;
    }

    /// <summary>
    /// Hide the game over screen
    /// </summary>
    public void HideGameOver()
    {
        // Track if panel was active before hiding
        bool wasActive = gameOverPanel != null && gameOverPanel.activeSelf;

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (screenOverlay != null)
            screenOverlay.gameObject.SetActive(false);

        // Notify CursorManager that UI panel closed (only if it was actually active)
        if (wasActive)
        {
            _cursorManager.OnUIPanelClosed();
            Debug.Log("[GameOverUI] Notified CursorManager - UI panel closed");
        }
    }

    #region Button Handlers

    private void OnRestartClicked()
    {
        Debug.Log("[GameOverUI] Restart button clicked");

        HideGameOver();
        StartCoroutine(ReloadSceneCoroutine());
    }

    private IEnumerator ReloadSceneCoroutine()
    {
        // Wait one frame to ensure all cleanup is complete
        yield return null;
        yield return null;
        yield return null;

        // Use state machine if available, otherwise fallback to direct restart
        if (_gameStateMachineManager != null)
            _gameStateMachineManager.RestartGame();
    }

    private void OnMainMenuClicked()
    {
        Debug.Log("[GameOverUI] Main Menu button clicked");

        HideGameOver();
        StartCoroutine(LoadMainMenuCoroutine());
    }

    private IEnumerator LoadMainMenuCoroutine()
    {
        // Wait one frame to ensure cleanup
        yield return null;
        yield return null;
        yield return null;

        // Load main menu scene
        SceneManager.LoadScene(0);
    }

    private void OnQuitClicked()
    {
        Debug.Log("[GameOverUI] Quit button clicked");

        // Quit application
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    #endregion Button Handlers

    #region Public API

    /// <summary>
    /// Check if game over screen is currently showing
    /// </summary>
    public bool IsShowing() => gameOverPanel != null && gameOverPanel.activeSelf;

    /// <summary>
    /// Set whether to use animation when showing
    /// </summary>
    public void SetUseAnimation(bool useAnim) => useAnimation = useAnim;

    /// <summary>
    /// Set fade in duration
    /// </summary>
    public void SetFadeInDuration(float duration) => fadeInDuration = Mathf.Max(0.1f, duration);

    #endregion Public API

    #region Inspector Test Functions

    [ContextMenu("Test Game Over (3 Hosts, 120s)")]
    private void TestGameOver()
    {
        ShowGameOver(3, 120f);
    }

    [ContextMenu("Test Game Over (0 Hosts, 30s)")]
    private void TestGameOverBadRun()
    {
        ShowGameOver(0, 30f);
    }

    [ContextMenu("Test Game Over (10 Hosts, 300s)")]
    private void TestGameOverGoodRun()
    {
        ShowGameOver(10, 300f);
    }

    [ContextMenu("Hide Game Over")]
    private void TestHideGameOver()
    {
        HideGameOver();
    }

    #endregion Inspector Test Functions
}