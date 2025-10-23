using UnityEngine;

/// <summary>
/// Simple procedural recoil system for weapons.
/// Applies camera shake and weapon kickback for better feel.
/// FIXED: Now uses absolute rotation from base to prevent accumulation and drift.
/// </summary>
public class WeaponRecoilController : MonoBehaviour
{
    [Header("Camera Recoil")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float cameraRecoilAmount = 1f;
    [SerializeField] private float cameraRecoilSpeed = 10f;
    [SerializeField] private float cameraReturnSpeed = 5f;
    [SerializeField] private Vector2 recoilPattern = new Vector2(0f, 1f); // X = horizontal, Y = vertical

    [Header("Weapon Bob")]
    [SerializeField] private bool enableWeaponBob = true;
    [SerializeField] private float bobSpeed = 10f;
    [SerializeField] private float bobAmount = 0.05f;
    [SerializeField] private float sprintBobMultiplier = 1.5f;

    private Vector3 targetCameraRotation;
    private Vector3 currentCameraRotation;
    private float bobTimer;
    
    // Store the weapon's starting position from the prefab/scene (set in editor)
    [SerializeField] private Vector3 weaponBasePosition;
    private bool hasStoredPosition = false;

    // Store base camera rotation to apply recoil as offset
    private Quaternion baseCameraRotation;
    private bool hasStoredCameraRotation = false;

    private InputManager inputManager;
    private FirstPersonZoneController controller;

    private void Start()
    {
        if (!cameraTransform)
            cameraTransform = Camera.main.transform;

        inputManager = ServiceLocator.Instance.GetService<InputManager>();
        controller = GetComponentInParent<FirstPersonZoneController>();

        // Store the weapon's position ONLY if not already set in inspector
        if (!hasStoredPosition && transform.parent)
        {
            weaponBasePosition = transform.localPosition;
            hasStoredPosition = true;
        }
    }

    // Allow setting base position from inspector for persistence
    private void OnValidate()
    {
        if (Application.isPlaying) return;
        
        // Store position in editor so it persists to play mode
        if (transform.parent && weaponBasePosition == Vector3.zero)
        {
            weaponBasePosition = transform.localPosition;
            hasStoredPosition = true;
        }
    }

    private void LateUpdate()
    {
        // Store base camera rotation every frame (after player look has been applied)
        // This way recoil is always applied relative to current look direction
        if (cameraTransform && !hasStoredCameraRotation)
        {
            baseCameraRotation = cameraTransform.localRotation;
            hasStoredCameraRotation = true;
        }

        UpdateCameraRecoil();
        if (enableWeaponBob)
            UpdateWeaponBob();
    }

    public void ApplyRecoil()
    {
        // Add random recoil pattern
        float recoilX = Random.Range(-recoilPattern.x, recoilPattern.x) * cameraRecoilAmount;
        float recoilY = recoilPattern.y * cameraRecoilAmount;

        targetCameraRotation += new Vector3(-recoilY, recoilX, 0f);
    }

    private void UpdateCameraRecoil()
    {
        if (!cameraTransform) return;

        // Update base rotation every frame (BEFORE applying recoil)
        // This captures the player's look input
        if (currentCameraRotation.sqrMagnitude < 0.001f && targetCameraRotation.sqrMagnitude < 0.001f)
        {
            // No recoil active, update base to current rotation
            baseCameraRotation = cameraTransform.localRotation;
        }

        // Smoothly move towards target recoil
        currentCameraRotation = Vector3.Lerp(currentCameraRotation, targetCameraRotation, cameraRecoilSpeed * Time.deltaTime);
        
        // Apply recoil as OFFSET from base rotation (NOT accumulative)
        Quaternion recoilRotation = Quaternion.Euler(currentCameraRotation);
        cameraTransform.localRotation = baseCameraRotation * recoilRotation;

        // Return to zero
        targetCameraRotation = Vector3.Lerp(targetCameraRotation, Vector3.zero, cameraReturnSpeed * Time.deltaTime);
        
        // If recoil is nearly zero, update base rotation for next frame
        if (targetCameraRotation.sqrMagnitude < 0.01f)
        {
            currentCameraRotation = Vector3.Lerp(currentCameraRotation, Vector3.zero, cameraReturnSpeed * Time.deltaTime);
            if (currentCameraRotation.sqrMagnitude < 0.001f)
            {
                currentCameraRotation = Vector3.zero;
                targetCameraRotation = Vector3.zero;
                baseCameraRotation = cameraTransform.localRotation; // Store new base after recoil finishes
            }
        }
    }

    private void UpdateWeaponBob()
    {
        if (inputManager == null) return;

        Vector2 moveInput = inputManager.PlayerActions.Move.ReadValue<Vector2>();
        bool isSprinting = false;
        
        if (moveInput.magnitude > 0.1f)
        {
            // Calculate bob
            float speedMultiplier = isSprinting ? sprintBobMultiplier : 1f;
            bobTimer += Time.deltaTime * bobSpeed * speedMultiplier;

            float bobX = Mathf.Sin(bobTimer) * bobAmount * speedMultiplier;
            float bobY = Mathf.Sin(bobTimer * 2f) * bobAmount * speedMultiplier;

            // Apply bob as offset from base position
            transform.localPosition = weaponBasePosition + new Vector3(bobX, bobY, 0f);
        }
        else
        {
            // Return to base position
            bobTimer = 0f;
            transform.localPosition = Vector3.Lerp(transform.localPosition, weaponBasePosition, Time.deltaTime * 5f);
        }
    }

    /// <summary>
    /// Manually set the base weapon position (useful if position changes at runtime)
    /// </summary>
    public void SetBasePosition(Vector3 position)
    {
        weaponBasePosition = position;
        hasStoredPosition = true;
    }

    /// <summary>
    /// Reset weapon to base position immediately
    /// </summary>
    public void ResetToBasePosition()
    {
        if (hasStoredPosition)
        {
            transform.localPosition = weaponBasePosition;
            bobTimer = 0f;
        }
    }

    /// <summary>
    /// Update base camera rotation manually (useful after teleporting or major camera changes)
    /// </summary>
    public void UpdateBaseCameraRotation()
    {
        if (cameraTransform)
        {
            baseCameraRotation = cameraTransform.localRotation;
            currentCameraRotation = Vector3.zero;
            targetCameraRotation = Vector3.zero;
        }
    }
}
