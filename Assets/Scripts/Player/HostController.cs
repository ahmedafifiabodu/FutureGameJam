using UnityEngine;
using AI.Enemy;

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

    [SerializeField] private FirstPersonZoneController hostMovementController;
    [SerializeField] private WeaponManager weaponManager;
    [SerializeField] private RangedWeaponProfile weaponProfile;
    [SerializeField] private ShootingFeedbackProfile weaponFeedbackProfile;

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

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;

    [SerializeField] private AudioClip exitSound; // Sound when parasite exits the host
    [SerializeField] private AudioClip hitSound; // Sound when host takes damage
    [SerializeField] private AudioClip deathSound; // Sound when host dies

    [Header("Visual Feedback")]
    [SerializeField] private Renderer hostRenderer;

    [SerializeField] private Color damageColor = Color.red;
    [SerializeField] private float damageFlashDuration = 0.1f;

    [Header("Enemy Host Settings")]
    [SerializeField] private SkinnedMeshRenderer[] skinnedMeshRenderers;

    [SerializeField] private bool isEnemyHost = false; // Set to true if this host is an enemy

    [SerializeField] private bool destroyParentOnDeath = false; // Destroy parent GameObject on death if true

    private bool isControlled = false;
    private float remainingLifetime;
    private float lastExitAttemptTime = -10f;
    private bool isShowingExitTrajectory = false;
    private float timeSinceAttached;
    private bool dead = false;
    private bool exitingHost = false;

    // Public property to check if host is controlled by parasite
    public bool IsControlled => isControlled;

    private ParasiteController attachedParasite;
    private InputManager _inputManager;
    private FirstPersonZoneController zoneController;
    private GameStateManager _gameStateManager;
    private ShootingFeedbackSystem feedbackSystem;

    // Enemy component references
    private EnemyController enemyController;

    private UnityEngine.AI.NavMeshAgent navMeshAgent;
    private Animator animator;

    // Visual feedback
    private Color originalColor;

    private Material materialInstance;
    private bool isFlashing;
    private float flashTimer;

    private static int hostCount = 0;

    private void Awake()
    {
        if (!hostMovementController)
            hostMovementController = GetComponent<FirstPersonZoneController>();

        if (!weaponManager)
            weaponManager = FindFirstObjectByType<WeaponManager>();

        feedbackSystem = GetComponentInChildren<ShootingFeedbackSystem>();

        // Get zone controller reference
        zoneController = GetComponent<FirstPersonZoneController>();

        // Get or add AudioSource component
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 1f; // 3D sound
            }
        }

        // Setup visual feedback renderer
        if (hostRenderer == null)
            hostRenderer = GetComponentInChildren<Renderer>();

        if (hostRenderer != null && hostRenderer.material != null)
        {
            // Create material instance to avoid modifying shared material
            materialInstance = new Material(hostRenderer.material);
            hostRenderer.material = materialInstance;
            originalColor = materialInstance.color;
        }

        // Cache enemy components if this is an enemy host
        if (isEnemyHost)
        {
            enemyController = GetComponent<EnemyController>();
            navMeshAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            animator = GetComponent<Animator>();
        }
    }

    private void Start()
    {
        _inputManager = ServiceLocator.Instance.GetService<InputManager>();
        _gameStateManager = ServiceLocator.Instance.GetService<GameStateManager>();

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

        // Update visual feedback flash
        if (isFlashing)
        {
            flashTimer -= Time.deltaTime;
            if (flashTimer <= 0)
            {
                isFlashing = false;
                if (materialInstance != null)
                    materialInstance.color = originalColor;
            }
        }

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

        // Get trajectory system from the parasite
        if (trajectorySystem == null && parasite != null)
        {
            trajectorySystem = parasite.GetComponent<ParasiteLaunchTrajectory>();
            if (trajectorySystem != null)
            {
                Debug.Log("[Host] Acquired trajectory system from parasite");
            }
            else
            {
                Debug.LogWarning("[Host] No trajectory system found on parasite!");
            }
        }

        // Enable weapon GameObject when possessing host
        if (parasite != null)
        {
            parasite.EnableWeaponGameObject();
        }

        // This allows the room to update its enemy tracking and potentially open the exit door
        if (isEnemyHost)
        {
            if (TryGetComponent<AI.Enemy.EnemyController>(out var enemyController))
            {
                // Find the room this enemy belongs to
                if (enemyController.CurrentRoom.TryGetComponent<ProceduralGeneration.Room>(out var room))
                    room.OnEnemyPossessed(enemyController);
            }
        }

        // Disable enemy AI components if this is an enemy host
        if (isEnemyHost)
        {
            DisableEnemyComponents();
        }

        // Enable host movement _controller
        if (hostMovementController)
            hostMovementController.enabled = true;

        // Enable weapon manager and apply weapon profiles
        if (weaponManager)
        {
            weaponManager.Enable();
            RangedWeapon weapon = weaponManager.GetPrimaryWeapon() as RangedWeapon;

            if (weapon != null)
            {
                // Apply weapon profile
                if (weaponProfile != null)
                {
                    weapon.SwitchWeaponProfile(weaponProfile);
                    Debug.Log($"[Host] Applied weapon profile: {weaponProfile.weaponName}");
                }

                // Apply weapon feedback profile to shooting feedback system
                if (weaponFeedbackProfile != null)
                {
                    if (feedbackSystem != null)
                        feedbackSystem.SwitchProfile(weaponFeedbackProfile);
                }
            }
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

        // Disable weapon GameObject when exiting host
        if (attachedParasite != null)
        {
            attachedParasite.DisableWeaponGameObject();
        }

        // Re-enable enemy AI components if this is an enemy host
        if (isEnemyHost)
        {
            EnableEnemyComponents();
        }

        // Disable host movement _controller
        if (hostMovementController)
            hostMovementController.enabled = false;

        // Disable weapon manager
        if (weaponManager)
            weaponManager.Disable();

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

        // Play exit sound
        if (audioSource && exitSound)
        {
            audioSource.PlayOneShot(exitSound);
        }

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

        // Play death sound
        if (audioSource && deathSound)
        {
            audioSource.PlayOneShot(deathSound);
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

        // Play death sound
        if (audioSource && deathSound)
        {
            audioSource.PlayOneShot(deathSound);
        }

        if (attachedParasite != null)
            _gameStateManager.OnHostDied(attachedParasite);

        EnableRagdoll();

        // Determine what to destroy
        GameObject objectToDestroy = gameObject;

        // If destroyParentOnDeath is true and we have a parent, destroy the parent instead
        if (destroyParentOnDeath && transform.parent != null)
        {
            objectToDestroy = transform.parent.gameObject;
            Debug.Log($"[Host] Destroying parent GameObject: {objectToDestroy.name}");
        }

        Destroy(objectToDestroy, ragdollDuration);
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

    #region Enemy Host Management

    /// <summary>
    /// Disable enemy AI components when possessed by parasite
    /// </summary>
    private void DisableEnemyComponents()
    {
        if (!isEnemyHost) return;

        if (enemyController != null)
            enemyController.enabled = false;

        if (navMeshAgent != null)
            navMeshAgent.enabled = false;

        // Disable Animator to stop root motion and animations
        if (animator != null)
            animator.enabled = false;

        // Enable CharacterController when possessed (it's disabled during AI movement)
        var characterController = GetComponent<CharacterController>();
        if (characterController != null)
        {
            characterController.enabled = true;
            Debug.Log("[Host] CharacterController enabled for player control");
        }

        // Disable SkinnedMeshRenderers to hide enemy mesh
        if (skinnedMeshRenderers != null && skinnedMeshRenderers.Length > 0)
        {
            foreach (var smr in skinnedMeshRenderers)
            {
                if (smr != null)
                    smr.enabled = false;
            }
        }
    }

    /// <summary>
    /// Re-enable enemy AI components when parasite exits (before lifetime expires)
    /// </summary>
    private void EnableEnemyComponents()
    {
        if (!isEnemyHost) return;

        if (enemyController != null)
            enemyController.enabled = true;

        if (navMeshAgent != null)
            navMeshAgent.enabled = true;

        // Re-enable Animator to resume root motion and animations
        if (animator != null)
            animator.enabled = true;

        // Disable CharacterController when returning to AI control
        var characterController = GetComponent<CharacterController>();
        if (characterController != null)
        {
            characterController.enabled = false;
            Debug.Log("[Host] CharacterController disabled for AI control");
        }

        // Re-enable SkinnedMeshRenderers to show enemy mesh
        if (skinnedMeshRenderers != null && skinnedMeshRenderers.Length > 0)
        {
            foreach (var smr in skinnedMeshRenderers)
            {
                if (smr != null)
                    smr.enabled = true;
            }
        }
    }

    #endregion Enemy Host Management

    public static void ResetHostCount() => hostCount = 0;

    #region IDamageable Implementation

    /// <summary>
    /// Take damage - reduces host lifetime
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (!isControlled)
            return;

        // Reduce lifetime by damage amount
        remainingLifetime -= damage;

        // Visual feedback - damage flash
        StartDamageFlash();

        // Audio feedback - hit sound
        if (audioSource && hitSound)
            audioSource.PlayOneShot(hitSound);

        // Check if host should die
        if (remainingLifetime <= 0f)
        {
            remainingLifetime = 0f;
            Die();
        }
    }

    /// <summary>
    /// Start the damage flash visual feedback
    /// </summary>
    private void StartDamageFlash()
    {
        isFlashing = true;
        flashTimer = damageFlashDuration;

        if (materialInstance != null)
            materialInstance.color = damageColor;
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