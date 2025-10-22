using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal; // URP namespace

/// <summary>
/// Configurable shooting feedback system that uses profiles.
/// All settings come from ShootingFeedbackProfile - no redundant SerializeFields!
/// Now supports URP post-processing effects: Chromatic Aberration and Vignette!
/// </summary>
public class ShootingFeedbackSystem : MonoBehaviour
{
    [Header("Feedback Profile")]
    [SerializeField] private ShootingFeedbackProfile feedbackProfile;

    [Header("References")]
    [SerializeField] private CameraShakeManager cameraShake;

    [SerializeField] private AudioSource feedbackAudioSource;

    [Header("URP Post-Processing")]
    [SerializeField] private Volume postProcessVolume; // Assign in Inspector

    [Tooltip("If true, system will try to find Volume automatically")]
    [SerializeField] private bool autoFindVolume = true;

    // Runtime state only (not in profile)
    private float screenFlashTimer = 0f;

    private float chromaticTimer = 0f;
    private float vignetteTimer = 0f;
    private float hitMarkerTimer = 0f;
    private int currentDecalCount = 0;
    private Texture2D flashTexture;

    // URP Post-Processing effects
    private ChromaticAberration chromaticAberration;

    private Vignette vignette;
    private bool hasPostProcessing = false;

    private void Awake()
    {
        if (!cameraShake)
            cameraShake = FindFirstObjectByType<CameraShakeManager>();

        if (!feedbackAudioSource)
            feedbackAudioSource = GetComponent<AudioSource>();

        // Try to find Volume if not assigned
        if (autoFindVolume && !postProcessVolume)
        {
            postProcessVolume = FindFirstObjectByType<Volume>();
        }

        // Get post-processing effects from Volume
        SetupPostProcessing();

        // Create flash texture
        flashTexture = new Texture2D(1, 1);
        flashTexture.SetPixel(0, 0, Color.white);
        flashTexture.Apply();

        if (!feedbackProfile)
        {
            Debug.LogWarning("[ShootingFeedbackSystem] No feedback profile assigned! Feedback will not work.");
        }
    }

    private void SetupPostProcessing()
    {
        if (!postProcessVolume)
        {
            Debug.LogWarning("[ShootingFeedbackSystem] No Volume assigned! Chromatic Aberration and Vignette will not work. " +
                           "Add a Volume component to your scene or assign it in the Inspector.");
            return;
        }

        VolumeProfile profile = postProcessVolume.profile;
        if (!profile)
        {
            Debug.LogWarning("[ShootingFeedbackSystem] Volume has no profile assigned!");
            return;
        }

        // Try to get existing effects
        if (!profile.TryGet(out chromaticAberration))
        {
            chromaticAberration = profile.Add<ChromaticAberration>(false);
            Debug.Log("[ShootingFeedbackSystem] Added ChromaticAberration to Volume profile");
        }

        if (!profile.TryGet(out vignette))
        {
            vignette = profile.Add<Vignette>(false);
            Debug.Log("[ShootingFeedbackSystem] Added Vignette to Volume profile");
        }

        // Initialize to zero
        if (chromaticAberration)
        {
            chromaticAberration.intensity.value = 0f;
            chromaticAberration.active = false;
        }

        if (vignette)
        {
            vignette.intensity.value = 0f;
            vignette.active = false;
        }

        hasPostProcessing = true;
        Debug.Log("[ShootingFeedbackSystem] Post-processing setup complete!");
    }

    private void Update()
    {
        if (!feedbackProfile) return;

        // Update timers and effects
        UpdateScreenFlash();
        UpdateChromaticAberration();
        UpdateVignette();
        UpdateHitMarker();
    }

    private void UpdateScreenFlash()
    {
        if (screenFlashTimer > 0f)
            screenFlashTimer -= Time.deltaTime;
    }

    private void UpdateChromaticAberration()
    {
        if (chromaticTimer > 0f)
        {
            chromaticTimer -= Time.deltaTime;

            if (hasPostProcessing && chromaticAberration)
            {
                // Calculate intensity based on timer (fade out)
                float t = chromaticTimer / feedbackProfile.chromaticDuration;
                float intensity = feedbackProfile.chromaticIntensity * t;

                chromaticAberration.intensity.value = intensity;
                chromaticAberration.active = true;
            }
        }
        else if (hasPostProcessing && chromaticAberration && chromaticAberration.active)
        {
            // Turn off when done
            chromaticAberration.intensity.value = 0f;
            chromaticAberration.active = false;
        }
    }

    private void UpdateVignette()
    {
        if (vignetteTimer > 0f)
        {
            vignetteTimer -= Time.deltaTime;

            if (hasPostProcessing && vignette)
            {
                // Calculate intensity based on timer (fade out)
                float t = vignetteTimer / feedbackProfile.vignetteDuration;
                float intensity = feedbackProfile.vignetteIntensity * t;

                vignette.intensity.value = intensity;
                vignette.active = true;
            }
        }
        else if (hasPostProcessing && vignette && vignette.active)
        {
            // Turn off when done
            vignette.intensity.value = 0f;
            vignette.active = false;
        }
    }

    private void UpdateHitMarker()
    {
        if (hitMarkerTimer > 0f)
            hitMarkerTimer -= Time.deltaTime;
    }

