using UnityEngine;

/// <summary>
/// Controls a host body that the parasite can attach to.
/// Manages host health/timer, death, and weapons.
/// </summary>
public class HostController : MonoBehaviour, IDamageable
{
    [Header("Host Stats")]
    [SerializeField] private float hostLifetime = 30f;

    [SerializeField] private bool decreaseLifetimeEachHost = true;
    [SerializeField] private float lifetimeDecreasePerHost = 5f;
    [SerializeField] private float minLifetime = 5f;

    [Header("References")]
    [SerializeField] private Transform cameraPivot;

    [SerializeField] private Collider hostHeadCollider;
    [SerializeField] private FirstPersonZoneController hostMovementController;
    [SerializeField] private WeaponManager weaponManager;
    [SerializeField] private RangedWeaponProfile weaponProfile;

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
    private float lastExitAttemptTime = -10f;
    private bool isShowingExitTrajectory = false;
    private float timeSinceAttached;
    private float gravity;
    private bool dead = false;
    private bool exitingHost = false;

    private ParasiteController attachedParasite;
    private InputManager _inputManager;
    private FirstPersonZoneController zoneController;
    private GameStateManager _gameStateManager;

    private static int hostCount = 0;

    private void Awake()
    {
        if (!hostMovementController)
            hostMovementController = GetComponent<FirstPersonZoneController>();

        if (!weaponManager)
            weaponManager = GetComponent<WeaponManager>();

        if (!cameraPivot)
            Debug.LogWarning($"[Host] CameraPivot not assigned on {gameObject.name}");

        // Get zone _controller for gravity settings
        zoneController = GetComponent<FirstPersonZoneController>();
        if (zoneController != null)
            gravity = zoneController.Gravity;

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
        _inputManager = ServiceLocator.Instance.GetService<InputManager>();
        _gameStateManager = ServiceLocator.Instance.GetService<GameStateManager>();

        // Initially disable host movement
        if (hostMovementController)
            hostMovementController.enabled = false;

        // Initialize weapon manager
        if (weaponManager && _inputManager)
        {
            weaponManager.Initialize(_inputManager);
            weaponManager.Disable();
        }

        // Calculate lifetime based on host count
        remainingLifetime = hostLifetime;
        if (decreaseLifetimeEachHost && hostCount > 0)
            remainingLifetime = Mathf.Max(minLifetime, hostLifetime - (hostCount * lifetimeDecreasePerHost));
    }

    private void Update()
    {
        if (!isControlled || exitingHost || dead) return;

        // Count down lifetime
        timeSinceAttached += Time.deltaTime;
        remainingLifetime -= Time.deltaTime;

        if (remainingLifetime <= 0f)
        {
            remainingLifetime = 0f;
            Die();
        }
        // Check for voluntary exit input
        if (allowVoluntaryExit && _inputManager != null && Time.time - lastExitAttemptTime >= exitCooldown)
        {
            // Check if exit button is being held
            bool exitButtonHeld = _inputManager.ParasiteActions.ExitForHost.IsPressed();

            if (exitButtonHeld)
            {
                // Show trajectory while button is held
                if (!isShowingExitTrajectory)
                    isShowingExitTrajectory = true;

                if (showExitTrajectory && trajectorySystem != null)
                    UpdateExitTrajectoryVisualization();
            }
            else if (isShowingExitTrajectory)
            {
                // Button released - exit the host
                isShowingExitTrajectory = false;
                lastExitAttemptTime = Time.time;

                if (trajectorySystem != null)
                    trajectorySystem.HideTrajectory();

                ExitHost();
            }
        }
        else if (isShowingExitTrajectory)
        {
            // Hide trajectory if cooldown active
            isShowingExitTrajectory = false;
            if (trajectorySystem != null)
                trajectorySystem.HideTrajectory();
        }
    }

    private void UpdateExitTrajectoryVisualization()
    {
        Vector3 exitDirection = cameraPivot ? cameraPivot.forward : transform.forward;
        Vector3 exitVelocity = exitDirection * exitLaunchForce;

        // Calculate exit spawn position (above the host)
        Vector3 exitStartPosition = cameraPivot.position;

        // Show trajectory with exit parameters
        trajectorySystem.SimulateTrajectory(
            exitStartPosition,
            exitVelocity,
            attachedParasite.gravity,
            maxExitDistance,
            exitSimulationLayers, // Use simulation layers instead of host head mask
            attachedParasite.startGravityMultiplier,
            attachedParasite.endGravityMultiplier,
            attachedParasite.launchDuration,
            true
        );
    }

    public void OnParasiteAttached(ParasiteController parasite)
    {
        attachedParasite = parasite;
        isControlled = true;
        hostCount++;
        if (hostHeadCollider != null)
            hostHeadCollider.enabled = false;

        // Enable host movement _controller
        if (hostMovementController)
            hostMovementController.enabled = true;

        // Enable weapon manager
        if (weaponManager)
        {
            weaponManager.Enable();
            RangedWeapon weapon = weaponManager.GetPrimaryWeapon() as RangedWeapon;
            weapon.SwitchWeaponProfile(weaponProfile);
        }

        Camera transferredCamera = cameraPivot.GetComponentInChildren<Camera>();
        if (transferredCamera != null)
            transferredCamera.enabled = true;

        // Disable the parasite object visually
        if (parasite != null)
            parasite.gameObject.SetActive(false);

        Debug.Log($"[Host] Parasite attached! Lifetime: {remainingLifetime:F1}s");
    }

