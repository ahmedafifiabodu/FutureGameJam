using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class ParasiteController : MonoBehaviour
{
    [Header("Crawling Movement")]
    [SerializeField] private float crawlSpeed = 2.5f;

    [Header("Launch Attack")]
    [SerializeField] private float launchForce = 15f;

    [SerializeField] private float launchCooldown = 1f;
    [SerializeField] private float maxLaunchDistance = 10f;
    [SerializeField] private float minLaunchDistance = 1.5f;
    [SerializeField] private float launchDuration = 2f;

    [Header("Host Detection")]
    [SerializeField] private LayerMask hostHeadLayerMask;

    [Header("Trajectory Visualization")]
    [SerializeField] private ParasiteLaunchTrajectory trajectorySystem;

    [SerializeField] private bool showTrajectory = true;

    [Header("Debug")]
    [SerializeField] private bool showDebug = true;

    [SerializeField] private Color aimColor = Color.red;
    [SerializeField] private Color tooCloseColor = Color.yellow;

    private CharacterController controller;
    private InputManager inputManager;
    private FirstPersonZoneController zoneController;

    // Shared settings from FirstPersonZoneController
    private Transform cameraPivot;

    private float mouseSensitivity;
    private bool lookInputIsDelta;
    private float gravity;

    private float yaw, pitch;
    private float yVel;
    private bool isLaunching;
    private bool isAiming; // Track if player is holding aim button
    private float lastLaunchTime;
    private float launchStartTime;
    private Vector3 launchVelocity;
    private bool canLaunch; // Track if target is far enough to launch

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        // Get shared settings from FirstPersonZoneController
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
    }

    private void OnEnable()
    {
        // When parasite controller is enabled, disable FirstPersonZoneController
        if (zoneController != null && zoneController.enabled)
        {
            zoneController.enabled = false;
            if (showDebug)
                Debug.Log("[Parasite] Disabled FirstPersonZoneController to avoid camera conflict");
        }
    }

    private void OnDisable()
    {
        // When parasite controller is disabled, re-enable FirstPersonZoneController
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
        inputManager = ServiceLocator.Instance.GetService<InputManager>();

        if (!cameraPivot)
            Debug.LogWarning("[Parasite] CameraPivot not found! Ensure FirstPersonZoneController has cameraPivot assigned.");

        inputManager.EnableParasiteActions();
    }

    private void Update()
    {
        if (inputManager == null) return;

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
    }

    private void Look()
    {
        if (!cameraPivot) return;

        Vector2 look = inputManager.ParasiteActions.Look.ReadValue<Vector2>();
        float mx = look.x * mouseSensitivity * (lookInputIsDelta ? 1f : Time.deltaTime);
        float my = look.y * mouseSensitivity * (lookInputIsDelta ? 1f : Time.deltaTime);

        yaw += mx;
        pitch = Mathf.Clamp(pitch - my, -60f, 60f);

        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void HandleCrawling()
    {
        Vector2 moveInput = inputManager.ParasiteActions.Move.ReadValue<Vector2>();

        if (moveInput.sqrMagnitude > 1f)
            moveInput.Normalize();

        Vector3 moveDir = (transform.right * moveInput.x + transform.forward * moveInput.y);
        moveDir.y = 0f;

        Vector3 move = moveDir * crawlSpeed;

        if (controller.isGrounded && yVel < 0f)
        {
            yVel = -2f;
        }

        yVel += gravity * Time.deltaTime;
        move.y = yVel;

        controller.Move(move * Time.deltaTime);
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
        bool attackHeld = inputManager.ParasiteActions.Attack.IsPressed();

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
                    Debug.Log("[Parasite] Target too close - launch cancelled");
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

        if (Physics.Raycast(transform.position, launchDir, out RaycastHit hit, maxLaunchDistance, hostHeadLayerMask))
        {
            launchDir = (hit.point - transform.position).normalized;
        }

        launchVelocity = launchDir * launchForce;
        isLaunching = true;
        lastLaunchTime = Time.time;
        launchStartTime = Time.time;

        if (showDebug)
            Debug.Log($"[Parasite] Launched! Direction: {launchDir}");
    }

    private void HandleLaunchMovement()
    {
        launchVelocity.y += gravity * Time.deltaTime;

        CollisionFlags flags = controller.Move(launchVelocity * Time.deltaTime);

        if ((flags & CollisionFlags.Sides) != 0 || (flags & CollisionFlags.Above) != 0)
        {
            CheckForHostCollision();
        }

        if ((flags & CollisionFlags.Below) != 0 && launchVelocity.y < 0)
        {
            ResetToGroundedState();
        }
    }

    private void CheckForLaunchTimeout()
    {
        if (Time.time - launchStartTime > launchDuration)
        {
            if (showDebug)
                Debug.Log("[Parasite] Launch timeout - returning to ground");
            ResetToGroundedState();
        }
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (!isLaunching) return;

        if (IsHostHead(hit.gameObject))
            AttachToHost(hit.gameObject);
    }

    private void CheckForHostCollision()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, 0.5f, hostHeadLayerMask);

        if (hits.Length > 0)
            AttachToHost(hits[0].gameObject);
        else
            Invoke(nameof(ResetToGroundedState), 0.3f);
    }

    private bool IsHostHead(GameObject obj) => ((1 << obj.layer) & hostHeadLayerMask) != 0;

    private void AttachToHost(GameObject hostHead)
    {
        if (showDebug)
            Debug.Log($"[Parasite] Attached to host: {hostHead.name}");

        var hostController = hostHead.GetComponentInParent<HostController>();
        if (hostController != null)
            hostController.OnParasiteAttached(this);

        GameStateManager.Instance.SwitchToHostMode(hostHead.transform.root.gameObject);

        this.enabled = false;
    }

    private void ResetToGroundedState()
    {
        isLaunching = false;
        launchVelocity = Vector3.zero;

        if (showDebug)
            Debug.Log("[Parasite] Returned to crawling state");
    }

    private void UpdateTrajectoryVisualization()
    {
        Vector3 launchDir = cameraPivot ? cameraPivot.forward : transform.forward;
        Vector3 launchVel = launchDir * launchForce;

        // Check if target is within valid distance range
        float distanceToTarget = CalculateDistanceToTarget(launchDir);
        canLaunch = distanceToTarget >= minLaunchDistance;

        // Show trajectory with color indicating if launch is valid
        trajectorySystem.SimulateTrajectory(
            transform.position,
            launchVel,
            gravity,
            maxLaunchDistance,
            hostHeadLayerMask,
            canLaunch
        );
    }

    private float CalculateDistanceToTarget(Vector3 direction)
    {
        if (Physics.Raycast(transform.position, direction, out RaycastHit hit, maxLaunchDistance, hostHeadLayerMask))
        {
            return hit.distance;
        }

        // No target found - return max distance
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

    private void OnGUI()
    {
        if (!showDebug || inputManager == null) return;

        float cooldownRemaining = Mathf.Max(0, launchCooldown - (Time.time - lastLaunchTime));

        string aimStatus = isAiming ? (canLaunch ? "READY" : "TOO CLOSE") : "Not Aiming";
        Color statusColor = isAiming ? (canLaunch ? Color.green : Color.red) : Color.white;

        GUI.color = statusColor;
        GUI.Label(new Rect(8, 8, 480, 20), $"Parasite Mode | {aimStatus} | Cooldown: {cooldownRemaining:F1}s");
        GUI.color = Color.white;

        Vector2 mv = inputManager.ParasiteActions.Move.ReadValue<Vector2>();
        GUI.Label(new Rect(8, 28, 300, 20), $"Crawl input: {mv}");

        GUI.Label(new Rect(8, 48, 300, 20), $"Grounded: {controller.isGrounded} | Velocity: {launchVelocity.magnitude:F1}");
        GUI.Label(new Rect(8, 68, 300, 20), $"Yaw: {yaw:F1}° | Pitch: {pitch:F1}°");

        if (isAiming)
        {
            float distance = CalculateDistanceToTarget(cameraPivot ? cameraPivot.forward : transform.forward);
            GUI.Label(new Rect(8, 88, 400, 20), $"Target Distance: {distance:F1}m (Min: {minLaunchDistance:F1}m)");
        }
    }
}