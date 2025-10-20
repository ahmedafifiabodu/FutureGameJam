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
    [SerializeField] private ParticleSystem muzzleFlash;
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
    private int currentAmmo;
    private int reserveAmmo;
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

    // Store base positions (set from prefab)
    private Vector3 weaponHolderBasePosition;
    private Quaternion weaponHolderBaseRotation;
    private bool hasStoredBaseTransform = false;

    // ADS (Aim Down Sights) tracking
    private bool isAiming = false;
    private float defaultFOV;
    private float currentFOV;
    private Vector3 defaultWeaponPosition;
    private Vector3 aimWeaponPosition;
    private bool hasStoredDefaultFOV = false;

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

        // Initialize ammo from profile
        currentAmmo = weaponProfile.startingAmmo;
        reserveAmmo = weaponProfile.reserveAmmo;

        if (!audioSource)
            audioSource = GetComponent<AudioSource>();

        if (!playerCamera)
            playerCamera = Camera.main;

        if (!feedbackSystem)
            feedbackSystem = FindObjectOfType<ShootingFeedbackSystem>();

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

        // Store default camera FOV
        if (playerCamera && !hasStoredDefaultFOV)
        {
            defaultFOV = playerCamera.fieldOfView;
            currentFOV = defaultFOV;
            hasStoredDefaultFOV = true;
        }

        // Cache animation parameter hashes
        reloadingHash = Animator.StringToHash(GameConstant.AnimationParameters.Reloading);
        aimingHash = Animator.StringToHash(GameConstant.AnimationParameters.Aiming);
    }

    public override void Update()
    {
        if (!isEquipped || !weaponProfile) return;

        HandleShooting();
        HandleReload();
        HandleADS(); // Handle zoom before recoil/sway

        // Coordinate with WeaponRecoilController
        if (weaponRecoilController)
        {
            // Toggle bob based on aiming state (disable when aiming for stability)
            bool shouldEnableBob = !isAiming;
            
            if (weaponRecoilController.enabled != shouldEnableBob)
            {
                weaponRecoilController.enabled = shouldEnableBob;
            }

            // Choose which recoil handler to use based on whether bob is active
            if (weaponRecoilController.enabled)
            {
                // WeaponRecoilController handles camera recoil and weapon bob
                // RangedWeapon handles only shooting recoil (rotation)
                HandleRecoilRotationOnly();
            }
            else
            {
                // WeaponRecoilController is disabled (aiming)
                // RangedWeapon handles everything (full control)
                HandleRecoil();
                HandleSway();
            }
        }
        else
        {
            // No WeaponRecoilController - RangedWeapon handles everything
            HandleRecoil();
            HandleSway();
        }
    }

    private void HandleShooting()
    {
        if (isReloading) return;

        // Check for fire input
        bool firePressed = inputManager.PlayerActions.Attack.IsPressed();
        bool aimPressed = inputManager.PlayerActions.Aim.IsPressed();
        bool sprintPressed = inputManager.PlayerActions.Sprint.IsPressed();

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
            if (currentAmmo > 0)
            {
                Shoot();
                nextFireTime = Time.time + weaponProfile.fireRate;
            }
            else
            {
                // Play empty sound
                PlaySound(weaponProfile.emptySound);
                nextFireTime = Time.time + 0.3f; // Prevent spam
            }
        }
    }

    private void HandleADS()
    {
        if (!weaponProfile.enableADS || !playerCamera) return;

        // Smoothly interpolate FOV
        float targetFOV = isAiming ? weaponProfile.aimFOV : defaultFOV;
        currentFOV = Mathf.Lerp(currentFOV, targetFOV, Time.deltaTime * weaponProfile.aimSpeed);
        playerCamera.fieldOfView = currentFOV;

        // Smoothly interpolate weapon position
        if (weaponHolder)
        {
            // Update base position for aim offset
            Vector3 targetPosition = isAiming ? aimWeaponPosition : defaultWeaponPosition;
            weaponHolderBasePosition = Vector3.Lerp(weaponHolderBasePosition, targetPosition, Time.deltaTime * weaponProfile.aimSpeed);
            
            // Sync with WeaponRecoilController if active
            if (weaponRecoilController && weaponRecoilController.enabled)
            {
                weaponRecoilController.SetBasePosition(weaponHolderBasePosition);
            }
        }

        // Update animation
        SetAiming(isAiming);
    }

    private void Shoot()
    {
        currentAmmo--;

        // Play shoot sound
        PlaySound(weaponProfile.shootSound);

        // Play muzzle flash
        if (muzzleFlash)
            muzzleFlash.Play();

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

        // Spawn bullet trail
        if (weaponProfile.bulletTrailPrefab && muzzlePoint)
        {
            TrailRenderer trail = Instantiate(weaponProfile.bulletTrailPrefab, muzzlePoint.position, Quaternion.identity);
            StartCoroutine(SpawnTrail(trail, targetPoint));
        }

        // Eject shell casing
        if (shellEjector)
            shellEjector.EjectShell();
    }

    private System.Collections.IEnumerator SpawnTrail(TrailRenderer trail, Vector3 targetPoint)
    {
        float time = 0;
        Vector3 startPosition = trail.transform.position;

        while (time < 1f)
        {
            trail.transform.position = Vector3.Lerp(startPosition, targetPoint, time);
            time += Time.deltaTime / trail.time;
            yield return null;
        }

        trail.transform.position = targetPoint;
        Destroy(trail.gameObject, trail.time);
    }

    private void HandleReload()
    {
        // Auto reload when empty
        if (currentAmmo == 0 && reserveAmmo > 0 && !isReloading)
        {
            StartReload();
        }

        // Manual reload
        if (inputManager.PlayerActions.Interact.triggered && currentAmmo < weaponProfile.magazineSize && reserveAmmo > 0 && !isReloading)
        {
            StartReload();
        }

        // Process reload timer
        if (isReloading)
        {
            reloadTimer -= Time.deltaTime;
            if (reloadTimer <= 0f)
            {
                CompleteReload();
            }
        }
    }

    private void StartReload()
    {
        isReloading = true;
        reloadTimer = weaponProfile.reloadTime;

        // Play reload sound
        PlaySound(weaponProfile.reloadSound);

        // Set animation parameter using cached hash
        if (weaponAnimator)
            weaponAnimator.SetBool(reloadingHash, true);

        Debug.Log($"[Weapon] Reloading... ({reserveAmmo} reserve ammo)");
    }

    private void CompleteReload()
    {
        int ammoNeeded = weaponProfile.magazineSize - currentAmmo;
        int ammoToReload = Mathf.Min(ammoNeeded, reserveAmmo);

        currentAmmo += ammoToReload;
        reserveAmmo -= ammoToReload;

        isReloading = false;

        // Reset animation parameter using cached hash
        if (weaponAnimator)
            weaponAnimator.SetBool(reloadingHash, false);

        Debug.Log($"[Weapon] Reload complete! {currentAmmo}/{weaponProfile.magazineSize} (Reserve: {reserveAmmo})");
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

        // Apply rotation and position
        weaponHolder.SetLocalPositionAndRotation(
            weaponHolderBasePosition + currentRecoilPosition + swayPosition,
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

    private void HandleSway()
    {
        if (!weaponHolder) return;

        // Get mouse input for sway
        Vector2 look = inputManager.PlayerActions.Look.ReadValue<Vector2>();

        // Calculate sway with ADS modifier
        float swayMultiplier = isAiming ? weaponProfile.aimSwayMultiplier : 1f;
        float swayX = -look.x * weaponProfile.swayAmount * swayMultiplier;
        float swayY = -look.y * weaponProfile.swayAmount * swayMultiplier;

        Vector3 targetSwayPosition = new Vector3(swayX, swayY, 0);
        swayPosition = Vector3.Lerp(swayPosition, targetSwayPosition, weaponProfile.swaySpeed * Time.deltaTime);

        // Reset sway when not moving mouse
        swayPosition = Vector3.Lerp(swayPosition, Vector3.zero, weaponProfile.swayResetSpeed * Time.deltaTime);
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource && clip)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    public override void Attack()
    {
        if (!isReloading && currentAmmo > 0)
        {
            Shoot();
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

        // Reinitialize ammo
        currentAmmo = weaponProfile.startingAmmo;
        reserveAmmo = weaponProfile.reserveAmmo;

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
        
        // Reset FOV when equipping
        if (playerCamera && hasStoredDefaultFOV)
        {
            currentFOV = defaultFOV;
            playerCamera.fieldOfView = defaultFOV;
        }
        
        isAiming = false;
    }

    public override void Unequip()
    {
        base.Unequip();
        
        // Reset FOV when unequipping
        if (playerCamera && hasStoredDefaultFOV)
        {
            playerCamera.fieldOfView = defaultFOV;
        }
        
        // Re-enable WeaponRecoilController if it was disabled
        if (weaponRecoilController)
        {
            weaponRecoilController.enabled = true;
        }
        
        isAiming = false;
    }

    // Public getters for UI
    public int GetCurrentAmmo() => currentAmmo;
    public int GetMagazineSize() => weaponProfile ? weaponProfile.magazineSize : 0;
    public int GetReserveAmmo() => reserveAmmo;
    public bool IsReloading() => isReloading;
    public bool IsAiming() => isAiming;
    public float GetAimingMoveSpeedMultiplier() => isAiming ? aimingMoveSpeedMultiplier : 1f;
    public float GetReloadProgress() => isReloading && weaponProfile ? 1f - (reloadTimer / weaponProfile.reloadTime) : 1f;
    public RangedWeaponProfile GetCurrentProfile() => weaponProfile;

    // Debug UI
    private void OnGUI()
    {
        if (!isEquipped || !weaponProfile) return;

        // Ammo display
        GUI.Label(new Rect(Screen.width - 220, Screen.height - 80, 200, 30),
            $"Ammo: {currentAmmo}/{weaponProfile.magazineSize}",
            new GUIStyle(GUI.skin.label) { fontSize = 20, normal = { textColor = Color.white } });

        GUI.Label(new Rect(Screen.width - 220, Screen.height - 50, 200, 30),
            $"Reserve: {reserveAmmo}",
            new GUIStyle(GUI.skin.label) { fontSize = 16, normal = { textColor = Color.gray } });

        // Aiming indicator
        if (isAiming)
        {
            GUI.Label(new Rect(Screen.width - 220, Screen.height - 110, 200, 20),
                "AIMING",
                new GUIStyle(GUI.skin.label) { fontSize = 14, normal = { textColor = Color.yellow } });
        }

        // Reload progress bar
        if (isReloading)
        {
            float barWidth = 200f;
            float barHeight = 20f;
            float barX = Screen.width - barWidth - 10f;
            float barY = Screen.height - 140f;

            GUI.Box(new Rect(barX, barY, barWidth, barHeight), "");
            GUI.Box(new Rect(barX, barY, barWidth * GetReloadProgress(), barHeight), "",
                new GUIStyle(GUI.skin.box) { normal = { background = Texture2D.whiteTexture } });

            GUI.Label(new Rect(barX, barY - 20, barWidth, 20), "RELOADING...",
                new GUIStyle(GUI.skin.label) { fontSize = 14, normal = { textColor = Color.yellow } });
        }
    }
}

/// <summary>
/// Interface for objects that can take damage
/// </summary>
public interface IDamageable
{
    void TakeDamage(float damage);
}