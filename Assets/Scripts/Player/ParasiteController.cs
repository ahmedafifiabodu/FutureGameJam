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
    [SerializeField] private LayerMask hostLayerMask;
    [SerializeField] private float launchDuration = 2f;

    [Header("Host Detection")]
    [SerializeField] private LayerMask hostHeadLayerMask;

    [Header("Debug")]
    [SerializeField] private bool showDebug = true;

    [SerializeField] private Color aimColor = Color.red;

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
    private float lastLaunchTime;
    private float launchStartTime;
    private Vector3 launchVelocity;

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
    }

    private void Start()
    {
        inputManager = ServiceLocator.Instance.GetService<InputManager>();

        if (!cameraPivot)
            Debug.LogWarning("[Parasite] CameraPivot not found! Ensure FirstPersonZoneController has cameraPivot assigned.");

        inputManager?.EnableParasiteActions();
    }

    private void Update()
    {
        if (inputManager == null) return;

        Look();

        if (!isLaunching)
        {
            HandleCrawling();
            HandleLaunchInput();
        }
        else
        {
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

    private void HandleLaunchInput()
    {
        if (Time.time - lastLaunchTime < launchCooldown)
            return;

        bool attackPressed = inputManager.ParasiteActions.Attack.triggered;
        bool interactPressed = inputManager.ParasiteActions.Interact.triggered;

        if (attackPressed || interactPressed)
        {
            LaunchAtTarget();
        }
    }

    private void LaunchAtTarget()
    {
        Vector3 launchDir = cameraPivot ? cameraPivot.forward : transform.forward;

        if (Physics.Raycast(transform.position, launchDir, out RaycastHit hit, maxLaunchDistance, hostLayerMask))
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
        {
            AttachToHost(hit.gameObject);
        }
    }

    private void CheckForHostCollision()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, 0.5f, hostHeadLayerMask);

        if (hits.Length > 0)
        {
            AttachToHost(hits[0].gameObject);
        }
        else
        {
            Invoke(nameof(ResetToGroundedState), 0.3f);
        }
    }

    private bool IsHostHead(GameObject obj)
    {
        return ((1 << obj.layer) & hostHeadLayerMask) != 0;
    }

    private void AttachToHost(GameObject hostHead)
    {
        if (showDebug)
            Debug.Log($"[Parasite] Attached to host: {hostHead.name}");

        var hostController = hostHead.GetComponentInParent<HostController>();
        if (hostController != null)
        {
            hostController.OnParasiteAttached(this);
        }

        GameStateManager.Instance?.SwitchToHostMode(hostHead.transform.root.gameObject);

        this.enabled = false;
    }

    private void ResetToGroundedState()
    {
        isLaunching = false;
        launchVelocity = Vector3.zero;

        if (showDebug)
            Debug.Log("[Parasite] Returned to crawling state");
    }

    private void OnDrawGizmos()
    {
        if (!showDebug || !Application.isPlaying) return;

        if (!isLaunching && cameraPivot)
        {
            Gizmos.color = aimColor;
            Vector3 aimDir = cameraPivot.forward;
            Gizmos.DrawRay(transform.position, aimDir * maxLaunchDistance);
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
        GUI.Label(new Rect(8, 8, 480, 20), $"Parasite Mode | Launching: {isLaunching} | Cooldown: {cooldownRemaining:F1}s");

        Vector2 mv = inputManager.ParasiteActions.Move.ReadValue<Vector2>();
        GUI.Label(new Rect(8, 28, 300, 20), $"Crawl input: {mv}");

        GUI.Label(new Rect(8, 48, 300, 20), $"Grounded: {controller.isGrounded} | Velocity: {launchVelocity.magnitude:F1}");
        GUI.Label(new Rect(8, 68, 300, 20), $"Yaw: {yaw:F1}° | Pitch: {pitch:F1}°");
    }
}