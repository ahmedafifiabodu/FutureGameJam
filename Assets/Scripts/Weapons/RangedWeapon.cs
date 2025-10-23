using UnityEngine;

/// <summary>
/// Handles ranged weapon (FPS gun) mechanics including shooting, reloading, recoil, and muzzle flash.
/// Designed to feel responsive and satisfying.
/// All weapon stats come from RangedWeaponProfile - no redundant SerializeFields!
/// </summary>
public class RangedWeapon : WeaponBase
{
    [Header("Weapon Profile")]
    [SerializeField] private RangedWeaponProfile weaponProfile;

    [Header("Visual Effects (Instance References)")]

    [SerializeField] private Transform muzzlePoint; // Where bullets spawn from

    [Header("Shooting Feedback")]
    [SerializeField] private ShootingFeedbackSystem feedbackSystem;

    [SerializeField] private ShellEjector shellEjector;
    [SerializeField] private bool useFeedbackSystem = true;

    [Header("References")]
    [SerializeField] private Transform weaponHolder; // Parent of the weapon for recoil

    [SerializeField] private Camera playerCamera;
    [SerializeField] private AudioSource audioSource;

    [Header("Optional Components")]
    [SerializeField] private WeaponRecoilController weaponRecoilController; // Optional, disable bob if used

    [Header("ADS Movement Settings")]
    [SerializeField] private bool disableSprintWhileAiming = true; // Prevent sprint exploit

    [SerializeField] private float aimingMoveSpeedMultiplier = 0.5f; // Slow movement when aiming

    // Runtime state only (not serialized)
    private float nextFireTime;
    private bool isReloading;
    private float reloadTimer;

    // Recoil tracking
    private Vector3 currentRecoilRotation;

    private Vector3 currentRecoilPosition;
    private Vector3 targetRecoilRotation;
    private Vector3 targetRecoilPosition;

    // Sway tracking
    private Vector3 swayPosition;
    private float bobTimer = Mathf.PI / 2;

    // Store base positions (set from prefab)
    private Vector3 weaponHolderBasePosition;

    private Quaternion weaponHolderBaseRotation;
    private bool hasStoredBaseTransform = false;

    // ADS (Aim Down Sights) tracking
    private bool isAiming = false;
    private Vector3 defaultWeaponPosition;
    private Vector3 aimWeaponPosition;

    // Animation parameter hashes (cached for performance)
    private int reloadingHash;

    private int aimingHash;

    // Reference to player controller for movement restrictions
    private FirstPersonZoneController playerController;

    private void Awake()
    {
        if (!weaponProfile)
        {
            Debug.LogError("[RangedWeapon] No weapon profile assigned! Weapon will not work properly.");
            return;
        }

        if (!audioSource)
            audioSource = GetComponent<AudioSource>();

        if (!playerCamera)
            playerCamera = Camera.main;

        if (!feedbackSystem)
            feedbackSystem = FindFirstObjectByType<ShootingFeedbackSystem>();

        // Auto-find WeaponRecoilController if not assigned
        if (!weaponRecoilController)
            weaponRecoilController = GetComponent<WeaponRecoilController>();

        // Find player controller for movement restrictions
        if (!playerController)
            playerController = GetComponentInParent<FirstPersonZoneController>();

        // Use weapon's feedback profile if specified
        if (weaponProfile.feedbackProfile && feedbackSystem)
        {
            feedbackSystem.SwitchProfile(weaponProfile.feedbackProfile);
        }

        // Store weapon holder's base transform
        if (weaponHolder && !hasStoredBaseTransform)
        {
            weaponHolderBasePosition = weaponHolder.localPosition;
            weaponHolderBaseRotation = weaponHolder.localRotation;
            hasStoredBaseTransform = true;

            // Calculate aim position
            defaultWeaponPosition = weaponHolderBasePosition;
            aimWeaponPosition = weaponHolderBasePosition + weaponProfile.aimPositionOffset;

            // Update WeaponRecoilController's base position to match
            if (weaponRecoilController)
            {
                weaponRecoilController.SetBasePosition(weaponHolderBasePosition);
            }
        }

        // Cache animation parameter hashes
        reloadingHash = Animator.StringToHash(GameConstant.AnimationParameters.Reloading);
        aimingHash = Animator.StringToHash(GameConstant.AnimationParameters.Aiming);
    }

    public override void Update()
    {
        if (!isEquipped || !weaponProfile) return;

        HandleShooting();
        HandleRecoil();
    }