    public void OnParasiteDetached()
    {
        exitingHost = false;
        isControlled = false;
        isShowingExitTrajectory = false;

        // Hide trajectory
        if (trajectorySystem != null)
            trajectorySystem.HideTrajectory();

        // Disable host movement _controller
        if (hostMovementController)
            hostMovementController.enabled = false;

        // Disable weapon manager
        if (weaponManager)
            weaponManager.Disable();

        // Disable specific host head collider if assigned
        if (hostHeadCollider != null)
        {
            Invoke(nameof(EnableCollider), 1.5f);
        }

        // Reset parasite lifetime when exiting host
        if (attachedParasite != null)
            attachedParasite.ResetLifetime();

        Debug.Log($"[Host] Parasite detached - all colliders disabled");
    }

    /// <summary>
    /// Called when player voluntarily exits the host (by pressing exit button)
    /// </summary>
    private void ExitHost()
    {
        if (exitingHost)
            return;
        exitingHost = true;
        Debug.Log($"[Host] Player initiated voluntary exit from host");

        // Notify game manager to handle the voluntary exit
        _gameStateManager.OnVoluntaryHostExit(attachedParasite, cameraPivot.forward, exitLaunchForce);
    }

    private void Die()
    {
        if (dead)
            return;
        dead = true;
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

        if  (attachedParasite != null)
            _gameStateManager.OnHostDied(attachedParasite);
        EnableRagdoll();
        Destroy(gameObject, ragdollDuration);
    }

    private void EnableRagdoll()
    {
        // Disable character _controller
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

    public float GetLifetimePercentage() => remainingLifetime / hostLifetime;

    public void EnableCollider()
    {
        if (hostHeadCollider != null)
            hostHeadCollider.enabled = true;
    }

    //private void OnGUI()
    //{
    //    if (!isControlled) return;

    //    // Display lifetime warning
    //    float screenWidth = Screen.width;

    //    GUI.Label(new Rect(screenWidth - 220, 8, 200, 30), $"Host Time: {remainingLifetime:F1}s", new GUIStyle(GUI.skin.label) { fontSize = 18, normal = { textColor = remainingLifetime < 10f ? Color.red : Color.white } });

    //    // Lifetime bar
    //    float barWidth = 200f;
    //    float barHeight = 20f;
    //    float barX = screenWidth - barWidth - 10f;
    //    float barY = 40f;

    //    GUI.Box(new Rect(barX, barY, barWidth, barHeight), "");
    //    GUI.Box(new Rect(barX, barY, barWidth * GetLifetimePercentage(), barHeight), "", new GUIStyle(GUI.skin.box) { normal = { background = Texture2D.whiteTexture } });

    //    // Exit hint
    //    if (allowVoluntaryExit)
    //    {
    //        string exitHint = isShowingExitTrajectory ?
    //        "Release J to Exit" : "Hold J to Show Exit Trajectory";

    //        Color hintColor = isShowingExitTrajectory ? Color.green : Color.yellow;

    //        GUI.Label(new Rect(screenWidth - 220, 70, 200, 20), exitHint, new GUIStyle(GUI.skin.label) { fontSize = 12, normal = { textColor = hintColor } });
    //    }
    //}

    public static void ResetHostCount() => hostCount = 0;

    #region IDamageable Implementation

    /// <summary>
    /// Take damage - reduces host lifetime
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (!isControlled)
        {
            Debug.LogWarning("[Host] Cannot take damage - not currently controlled by parasite!");
            return;
        }

        //remainingLifetime -= damage;

        Debug.Log($"[Host] Took {damage} damage! Remaining lifetime: {remainingLifetime:F1}s");

        if (remainingLifetime <= 0f)
        {
            remainingLifetime = 0f;
            Die();
        }
    }

    #endregion IDamageable Implementation

    #region Inspector Test Functions

    [ContextMenu("Take 5s Damage")]
    public void TakeDamage5Seconds()
    {
        TakeDamage(5f);
    }

    [ContextMenu("Take 10s Damage")]
    public void TakeDamage10Seconds()
    {
        TakeDamage(10f);
    }

    [ContextMenu("Take 15s Damage")]
    public void TakeDamage15Seconds()
    {
        TakeDamage(15f);
    }

    [ContextMenu("Take Half Lifetime Damage")]
    public void TakeHalfLifetimeDamage()
    {
        float damage = remainingLifetime * 0.5f;
        TakeDamage(damage);
    }

    [ContextMenu("Kill Host")]
    public void KillHostFromInspector()
    {
        if (isControlled)
            TakeDamage(remainingLifetime);
    }

    [ContextMenu("Show Host Lifetime")]
    public void ShowHostLifetime()
    {
        Debug.Log($"[Host] Lifetime: {remainingLifetime:F1}s / {hostLifetime:F1}s ({GetLifetimePercentage() * 100f:F1}%)");
        Debug.Log($"[Host] Is Controlled: {isControlled}");
    }

    [ContextMenu("Force Exit Host")]
    public void ForceExitHostFromInspector()
    {
        if (isControlled)
            ExitHost();
    }

    #endregion Inspector Test Functions
}