using UnityEngine;

/// <summary>
/// Controls a host body that the parasite can attach to.
/// Manages host health/timer, death, and weapons.
/// </summary>
public class HostController : MonoBehaviour
{
    [Header("Host Stats")]
    [SerializeField] private float hostLifetime = 30f; // How long the host survives

    [SerializeField] private bool decreaseLifetimeEachHost = true;
    [SerializeField] private float lifetimeDecreasePerHost = 5f;
    [SerializeField] private float minLifetime = 5f;

    [Header("References")]
    [SerializeField] private Transform cameraPivot;

    [SerializeField] private FirstPersonZoneController hostMovementController;
    [SerializeField] private WeaponManager weaponManager;

    [Header("Death")]
    [SerializeField] private GameObject deathEffect;

    [SerializeField] private float ragdollDuration = 3f;

    [Header("Voluntary Exit")]
    [SerializeField] private bool allowVoluntaryExit = true;

    [SerializeField] private float exitLaunchForce = 15f;
    [SerializeField] private float exitCooldown = 1f;
    [SerializeField] private ParasiteLaunchTrajectory trajectorySystem;
    [SerializeField] private bool showExitTrajectory = true;
    [SerializeField] private float maxExitDistance = 10f;
    [SerializeField] private LayerMask exitSimulationLayers = -1;

    private bool isControlled = false;
    private float remainingLifetime;
    private float timeSinceAttached;
    private ParasiteController attachedParasite;
    private InputManager inputManager;
    private float lastExitAttemptTime = -10f;
    private bool isShowingExitTrajectory = false;
    private FirstPersonZoneController zoneController;
    private float gravity;

    private static int hostCount = 0; // Track number of hosts used

    private void Awake()
    {
        if (!hostMovementController)
            hostMovementController = GetComponent<FirstPersonZoneController>();

        if (!weaponManager)
            weaponManager = GetComponent<WeaponManager>();

        if (!cameraPivot)
            Debug.LogWarning($"[Host] CameraPivot not assigned on {gameObject.name}");

        // Get zone controller for gravity settings
        zoneController = GetComponent<FirstPersonZoneController>();
        if (zoneController != null)
        {
            gravity = zoneController.Gravity;
        }

        // Setup trajectory system if not assigned
        if (trajectorySystem == null && showExitTrajectory)
        {
            trajectorySystem = GetComponent<ParasiteLaunchTrajectory>();
            if (trajectorySystem == null)
            {
                trajectorySystem = GetComponentInChildren<ParasiteLaunchTrajectory>();
            }

            if (trajectorySystem == null)
            {
                Debug.LogWarning("[Host] ParasiteLaunchTrajectory component not found. Exit trajectory visualization will be disabled.");
                showExitTrajectory = false;
            }
        }
    }

    private void Start()
    {
        // Get InputManager from ServiceLocator
        inputManager = ServiceLocator.Instance.GetService<InputManager>();

        // Initially disable host movement
        if (hostMovementController)
            hostMovementController.enabled = false;

        // Initialize weapon manager
        if (weaponManager && inputManager)
        {
            weaponManager.Initialize(inputManager);
            weaponManager.Disable();
        }

        // Calculate lifetime based on host count
        remainingLifetime = hostLifetime;
        if (decreaseLifetimeEachHost && hostCount > 0)
        {
            remainingLifetime = Mathf.Max(minLifetime, hostLifetime - (hostCount * lifetimeDecreasePerHost));
        }
    }

    private void Update()
    {
        if (!isControlled) return;

        // Count down lifetime
        timeSinceAttached += Time.deltaTime;
        remainingLifetime -= Time.deltaTime;

        if (remainingLifetime <= 0f)
        {
            Die();
        }

        // Check for voluntary exit input
        if (allowVoluntaryExit && inputManager != null && Time.time - lastExitAttemptTime >= exitCooldown)
        {
            // Check if exit button is being held
            bool exitButtonHeld = inputManager.ParasiteActions.ExitForHost.IsPressed();

            if (exitButtonHeld)
            {
                // Show trajectory while button is held
                if (!isShowingExitTrajectory)
                {
                    isShowingExitTrajectory = true;
                    Debug.Log("[Host] Showing exit trajectory preview");
                }

                if (showExitTrajectory && trajectorySystem != null)
                {
                    UpdateExitTrajectoryVisualization();
                }
            }
            else if (isShowingExitTrajectory)
            {
                // Button released - exit the host
                isShowingExitTrajectory = false;
                lastExitAttemptTime = Time.time;

                if (trajectorySystem != null)
                {
                    trajectorySystem.HideTrajectory();
                }

                ExitHost();
            }
        }
        else if (isShowingExitTrajectory)
        {
            // Hide trajectory if cooldown active
            isShowingExitTrajectory = false;
            if (trajectorySystem != null)
            {
                trajectorySystem.HideTrajectory();
            }
        }
    }

    private void UpdateExitTrajectoryVisualization()
    {
        Vector3 exitDirection = cameraPivot ? cameraPivot.forward : transform.forward;
        Vector3 exitVelocity = exitDirection * exitLaunchForce;

        // Calculate exit spawn position (above the host)
        Vector3 exitStartPosition = transform.position + Vector3.up * 1.5f;

        // Show trajectory with exit parameters
        trajectorySystem.SimulateTrajectory(
            exitStartPosition,
            exitVelocity,
            gravity,
            maxExitDistance,
            exitSimulationLayers, // Use simulation layers instead of host head mask
            true // Always valid since it's voluntary
        );
    }

