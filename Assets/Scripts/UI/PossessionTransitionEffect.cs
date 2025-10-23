using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Handles the visual transition effect when the parasite possesses a host.
/// Creates an organic "parasite invasion" effect with slimy, pulsating visuals.
/// </summary>
public class PossessionTransitionEffect : MonoBehaviour
{
    [Header("Transition Settings")]
    [SerializeField] private Image fadeImage;

    [SerializeField] private float fadeInDuration = 0.2f;  // Parasite entering
    [SerializeField] private float holdDuration = 0.1f;    // Possession moment
    [SerializeField] private float fadeOutDuration = 0.3f;  // Host vision clearing
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Exit Transition Settings")]
    [SerializeField] private float exitFadeInDuration = 0.25f;  // Host dying/parasite escaping

    [SerializeField] private float exitHoldDuration = 0.08f;    // Ejection moment
    [SerializeField] private float exitFadeOutDuration = 0.35f; // Parasite vision restoring

    [Header("Organic Effect Settings")]
    [SerializeField] private bool useOrganicEffect = true;

    [SerializeField] private Color parasiteColor1 = new(0.8f, 0.1f, 0.5f, 1f); // vurple
    [SerializeField] private Color parasiteColor2 = new(0.2f, 0f, 0f, 1f); //red
    [SerializeField] private Color bloodTint = new(0.8f, 0.1f, 0.1f, 0.3f); // Reddish tint
    [SerializeField] private float pulseSpeed = 8f;
    [SerializeField] private float vignetteIntensity = 0.7f;
    [SerializeField] private int pulseCount = 3; // Number of pulsing waves during transition

    [Header("Exit Effect Colors")]
    [SerializeField] private Color deathColor = new(0.2f, 0f, 0f, 1f); // Dark blood red

    [SerializeField] private Color escapeColor = new(0.8f, 0.1f, 0.5f, 1f); // Escape green
    [SerializeField] private int exitPulseCount = 4; // More frantic pulsing when escaping

    [Header("Vignette Effect")]
    [SerializeField] private Image vignetteImage;

    [SerializeField] private Sprite vignetteSprite; // Assign a radial gradient sprite

    private static PossessionTransitionEffect instance;
    private Canvas transitionCanvas;

    private void Awake()
    {
        ServiceLocator.Instance.RegisterService(this, false);

        SetupCanvas();
    }

    private void SetupCanvas()
    {
        // Get or create canvas
        transitionCanvas = GetComponent<Canvas>();
        if (transitionCanvas == null)
            transitionCanvas = gameObject.AddComponent<Canvas>();

        transitionCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        transitionCanvas.sortingOrder = 9999; // Render on top of everything

        // Setup main fade image
        if (fadeImage == null)
        {
            GameObject imageObj = new("FadeImage");
            imageObj.transform.SetParent(transform);

            fadeImage = imageObj.AddComponent<Image>();
            fadeImage.color = new Color(0, 0, 0, 0);

            RectTransform rect = fadeImage.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
        }

        // Setup vignette image for organic effect
        if (vignetteImage == null && useOrganicEffect)
        {
            GameObject vignetteObj = new("VignetteImage");
            vignetteObj.transform.SetParent(transform);

            vignetteImage = vignetteObj.AddComponent<Image>();
            vignetteImage.color = new Color(0, 0, 0, 0);

            RectTransform rect = vignetteImage.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;

            // Create a simple radial gradient if no sprite assigned
            if (vignetteSprite == null)
            {
                CreateRadialGradientTexture();
            }
            else
            {
                vignetteImage.sprite = vignetteSprite;
            }
        }

        SetAlpha(0);
        if (vignetteImage != null)
            vignetteImage.color = new Color(vignetteImage.color.r, vignetteImage.color.g, vignetteImage.color.b, 0);
    }

