using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Manages game state transitions between Parasite mode and Host mode
/// Works in conjunction with GameStateMachineManager for lifecycle management
/// </summary>
public class GameStateManager : MonoBehaviour
{
    [Header("Mode Management")]
    [SerializeField] private GameMode currentMode = GameMode.Parasite;

    [Header("Player References")]
    [SerializeField] private GameObject parasitePlayer;

    [SerializeField] private ParasiteController parasiteController;
    [SerializeField] private GameObject currentHost;
    [SerializeField] private HostController currentHostController;
    [SerializeField] private Volume volume;

    [Header("Spawning")]
    [SerializeField] private Transform parasiteSpawnPoint;

    [SerializeField] private GameObject parasitePrefab;

    [Header("Game Stats")]
    [SerializeField] private int hostsConsumed = 0;

    [SerializeField] private float totalSurvivalTime = 0f;

    [Header("Voluntary Exit")]
    [SerializeField] private bool useTransitionForExit = true;

    [Header("Game Over UI")]
    [SerializeField] private GameOverUI gameOverUI;

    [SerializeField] private bool autoFindGameOverUI = true;

    private InputManager _inputManager;
    private PossessionTransitionEffect _possessionTransitionEffect;
    private Transform parasiteCameraPivot;
    private Camera _parasiteCamera; // Cached camera reference
    private GameStateMachine.GameStateMachineManager _stateMachine;
    private GameplayHUD _gameplayHUD;

    public enum GameMode
    {
        Parasite,
        Host
    }

    public GameObject CurrentHost => currentHost;
    public HostController CurrentHostController => currentHostController;
    public GameObject ParasitePlayer => parasitePlayer;
    public ParasiteController ParasiteController => parasiteController;
    private Coroutine hitstopCoroutine;

    public int GetHostsConsumed() => hostsConsumed;

    public float GetTotalSurvivalTime() => totalSurvivalTime;

    public GameMode GetCurrentMode() => currentMode;

    private void Awake()
    {
        ServiceLocator.Instance.RegisterService(this, false);
    }

    private void Start()
    {
        _inputManager = ServiceLocator.Instance.GetService<InputManager>();
        _possessionTransitionEffect = ServiceLocator.Instance.GetService<PossessionTransitionEffect>();
        _gameplayHUD = ServiceLocator.Instance.GetService<GameplayHUD>();

        // Try to get state machine (optional - for integration)
        if (ServiceLocator.Instance.TryGetService(out GameStateMachine.GameStateMachineManager stateMachine))
        {
            _stateMachine = stateMachine;
        }

        // Try to find HUD if not registered
        if (_gameplayHUD == null)
        {
            _gameplayHUD = FindFirstObjectByType<GameplayHUD>();

            if (_gameplayHUD == null)
            {
                Debug.LogWarning("[GameStateManager] GameplayHUD not found! UI panels won't switch correctly.");
            }
        }

        InitializeParasite();
        InitializeGameOverUI();
        StartParasiteMode();
    }

    private void Update()
    {
        if (currentMode == GameMode.Host)
            totalSurvivalTime += Time.deltaTime;
    }

    #region Initialization

    private void InitializeParasite()
    {
        // Find or spawn parasite
        if (!parasitePlayer)
        {
            var parasiteInScene = FindFirstObjectByType<ParasiteController>();
            if (parasiteInScene != null)
                parasitePlayer = parasiteInScene.gameObject;

            if (!parasitePlayer && parasitePrefab && parasiteSpawnPoint)
                parasitePlayer = Instantiate(parasitePrefab, parasiteSpawnPoint.position, Quaternion.identity);
        }

        if (parasitePlayer)
        {
            parasiteController = parasitePlayer.GetComponent<ParasiteController>();

            if (parasitePlayer.TryGetComponent<FirstPersonZoneController>(out var zoneController))
            {
                parasiteCameraPivot = zoneController.CameraPivot;

                // Cache camera reference
                if (parasiteCameraPivot != null)
                {
                    _parasiteCamera = parasiteCameraPivot.GetComponentInChildren<Camera>();
                    if (_parasiteCamera == null)
                    {
                        Debug.LogWarning("[GameStateManager] Camera not found in parasite camera pivot!");
                    }
                }
            }
        }
    }

    private void InitializeGameOverUI()
    {
        if (gameOverUI == null && autoFindGameOverUI)
        {
            gameOverUI = FindFirstObjectByType<GameOverUI>();

            if (gameOverUI == null)
            {
                Debug.LogWarning("[GameState] No GameOverUI found in scene! Please create one using Tools/Game Over UI/Setup Wizard");
            }
        }
    }

    #endregion Initialization

    #region Mode Switching

