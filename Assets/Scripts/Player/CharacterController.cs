using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonZoneController : MonoBehaviour
{
    [Header("View")]
    [SerializeField] private Transform cameraPivot;            // Make this a child of the Player

    [SerializeField] private float mouseSensitivity = 2.0f;    // Tuned for Mouse Delta
    [SerializeField] private bool lookInputIsDelta = true;     // True for Mouse Delta; false if using absolute stick look

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;

    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float gravity = -30f;             // Keep negative
    [SerializeField] private bool sprintAlwaysOn = false;      // No sprint input; set true to always sprint
    [SerializeField] private float sprintMultiplier = 1.6f;

    [Header("Zones")]
    [SerializeField] private BoxCollider[] allowedZones;

    [SerializeField] private bool restrictVertical = true;     // If false, only XZ containment enforced

    [Tooltip("Extra clearance from zone edges. Applied on top of CharacterController.radius.")]
    [SerializeField] private float zoneMargin = 0f;

    [Header("Safety/Debug")]
    [SerializeField] private bool forceZonesToTriggers = true;

    [SerializeField] private bool showDebug = true;
    [SerializeField] private bool debugOverlaps = true;
    [SerializeField] private int overlapDebugFrames = 90;

    [Header("Controller Tuning (optional)")]
    [SerializeField] private bool overrideControllerTuning = false;

    [SerializeField] private float tunedSlopeLimit = 45f;
    [SerializeField] private float tunedStepOffset = 0.3f;
    [SerializeField] private float tunedSkinWidth = 0.08f;
    [SerializeField] private float tunedMinMoveDistance = 0f;

    [Header("Runtime Info (read-only)")]
    [SerializeField] private Collider[] overlapBuffer = new Collider[32];

    private CharacterController controller;
    private InputManager inputManager;
    private float yaw, pitch, yVel;
    private int overlapFramesLeft;

    // Public properties for sharing with other controllers
    public Transform CameraPivot => cameraPivot;
    public float MouseSensitivity => mouseSensitivity;
    public bool LookInputIsDelta => lookInputIsDelta;
    public float Gravity => gravity;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (!cameraPivot) cameraPivot = Camera.main ? Camera.main.transform : null;

        overlapFramesLeft = overlapDebugFrames;
        if (gravity > 0) gravity = -Mathf.Abs(gravity);
    }

    private void OnValidate()
    {
        if (gravity > 0)
            gravity = -Mathf.Abs(gravity);

        if (forceZonesToTriggers && allowedZones != null)
            foreach (var b in allowedZones) if (b) b.isTrigger = true;
    }

    private void Start()
    {
        // Get InputManager from ServiceLocator
        inputManager = ServiceLocator.Instance.GetService<InputManager>();

        if (overrideControllerTuning)
        {
            controller.slopeLimit = tunedSlopeLimit;
            controller.stepOffset = tunedStepOffset;
            controller.skinWidth = tunedSkinWidth;
            controller.minMoveDistance = tunedMinMoveDistance;
        }

        if (!cameraPivot)
            Debug.LogWarning("Assign cameraPivot (a child of the Player).");
        else if (!cameraPivot.IsChildOf(transform))
            Debug.LogWarning("cameraPivot should be a CHILD of the Player for pitch to work.");

        if (forceZonesToTriggers && allowedZones != null)
        {
            foreach (var b in allowedZones)
                if (b && !b.isTrigger) { b.isTrigger = true; Debug.Log($"[ZoneFix] Set {b.name} isTrigger = true"); }
        }

        if (allowedZones != null && allowedZones.Length > 0 && !CapsuleInsideAnyZone(GetControllerWorldCenter()))
            Debug.LogWarning("Player starts outside all allowed zones.");
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        if (inputManager == null) return;

        Look();

        // Compute desired deltas (we apply deltaTime after constraints)
        Vector3 desiredH = GetHorizontalMove();
        Vector3 desiredV = ApplyGravityAndJump();

        Vector3 center = GetControllerWorldCenter();

        // Hard zone constraints (no slowdown)
        Vector3 hDelta = ConstrainHorizontal(center, desiredH * Time.deltaTime);
        Vector3 afterHCenter = center + hDelta;

        float yDelta = restrictVertical
            ? ConstrainVertical(afterHCenter, desiredV.y * Time.deltaTime)
            : desiredV.y * Time.deltaTime;

        if (restrictVertical && Mathf.Approximately(yDelta, 0f) && Mathf.Abs(yVel) > 0f)
            yVel = 0f;

        controller.Move(hDelta + new Vector3(0f, yDelta, 0f));

        if (debugOverlaps && overlapFramesLeft-- > 0) DebugOverlaps();
    }

    private void Look()
    {
        Vector2 look = inputManager.PlayerActions.Look.ReadValue<Vector2>();
        float mx = look.x * mouseSensitivity * (lookInputIsDelta ? 1f : Time.deltaTime);
        float my = look.y * mouseSensitivity * (lookInputIsDelta ? 1f : Time.deltaTime);

        yaw += mx;
        pitch = Mathf.Clamp(pitch - my, -85f, 85f);

        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        if (cameraPivot) cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private Vector3 GetHorizontalMove()
    {
        Vector2 mv = inputManager.PlayerActions.Move.ReadValue<Vector2>();
        if (mv.sqrMagnitude > 1f) mv.Normalize();

        // Check for sprint input if not always on
        bool isSprinting = sprintAlwaysOn || inputManager.PlayerActions.Sprint.IsPressed();
        float speed = moveSpeed * (isSprinting ? sprintMultiplier : 1f);

        Vector3 dir = (transform.right * mv.x + transform.forward * mv.y);
        return new Vector3(dir.x, 0f, dir.z) * speed;
    }

    private Vector3 ApplyGravityAndJump()
    {
        if (controller.isGrounded && yVel < 0f)
            yVel = -2f;

        if (controller.isGrounded && inputManager.PlayerActions.Jump.triggered)
            yVel = Mathf.Sqrt(jumpHeight * -2f * gravity);

        yVel += gravity * Time.deltaTime;
        return new Vector3(0f, yVel, 0f);
    }

    // ----------------- Zone constraints (full capsule containment) -----------------

    private Vector3 ConstrainHorizontal(Vector3 worldCenter, Vector3 hDelta)
    {
        if (hDelta == Vector3.zero) return Vector3.zero;

        bool insideNow = CapsuleInsideAnyZone(worldCenter);

        // If currently inside and the whole move stays inside, allow it
        if (insideNow && CapsuleInsideAnyZone(worldCenter + hDelta))
            return hDelta;

        // Try axis-wise slide (keeps it simple and stable)
        Vector3 dx = new(hDelta.x, 0f, 0f);
        if (!Mathf.Approximately(dx.x, 0f) && CapsuleInsideAnyZone(worldCenter + dx))
            return dx;

        Vector3 dz = new(0f, 0f, hDelta.z);
        if (!Mathf.Approximately(dz.z, 0f) && CapsuleInsideAnyZone(worldCenter + dz))
            return dz;

        // If outside or the move would exit, remove the outward component so you can move tangentially/inward
        Vector3 outward = ComputeOutwardNormalXZ(worldCenter);
        if (outward.sqrMagnitude > 1e-6f)
        {
            Vector3 slide = hDelta - outward * Mathf.Max(0f, Vector3.Dot(hDelta, outward));
            if (slide.sqrMagnitude > 1e-6f)
                return slide;
        }

        return Vector3.zero;
    }

    private float ConstrainVertical(Vector3 worldCenterAfterHorizontal, float yDelta)
    {
        if (Mathf.Approximately(yDelta, 0f)) return 0f;
        return CapsuleInsideAnyZone(worldCenterAfterHorizontal + new Vector3(0f, yDelta, 0f)) ? yDelta : 0f;
    }

    private bool CapsuleInsideAnyZone(Vector3 worldCenter)
    {
        if (allowedZones == null || allowedZones.Length == 0) return true;

        foreach (var box in allowedZones)
        {
            if (!box || !box.enabled) continue;
            if (CapsuleInsideBox(box, worldCenter, out _, out _))
                return true;
        }
        return false;
    }

    // Returns whether the entire capsule fits in 'box'. Also returns the minimal XZ inside-distance and an outward XZ normal.
    private bool CapsuleInsideBox(BoxCollider box, Vector3 worldCenter, out float minHorizInsideDist, out Vector3 horizNormalWorld)
    {
        GetCapsule(out var bottom, out var top, out float r);
        float rMargin = r + Mathf.Max(0f, zoneMargin);

        // Move the capsule endpoints by the offset from center so we can test at 'worldCenter'
        Vector3 center = GetControllerWorldCenter();
        Vector3 offset = worldCenter - center;
        Vector3 topAt = top + offset;
        Vector3 botAt = bottom + offset;

        // Transform to box local space
        var t = box.transform;
        Vector3 topL = t.InverseTransformPoint(topAt) - box.center;
        Vector3 botL = t.InverseTransformPoint(botAt) - box.center;

        Vector3 half = box.size * 0.5f;

        // Horizontal inside distances for top/bottom sphere centers
        float dxTop = half.x - Mathf.Abs(topL.x) - rMargin;
        float dzTop = half.z - Mathf.Abs(topL.z) - rMargin;
        float dxBot = half.x - Mathf.Abs(botL.x) - rMargin;
        float dzBot = half.z - Mathf.Abs(botL.z) - rMargin;

        // Vertical inside distances
        float dyTop = half.y - Mathf.Abs(topL.y) - rMargin;
        float dyBot = half.y - Mathf.Abs(botL.y) - rMargin;

        bool insideHoriz = dxTop >= 0f && dzTop >= 0f && dxBot >= 0f && dzBot >= 0f;
        bool insideVert = !restrictVertical || (dyTop >= 0f && dyBot >= 0f);

        minHorizInsideDist = Mathf.Min(dxTop, dzTop, dxBot, dzBot);

        // Outward horizontal normal toward the nearest face
        float minVal = minHorizInsideDist;
        Vector3 localNormal;

        if (minVal == dxTop)
            localNormal = new Vector3(Mathf.Sign(topL.x), 0f, 0f);
        else if (minVal == dzTop)
            localNormal = new Vector3(0f, 0f, Mathf.Sign(topL.z));
        else if (minVal == dxBot)
            localNormal = new Vector3(Mathf.Sign(botL.x), 0f, 0f);
        else
            localNormal = new Vector3(0f, 0f, Mathf.Sign(botL.z));

        horizNormalWorld = t.TransformDirection(localNormal);
        horizNormalWorld.y = 0f;

        float mag = horizNormalWorld.magnitude;
        if (mag > 1e-5f)
            horizNormalWorld /= mag;
        else horizNormalWorld = Vector3.zero;

        return insideHoriz && insideVert;
    }

    // Outward normal of the nearest boundary of the union of zones (in XZ)
    private Vector3 ComputeOutwardNormalXZ(Vector3 worldCenter)
    {
        if (allowedZones == null || allowedZones.Length == 0) return Vector3.zero;

        float bestDist = float.NegativeInfinity;
        Vector3 bestNormal = Vector3.zero;
        foreach (var box in allowedZones)
        {
            if (!box || !box.enabled)
                continue;

            CapsuleInsideBox(box, worldCenter, out float d, out Vector3 n);

            if (d > bestDist)
                bestDist = d; bestNormal = n;
        }
        return bestNormal;
    }

    // ----------------- Helpers/Debug -----------------

    private Vector3 GetControllerWorldCenter() => transform.TransformPoint(controller.center);

    private void GetCapsule(out Vector3 bottom, out Vector3 top, out float radius)
    {
        float h = Mathf.Max(controller.height, controller.radius * 2f);
        radius = Mathf.Max(0.01f, controller.radius * 0.98f);
        Vector3 center = GetControllerWorldCenter();
        Vector3 up = transform.up;
        float half = h * 0.5f;
        bottom = center - up * (half - radius);
        top = center + up * (half - radius);
    }

    private void DebugOverlaps()
    {
        GetCapsule(out var bottom, out var top, out var radius);
        int count = Physics.OverlapCapsuleNonAlloc(bottom, top, radius, overlapBuffer, ~0, QueryTriggerInteraction.Ignore);

        if (count == overlapBuffer.Length)
        {
            Debug.LogWarning($"[Overlap] Buffer full ({count}). Increase overlapBuffer size to avoid missed hits.");
        }

        for (int i = 0; i < count; i++)
        {
            var h = overlapBuffer[i];
            if (h == null) continue;
            if (h.transform == transform || h.transform.IsChildOf(transform)) continue;
            Debug.Log($"[Overlap] Touching: {h.name} (trigger={h.isTrigger}) layer={LayerMask.LayerToName(h.gameObject.layer)}");
        }
    }

    private void OnDrawGizmos()
    {
        if (allowedZones == null) return;
        foreach (var box in allowedZones)
        {
            if (!box) continue;
            var prev = Gizmos.matrix;
            Gizmos.matrix = box.transform.localToWorldMatrix;
            Gizmos.color = Color.cyan;
            float r = controller ? controller.radius : 0.5f;
            float m = Mathf.Max(0f, zoneMargin) + r; // draw where capsule center may move
            Vector3 size = box.size - new Vector3(m * 2f, restrictVertical ? m * 2f : 0f, m * 2f);
            size = new Vector3(Mathf.Max(0, size.x), Mathf.Max(0, size.y), Mathf.Max(0, size.z));
            Gizmos.DrawWireCube(box.center, size);
            Gizmos.matrix = prev;
        }
    }

    private void OnGUI()
    {
        if (!showDebug || inputManager == null) return;
        GUI.Label(new Rect(8, 8, 480, 20), $"In zone: {CapsuleInsideAnyZone(GetControllerWorldCenter())} | yVel: {yVel:F2} | gravity: {gravity:F2}");
        Vector2 mv = inputManager.PlayerActions.Move.ReadValue<Vector2>();
        GUI.Label(new Rect(8, 28, 300, 20), $"Move input: {mv}");
    }
}