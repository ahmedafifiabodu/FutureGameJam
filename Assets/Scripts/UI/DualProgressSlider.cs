using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A custom slider that progresses from the center outward in both directions.
/// Perfect for displaying time-based values that feel balanced and symmetrical.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class DualProgressSlider : MonoBehaviour
{
    [Header("Fill References")]
    [SerializeField] private Image leftFillImage;

    [SerializeField] private Image rightFillImage;

    [Header("Settings")]
    [SerializeField] private Color fillColor = Color.green;

    [SerializeField] private Color backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
    [SerializeField] private bool smoothTransition = true;
    [SerializeField] private float transitionSpeed = 5f;

    [Header("Optional Background")]
    [SerializeField] private Image backgroundImage;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    private float currentProgress = 1f;
    private float targetProgress = 1f;

    private void Awake()
    {
        ValidateComponents();
        InitializeSlider();
    }

    private void ValidateComponents()
    {
        if (!leftFillImage || !rightFillImage)
        {
            Debug.LogError($"[DualProgressSlider] Missing fill images on {gameObject.name}! Please assign them in the inspector or run the setup wizard.");
        }
    }

    private void InitializeSlider()
    {
        if (leftFillImage)
        {
            leftFillImage.color = fillColor;
            leftFillImage.type = Image.Type.Filled;
            leftFillImage.fillMethod = Image.FillMethod.Horizontal;
            leftFillImage.fillOrigin = (int)Image.OriginHorizontal.Right; // Fill from right to left

            if (showDebugLogs)
                Debug.Log($"[DualProgressSlider] Left fill initialized: {leftFillImage.name}");
        }

        if (rightFillImage)
        {
            rightFillImage.color = fillColor;
            rightFillImage.type = Image.Type.Filled;
            rightFillImage.fillMethod = Image.FillMethod.Horizontal;
            rightFillImage.fillOrigin = (int)Image.OriginHorizontal.Left; // Fill from left to right

            if (showDebugLogs)
                Debug.Log($"[DualProgressSlider] Right fill initialized: {rightFillImage.name}");
        }

        if (backgroundImage)
        {
            backgroundImage.color = backgroundColor;
        }

        UpdateFillAmounts(1f);

        if (showDebugLogs)
            Debug.Log($"[DualProgressSlider] Initialized on {gameObject.name} with progress: 1.0");
    }

    private void Update()
    {
        if (smoothTransition && !Mathf.Approximately(currentProgress, targetProgress))
        {
            currentProgress = Mathf.Lerp(currentProgress, targetProgress, Time.deltaTime * transitionSpeed);
            UpdateFillAmounts(currentProgress);

            if (showDebugLogs)
                Debug.Log($"[DualProgressSlider] Updating: current={currentProgress:F2}, target={targetProgress:F2}");
        }
    }

    /// <summary>
    /// Set the progress value (0 to 1)
    /// </summary>
    public void SetProgress(float progress)
    {
        targetProgress = Mathf.Clamp01(progress);

        if (showDebugLogs)
            Debug.Log($"[DualProgressSlider] SetProgress called: {progress:F2} -> {targetProgress:F2} on {gameObject.name}");

        if (!smoothTransition)
        {
            currentProgress = targetProgress;
            UpdateFillAmounts(currentProgress);
        }
    }

    /// <summary>
    /// Set the progress immediately without smooth transition
    /// </summary>
    public void SetProgressImmediate(float progress)
    {
        targetProgress = Mathf.Clamp01(progress);
        currentProgress = targetProgress;
        UpdateFillAmounts(currentProgress);

        if (showDebugLogs)
            Debug.Log($"[DualProgressSlider] SetProgressImmediate: {progress:F2} on {gameObject.name}");
    }

    /// <summary>
    /// Update the fill amounts for both sides
    /// </summary>
    private void UpdateFillAmounts(float progress)
    {
        if (leftFillImage)
        {
            leftFillImage.fillAmount = progress;

            if (showDebugLogs && Time.frameCount % 60 == 0) // Log every 60 frames to avoid spam
                Debug.Log($"[DualProgressSlider] Left fill amount: {progress:F2}");
        }

        if (rightFillImage)
        {
            rightFillImage.fillAmount = progress;

            if (showDebugLogs && Time.frameCount % 60 == 0)
                Debug.Log($"[DualProgressSlider] Right fill amount: {progress:F2}");
        }
    }

    /// <summary>
    /// Set the color of both fill images
    /// </summary>
    public void SetColor(Color color)
    {
        fillColor = color;

        if (leftFillImage)
            leftFillImage.color = color;

        if (rightFillImage)
            rightFillImage.color = color;

        if (showDebugLogs)
            Debug.Log($"[DualProgressSlider] Color set to: {color} on {gameObject.name}");
    }

    /// <summary>
    /// Set the background color
    /// </summary>
    public void SetBackgroundColor(Color color)
    {
        backgroundColor = color;

        if (backgroundImage)
            backgroundImage.color = color;
    }

    /// <summary>
    /// Enable or disable smooth transitions
    /// </summary>
    public void SetSmoothTransition(bool enabled)
    {
        smoothTransition = enabled;
    }

    /// <summary>
    /// Set the transition speed
    /// </summary>
    public void SetTransitionSpeed(float speed)
    {
        transitionSpeed = Mathf.Max(0.1f, speed);
    }

    /// <summary>
    /// Get current progress value
    /// </summary>
    public float GetProgress() => currentProgress;

    /// <summary>
    /// Get target progress value
    /// </summary>
    public float GetTargetProgress() => targetProgress;

    #region Editor Helper Methods

    /// <summary>
    /// Called by editor scripts to setup the slider structure
    /// </summary>
    public void EditorSetup(Image left, Image right, Image background)
    {
        leftFillImage = left;
        rightFillImage = right;
        backgroundImage = background;

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    #endregion Editor Helper Methods

    #region Debug Helpers

    [ContextMenu("Test Progress 0%")]
    private void TestProgress0()
    {
        SetProgressImmediate(0f);
    }

    [ContextMenu("Test Progress 25%")]
    private void TestProgress25()
    {
        SetProgressImmediate(0.25f);
    }

    [ContextMenu("Test Progress 50%")]
    private void TestProgress50()
    {
        SetProgressImmediate(0.5f);
    }

    [ContextMenu("Test Progress 75%")]
    private void TestProgress75()
    {
        SetProgressImmediate(0.75f);
    }

    [ContextMenu("Test Progress 100%")]
    private void TestProgress100()
    {
        SetProgressImmediate(1f);
    }

    [ContextMenu("Log Current State")]
    private void LogCurrentState()
    {
        Debug.Log($"[DualProgressSlider] State for {gameObject.name}:");
        Debug.Log($"  Current Progress: {currentProgress:F2}");
        Debug.Log($"  Target Progress: {targetProgress:F2}");
        Debug.Log($"  Smooth Transition: {smoothTransition}");
        Debug.Log($"  Transition Speed: {transitionSpeed}");
        Debug.Log($"  Left Fill: {(leftFillImage ? leftFillImage.fillAmount.ToString("F2") : "NULL")}");
        Debug.Log($"  Right Fill: {(rightFillImage ? rightFillImage.fillAmount.ToString("F2") : "NULL")}");
        Debug.Log($"  Fill Color: {fillColor}");
    }

    #endregion Debug Helpers
}