    public void SwitchToHostMode(GameObject host)
    {
        currentMode = GameMode.Host;
        currentHost = host;
        hostsConsumed++;

        currentHostController = host.GetComponent<HostController>();

        // Disable fisheye effect for host mode
        SetFisheyeEffect(false);

        // Disable parasite
        if (parasitePlayer)
            parasitePlayer.SetActive(false);

        // Switch input to Player actions
        _inputManager.EnablePlayerActions();

        // Update HUD to show host UI
        if (_gameplayHUD != null && currentHostController != null)
        {
            _gameplayHUD.ShowHostUI(currentHostController);
        }
        else if (_gameplayHUD == null)
        {
            Debug.LogWarning("[GameStateManager] Cannot update HUD - GameplayHUD not found!");
        }

        // Notify state machine if available
        _stateMachine?.SwitchToHostMode();

        Debug.Log($"[GameState] Switched to Host Mode. Hosts consumed: {hostsConsumed}");
    }

    private void StartParasiteMode()
    {
        currentMode = GameMode.Parasite;

        Vector3 spawnPosition = GetParasiteSpawnPosition();
        currentHost = null;
        currentHostController = null;

        EnableParasite(spawnPosition);
        SetFisheyeEffect(true);
        EnableParasiteCamera();

        _inputManager.EnableParasiteActions();

        // Update HUD to show parasite UI
        if (_gameplayHUD != null)
        {
            _gameplayHUD.ShowParasiteUI();
        }

        // Notify state machine if available
        _stateMachine.SwitchToParasiteMode();

        Debug.Log("[GameState] Switched to Parasite Mode.");
    }

    private void StartParasiteModeWithLaunch(Vector3 spawnPosition, Vector3 launchDirection, float launchForce, Quaternion parasiteRotation = default)
    {
        currentMode = GameMode.Parasite;
        currentHost = null;
        currentHostController = null;

        SetFisheyeEffect(true);
        EnableParasite(spawnPosition);

        if (parasiteController)
        {
            parasiteController.enabled = true;

            // Set rotation if provided, otherwise keep current
            if (parasiteRotation != default)
            {
                parasiteController.SetRotation(parasiteRotation);
            }

            parasiteController.ExitLaunch(launchDirection * launchForce);
        }

        EnableParasiteCamera();
        _inputManager.EnableParasiteActions();

        // Update HUD to show parasite UI
        if (_gameplayHUD != null)
            _gameplayHUD.ShowParasiteUI();

        // Notify state machine if available
        _stateMachine.SwitchToParasiteMode();
    }

    #endregion Mode Switching

    #region Host Exit/Death

    public void OnVoluntaryHostExit(ParasiteController parasite, Vector3 exitDirection, float exitForce)
    {
        Debug.Log("[GameState] Player voluntarily exiting host.");

        Transform hostCameraPivot = currentHostController.GetCameraPivot();
        Camera transferredCamera = hostCameraPivot.GetComponentInChildren<Camera>();
        Vector3 exitPosition = currentHost.transform.position;

        if (useTransitionForExit)
        {
            var transitionEffect = _possessionTransitionEffect ?? PossessionTransitionEffect.CreateInstance();

            if (transferredCamera != null && parasiteCameraPivot != null)
            {
                transitionEffect.PlayExitTransition(transferredCamera, parasiteCameraPivot, () =>
                {
                    CompleteVoluntaryExit(parasite, exitPosition, exitDirection, exitForce);
                });
            }
            else
            {
                transitionEffect.PlayExitTransition(() =>
                {
                    CompleteVoluntaryExit(parasite, exitPosition, exitDirection, exitForce);
                });
            }
        }
        else
        {
            if (transferredCamera != null && parasiteCameraPivot != null)
            {
                transferredCamera.transform.SetParent(parasiteCameraPivot, false);
                transferredCamera.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            }
            CompleteVoluntaryExit(parasite, exitPosition, exitDirection, exitForce);
        }
    }

    private void CompleteVoluntaryExit(ParasiteController parasite, Vector3 exitPosition, Vector3 exitDirection, float exitForce)
    {
        // Capture host rotation BEFORE clearing references
        Quaternion hostRotation = currentHostController != null ? currentHostController.transform.rotation : Quaternion.identity;

        if (currentHostController != null)
            currentHostController.OnParasiteDetached();

        if (parasite != null && parasiteController == null)
            parasiteController = parasite;

        StartParasiteModeWithLaunch(exitPosition, exitDirection, exitForce, hostRotation);
    }

    public void OnHostDied(ParasiteController parasite)
    {
        Debug.Log("[GameState] Host died. Returning to Parasite Mode.");

        // CRITICAL: Capture host position BEFORE any transitions or destruction
        Vector3 hostPosition = currentHost != null ? currentHost.transform.position : GetParasiteSpawnPosition();

        Transform hostCameraPivot = currentHostController.GetCameraPivot();
        Camera transferredCamera = hostCameraPivot.GetComponentInChildren<Camera>();

        var transitionEffect = _possessionTransitionEffect ?? PossessionTransitionEffect.CreateInstance();

        if (transferredCamera != null && parasiteCameraPivot != null)
        {
            // Pass captured position to callback
            transitionEffect.PlayExitTransition(transferredCamera, parasiteCameraPivot, () =>
 {
     CompleteHostDeath(parasite, hostPosition);
 });
        }
        else
        {
            // Pass captured position to callback
            transitionEffect.PlayExitTransition(() =>
                  {
                      CompleteHostDeath(parasite, hostPosition);
                  });
        }
    }