    /// <summary>
    /// Switch to a different profile at runtime
    /// </summary>
    public void SwitchProfile(ShootingFeedbackProfile newProfile)
    {
        feedbackProfile = newProfile;
        Debug.Log($"[ShootingFeedbackSystem] Switched to profile: {newProfile?.profileName ?? "None"}");
    }

    /// <summary>
    /// Trigger all shooting feedback effects.
    /// </summary>
    public void TriggerShootFeedback()
    {
        if (!feedbackProfile) return;

        if (feedbackProfile.enableCameraShake && cameraShake)
        {
            cameraShake.AddTrauma(feedbackProfile.shakeIntensity);
        }

        if (feedbackProfile.enableScreenFlash)
        {
            screenFlashTimer = feedbackProfile.flashDuration;
        }

        if (feedbackProfile.enableChromaticAberration)
        {
            chromaticTimer = feedbackProfile.chromaticDuration;
        }

        if (feedbackProfile.enableVignette)
        {
            vignetteTimer = feedbackProfile.vignetteDuration;
        }
    }

    /// <summary>
    /// Trigger feedback when hitting a target.
    /// </summary>
    public void TriggerHitFeedback(Vector3 hitPoint, Vector3 hitNormal)
    {
        if (!feedbackProfile) return;

        // Show hit marker
        if (feedbackProfile.enableHitMarker)
        {
            hitMarkerTimer = feedbackProfile.hitMarkerDuration;
        }

        // Spawn impact decal
        if (feedbackProfile.impactDecalPrefab && currentDecalCount < feedbackProfile.maxDecals)
        {
            GameObject decal = Instantiate(feedbackProfile.impactDecalPrefab, hitPoint, Quaternion.LookRotation(hitNormal));
            Destroy(decal, feedbackProfile.decalLifetime);
            currentDecalCount++;

            if (currentDecalCount >= feedbackProfile.maxDecals)
            {
                Invoke(nameof(ResetDecalCount), feedbackProfile.decalLifetime);
            }
        }

        // Play impact sound
        if (feedbackAudioSource && feedbackProfile.impactSounds != null && feedbackProfile.impactSounds.Length > 0)
        {
            AudioClip clip = feedbackProfile.impactSounds[Random.Range(0, feedbackProfile.impactSounds.Length)];
            feedbackAudioSource.PlayOneShot(clip, feedbackProfile.impactSoundVolume);
        }

        // Extra shake on hit
        if (feedbackProfile.enableCameraShake && cameraShake)
        {
            cameraShake.AddTrauma(feedbackProfile.shakeIntensity * 0.5f);
        }
    }

    /// <summary>
    /// Trigger heavy feedback for explosions or heavy impacts.
    /// </summary>
    public void TriggerHeavyImpact(float intensity = 1f)
    {
        if (!feedbackProfile) return;

        if (feedbackProfile.enableCameraShake && cameraShake)
        {
            cameraShake.AddTrauma(feedbackProfile.shakeIntensity * intensity * 3f);
        }

        if (feedbackProfile.enableScreenFlash)
        {
            screenFlashTimer = feedbackProfile.flashDuration * intensity;
        }

        if (feedbackProfile.enableVignette)
        {
            vignetteTimer = feedbackProfile.vignetteDuration * intensity;
        }

        if (feedbackProfile.enableChromaticAberration)
        {
            chromaticTimer = feedbackProfile.chromaticDuration * intensity;
        }
    }

    private void ResetDecalCount()
    {
        currentDecalCount = 0;
    }

    private void OnGUI()
    {
        if (!feedbackProfile) return;

        // Draw screen flash
        if (screenFlashTimer > 0f)
        {
            float alpha = (screenFlashTimer / feedbackProfile.flashDuration) * feedbackProfile.flashColor.a;
            Color color = new(
                feedbackProfile.flashColor.r,
                feedbackProfile.flashColor.g,
                feedbackProfile.flashColor.b,
                alpha
            );
            GUI.color = color;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), flashTexture);
            GUI.color = Color.white;
        }

        // Draw hit marker
        if (hitMarkerTimer > 0f)
        {
            float alpha = hitMarkerTimer / feedbackProfile.hitMarkerDuration;
            Color color = new(
                feedbackProfile.hitMarkerColor.r,
                feedbackProfile.hitMarkerColor.g,
                feedbackProfile.hitMarkerColor.b,
                alpha
            );

            float centerX = Screen.width / 2f;
            float centerY = Screen.height / 2f;
            float size = feedbackProfile.hitMarkerSize;
            float thickness = 2f;

            GUI.color = color;

            // Draw X-shaped hit marker
            GUI.DrawTexture(new Rect(centerX - size, centerY - size, size * 2, thickness), flashTexture);
            GUI.DrawTexture(new Rect(centerX - size, centerY + size, size * 2, thickness), flashTexture);

            GUI.color = Color.white;
        }
    }

    // Public getters
    public bool IsFlashing() => screenFlashTimer > 0f;

    public float GetFlashProgress() => feedbackProfile ? screenFlashTimer / feedbackProfile.flashDuration : 0f;

    public ShootingFeedbackProfile GetCurrentProfile() => feedbackProfile;

    public bool HasPostProcessing() => hasPostProcessing;
}