    private void HandleShooting()
    {
        if (isReloading) return;

        // Check for fire input
        bool firePressed = inputManager.PlayerActions.Attack.IsPressed();
        bool aimPressed = inputManager.PlayerActions.Aim.IsPressed();
        bool sprintPressed = false;

        // Prevent sprint while aiming (balance)
        if (disableSprintWhileAiming && aimPressed && sprintPressed)
        {
            // Cancel sprint when trying to aim
            // The player controller will handle this, we just update state
            Debug.Log("[RangedWeapon] Cannot sprint while aiming!");
        }

        // Update aiming state (cannot aim while sprinting)
        bool canAim = weaponProfile.enableADS && !sprintPressed;
        isAiming = aimPressed && canAim;

        if (firePressed && Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + weaponProfile.fireRate;
        }
    }

    private void Shoot()
    {

        if (weaponProfile.shootSounds.Length > 0)
            PlaySound(weaponProfile.shootSounds[Random.Range(0, weaponProfile.shootSounds.Length)]);

        // Add recoil (with ADS modifier if aiming)
        ApplyRecoil();

        // Also apply recoil to WeaponRecoilController if it exists
        if (weaponRecoilController && weaponRecoilController.enabled && !isAiming)
        {
            // Only apply camera recoil when not aiming (more stable when aimed)
            weaponRecoilController.ApplyRecoil();
        }

        // Trigger shooting feedback (camera shake, screen flash, etc.)
        if (useFeedbackSystem && feedbackSystem)
            feedbackSystem.TriggerShootFeedback();

        // Raycast for hit detection
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        Vector3 targetPoint;
        if (Physics.Raycast(ray, out RaycastHit hit, weaponProfile.range, weaponProfile.hitLayers))
        {
            targetPoint = hit.point;

            // Apply damage if target has health
            if (hit.collider.TryGetComponent<IDamageable>(out var damageable))
            {
                damageable.TakeDamage(weaponProfile.damage);

                // Trigger hit feedback (hit marker, extra shake)
                if (useFeedbackSystem && feedbackSystem)
                    feedbackSystem.TriggerHitFeedback(hit.point, hit.normal);
            }

            // Spawn impact effect
            if (weaponProfile.impactEffectPrefab)
            {
                GameObject impact = Instantiate(weaponProfile.impactEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(impact, 2f);
            }
        }
        else
        {
            targetPoint = ray.GetPoint(weaponProfile.range);
        }

        // Play muzzle flash
        if (weaponProfile.muzzleFlash)
            Instantiate(weaponProfile.muzzleFlash, muzzlePoint.position, Quaternion.LookRotation(muzzlePoint.transform.position - targetPoint));


        // Spawn bullet trail
        if (weaponProfile.bulletTrailPrefab && muzzlePoint)
        {
            TrailRenderer trail = Instantiate(weaponProfile.bulletTrailPrefab, muzzlePoint.position, Quaternion.identity).GetComponent<TrailRenderer>();
            StartCoroutine(SpawnTrail(trail, targetPoint, 50f));
        }

        // Eject shell casing
        if (shellEjector)
            shellEjector.EjectShell();
    }

    private System.Collections.IEnumerator SpawnTrail(TrailRenderer trail, Vector3 targetPoint, float speed)
    {
        Vector3 startPosition = trail.transform.position;
        float distance = Vector3.Distance(startPosition, targetPoint);
        float duration = distance / speed; // time = distance / speed
        float time = 0f;

        while (time < duration)
        {
            float t = time / duration;
            trail.transform.position = Vector3.Lerp(startPosition, targetPoint, t);
            time += Time.deltaTime;
            yield return null;
        }

        trail.transform.position = targetPoint;
        Destroy(trail.gameObject, trail.time); // still use trail.time for how long the trail stays
    }

    public override void Attack()
    {
        return;
    }

    private void ApplyRecoil()
    {
        // Apply recoil with ADS modifier if aiming
        float recoilMultiplier = isAiming ? weaponProfile.aimRecoilMultiplier : 1f;

        targetRecoilRotation += weaponProfile.recoilRotation * recoilMultiplier;
        targetRecoilPosition += weaponProfile.recoilKickback * recoilMultiplier;
    }

    private void HandleRecoil()
    {
        if (!weaponHolder) return;

        // Interpolate to target recoil
        currentRecoilRotation = Vector3.Slerp(currentRecoilRotation, targetRecoilRotation, weaponProfile.recoilSpeed * Time.deltaTime);
        currentRecoilPosition = Vector3.Slerp(currentRecoilPosition, targetRecoilPosition, weaponProfile.recoilSpeed * Time.deltaTime);

        // Return to zero
        targetRecoilRotation = Vector3.Lerp(targetRecoilRotation, Vector3.zero, weaponProfile.recoilReturnSpeed * Time.deltaTime);
        targetRecoilPosition = Vector3.Lerp(targetRecoilPosition, Vector3.zero, weaponProfile.recoilReturnSpeed * Time.deltaTime);

        // Calculate sway with ADS modifier
        float swayMultiplier = isAiming ? weaponProfile.aimSwayMultiplier : 1f;
        Vector2 look = inputManager.PlayerActions.Look.ReadValue<Vector2>();
        float swayX = -look.x * weaponProfile.swayAmount * swayMultiplier;
        float swayY = -look.y * weaponProfile.swayAmount * swayMultiplier;
        Vector3 targetSwayPosition = new(swayX, swayY, 0);
        swayPosition = Vector3.Lerp(swayPosition, targetSwayPosition, weaponProfile.swaySpeed * Time.deltaTime);
        swayPosition = Vector3.Lerp(swayPosition, Vector3.zero, weaponProfile.swayResetSpeed * Time.deltaTime);

        Vector2 moveInput = inputManager.PlayerActions.Move.ReadValue<Vector2>();
        if (moveInput != Vector2.zero)
        {
            bobTimer += weaponProfile.bobSpeed * Time.deltaTime;
        }

        if (bobTimer > Mathf.PI * 2)
        {
            bobTimer -= Mathf.PI * 2;
        }

        // Apply rotation and position
        weaponHolder.SetLocalPositionAndRotation(
            weaponHolderBasePosition + currentRecoilPosition + swayPosition + new Vector3(Mathf.Sin(bobTimer) * weaponProfile.bobAmount * 0.1f, 0,
                Mathf.Sin(bobTimer * 2f) * weaponProfile.bobAmount),
            weaponHolderBaseRotation * Quaternion.Euler(currentRecoilRotation)
        );
    }

    /// <summary>
    /// Handle only rotation recoil when WeaponRecoilController handles position (bob)
    /// </summary>
    private void HandleRecoilRotationOnly()
    {
        if (!weaponHolder) return;

        // Interpolate to target recoil
        currentRecoilRotation = Vector3.Slerp(currentRecoilRotation, targetRecoilRotation, weaponProfile.recoilSpeed * Time.deltaTime);

        // Return to zero
        targetRecoilRotation = Vector3.Lerp(targetRecoilRotation, Vector3.zero, weaponProfile.recoilReturnSpeed * Time.deltaTime);

        // Apply only rotation, let WeaponRecoilController handle position
        weaponHolder.localRotation = weaponHolderBaseRotation * Quaternion.Euler(currentRecoilRotation);
    }


    private void PlaySound(AudioClip clip)
    {
        if (audioSource && clip)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    /// <summary>
    /// Set aiming state (for animation)
    /// </summary>
    public void SetAiming(bool aiming)
    {
        if (weaponAnimator)
            weaponAnimator.SetBool(aimingHash, aiming);
    }

    /// <summary>
    /// Switch to a different weapon profile at runtime
    /// </summary>
    public void SwitchWeaponProfile(RangedWeaponProfile newProfile)
    {
        weaponProfile = newProfile;

        // Update aim positions
        if (weaponHolder)
        {
            defaultWeaponPosition = weaponHolderBasePosition;
            aimWeaponPosition = weaponHolderBasePosition + weaponProfile.aimPositionOffset;
        }

        // Switch feedback profile if specified
        if (weaponProfile.feedbackProfile && feedbackSystem)
        {
            feedbackSystem.SwitchProfile(weaponProfile.feedbackProfile);
        }

        Debug.Log($"[RangedWeapon] Switched to profile: {weaponProfile?.weaponName ?? "None"}");
    }

    public override void Equip()
    {
        base.Equip();

        isAiming = false;
    }

    public override void Unequip()
    {
        base.Unequip();

        // Re-enable WeaponRecoilController if it was disabled
        if (weaponRecoilController)
        {
            weaponRecoilController.enabled = true;
        }

        isAiming = false;
    }

    public bool IsReloading() => isReloading;

    public bool IsAiming() => isAiming;

    public float GetAimingMoveSpeedMultiplier() => isAiming ? aimingMoveSpeedMultiplier : 1f;

    public float GetReloadProgress() => isReloading && weaponProfile ? 1f - (reloadTimer / weaponProfile.reloadTime) : 1f;

    public RangedWeaponProfile GetCurrentProfile() => weaponProfile;

}
/// <summary>
/// Interface for objects that can take damage
/// </summary>
public interface IDamageable
{
    void TakeDamage(float damage);
}