    public void OnParasiteAttached(ParasiteController parasite)
    {
        attachedParasite = parasite;
        isControlled = true;
        hostCount++;

        // Enable host movement controller
        if (hostMovementController)
            hostMovementController.enabled = true;

        // Enable weapon manager
        if (weaponManager)
            weaponManager.Enable();

        Camera transferredCamera = cameraPivot.GetComponentInChildren<Camera>();
        if (transferredCamera != null)
        {
            transferredCamera.enabled = true;
            Debug.Log($"[Host] Using transferred camera for control");
        }
        else
        {
            Debug.LogWarning("[Host] No camera found in camera pivot after transfer!");
        }

        // Disable the parasite object visually
        if (parasite != null)
            parasite.gameObject.SetActive(false);

        Debug.Log($"[Host] Parasite attached! Lifetime: {remainingLifetime:F1}s");
    }

    public void OnParasiteDetached()
    {
        isControlled = false;
        isShowingExitTrajectory = false;

        // Hide trajectory
        if (trajectorySystem != null)
        {
            trajectorySystem.HideTrajectory();
        }

        // Get camera before detaching (will be moved back to parasite)
        Camera transferredCamera = cameraPivot.GetComponentInChildren<Camera>();
        if (transferredCamera != null)
        {
            // Camera will be moved back to parasite by GameStateManager
            Debug.Log($"[Host] Camera will be transferred back to parasite");
        }

        // Disable host movement controller
        if (hostMovementController)
            hostMovementController.enabled = false;

        // Disable weapon manager
        if (weaponManager)
            weaponManager.Disable();

        Debug.Log($"[Host] Parasite detached");
    }

    /// <summary>
    /// Called when player voluntarily exits the host (by pressing exit button)
    /// </summary>
    private void ExitHost()
    {
        Debug.Log($"[Host] Player initiated voluntary exit from host");

        // Notify game manager to handle the voluntary exit
        GameStateManager.Instance.OnVoluntaryHostExit(attachedParasite, cameraPivot.forward, exitLaunchForce);
    }

    private void Die()
    {
        Debug.Log($"[Host] Host died! Survived: {timeSinceAttached:F1}s");

        // Hide trajectory if showing
        if (trajectorySystem != null)
        {
            trajectorySystem.HideTrajectory();
        }


        // Disable movement
        if (hostMovementController)
            hostMovementController.enabled = false;

        // Disable weapons
        if (weaponManager)
            weaponManager.Disable();

        // Spawn death effect
        if (deathEffect)
            Instantiate(deathEffect, transform.position, Quaternion.identity);

        // Notify game manager to switch back to parasite mode
        GameStateManager.Instance.OnHostDied(attachedParasite);

        // Optional: Enable ragdoll or death animation
        EnableRagdoll();

        // Destroy host after ragdoll duration
        Destroy(gameObject, ragdollDuration);
    }

    private void EnableRagdoll()
    {
        // Disable character controller
        var cc = GetComponent<CharacterController>();
        if (cc) cc.enabled = false;

        // Enable ragdoll physics on all rigidbodies
        foreach (var rb in GetComponentsInChildren<Rigidbody>())
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }
    }

    public Transform GetCameraPivot() => cameraPivot;

    public float GetRemainingLifetime() => remainingLifetime;

    public float GetLifetimePercentage() => remainingLifetime / hostLifetime;

    public bool IsControlled() => isControlled;

    public WeaponManager GetWeaponManager() => weaponManager;

    private void OnGUI()
    {
        if (!isControlled) return;

        // Display lifetime warning
        float screenWidth = Screen.width;

        GUI.Label(new Rect(screenWidth - 220, 8, 200, 30),
   $"Host Time: {remainingLifetime:F1}s",
     new GUIStyle(GUI.skin.label) { fontSize = 18, normal = { textColor = remainingLifetime < 10f ? Color.red : Color.white } });

        // Lifetime bar
        float barWidth = 200f;
        float barHeight = 20f;
        float barX = screenWidth - barWidth - 10f;
        float barY = 40f;

        GUI.Box(new Rect(barX, barY, barWidth, barHeight), "");
        GUI.Box(new Rect(barX, barY, barWidth * GetLifetimePercentage(), barHeight), "",
    new GUIStyle(GUI.skin.box) { normal = { background = Texture2D.whiteTexture } });

        // Exit hint
        if (allowVoluntaryExit)
        {
            string exitHint = isShowingExitTrajectory ?
 "Release RMB to Exit" :
       "Hold RMB to Show Exit Trajectory";

            Color hintColor = isShowingExitTrajectory ? Color.green : Color.yellow;

            GUI.Label(new Rect(screenWidth - 220, 70, 200, 20),
          exitHint,
    new GUIStyle(GUI.skin.label) { fontSize = 12, normal = { textColor = hintColor } });
        }
    }

    public static void ResetHostCount()
    {
        hostCount = 0;
    }
}