    private void CreateRadialGradientTexture()
    {
        // Create a simple radial gradient texture for vignette effect
        int size = 256;
        Texture2D tex = new(size, size, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[size * size];

        Vector2 center = new(size * 0.5f, size * 0.5f);
        float maxDist = Vector2.Distance(Vector2.zero, center);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.Clamp01(dist / maxDist);
                alpha = Mathf.Pow(alpha, 2f); // Make it more pronounced at edges
                pixels[y * size + x] = new Color(0, 0, 0, alpha);
            }
        }

        tex.SetPixels32(pixels, 0);
        tex.Apply();

        vignetteImage.sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    /// <summary>
    /// Triggers the possession transition effect
    /// </summary>
    /// <param name="onTransitionMidpoint">Callback invoked at the midpoint (when screen is fully black)</param>
    public void PlayPossessionTransition(System.Action onTransitionMidpoint = null)
    {
        if (useOrganicEffect)
            StartCoroutine(OrganicTransitionCoroutine(onTransitionMidpoint));
        else
            StartCoroutine(TransitionCoroutine(onTransitionMidpoint));
    }

    /// <summary>
    /// Triggers the possession transition effect with camera transfer
    /// </summary>
    /// <param name="camera">Camera to transfer between pivots</param>
    /// <param name="targetPivot">Target camera pivot (host's pivot)</param>
    /// <param name="onTransitionMidpoint">Additional callback invoked at the midpoint</param>
    public void PlayPossessionTransition(Camera camera, Transform targetPivot, System.Action onTransitionMidpoint = null)
    {
        if (useOrganicEffect)
            StartCoroutine(OrganicTransitionWithCameraTransfer(camera, targetPivot, onTransitionMidpoint));
        else
            StartCoroutine(TransitionWithCameraTransfer(camera, targetPivot, onTransitionMidpoint));
    }

    /// <summary>
    /// Triggers the exit/detachment transition effect when parasite leaves dying host
    /// </summary>
    /// <param name="onTransitionMidpoint">Callback invoked at the midpoint (parasite ejection)</param>
    public void PlayExitTransition(System.Action onTransitionMidpoint = null)
    {
        if (useOrganicEffect)
            StartCoroutine(OrganicExitTransitionCoroutine(onTransitionMidpoint));
        else
            StartCoroutine(TransitionCoroutine(onTransitionMidpoint));
    }

    /// <summary>
    /// Triggers the exit transition with camera transfer back to parasite
    /// </summary>
    /// <param name="camera">Camera to transfer back to parasite pivot</param>
    /// <param name="parasitePivot">Parasite's camera pivot</param>
    /// <param name="onTransitionMidpoint">Additional callback invoked at the midpoint</param>
    public void PlayExitTransition(Camera camera, Transform parasitePivot, System.Action onTransitionMidpoint = null)
    {
        if (useOrganicEffect)
            StartCoroutine(OrganicExitTransitionWithCameraTransfer(camera, parasitePivot, onTransitionMidpoint));
        else
            StartCoroutine(TransitionWithCameraTransfer(camera, parasitePivot, onTransitionMidpoint));
    }

    private IEnumerator OrganicTransitionCoroutine(System.Action onTransitionMidpoint)
    {
        // Phase 1: Parasite invasion - pulsing green/organic colors closing in
        yield return OrganicFadeIn(fadeInDuration);

        // Phase 2: Possession moment - brief full darkness with color pulse
        yield return new WaitForSeconds(holdDuration);

        // Invoke callback at midpoint (switch cameras here)
        onTransitionMidpoint?.Invoke();

        // Phase 3: Host vision clearing - blood/organic fade out
        yield return OrganicFadeOut(fadeOutDuration);
    }

    private IEnumerator OrganicTransitionWithCameraTransfer(Camera camera, Transform targetPivot, System.Action onTransitionMidpoint)
    {
        // Phase 1: Parasite invasion
        yield return OrganicFadeIn(fadeInDuration, camera);

        // Phase 2: Hold
        yield return new WaitForSeconds(holdDuration);

        // Transfer camera at midpoint
        if (camera != null && targetPivot != null)
        {
            TransferCamera(camera, targetPivot);
        }

        onTransitionMidpoint?.Invoke();

        // Phase 3: Host vision clearing
        yield return OrganicFadeOut(fadeOutDuration, camera);
    }

