using UnityEngine;

/// <summary>
/// Scriptable Object for ranged weapon configuration.
/// Create different profiles for pistol, rifle, shotgun, sniper, etc.
/// Easily share and reuse weapon configurations.
/// </summary>
[CreateAssetMenu(fileName = "RangedWeaponProfile", menuName = "Weapons/Ranged Weapon Profile")]
public class RangedWeaponProfile : ScriptableObject
{
    [Header("Profile Info")]
    public string weaponName = "Default Weapon";

    [TextArea(2, 4)]
    public string description = "Default ranged weapon configuration";

    [Header("Shooting Stats")]
    [Tooltip("Time between shots in seconds")]
    public float fireRate = 0.15f;

    [Tooltip("Maximum damage per shot")]
    public float damage = 10f;

    [Tooltip("Maximum effective range")]
    public float range = 100f;

    [Tooltip("What layers can be hit by bullets")]
    public LayerMask hitLayers = ~0;

    [Header("Ammo")]
    public int magazineSize = 30;

    public int startingAmmo = 30; // How much ammo in mag at start
    public int reserveAmmo = 90;

    [Header("Reload")]
    [Tooltip("Time to complete reload in seconds")]
    public float reloadTime = 2f;

    [Header("Recoil")]
    public float recoilAmount = 2f;

    public float recoilSpeed = 10f;
    public float recoilReturnSpeed = 5f;
    public Vector3 recoilRotation = new Vector3(-2f, 0f, 0f);
    public Vector3 recoilKickback = new Vector3(0f, 0f, -0.05f);

    [Header("Weapon Sway")]
    public float swayAmount = 0.02f;

    public float swaySpeed = 2f;
    public float swayResetSpeed = 2f;
    public float bobSpeed = 5f;
    public float bobAmount = 0.05f;

    [Header("Aim Down Sights (ADS)")]
    [Tooltip("Enable ADS zoom feature")]
    public bool enableADS = true;

    [Tooltip("Target FOV when aiming (lower = more zoom). Default camera FOV is usually 60-90")]
    [Range(20f, 90f)]
    public float aimFOV = 40f;

    [Tooltip("How fast to zoom in/out")]
    public float aimSpeed = 8f;

    [Tooltip("Weapon position offset when aiming (relative to current position)")]
    public Vector3 aimPositionOffset = new Vector3(0f, -0.1f, 0.2f);

    [Tooltip("Reduce sway when aiming")]
    [Range(0f, 1f)]
    public float aimSwayMultiplier = 0.3f;

    [Tooltip("Reduce recoil when aiming")]
    [Range(0f, 1f)]
    public float aimRecoilMultiplier = 0.7f;

    [Header("Visual Effects")]
    public ParticleSystem muzzleFlashPrefab;

    public GameObject impactEffectPrefab;
    public GameObject bulletTrailPrefab;

    [Header("Audio")]
    public AudioClip shootSound;

    public AudioClip reloadSound;
    public AudioClip emptySound;

    [Header("Feedback")]
    [Tooltip("Optional: Override feedback profile for this weapon")]
    public ShootingFeedbackProfile feedbackProfile;

    #region Preset Factory Methods

    /// <summary>
    /// Create a pistol preset with light, fast shooting
    /// </summary>
    public static RangedWeaponProfile CreatePistolPreset()
    {
        var preset = CreateInstance<RangedWeaponProfile>();
        preset.weaponName = "Pistol";
        preset.description = "Light, semi-automatic pistol";

        preset.fireRate = 0.2f;
        preset.damage = 15f;
        preset.range = 50f;
        preset.magazineSize = 12;
        preset.startingAmmo = 12;
        preset.reserveAmmo = 48;
        preset.reloadTime = 1.5f;

        preset.recoilAmount = 1.5f;
        preset.recoilRotation = new Vector3(-1.5f, 0f, 0f);
        preset.recoilKickback = new Vector3(0f, 0f, -0.03f);

        // ADS settings
        preset.enableADS = true;
        preset.aimFOV = 50f;
        preset.aimSpeed = 10f;
        preset.aimPositionOffset = new Vector3(0f, -0.05f, 0.15f);

        return preset;
    }

