using UnityEngine;

/// <summary>
/// Scriptable Object for weapon feedback settings.
/// Create different profiles for pistol, rifle, shotgun, etc.
/// Easily share and reuse feedback configurations across weapons.
/// </summary>
[CreateAssetMenu(fileName = "ShootingFeedbackProfile", menuName = "Weapons/Shooting Feedback Profile")]
public class ShootingFeedbackProfile : ScriptableObject
{
    [Header("Profile Info")]
    public string profileName = "Default";

    [TextArea(2, 4)]
    public string description = "Default shooting feedback settings";

    [Header("Camera Shake")]
    public bool enableCameraShake = true;

    [Range(0f, 1f)]
    public float shakeIntensity = 0.08f; // Reduced from 0.15f

    public float shakeDuration = 0.1f;
    public AnimationCurve shakeIntensityCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    [Header("Screen Flash")]
    public bool enableScreenFlash = true;

    public Color flashColor = new(1f, 1f, 1f, 0.1f);
    public float flashDuration = 0.05f;

    [Header("Chromatic Aberration (Optional)")]
    public bool enableChromaticAberration = false;

    public float chromaticIntensity = 0.5f;
    public float chromaticDuration = 0.1f;

    [Header("Vignette (Optional)")]
    public bool enableVignette = false;

    public float vignetteIntensity = 0.3f;
    public float vignetteDuration = 0.15f;

    [Header("Impact Effects")]
    public GameObject impactDecalPrefab;

    public float decalLifetime = 10f;
    public int maxDecals = 50;

    [Header("Hit Marker")]
    public bool enableHitMarker = true;

    public Color hitMarkerColor = Color.white;
    public float hitMarkerSize = 20f;
    public float hitMarkerDuration = 0.2f;

    [Header("Audio")]
    public AudioClip[] impactSounds;

    public float impactSoundVolume = 0.5f;

    #region Preset Factory Methods

    /// <summary>
    /// Create a pistol preset with light, snappy feedback
    /// </summary>
    public static ShootingFeedbackProfile CreatePistolPreset()
    {
        var preset = CreateInstance<ShootingFeedbackProfile>();
        preset.profileName = "Pistol";
        preset.description = "Light, snappy feedback for pistols";

        preset.shakeIntensity = 0.06f; // Reduced from 0.1f
        preset.shakeDuration = 0.08f;
        preset.flashColor = new Color(1f, 0.95f, 0.8f, 0.08f);
        preset.flashDuration = 0.04f;
        preset.hitMarkerSize = 18f;

        return preset;
    }

    /// <summary>
    /// Create a rifle preset with medium impact feedback
    /// </summary>
    public static ShootingFeedbackProfile CreateRiflePreset()
    {
        var preset = CreateInstance<ShootingFeedbackProfile>();
        preset.profileName = "Rifle";
        preset.description = "Medium impact feedback for rifles";

        preset.shakeIntensity = 0.08f; // Reduced from 0.15f
        preset.shakeDuration = 0.1f;
        preset.flashColor = new Color(1f, 0.9f, 0.7f, 0.1f);
        preset.flashDuration = 0.05f;
        preset.hitMarkerSize = 20f;

        return preset;
    }

    /// <summary>
    /// Create a shotgun preset with heavy, punchy feedback
    /// </summary>
    public static ShootingFeedbackProfile CreateShotgunPreset()
    {
        var preset = CreateInstance<ShootingFeedbackProfile>();
        preset.profileName = "Shotgun";
        preset.description = "Heavy, punchy feedback for shotguns";

        preset.shakeIntensity = 0.2f; // Reduced from 0.35f
        preset.shakeDuration = 0.15f;
        preset.flashColor = new Color(1f, 0.8f, 0.5f, 0.15f);
        preset.flashDuration = 0.08f;
        preset.hitMarkerSize = 25f;

        return preset;
    }

    /// <summary>
    /// Create a sniper preset with heavy, sustained feedback
    /// </summary>
    public static ShootingFeedbackProfile CreateSniperPreset()
    {
        var preset = CreateInstance<ShootingFeedbackProfile>();
        preset.profileName = "Sniper";
        preset.description = "Heavy, sustained feedback for snipers";

        preset.shakeIntensity = 0.25f; // Reduced from 0.4f
        preset.shakeDuration = 0.2f;
        preset.flashColor = new Color(1f, 1f, 1f, 0.12f);
        preset.flashDuration = 0.06f;
        preset.hitMarkerSize = 22f;

        return preset;
    }

    #endregion Preset Factory Methods
}