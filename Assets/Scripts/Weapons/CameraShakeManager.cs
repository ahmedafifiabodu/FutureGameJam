using UnityEngine;
using System.Collections;

/// <summary>
/// Manages camera shake effects for weapons and impacts.
/// Provides multiple shake patterns and intensity levels.
/// </summary>
public class CameraShakeManager : MonoBehaviour
{
    [Header("Shake Settings")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float maxShakeDuration = 0.5f;
    [SerializeField] private float shakeMultiplier = 1f;

    [Header("Noise Settings")]
    [SerializeField] private float noiseFrequency = 25f;
    [SerializeField] private float traumaPower = 2f; // Exponential falloff
    [SerializeField] private float traumaDecayRate = 1f; // How fast trauma decays per second

    [Header("Limits")]
    [SerializeField] private float maxTrauma = 1f; // Cap trauma to prevent excessive shake
    [SerializeField] private float traumaPerShot = 0.15f; // Default trauma added per shot

    [Header("Debug")]
    [SerializeField] private bool showDebug = false;

    private float trauma = 0f; // 0 to 1
    private float shakeTimer = 0f;
    private bool isShaking = false;

    // Store shake offset instead of accumulating
    private Vector3 currentShakeOffset = Vector3.zero;
    private Quaternion currentShakeRotation = Quaternion.identity;
    
    // Store base transform to apply shake as offset
    private Vector3 baseLocalPosition;
    private Quaternion baseLocalRotation;
    private bool hasStoredBase = false;

    private void Awake()
    {
        if (!cameraTransform)
            cameraTransform = Camera.main.transform;
    }

    private void LateUpdate()
    {
        // Store base transform on first frame
        if (!hasStoredBase)
        {
            StoreBaseTransform();
        }

        if (!cameraTransform)
            return;

        // Decay trauma over time
        if (trauma > 0f)
        {
            trauma = Mathf.Max(0f, trauma - Time.deltaTime * traumaDecayRate);
            ApplyShake();
        }
        else
        {
            // Reset shake when trauma is zero
            if (isShaking)
            {
                currentShakeOffset = Vector3.zero;
                currentShakeRotation = Quaternion.identity;
                ApplyShakeOffset();
                isShaking = false;
            }
        }
    }

    private void StoreBaseTransform()
    {
        if (cameraTransform)
        {
            baseLocalPosition = cameraTransform.localPosition;
            baseLocalRotation = cameraTransform.localRotation;
            hasStoredBase = true;
        }
    }

    private void ApplyShake()
    {
        isShaking = true;

        // Calculate shake intensity using trauma power curve
        float shake = Mathf.Pow(trauma, traumaPower) * shakeMultiplier;

        // Generate Perlin noise for smooth, natural shake
        float seed = Time.time * noiseFrequency;

        // Position shake (small values to prevent excessive movement)
        float offsetX = (Mathf.PerlinNoise(seed, 0f) - 0.5f) * 2f * shake;
        float offsetY = (Mathf.PerlinNoise(0f, seed) - 0.5f) * 2f * shake;
        float offsetZ = (Mathf.PerlinNoise(seed, seed) - 0.5f) * 2f * shake * 0.5f;

        currentShakeOffset = new Vector3(offsetX, offsetY, offsetZ) * 0.05f; // Reduced from 0.1f

        // Rotation shake (subtle)
        float rotX = (Mathf.PerlinNoise(seed + 10f, 0f) - 0.5f) * 2f * shake;
        float rotY = (Mathf.PerlinNoise(0f, seed + 10f) - 0.5f) * 2f * shake;
        float rotZ = (Mathf.PerlinNoise(seed + 10f, seed + 10f) - 0.5f) * 2f * shake * 0.5f;

        currentShakeRotation = Quaternion.Euler(rotX, rotY, rotZ);

        ApplyShakeOffset();
    }

    private void ApplyShakeOffset()
    {
        // Apply shake as OFFSET from base transform (NOT accumulative)
        cameraTransform.localPosition = baseLocalPosition + currentShakeOffset;
        cameraTransform.localRotation = baseLocalRotation * currentShakeRotation;
    }

    /// <summary>
    /// Add trauma to the camera shake. Values are clamped to maxTrauma.
    /// </summary>
    public void AddTrauma(float amount)
    {
        trauma = Mathf.Clamp(trauma + amount, 0f, maxTrauma);

        if (showDebug)
            Debug.Log($"[CameraShake] Trauma: {trauma:F2}");
    }

    /// <summary>
    /// Quick shake with preset intensity.
    /// </summary>
    public void Shake(ShakeIntensity intensity)
    {
        switch (intensity)
        {
            case ShakeIntensity.Light:
                AddTrauma(0.1f);
                break;
            case ShakeIntensity.Medium:
                AddTrauma(0.3f);
                break;
            case ShakeIntensity.Heavy:
                AddTrauma(0.6f);
                break;
            case ShakeIntensity.Extreme:
                AddTrauma(1f);
                break;
        }
    }

    /// <summary>
    /// Custom shake with duration and intensity.
    /// </summary>
    public void CustomShake(float intensity, float duration)
    {
        StartCoroutine(CustomShakeCoroutine(intensity, duration));
    }

    private IEnumerator CustomShakeCoroutine(float intensity, float duration)
    {
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float currentIntensity = intensity * (1f - t); // Fade out
            AddTrauma(currentIntensity * Time.deltaTime * 2f);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    /// <summary>
    /// Stop all shaking immediately.
    /// </summary>
    public void StopShake()
    {
        trauma = 0f;
        currentShakeOffset = Vector3.zero;
        currentShakeRotation = Quaternion.identity;
        isShaking = false;
        
        if (hasStoredBase && cameraTransform)
        {
            cameraTransform.localPosition = baseLocalPosition;
            cameraTransform.localRotation = baseLocalRotation;
        }
    }

    /// <summary>
    /// Update base transform when camera moves (e.g., player turns)
    /// </summary>
    public void UpdateBaseTransform()
    {
        StoreBaseTransform();
    }

    // Public getters
    public float GetTrauma() => trauma;
    public bool IsShaking() => isShaking;

    private void OnGUI()
    {
        if (!showDebug) return;

        GUI.Label(new Rect(10, 100, 200, 20), 
            $"Camera Trauma: {trauma:F2}",
            new GUIStyle(GUI.skin.label) { fontSize = 14, normal = { textColor = Color.cyan } });
    }
}

/// <summary>
/// Preset shake intensity levels.
/// </summary>
public enum ShakeIntensity
{
    Light,
    Medium,
    Heavy,
    Extreme
}