    /// <summary>
    /// Create a rifle preset with automatic fire
    /// </summary>
    public static RangedWeaponProfile CreateRiflePreset()
    {
        var preset = CreateInstance<RangedWeaponProfile>();
        preset.weaponName = "Assault Rifle";
        preset.description = "Automatic rifle with medium damage";

        preset.fireRate = 0.1f;
        preset.damage = 20f;
        preset.range = 100f;
        preset.magazineSize = 30;
        preset.startingAmmo = 30;
        preset.reserveAmmo = 120;
        preset.reloadTime = 2.0f;

        preset.recoilAmount = 2f;
        preset.recoilRotation = new Vector3(-2f, 0f, 0f);
        preset.recoilKickback = new Vector3(0f, 0f, -0.05f);

        // ADS settings
        preset.enableADS = true;
        preset.aimFOV = 45f;
        preset.aimSpeed = 8f;
        preset.aimPositionOffset = new Vector3(0f, -0.08f, 0.2f);

        return preset;
    }

    /// <summary>
    /// Create a shotgun preset with heavy damage, slow fire
    /// </summary>
    public static RangedWeaponProfile CreateShotgunPreset()
    {
        var preset = CreateInstance<RangedWeaponProfile>();
        preset.weaponName = "Shotgun";
        preset.description = "Heavy damage pump-action shotgun";

        preset.fireRate = 0.8f;
        preset.damage = 60f;
        preset.range = 30f;
        preset.magazineSize = 8;
        preset.startingAmmo = 8;
        preset.reserveAmmo = 32;
        preset.reloadTime = 2.5f;

        preset.recoilAmount = 4f;
        preset.recoilRotation = new Vector3(-4f, 0f, 0f);
        preset.recoilKickback = new Vector3(0f, 0f, -0.1f);

        // ADS settings
        preset.enableADS = true;
        preset.aimFOV = 50f;
        preset.aimSpeed = 6f;
        preset.aimPositionOffset = new Vector3(0f, -0.1f, 0.25f);

        return preset;
    }

    /// <summary>
    /// Create a sniper preset with high damage, slow fire
    /// </summary>
    public static RangedWeaponProfile CreateSniperPreset()
    {
        var preset = CreateInstance<RangedWeaponProfile>();
        preset.weaponName = "Sniper Rifle";
        preset.description = "High-damage, long-range precision rifle";

        preset.fireRate = 1.2f;
        preset.damage = 100f;
        preset.range = 300f;
        preset.magazineSize = 5;
        preset.startingAmmo = 5;
        preset.reserveAmmo = 20;
        preset.reloadTime = 3.0f;

        preset.recoilAmount = 5f;
        preset.recoilRotation = new Vector3(-5f, 0f, 0f);
        preset.recoilKickback = new Vector3(0f, 0f, -0.15f);

        // ADS settings - sniper has most zoom
        preset.enableADS = true;
        preset.aimFOV = 25f; // High zoom
        preset.aimSpeed = 5f; // Slower zoom for precision
        preset.aimPositionOffset = new Vector3(0f, -0.15f, 0.3f);
        preset.aimSwayMultiplier = 0.1f; // Very stable when aimed
        preset.aimRecoilMultiplier = 0.5f; // Less recoil when aimed

        return preset;
    }

    /// <summary>
    /// Create an SMG preset with very fast fire, low damage
    /// </summary>
    public static RangedWeaponProfile CreateSMGPreset()
    {
        var preset = CreateInstance<RangedWeaponProfile>();
        preset.weaponName = "SMG";
        preset.description = "High rate of fire submachine gun";

        preset.fireRate = 0.06f;
        preset.damage = 12f;
        preset.range = 40f;
        preset.magazineSize = 40;
        preset.startingAmmo = 40;
        preset.reserveAmmo = 160;
        preset.reloadTime = 1.8f;

        preset.recoilAmount = 1f;
        preset.recoilRotation = new Vector3(-1f, 0f, 0f);
        preset.recoilKickback = new Vector3(0f, 0f, -0.02f);

        // ADS settings
        preset.enableADS = true;
        preset.aimFOV = 55f;
        preset.aimSpeed = 12f; // Fast zoom for close quarters
        preset.aimPositionOffset = new Vector3(0f, -0.05f, 0.15f);

        return preset;
    }

    #endregion Preset Factory Methods
}