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

    private InputManager inputManager;
    private Camera parasiteCamera;

    public enum GameMode
    {
        Parasite,
        Host
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
    }

    private void Start()
    {
        // Get InputManager from ServiceLocator
        inputManager = ServiceLocator.Instance.GetService<InputManager>();

        // Find or spawn parasite
        if (!parasitePlayer)
        {
            parasitePlayer = FindObjectOfType<ParasiteController>()?.gameObject;
            
            if (!parasitePlayer && parasitePrefab && parasiteSpawnPoint)
            {
                parasitePlayer = Instantiate(parasitePrefab, parasiteSpawnPoint.position, Quaternion.identity);
            }
        }

        if (parasitePlayer)
        {
            parasiteController = parasitePlayer.GetComponent<ParasiteController>();
            parasiteCamera = parasitePlayer.GetComponentInChildren<Camera>();
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

        // Disable parasite camera
        if (parasiteCamera)
        {
            parasiteCamera.enabled = false;
            Debug.Log("[GameState] Disabled parasite camera");
        }

        // Disable parasite
        if (parasitePlayer)
            parasitePlayer.SetActive(false);

        // Enable host controller (this will enable host camera via OnParasiteAttached)
        var hostCtrl = host.GetComponent<FirstPersonZoneController>();
        if (hostCtrl)
            hostCtrl.enabled = true;

        // Switch input to Player actions
        inputManager?.EnablePlayerActions();

        Debug.Log($"[GameState] Switched to Host Mode. Hosts consumed: {hostsConsumed}");
    }

    public void OnHostDied(ParasiteController parasite)
    {
        Debug.Log("[GameState] Host died. Returning to Parasite Mode.");
        
        // Notify current host that parasite detached (this will disable host camera)
        if (currentHostController != null)
        {
            currentHostController.OnParasiteDetached();
        }
        
        if (parasite != null && parasiteController == null)
            parasiteController = parasite;

        StartParasiteMode();
    }

    private void StartParasiteMode()
    {
        currentMode = GameMode.Parasite;
        
        // Store last host position before clearing reference
        Vector3 spawnPosition = parasiteSpawnPoint ? parasiteSpawnPoint.position : Vector3.zero;
        if (currentHost != null)
        {
            spawnPosition = currentHost.transform.position;
        }
        
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
                parasiteController.enabled = true;
        }

        // Enable parasite camera
        if (parasiteCamera)
        {
            parasiteCamera.enabled = true;
            Debug.Log("[GameState] Enabled parasite camera");
        }

        // Switch input to Parasite actions
        inputManager?.EnableParasiteActions();

        Debug.Log("[GameState] Switched to Parasite Mode.");
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