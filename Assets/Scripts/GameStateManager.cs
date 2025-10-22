using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Manages game state transitions between Parasite mode and Host mode
/// </summary>
public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

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

    private InputManager inputManager;
    private Transform parasiteCameraPivot; // Store parasite's camera pivot reference

    public enum GameMode
    {
        Parasite,
        Host
    }

    // Public properties for external access
    public GameObject CurrentHost => currentHost;

    public HostController CurrentHostController => currentHostController;
    public GameObject ParasitePlayer => parasitePlayer;
    public ParasiteController ParasiteController => parasiteController;

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
    }

    private void Start()
    {
        // Get InputManager from ServiceLocator
        inputManager = ServiceLocator.Instance.GetService<InputManager>();

        // Find or spawn parasite
        if (!parasitePlayer)
        {
            parasitePlayer = FindFirstObjectByType<ParasiteController>().gameObject;

            if (!parasitePlayer && parasitePrefab && parasiteSpawnPoint)
                parasitePlayer = Instantiate(parasitePrefab, parasiteSpawnPoint.position, Quaternion.identity);
        }

        if (parasitePlayer)
        {
            parasiteController = parasitePlayer.GetComponent<ParasiteController>();

            if (parasitePlayer.TryGetComponent<FirstPersonZoneController>(out var zoneController))
                parasiteCameraPivot = zoneController.CameraPivot;
        }

        StartParasiteMode();
    }

    private void Update()
    {
        if (currentMode == GameMode.Host)
        {
            totalSurvivalTime += Time.deltaTime;
        }
    }

    public void SwitchToHostMode(GameObject host)
    {
        currentMode = GameMode.Host;
        currentHost = host;
        hostsConsumed++;

        // Get host controller
        currentHostController = host.GetComponent<HostController>();

        volume.profile.TryGet(out LensDistortion fisheye);
        {
            fisheye.active = false;
        }

        // Disable parasite
        if (parasitePlayer)
            parasitePlayer.SetActive(false);

        // NOTE: HostController.OnParasiteAttached() already enables the host's movement controller
        // We don't need to enable it here to avoid enabling the wrong controller

        // Switch input to Player actions
        inputManager.EnablePlayerActions();

        Debug.Log($"[GameState] Switched to Host Mode. Hosts consumed: {hostsConsumed}");
    }

    /// <summary>
    /// Called when player voluntarily exits from a host body
    /// </summary>
    public void OnVoluntaryHostExit(ParasiteController parasite, Vector3 exitDirection, float exitForce)
    {
        Debug.Log("[GameState] Player voluntarily exiting host.");

        // Get references before we clear them
        Transform hostCameraPivot = currentHostController.GetCameraPivot();
        Camera transferredCamera = hostCameraPivot.GetComponentInChildren<Camera>();
        Vector3 exitPosition = currentHost.transform.position; // Spawn above host

        if (useTransitionForExit)
        {
            // Play exit transition effect
            var transitionEffect = PossessionTransitionEffect.Instance != null ?
                   PossessionTransitionEffect.Instance : PossessionTransitionEffect.CreateInstance();

            if (transferredCamera != null && parasiteCameraPivot != null)
            {
                // Play transition with camera transfer back to parasite
                transitionEffect.PlayExitTransition(transferredCamera, parasiteCameraPivot, () =>
                {
                    // This callback happens at the midpoint (after camera transfer)
                    CompleteVoluntaryExit(parasite, exitPosition, exitDirection, exitForce);
                });
            }
            else
            {
                // Fallback without camera transfer
                transitionEffect.PlayExitTransition(() =>
                {
                    CompleteVoluntaryExit(parasite, exitPosition, exitDirection, exitForce);
                });
            }
        }
        else
        {
            // Immediate exit without transition
            if (transferredCamera != null && parasiteCameraPivot != null)
            {
                // Transfer camera immediately
                transferredCamera.transform.SetParent(parasiteCameraPivot, false);
                transferredCamera.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            }
            CompleteVoluntaryExit(parasite, exitPosition, exitDirection, exitForce);
        }
    }

    private void CompleteVoluntaryExit(ParasiteController parasite, Vector3 exitPosition, Vector3 exitDirection, float exitForce)
    {
        // Notify current host that parasite detached
        if (currentHostController != null)
        {
            currentHostController.OnParasiteDetached();
        }

        if (parasite != null && parasiteController == null)
            parasiteController = parasite;

        // Return to parasite mode
        StartParasiteModeWithLaunch(exitPosition, exitDirection, exitForce);
    }

    public void OnHostDied(ParasiteController parasite)
    {
        Debug.Log("[GameState] Host died. Returning to Parasite Mode.");

        // Get references before we clear them
        Transform hostCameraPivot = currentHostController.GetCameraPivot();
        Camera transferredCamera = hostCameraPivot.GetComponentInChildren<Camera>();

        // Play exit transition effect
        var transitionEffect = PossessionTransitionEffect.Instance != null ? PossessionTransitionEffect.Instance : PossessionTransitionEffect.CreateInstance();

        if (transferredCamera != null && parasiteCameraPivot != null)
        {
            // Play transition with camera transfer back to parasite
            transitionEffect.PlayExitTransition(transferredCamera, parasiteCameraPivot, () =>
            {
                // This callback happens at the midpoint (after camera transfer)
                CompleteHostDeath(parasite);
            });
        }
        else
        {
            // Fallback without camera transfer
            transitionEffect.PlayExitTransition(() =>
            {
                CompleteHostDeath(parasite);
            });
        }
    }

    private void CompleteHostDeath(ParasiteController parasite)
    {
        // Get host position and generate a random upward exit direction
        Vector3 hostPosition = currentHost != null ? currentHost.transform.position : (parasiteSpawnPoint ? parasiteSpawnPoint.position : Vector3.zero);
        Vector3 exitPosition = hostPosition; // Spawn above host

        // Generate a random direction for the exit (upward bias)
        Vector3 randomDirection = new Vector3(
            Random.Range(-0.5f, 0.5f), // Random horizontal X
            Random.Range(0.5f, 1f),    // Upward bias Y
            Random.Range(-0.5f, 0.5f)  // Random horizontal Z
        ).normalized;

        float exitForce = 5f; // Slightly less force than voluntary exit

        // Notify current host that parasite detached
        if (currentHostController != null)
        {
            currentHostController.OnParasiteDetached();
        }

        if (parasite != null && parasiteController == null)
            parasiteController = parasite;

        // Use the same launch behavior as voluntary exit
        StartParasiteModeWithLaunch(exitPosition, randomDirection, exitForce);

        Debug.Log($"[GameState] Host died - parasite launched from {exitPosition} in direction {randomDirection}");
    }

    private void StartParasiteMode()
    {
        currentMode = GameMode.Parasite;

        // Store last host position before clearing reference
        Vector3 spawnPosition = parasiteSpawnPoint ? parasiteSpawnPoint.position : Vector3.zero;
        if (currentHost != null)
            spawnPosition = currentHost.transform.position;

        currentHost = null;
        currentHostController = null;

        // Enable parasite
        if (parasitePlayer)
        {
            parasitePlayer.SetActive(true);
            parasitePlayer.transform.position = spawnPosition;

            if (parasiteController)
                parasiteController.enabled = true;
        }

        // Camera should already be in parasite pivot from OnHostDied transfer
        // Just ensure it's enabled
        if (parasiteCameraPivot != null)
        {
            Camera camera = parasiteCameraPivot.GetComponentInChildren<Camera>();
            if (camera != null)
            {
                camera.enabled = true;
                Debug.Log("[GameState] Parasite camera enabled");
            }
        }

        // Switch input to Parasite actions
        inputManager.EnableParasiteActions();

        Debug.Log("[GameState] Switched to Parasite Mode.");
    }

    /// <summary>
    /// Starts parasite mode with an initial launch velocity (used for voluntary exit)
    /// </summary>
    private void StartParasiteModeWithLaunch(Vector3 spawnPosition, Vector3 launchDirection, float launchForce)
    {
        currentMode = GameMode.Parasite;

        currentHost = null;
        currentHostController = null;

        volume.profile.TryGet(out LensDistortion fisheye);
        {
            fisheye.active = true;
        }

        // Enable parasite
        if (parasitePlayer)
        {
            parasitePlayer.SetActive(true);
            parasitePlayer.transform.position = spawnPosition;

            if (parasiteController)
            {
                parasiteController.enabled = true;
                // Trigger a launch with the exit trajectory
                parasiteController.ExitLaunch(launchDirection * launchForce);
            }
        }

        // Camera should already be in parasite pivot from exit transition
        // Just ensure it's enabled
        if (parasiteCameraPivot != null)
        {
            Camera camera = parasiteCameraPivot.GetComponentInChildren<Camera>();
            if (camera != null)
            {
                camera.enabled = true;
                Debug.Log("[GameState] Parasite camera enabled");
            }
        }

        // Switch input to Parasite actions
        inputManager.EnableParasiteActions();

        Debug.Log("[GameState] Switched to Parasite Mode with launch exit.");
    }

    public void RestartGame()
    {
        hostsConsumed = 0;
        totalSurvivalTime = 0f;
        HostController.ResetHostCount();

        StartParasiteMode();
    }

    public void GameOver()
    {
        Debug.Log($"[GameState] GAME OVER! Hosts: {hostsConsumed}, Time: {totalSurvivalTime:F1}s");

        // TODO: Show game over UI

        Time.timeScale = 0f;
    }

    public GameMode GetCurrentMode() => currentMode;

    public int GetHostsConsumed() => hostsConsumed;

    public float GetTotalSurvivalTime() => totalSurvivalTime;

    private void OnGUI()
    {
        GUI.Label(new Rect(8, 68, 300, 20), $"Mode: {currentMode} | Hosts: {hostsConsumed} | Time: {totalSurvivalTime:F1}s");
    }
}