    private IEnumerator OrganicExitTransitionCoroutine(System.Action onTransitionMidpoint)
    {
        // Phase 1: Host dying - blood red pulsing, vision failing
        yield return OrganicExitFadeIn(exitFadeInDuration);

        // Phase 2: Ejection moment - brief darkness
        yield return new WaitForSeconds(exitHoldDuration);

        // Invoke callback at midpoint (camera switch, state change)
        onTransitionMidpoint?.Invoke();

        // Phase 3: Parasite vision restoring - green organic fade out
        yield return OrganicExitFadeOut(exitFadeOutDuration);
    }

    private IEnumerator OrganicExitTransitionWithCameraTransfer(Camera camera, Transform parasitePivot, System.Action onTransitionMidpoint)
    {
        // Phase 1: Host dying
        yield return OrganicExitFadeIn(exitFadeInDuration, camera);

        // Phase 2: Hold at ejection moment
        yield return new WaitForSeconds(exitHoldDuration);

        // Transfer camera back to parasite at midpoint
        if (camera != null && parasitePivot != null)
        {
            TransferCamera(camera, parasitePivot);
        }

        onTransitionMidpoint?.Invoke();

        // Phase 3: Parasite vision restoring
        yield return OrganicExitFadeOut(exitFadeOutDuration, camera);
    }

    private IEnumerator OrganicExitFadeIn(float duration, Camera camera = null)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // More frantic pulsing as host dies - using pulseSpeed
            float pulse = Mathf.Sin(t * Mathf.PI * exitPulseCount * pulseSpeed) * 0.5f + 0.5f;
            Color currentColor = Color.Lerp(bloodTint, deathColor, pulse);

            // Apply curve for smooth transition
            float alpha = fadeCurve.Evaluate(t);
            currentColor.a = alpha;

            fadeImage.color = currentColor;

            if (camera != null)
            {
                camera.fieldOfView = Mathf.Lerp(camera.fieldOfView, 175f, t);
            }

            // Vignette pulsing with death colors
            if (vignetteImage != null)
            {
                Color vignetteColor = Color.Lerp(deathColor, Color.black, t);
                vignetteColor.a = alpha * vignetteIntensity;
                vignetteImage.color = vignetteColor;
            }