    private void CompleteHostDeath(ParasiteController parasite, Vector3 hostPosition)
    {
        // Use the passed host position (already captured before destruction)
        Vector3 exitPosition = hostPosition + Vector3.up; // Spawn above the host

        // Generate random upward direction for ejection
        Vector3 randomDirection = new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(0.7f, 1f), Random.Range(-0.3f, 0.3f)).normalized;

        float exitForce = 5f; // Stronger force for more noticeable ejection

        // Capture host rotation BEFORE clearing references
        Quaternion hostRotation = currentHostController != null ? currentHostController.transform.rotation : Quaternion.identity;

        // Detach parasite from host
        if (currentHostController != null)
            currentHostController.OnParasiteDetached();

        if (parasite != null && parasiteController == null)
            parasiteController = parasite;

        // IMPORTANT: Ensure parasite controller is properly reset before launching
        if (parasiteController != null)
            parasiteController.ResetParasiteState();

        // Launch parasite from host death position with captured rotation
        StartParasiteModeWithLaunch(exitPosition, randomDirection, exitForce, hostRotation);

        Debug.Log($"[GameState] Host died - parasite ejected from {exitPosition} in direction {randomDirection} with force {exitForce}");
    }

    #endregion Host Exit/Death

    #region Game Lifecycle

    public void RestartGame()
    {
        // Reset stats
        hostsConsumed = 0;
        totalSurvivalTime = 0f;
        HostController.ResetHostCount();

        // Hide game over UI
        if (gameOverUI != null && gameOverUI.IsShowing())
            gameOverUI.HideGameOver();

        // Reset parasite state
        if (parasiteController != null)
            parasiteController.ResetParasiteState();

        // If called directly (without state machine), handle restart locally
        // Otherwise, let the state machine handle the transition
        if (_stateMachine == null)
        {
            StartParasiteMode();
        }
        // Note: If _stateMachine exists, it will handle state transition via ChangeState()
        // We don't call _stateMachine.RestartGame() here to avoid circular dependency
    }

    public void GameOver()
    {
        Debug.Log($"[GameState] GAME OVER! Hosts: {hostsConsumed}, Time: {totalSurvivalTime:F1}s");

        // Show game over UI
        if (gameOverUI != null)
            gameOverUI.ShowGameOver(hostsConsumed, totalSurvivalTime);

        // Notify state machine if available
        _stateMachine?.TriggerGameOver(hostsConsumed, totalSurvivalTime);
    }

    #endregion Game Lifecycle

    #region Helper Methods

    private Vector3 GetParasiteSpawnPosition()
    {
        if (parasiteSpawnPoint != null)
            return parasiteSpawnPoint.position;

        if (currentHost != null)
            return currentHost.transform.position;

        return Vector3.zero;
    }

    private void EnableParasite(Vector3 position)
    {
        if (parasitePlayer)
        {
            parasitePlayer.SetActive(true);
            parasitePlayer.transform.position = position;

            if (parasiteController)
            {
                parasiteController.enabled = true;

                var characterController = parasiteController.GetComponent<CharacterController>();
                if (characterController != null && !characterController.enabled)
                {
                    characterController.enabled = true;
                    Debug.Log("[GameState] Re-enabled CharacterController");
                }
            }
        }
    }

    private void EnableParasiteCamera()
    {
        // Enable fisheye effect for parasite mode
        SetFisheyeEffect(true);

        if (parasiteCameraPivot != null)
        {
            // Only sync rotation if we have a host controller (during possession)
            if (currentHostController != null)
            {
                Transform hostCameraPivot = currentHostController.GetCameraPivot();
                if (hostCameraPivot != null)
                {
                    parasiteCameraPivot.rotation = hostCameraPivot.rotation;
                }
            }

            // Use cached camera reference instead of GetComponentInChildren
            if (_parasiteCamera != null)
            {
                _parasiteCamera.enabled = true;
                Debug.Log("[GameState] Parasite camera enabled");
            }
        }
    }

    private void SetFisheyeEffect(bool active)
    {
        // Enable or disable fisheye effect in the volume
        if (volume != null && volume.profile.TryGet(out LensDistortion fisheye))
            fisheye.active = active;
    }

    public void Hitstop(float timeScale = 1f, float duration = 1f)
    {
        if (hitstopCoroutine != null)
        {
            StopCoroutine(hitstopCoroutine);
        }
        Time.timeScale = timeScale;
        hitstopCoroutine = StartCoroutine(SlowTime(duration));
    }

    private IEnumerator SlowTime(float duration = 1f)
    {
        float startTime = Time.unscaledTime;
        while (Time.unscaledTime <= startTime + duration)
        {
            yield return null;
        }
        Time.timeScale = 1f;
    }

    #endregion Helper Methods
}