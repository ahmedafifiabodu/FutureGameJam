using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class ParasiteController : MonoBehaviour, IDamageable
{
    [Header("Crawling Movement")]
    [SerializeField] private float crawlSpeed = 2.5f;

    [SerializeField] private float airSpeed = 4f;

    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float airControl = 2.5f;
    [SerializeField] private float slideControl = 2f;
    [SerializeField] private float slideTime = 2f;

    [Header("Launch Attack")]
    [SerializeField] private float launchForce = 15f;

    [SerializeField] private float launchCooldown = 1f;
    [SerializeField] private float maxLaunchDistance = 10f;
    [SerializeField] private float minLaunchDistance = 1.5f;
    [SerializeField] private float launchDuration = 2f;
    [SerializeField] private float startGravityMultiplier = 0.5f;
    [SerializeField] private float endGravityMultiplier = 2f;
    [SerializeField] private float aimFov = 110f;
    [SerializeField] private float idleFov = 90f;
    [SerializeField] private float fovTimeChange = 100f;
    [SerializeField] private float cameraTilt = 5f;
    [SerializeField] private float tiltTimeChange = 50f;

    [Header("Launch Physics")]
    [SerializeField] private bool enableBounce = true;

    [Tooltip("Percentage of velocity retained after bouncing (0 = no bounce, 1 = perfect bounce)")]
    [SerializeField][Range(0f, 1f)] private float bounciness = 0.6f;

    [SerializeField] private bool enableFriction = true;

    [Tooltip("Friction applied to velocity per second while in air (higher = more friction)")]
    [SerializeField][Range(0f, 5f)] private float airFriction = 0.5f;

    [Tooltip("Friction applied to velocity per second when sliding on walls")]
    [SerializeField][Range(0f, 10f)] private float wallFriction = 2f;

    [Tooltip("Minimum velocity magnitude before stopping due to friction")]
    [SerializeField] private float minVelocityThreshold = 0.5f;

    [Header("Host Detection")]
    [SerializeField] private LayerMask hostHeadLayerMask;

    [Tooltip("Layers to check for trajectory collision (typically everything or ground + obstacles)")]
    [SerializeField] private LayerMask simulationLayers = -1; // Default to everything

    [Header("Trajectory Visualization")]
    [SerializeField] private ParasiteLaunchTrajectory trajectorySystem;

    [SerializeField] private bool showTrajectory = true;

    [Header("Debug")]
    [SerializeField] private bool showDebug = true;

    [SerializeField] private Color aimColor = Color.red;
    [SerializeField] private Color tooCloseColor = Color.yellow;

    [Header("Bobbing")]
    [SerializeField] private Vector3 restPosition;

    [SerializeField] private float bobSpeed = 4.8f;
    [SerializeField] private float bobAmount = 0.05f;
    [SerializeField] private float bobTimer = Mathf.PI / 2;

    [Header("Visual Effects")]
    [SerializeField] private bool usePossessionTransition = true;

    [Header("Parasite Lifetime")]
    [SerializeField] private float maxParasiteLifetime = 60f; // How long parasite can survive without a host

    [SerializeField] private bool enableLifetimeDecay = true;
    [SerializeField] private bool showLifetimeWarning = true;
    [SerializeField] private float lifetimeWarningThreshold = 15f;

    private CharacterController _controller;
    private InputManager _inputManager;
    private GameStateManager _gameStateManager;
    private FirstPersonZoneController zoneController;
    private PossessionTransitionEffect _possessionTransitionEffect;

    // Shared settings from FirstPersonZoneController
    private Transform cameraPivot;

    private float mouseSensitivity;
    private bool lookInputIsDelta;
    private float gravity;
    private bool launchTimedOut = false;

    private float yaw, pitch, roll;
    private Vector3 move;
    private float yVel;
    private bool isLaunching;
    private bool isAiming; // Track if player is holding aim button
    private float lastLaunchTime;
    private float launchStartTime;
    private float lastLandTime;
    private Vector3 launchVelocity;
    private bool canLaunch; // Track if target is far enough to launch
    private bool isAttachingToHost = false; // Prevent multiple host attachments
    private float currentParasiteLifetime;
    private bool isDead = false;

    private void Awake()
    {
        ServiceLocator.Instance.RegisterService(this, false);

        _controller = GetComponent<CharacterController>();
        zoneController = GetComponent<FirstPersonZoneController>();

        if (zoneController != null)
        {
            cameraPivot = zoneController.CameraPivot;
            mouseSensitivity = zoneController.MouseSensitivity;
            lookInputIsDelta = zoneController.LookInputIsDelta;
            gravity = zoneController.Gravity;

            if (showDebug && cameraPivot)
                Debug.Log($"[Parasite] Using shared settings - CameraPivot: {cameraPivot.name}, MouseSensitivity: {mouseSensitivity}");
        }
        else
        {
            Debug.LogError("[Parasite] FirstPersonZoneController not found on GameObject! ParasiteController requires it.");
        }

        // Setup trajectory system if not assigned
        if (trajectorySystem == null)
        {
            trajectorySystem = GetComponent<ParasiteLaunchTrajectory>();
            if (trajectorySystem == null && showTrajectory)
            {
                Debug.LogWarning("[Parasite] ParasiteLaunchTrajectory component not found. Trajectory visualization will be disabled.");
                showTrajectory = false;
            }
        }
        lastLandTime = -slideTime;

        // Initialize parasite lifetime
        currentParasiteLifetime = maxParasiteLifetime;
    }

    private void OnEnable()
    {
        // When parasite _controller is enabled, disable FirstPersonZoneController
        if (zoneController != null && zoneController.enabled)
        {
            zoneController.enabled = false;
            if (showDebug)
                Debug.Log("[Parasite] Disabled FirstPersonZoneController to avoid camera conflict");
        }

        // Reset attachment flag when enabled
        isAttachingToHost = false;
    }

    private void OnDisable()
    {
        // When parasite _controller is disabled, re-enable FirstPersonZoneController
        if (zoneController != null && !zoneController.enabled)
        {
            zoneController.enabled = true;
            if (showDebug)
                Debug.Log("[Parasite] Re-enabled FirstPersonZoneController");
        }

        // Hide trajectory when disabled
        if (trajectorySystem != null)
        {
            trajectorySystem.HideTrajectory();
        }
    }

    private void Start()
    {
        _inputManager = ServiceLocator.Instance.GetService<InputManager>();
        _gameStateManager = ServiceLocator.Instance.GetService<GameStateManager>();
        _possessionTransitionEffect = ServiceLocator.Instance.GetService<PossessionTransitionEffect>();

        if (!cameraPivot)
            Debug.LogWarning("[Parasite] CameraPivot not found! Ensure FirstPersonZoneController has cameraPivot assigned.");

        _inputManager.EnableParasiteActions();
    }

    private void Update()
    {
        if (_inputManager == null || isDead) return;

        // Count down parasite lifetime
        if (enableLifetimeDecay)
        {
            currentParasiteLifetime -= Time.deltaTime;

            if (currentParasiteLifetime <= 0f)
            {
                Die();
                return;
            }
        }

        Look();

        if (!isLaunching)
        {
            HandleCrawling();
            HandleAimingAndLaunch();
        }
        else
        {
            // Hide trajectory while launching
            if (showTrajectory && trajectorySystem != null)
            {
                trajectorySystem.HideTrajectory();
            }

            HandleLaunchMovement();
            CheckForLaunchTimeout();
        }
        Camera camera = gameObject.GetComponentInChildren<Camera>();
        camera.fieldOfView = Mathf.MoveTowards(camera.fieldOfView, isAiming ? aimFov : idleFov, fovTimeChange * Time.deltaTime);
    }

    private void Look()
    {
        if (!cameraPivot) return;

        Vector2 look = _inputManager.ParasiteActions.Look.ReadValue<Vector2>();
        float mx = look.x * mouseSensitivity * (lookInputIsDelta ? 1f : Time.deltaTime);
        float my = look.y * mouseSensitivity * (lookInputIsDelta ? 1f : Time.deltaTime);

        yaw += mx;
        pitch = Mathf.Clamp(pitch - my, -85f, 85f);

        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        Vector2 moveInput = _inputManager.ParasiteActions.Move.ReadValue<Vector2>();
        roll = Mathf.MoveTowards(roll, moveInput.x != 0f ? Mathf.Sign(moveInput.x) * cameraTilt : 0f, tiltTimeChange * Time.deltaTime);
        cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, roll);
    }

    private void HandleCrawling()
    {
        Vector2 moveInput = _inputManager.ParasiteActions.Move.ReadValue<Vector2>();

        if (moveInput.sqrMagnitude > 1f)
            moveInput.Normalize();

        Vector3 moveDir = (transform.right * moveInput.x + transform.forward * moveInput.y);
        moveDir.y = 0f;

        float moveControl = 1f;
        if (!_controller.isGrounded)
            moveControl = airControl * Time.deltaTime;
        else if (Time.time < lastLandTime + slideTime)
            moveControl = slideControl * Time.deltaTime;

        move = Vector3.Lerp(move, moveDir * (_controller.isGrounded ? crawlSpeed : airSpeed), moveControl);

        if (_controller.isGrounded && yVel < 0f)
            yVel = -2f;

        if (_controller.isGrounded && _inputManager.ParasiteActions.Jump.triggered)
            yVel = Mathf.Sqrt(jumpHeight * -2f * gravity);

        yVel += gravity * Time.deltaTime;
        move.y = yVel;

        _controller.Move(move * Time.deltaTime);
        if (!cameraPivot) return;
        if (moveDir != Vector3.zero && _controller.isGrounded)
        {
            bobTimer += bobSpeed * Time.deltaTime;
        }
        else
        {
            bobTimer = Mathf.MoveTowards(bobTimer, bobTimer > Mathf.PI / 2 ? Mathf.PI : Mathf.PI / 2, 10f * Time.deltaTime);
        }

        if (bobTimer > Mathf.PI * 2)
        {
            bobTimer -= Mathf.PI * 2;
        }
        cameraPivot.localPosition = new Vector3(restPosition.x + (Mathf.Sin(bobTimer) * bobAmount) * 0.1f,
            restPosition.y + (Mathf.Sin(bobTimer * 2f) * bobAmount), restPosition.z);
    }

    private void HandleAimingAndLaunch()
    {
        // Check if on cooldown
        if (Time.time - lastLaunchTime < launchCooldown)
        {
            isAiming = false;
            if (trajectorySystem != null)
            {
                trajectorySystem.HideTrajectory();
            }
            return;
        }

        // Check if attack button is held down
        bool attackHeld = _inputManager.ParasiteActions.Attack.IsPressed();

        if (attackHeld)
        {
            // Player is aiming - show trajectory
            isAiming = true;

            if (showTrajectory && trajectorySystem != null)
            {
                UpdateTrajectoryVisualization();
            }
        }
        else if (isAiming)
        {
            // Button released - try to launch if valid
            isAiming = false;

            if (trajectorySystem != null)
            {
                trajectorySystem.HideTrajectory();
            }

            if (canLaunch)
            {
                LaunchAtTarget();
            }
            else
            {
                if (showDebug)
                {
                    if (!canLaunch)
                        Debug.Log("[Parasite] Launch cancelled - target too close");
                }
            }
        }
        else
        {
            // Not aiming - hide trajectory
            if (trajectorySystem != null)
            {
                trajectorySystem.HideTrajectory();
            }
        }
    }

    private void LaunchAtTarget()
    {
        Vector3 launchDir = cameraPivot ? cameraPivot.forward : transform.forward;
        launchVelocity = launchDir * launchForce;
        isLaunching = true;
        lastLaunchTime = Time.time;
        launchStartTime = Time.time;
        gravity *= startGravityMultiplier;

        if (showDebug)
            Debug.Log($"[Parasite] Launched! Direction: {launchDir}");
    }

    private void HandleLaunchMovement()
    {
        launchVelocity.y += gravity * Time.deltaTime;

        // Apply air friction if enabled
        if (enableFriction && airFriction > 0f)
        {
            float frictionMultiplier = Mathf.Max(0f, 1f - (airFriction * Time.deltaTime));
            Vector3 horizontalVelocity = new Vector3(launchVelocity.x, 0f, launchVelocity.z);
            horizontalVelocity *= frictionMultiplier;
            launchVelocity.x = horizontalVelocity.x;
            launchVelocity.z = horizontalVelocity.z;

            // Stop if velocity is too low
            if (launchVelocity.magnitude < minVelocityThreshold)
            {
                if (showDebug)
                    Debug.Log("[Parasite] Velocity too low, stopping launch");
                ResetToGroundedState();
                return;
            }
        }

        CollisionFlags flags = _controller.Move(launchVelocity * Time.deltaTime);

        if ((flags & CollisionFlags.Below) != 0 && launchVelocity.y < 0)
        {
            ResetToGroundedState();
        }

        // Apply bounce effect if enabled and hitting the ground or a valid surface
        if (enableBounce && (flags & CollisionFlags.Below) != 0 && launchVelocity.y < 0)
        {
            launchVelocity.y = Mathf.Abs(launchVelocity.y) * bounciness;
            // Reduce horizontal velocity based on bounce angle (flattening the bounce)
            launchVelocity.x *= 1f - bounciness;
            launchVelocity.z *= 1f - bounciness;

            if (showDebug)
                Debug.Log($"[Parasite] Bounced! New Velocity: {launchVelocity}");
        }

        // Apply air friction
        if (!_controller.isGrounded && enableFriction)
        {
            float friction = airFriction * Time.deltaTime;
            launchVelocity.x = Mathf.MoveTowards(launchVelocity.x, 0, friction);
            launchVelocity.z = Mathf.MoveTowards(launchVelocity.z, 0, friction);

            // Slow down vertical velocity as well, preventing infinite ascent
            if (Mathf.Abs(launchVelocity.y) > minVelocityThreshold)
                launchVelocity.y = Mathf.MoveTowards(launchVelocity.y, 0, friction);
        }
    }

    private void CheckForLaunchTimeout()
    {
        if (isLaunching && !launchTimedOut && Time.time - launchStartTime > launchDuration)
        {
            launchTimedOut = true;
            if (showDebug)
                Debug.Log("[Parasite] Launch timeout - returning to ground");
            gravity /= startGravityMultiplier;
            gravity *= endGravityMultiplier;
        }
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (!isLaunching || isAttachingToHost) return;

        if (IsHostHead(hit.gameObject))
        {
            // Set flag immediately to prevent other hosts from being processed
            isAttachingToHost = true;
            AttachToHost(hit.gameObject);
        }
        else
        {
            // Hit something that's not a host - apply bounce and friction physics
            if (enableBounce && bounciness > 0f)
            {
                // Calculate bounce direction
                Vector3 normal = hit.normal;
                Vector3 reflectedVelocity = Vector3.Reflect(launchVelocity, normal);

                // Apply bounciness factor
                launchVelocity = reflectedVelocity * bounciness;

                // Apply wall friction if enabled and hitting a wall (not ground)
                if (enableFriction && wallFriction > 0f && Mathf.Abs(normal.y) < 0.7f) // Not a floor/ceiling
                {
                    float frictionMultiplier = Mathf.Max(0f, 1f - (wallFriction * Time.deltaTime));
                    launchVelocity *= frictionMultiplier;
                }

                if (showDebug)
                    Debug.Log($"[Parasite] Bounced off {hit.gameObject.name} with velocity {launchVelocity.magnitude:F1}");

                // Check if velocity is too low after bounce
                if (launchVelocity.magnitude < minVelocityThreshold)
                {
                    if (showDebug)
                        Debug.Log("[Parasite] Velocity too low after bounce, stopping");
                    ResetToGroundedState();
                }
            }
            else if (showDebug)
            {
                // Hit something that's not a host - will timeout and return to ground
                Debug.Log($"[Parasite] Hit non-host object: {hit.gameObject.name}");
            }
        }
    }

    private bool IsHostHead(GameObject obj) => ((1 << obj.layer) & hostHeadLayerMask) != 0;

    private void AttachToHost(GameObject hostHead)
    {
        if (showDebug)
            Debug.Log($"[Parasite] Attempting to attach to host: {hostHead.name}");

        var hostController = hostHead.GetComponentInParent<HostController>();

        if (hostController == null)
        {
            Debug.LogError($"[Parasite] No HostController found in parent of {hostHead.name}!");
            // Reset flag if attachment fails
            isAttachingToHost = false;
            return;
        }

        // Get the actual host GameObject (the one with HostController)
        GameObject hostGameObject = hostController.gameObject;

        if (showDebug)
            Debug.Log($"[Parasite] Successfully attaching to host: {hostGameObject.name}");

        // Get the parasite camera
        Camera parasiteCamera = cameraPivot.GetComponentInChildren<Camera>();
        Transform hostCameraPivot = hostController.GetCameraPivot();

        // Play transition effect with camera transfer
        var transitionEffect = _possessionTransitionEffect != null ? _possessionTransitionEffect : PossessionTransitionEffect.CreateInstance();

        if (parasiteCamera != null && hostCameraPivot != null)
        {
            // Transfer camera during transition
            transitionEffect.PlayPossessionTransition(parasiteCamera, hostCameraPivot, () =>
            {
                // This callback happens at the midpoint (after camera transfer)
                if (hostController != null)
                    hostController.OnParasiteAttached(this);

                // Pass the actual host GameObject, not the head
                _gameStateManager.SwitchToHostMode(hostGameObject);

                this.enabled = false;
            });
        }
        else
        {
            // Fallback to original behavior if camera or pivot not found
            Debug.LogWarning("[Parasite] Camera or host pivot not found. Using fallback possession.");
            transitionEffect.PlayPossessionTransition(() =>
            {
                if (hostController != null)
                    hostController.OnParasiteAttached(this);

                // Pass the actual host GameObject, not the head
                _gameStateManager.SwitchToHostMode(hostGameObject);

                this.enabled = false;
            });
        }
    }

    private void ResetToGroundedState()
    {
        lastLandTime = Time.time;
        launchTimedOut = false;
        isLaunching = false;
        isAttachingToHost = false; // Reset attachment flag
        move += launchVelocity;
        move.y = 0;
        launchVelocity = Vector3.zero;
        if (zoneController != null)
            gravity = zoneController.Gravity;

        if (showDebug)
            Debug.Log("[Parasite] Returned to crawling state");
    }

    private void UpdateTrajectoryVisualization()
    {
        Vector3 launchDir = cameraPivot ? cameraPivot.forward : transform.forward;
        Vector3 launchVel = launchDir * launchForce;

        // Calculate the predicted landing position first
        float distanceToTarget = CalculateDistanceToTarget(launchDir);

        // Check if the trajectory would land too close (nearly at feet)
        // This is more reliable than checking pitch angle
        bool isValidDistance = distanceToTarget >= minLaunchDistance;

        // Can only launch if both angle and distance are valid
        canLaunch = isValidDistance;

        // Hide trajectory if either condition fails
        if (!isValidDistance)
        {
            trajectorySystem.HideTrajectory();
            return;
        }

        // Show trajectory only when both conditions are met
        trajectorySystem.SimulateTrajectory(
                transform.position,
     launchVel,
     gravity,
             maxLaunchDistance,
          hostHeadLayerMask,
                isValidDistance
            );
    }

    private float CalculateDistanceToTarget(Vector3 direction)
    {
        // First check for a direct hit on a target
        if (Physics.Raycast(transform.position, direction, out RaycastHit hit, maxLaunchDistance, hostHeadLayerMask))
        {
            return hit.distance;
        }

        // If no target hit, simulate trajectory to find actual landing distance
        // This prevents launching at the ground near the player
        Vector3 currentPos = transform.position;
        Vector3 currentVelocity = direction * launchForce;
        float maxSimulationTime = launchDuration;
        float timeStep = Time.fixedDeltaTime;

        for (float t = 0; t < maxSimulationTime; t += timeStep)
        {
            // Apply gravity
            currentVelocity.y += gravity * timeStep;
            Vector3 nextPos = currentPos + currentVelocity * timeStep;

            // Check if we hit anything (including ground)
            Vector3 moveDelta = nextPos - currentPos;
            if (Physics.Raycast(currentPos, moveDelta.normalized, out RaycastHit groundHit, moveDelta.magnitude, simulationLayers))
            {
                // Calculate horizontal distance to landing point
                Vector3 landingPoint = groundHit.point;
                Vector3 horizontalDelta = landingPoint - transform.position;
                horizontalDelta.y = 0; // Only care about horizontal distance
                return horizontalDelta.magnitude;
            }

            currentPos = nextPos;

            // Safety check: if traveled too far horizontally, return that distance
            Vector3 traveledHorizontal = currentPos - transform.position;
            traveledHorizontal.y = 0;
            if (traveledHorizontal.magnitude > maxLaunchDistance)
            {
                return traveledHorizontal.magnitude;
            }
        }

        // No collision found - return max distance
        return maxLaunchDistance;
    }

    private void OnDrawGizmos()
    {
        if (!showDebug || !Application.isPlaying) return;

        if (!isLaunching && cameraPivot)
        {
            Gizmos.color = aimColor;
            Vector3 aimDir = cameraPivot.forward;
            Gizmos.DrawRay(transform.position, aimDir * maxLaunchDistance);

            // Draw minimum launch distance sphere
            Gizmos.color = tooCloseColor;
            Gizmos.DrawWireSphere(transform.position, minLaunchDistance);
        }

        if (isLaunching)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, launchVelocity.normalized * 2f);
        }
    }

    //private void OnGUI()
    //{
    //    if (!showDebug || _inputManager == null) return;

    //    float cooldownRemaining = Mathf.Max(0, launchCooldown - (Time.time - lastLaunchTime));

    //    // Determine aim status
    //    string aimStatus;
    //    Color statusColor;

    //    if (!isAiming)
    //    {
    //        aimStatus = "Not Aiming";
    //        statusColor = Color.white;
    //    }
    //    else if (!canLaunch)
    //    {
    //        aimStatus = "TOO CLOSE";
    //        statusColor = Color.red;
    //    }
    //    else
    //    {
    //        aimStatus = "READY";
    //        statusColor = Color.green;
    //    }

    //    GUI.color = statusColor;
    //    GUI.Label(new Rect(8, 8, 480, 20), $"Parasite Mode | {aimStatus} | Cooldown: {cooldownRemaining:F1}s");
    //    GUI.color = Color.white;

    //    Vector2 mv = _inputManager.ParasiteActions.Move.ReadValue<Vector2>();
    //    GUI.Label(new Rect(8, 28, 300, 20), $"Crawl input: {mv}");

    //    GUI.Label(new Rect(8, 48, 300, 20), $"Grounded: {_controller.isGrounded} | Velocity: {launchVelocity.magnitude:F1} | Gravity: {gravity:F1}");
    //    GUI.Label(new Rect(8, 68, 300, 20), $"Yaw: {yaw:F1}° | Pitch: {pitch:F1}°");

    //    // Show parasite lifetime
    //    Color lifetimeColor = currentParasiteLifetime <= lifetimeWarningThreshold ? Color.red : Color.yellow;
    //    GUI.color = lifetimeColor;
    //    GUI.Label(new Rect(8, 88, 400, 20), $"Parasite Lifetime: {currentParasiteLifetime:F1}s / {maxParasiteLifetime:F1}s");
    //    GUI.color = Color.white;

    //    if (isAiming)
    //    {
    //        float distance = CalculateDistanceToTarget(cameraPivot ? cameraPivot.forward : transform.forward);
    //        GUI.Label(new Rect(8, 108, 400, 20), $"Target Distance: {distance:F1}m (Min: {minLaunchDistance:F1}m)");
    //    }
    //}

    #region IDamageable Implementation

    /// <summary>
    /// Take damage - reduces parasite lifetime
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentParasiteLifetime -= damage;

        if (showDebug)
            Debug.Log($"[Parasite] Took {damage} damage! Remaining lifetime: {currentParasiteLifetime:F1}s");

        if (currentParasiteLifetime <= 0f)
        {
            Die();
        }
    }

    #endregion IDamageable Implementation

    #region Lifetime Management

    /// <summary>
    /// Reset parasite lifetime to max (called when exiting a host)
    /// </summary>
    public void ResetLifetime()
    {
        currentParasiteLifetime = maxParasiteLifetime;
        isDead = false;

        if (showDebug)
            Debug.Log($"[Parasite] Lifetime reset to {maxParasiteLifetime}s");
    }

    public void SetRotation(Quaternion rotation)
    {
        yaw = rotation.eulerAngles.y;
        transform.rotation = rotation;
    }

    /// <summary>
    /// Full reset of parasite state (called when restarting game)
    /// </summary>
    public void ResetParasiteState()
    {
        // Reset lifetime and death state
        currentParasiteLifetime = maxParasiteLifetime;
        isDead = false;

        // Reset physics state
        isLaunching = false;
        isAiming = false;
        isAttachingToHost = false;
        launchTimedOut = false;
        launchVelocity = Vector3.zero;
        move = Vector3.zero;
        yVel = 0f;

        // Reset rotation
        yaw = transform.eulerAngles.y;
        pitch = 0f;
        roll = 0f;

        // Reset timers
        lastLaunchTime = -launchCooldown;
        lastLandTime = -slideTime;
        bobTimer = Mathf.PI / 2;

        // Ensure controller is enabled
        if (_controller != null)
            _controller.enabled = true;

        // Ensure this component is enabled
        enabled = true;

        // Hide trajectory
        if (trajectorySystem != null)
            trajectorySystem.HideTrajectory();

        if (showDebug)
            Debug.Log("[Parasite] Full state reset complete");
    }

    /// <summary>
    /// Add time to parasite lifetime
    /// </summary>
    public void AddLifetime(float amount)
    {
        currentParasiteLifetime = Mathf.Min(currentParasiteLifetime + amount, maxParasiteLifetime);

        if (showDebug)
            Debug.Log($"[Parasite] Added {amount}s lifetime. New lifetime: {currentParasiteLifetime:F1}s");
    }

    /// <summary>
    /// Get current parasite lifetime
    /// </summary>
    public float GetRemainingLifetime() => currentParasiteLifetime;

    /// <summary>
    /// Get lifetime as percentage
    /// </summary>
    public float GetLifetimePercentage() => currentParasiteLifetime / maxParasiteLifetime;

    /// <summary>
    /// Check if parasite is dead
    /// </summary>
    public bool IsDead() => isDead;

    private void Die()
    {
        if (isDead) return;

        isDead = true;

        Debug.Log("[Parasite] Parasite died - Game Over!");

        // Disable controls
        if (_inputManager != null)
            _inputManager.DisableAllActions();

        // Disable _controller
        if (_controller != null)
            _controller.enabled = false;

        // Hide trajectory
        if (trajectorySystem != null)
            trajectorySystem.HideTrajectory();

        // Notify game manager
        _gameStateManager.GameOver();

        // Optional: Add death effect, animation, etc.
    }

    #endregion Lifetime Management

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

    [ContextMenu("Take 25s Damage")]
    public void TakeDamage25Seconds()
    {
        TakeDamage(25f);
    }

    [ContextMenu("Take Half Lifetime Damage")]
    public void TakeHalfLifetimeDamage()
    {
        float damage = currentParasiteLifetime * 0.5f;
        TakeDamage(damage);
    }

    [ContextMenu("Kill Parasite")]
    public void KillParasiteFromInspector()
    {
        TakeDamage(currentParasiteLifetime);
    }

    [ContextMenu("Show Parasite Lifetime")]
    public void ShowParasiteLifetime()
    {
        Debug.Log($"[Parasite] Lifetime: {currentParasiteLifetime:F1}s / {maxParasiteLifetime:F1}s ({GetLifetimePercentage() * 100f:F1}%)");
    }

    [ContextMenu("Reset Parasite Lifetime")]
    public void ResetLifetimeFromInspector()
    {
        ResetLifetime();
    }

    [ContextMenu("Add 10s Lifetime")]
    public void Add10SecondsLifetime()
    {
        AddLifetime(10f);
    }

    #endregion Inspector Test Functions

    /// <summary>
    /// Called when voluntarily exiting a host - launches the parasite with given velocity
    /// </summary>
    public void ExitLaunch(Vector3 velocity)
    {
        launchVelocity = velocity;
        isLaunching = true;
        isAttachingToHost = false; // Reset attachment flag for new launch
        lastLaunchTime = Time.time;
        launchStartTime = Time.time;
        gravity *= startGravityMultiplier;

        if (showDebug)
            Debug.Log($"[Parasite] Exit launched from host! Velocity: {velocity}");
    }
}