            yield return null;
        }

        // Full darkness at peak (moment of death/ejection)
        fadeImage.color = new Color(1, 1, 1, 1);
        if (vignetteImage != null)
            vignetteImage.color = new Color(0, 0, 0, vignetteIntensity);
    }

    private IEnumerator OrganicExitFadeOut(float duration, Camera camera = null)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Parasite vision returning - green slime fading - using pulseSpeed
            float pulse = Mathf.Sin(t * Mathf.PI * 2f * pulseSpeed) * 0.4f + 0.6f;
            Color currentColor = Color.Lerp(escapeColor, Color.black, t * pulse);

            float alpha = 1f - fadeCurve.Evaluate(t);
            currentColor.a = alpha;

            fadeImage.color = currentColor;

            if (camera != null)
            {
                camera.fieldOfView = Mathf.Lerp(camera.fieldOfView, 90f, t);
            }

            // Vignette fading with parasite colors
            if (vignetteImage != null)
            {
                Color vignetteColor = Color.Lerp(parasiteColor1, parasiteColor2, t);
                vignetteColor.a = alpha * vignetteIntensity * 0.4f;
                vignetteImage.color = vignetteColor;
            }

            yield return null;
        }

        SetAlpha(0);
        if (vignetteImage != null)
            vignetteImage.color = new Color(vignetteImage.color.r, vignetteImage.color.g, vignetteImage.color.b, 0);
    }

    private IEnumerator OrganicFadeIn(float duration, Camera camera = null)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Pulsing between parasite colors - using pulseSpeed
            float pulse = Mathf.Sin(t * Mathf.PI * pulseCount * pulseSpeed) * 0.5f + 0.5f;
            Color currentColor = Color.Lerp(parasiteColor1, parasiteColor2, pulse);

            // Apply curve for smooth transition
            float alpha = fadeCurve.Evaluate(t);
            currentColor.a = alpha;

            fadeImage.color = currentColor;

            if (camera != null)
            {
                camera.fieldOfView = Mathf.Lerp(camera.fieldOfView, 175f, t);
            }

            // Vignette effect creeping inward
            if (vignetteImage != null)
            {
                Color vignetteColor = Color.Lerp(parasiteColor1, Color.black, t);
                vignetteColor.a = alpha * vignetteIntensity;
                vignetteImage.color = vignetteColor;
            }

            yield return null;
        }

        // Full darkness at peak
        fadeImage.color = new Color(1, 1, 1, 1);
        if (vignetteImage != null)
            vignetteImage.color = new Color(0, 0, 0, vignetteIntensity);
    }

    private IEnumerator OrganicFadeOut(float duration, Camera camera = null)
    {
        if (camera != null)
        {
            camera.fieldOfView = 180f;
        }
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Start with blood tint, clear to normal with subtle pulse - using pulseSpeed
            float pulse = Mathf.Sin(t * Mathf.PI * 2f * pulseSpeed) * 0.3f + 0.7f; // Subtle pulse
            Color currentColor = Color.Lerp(bloodTint, Color.black, t * pulse); // Apply pulse to transition

            float alpha = 1f - fadeCurve.Evaluate(t);
            currentColor.a = alpha;

            fadeImage.color = currentColor;

            if (camera != null)
            {
                camera.fieldOfView = Mathf.Lerp(camera.fieldOfView, 90f, t);
            }

            // Vignette fading out
            if (vignetteImage != null)
            {
                Color vignetteColor = parasiteColor2;
                vignetteColor.a = alpha * vignetteIntensity * 0.5f;
                vignetteImage.color = vignetteColor;
            }

            yield return null;
        }

        SetAlpha(0);
        if (vignetteImage != null)
            vignetteImage.color = new Color(vignetteImage.color.r, vignetteImage.color.g, vignetteImage.color.b, 0);
    }

    // Simple transition (original mechanical version)
    private IEnumerator TransitionCoroutine(System.Action onTransitionMidpoint)
    {
        yield return FadeToBlack(fadeInDuration);
        yield return new WaitForSeconds(holdDuration);
        onTransitionMidpoint?.Invoke();
        yield return FadeFromBlack(fadeOutDuration);
    }

    private IEnumerator TransitionWithCameraTransfer(Camera camera, Transform targetPivot, System.Action onTransitionMidpoint)
    {
        yield return FadeToBlack(fadeInDuration);
        yield return new WaitForSeconds(holdDuration);

        if (camera != null && targetPivot != null)
        {
            TransferCamera(camera, targetPivot);
        }

        onTransitionMidpoint?.Invoke();
        yield return FadeFromBlack(fadeOutDuration);
    }

    private void TransferCamera(Camera camera, Transform targetPivot)
    {
        // Reparent camera to new pivot
        camera.transform.SetParent(targetPivot, false);

        // Reset local position and rotation to match the pivot's setup
        camera.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
    }

    private IEnumerator FadeToBlack(float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float alpha = fadeCurve.Evaluate(t);
            SetAlpha(alpha);
            yield return null;
        }

        SetAlpha(1);
    }

    private IEnumerator FadeFromBlack(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float alpha = 1f - fadeCurve.Evaluate(t);
            SetAlpha(alpha);
            yield return null;
        }
        SetAlpha(0);
    }

    private void SetAlpha(float alpha)
    {
        if (fadeImage != null)
        {
            Color color = fadeImage.color;
            color.a = alpha;
            fadeImage.color = color;
        }
    }

    /// <summary>
    /// Creates a PossessionTransitionEffect instance if it doesn't exist
    /// </summary>
    public static PossessionTransitionEffect CreateInstance()
    {
        if (instance != null) return instance;

        GameObject go = new("PossessionTransitionEffect");
        return go.AddComponent<PossessionTransitionEffect>();
    }
}