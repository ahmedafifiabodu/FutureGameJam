using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Canvas-based crosshair system that replaces OnGUI implementation.
/// Supports dynamic spreading based on movement and shooting.
/// Automatically updates weapon reference when switching hosts.
/// </summary>
public class CanvasCrosshair : MonoBehaviour
{
    [Header("Crosshair Elements")]
    [SerializeField] private RectTransform topLine;

    [SerializeField] private RectTransform bottomLine;
    [SerializeField] private RectTransform leftLine;
    [SerializeField] private RectTransform rightLine;
    [SerializeField] private Image centerDot;

    [Header("Settings")]
    [SerializeField] private bool showCrosshair = true;

    [SerializeField] private Color crosshairColor = Color.white;
    [SerializeField] private float baseSize = 10f;
    [SerializeField] private float thickness = 2f;
    [SerializeField] private float baseGap = 5f;

    [Header("Dynamic Spread")]
    [SerializeField] private bool dynamicSpread = true;

    [SerializeField] private float maxSpread = 30f;
    [SerializeField] private float spreadSpeed = 10f;
    [SerializeField] private float spreadRecoverySpeed = 5f;

    [Header("Center Dot")]
    [SerializeField] private bool showDotWhenAiming = true;

    [Tooltip("Show dot only when aiming, or always show it")]
    [SerializeField] private bool dotOnlyWhenAiming = true;

    [SerializeField] private Color dotColor = Color.red;
    [SerializeField] private float dotSize = 4f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    private float currentSpread = 0f;

    private RangedWeapon currentWeapon;

    private InputManager inputManager;
    private CanvasGroup canvasGroup;
    private GameplayHUD gameplayHUD;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (!canvasGroup)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void Start()
    {
        inputManager = ServiceLocator.Instance.GetService<InputManager>();
        gameplayHUD = ServiceLocator.Instance.GetService<GameplayHUD>();

        if (!gameplayHUD)
            gameplayHUD = FindFirstObjectByType<GameplayHUD>();

        InitializeCrosshair();
        UpdateVisibility();
    }

    private void InitializeCrosshair()
    {
        // Set colors for all elements
        SetColor(crosshairColor);

        // Set dot color separately
        if (centerDot)
        {
            centerDot.color = dotColor;
            centerDot.rectTransform.sizeDelta = new Vector2(dotSize, dotSize);
        }

        // Set base sizes
        UpdateCrosshairLayout(0f);

        // Update dot visibility
        UpdateCenterDot();

        if (showDebugLogs)
            Debug.Log($"[CanvasCrosshair] Initialized on {gameObject.name}");
    }

    private void Update()
    {
        if (!showCrosshair) return;

        if (dynamicSpread)
        {
            UpdateDynamicSpread();
            UpdateCrosshairLayout(currentSpread);
        }

        // Update center dot based on aiming state
        UpdateCenterDot();
    }

    private void UpdateDynamicSpread()
    {
        if (!inputManager) return;

        float targetSpread = 0f;

        // Increase spread when moving
        Vector2 moveInput = inputManager.PlayerActions.Move.ReadValue<Vector2>();
        bool isMoving = moveInput.magnitude > 0.1f;
        bool isSprinting = inputManager.PlayerActions.Sprint.IsPressed();

        if (isMoving)
        {
            targetSpread += 10f;
            if (isSprinting)
                targetSpread += 10f;
        }

        // Increase spread when shooting (using current weapon)
        if (currentWeapon != null && inputManager.PlayerActions.Attack.IsPressed())
        {
            targetSpread += 15f;
        }

        // Interpolate to target spread
        float speed = targetSpread > currentSpread ? spreadSpeed : spreadRecoverySpeed;
        currentSpread = Mathf.Lerp(currentSpread, targetSpread, speed * Time.deltaTime);
        currentSpread = Mathf.Clamp(currentSpread, 0f, maxSpread);
    }

    private void UpdateCenterDot()
    {
        if (!centerDot || !showDotWhenAiming)
        {
            if (centerDot)
                centerDot.gameObject.SetActive(false);
            return;
        }

        bool shouldShowDot = false;

        if (dotOnlyWhenAiming)
        {
            // Show dot only when aiming
            if (currentWeapon != null && currentWeapon.IsAiming())
            {
                shouldShowDot = true;
            }
        }
        else
        {
            // Always show dot when crosshair is visible
            shouldShowDot = showCrosshair;
        }

        centerDot.gameObject.SetActive(shouldShowDot);

        if (showDebugLogs && shouldShowDot != centerDot.gameObject.activeSelf)
        {
            Debug.Log($"[CanvasCrosshair] Center dot: {(shouldShowDot ? "SHOWN" : "HIDDEN")} (Aiming: {currentWeapon.IsAiming()})");
        }
    }

    private void UpdateCrosshairLayout(float spread)
    {
        float gap = baseGap + spread;

        // Position top line
        if (topLine)
        {
            topLine.anchoredPosition = new Vector2(0, gap + baseSize / 2f);
            topLine.sizeDelta = new Vector2(thickness, baseSize);
        }

        // Position bottom line
        if (bottomLine)
        {
            bottomLine.anchoredPosition = new Vector2(0, -gap - baseSize / 2f);
            bottomLine.sizeDelta = new Vector2(thickness, baseSize);
        }

        // Position left line
        if (leftLine)
        {
            leftLine.anchoredPosition = new Vector2(-gap - baseSize / 2f, 0);
            leftLine.sizeDelta = new Vector2(baseSize, thickness);
        }

        // Position right line
        if (rightLine)
        {
            rightLine.anchoredPosition = new Vector2(gap + baseSize / 2f, 0);
            rightLine.sizeDelta = new Vector2(baseSize, thickness);
        }

        // Update center dot size
        if (centerDot)
        {
            centerDot.rectTransform.sizeDelta = new Vector2(dotSize, dotSize);
        }
    }

    public void Show()
    {
        showCrosshair = true;
        UpdateVisibility();
        UpdateCenterDot();

        if (showDebugLogs)
            Debug.Log($"[CanvasCrosshair] Showing crosshair - Alpha: {(canvasGroup ? canvasGroup.alpha : -1)}");
    }

    public void Hide()
    {
        showCrosshair = false;
        UpdateVisibility();
        UpdateCenterDot();

        if (showDebugLogs)
            Debug.Log($"[CanvasCrosshair] Hiding crosshair - Alpha: {(canvasGroup ? canvasGroup.alpha : -1)}");
    }

    public void SetColor(Color color)
    {
        crosshairColor = color;

        if (topLine) topLine.GetComponent<Image>().color = color;
        if (bottomLine) bottomLine.GetComponent<Image>().color = color;
        if (leftLine) leftLine.GetComponent<Image>().color = color;
        if (rightLine) rightLine.GetComponent<Image>().color = color;
        // Don't change dot color - it has its own color
    }

    public void SetDotColor(Color color)
    {
        dotColor = color;
        if (centerDot)
            centerDot.color = color;
    }

    public void SetSize(float size)
    {
        baseSize = size;
        UpdateCrosshairLayout(currentSpread);
    }

    public void SetGap(float gap)
    {
        baseGap = gap;
        UpdateCrosshairLayout(currentSpread);
    }

    public void SetDotSize(float size)
    {
        dotSize = size;
        if (centerDot)
            centerDot.rectTransform.sizeDelta = new Vector2(dotSize, dotSize);
    }

    /// <summary>
    /// Set the current weapon reference for dynamic spread calculation.
    /// Called automatically by GameplayHUD when switching hosts.
    /// </summary>
    public void SetWeapon(RangedWeapon weapon)
    {
        currentWeapon = weapon;

        if (showDebugLogs)
        {
            if (weapon != null)
                Debug.Log($"[CanvasCrosshair] Weapon updated to: {weapon.gameObject.name}");
            else
                Debug.Log("[CanvasCrosshair] Weapon cleared (null)");
        }

        // Update dot immediately
        UpdateCenterDot();
    }

    private void UpdateVisibility()
    {
        if (canvasGroup)
        {
            float targetAlpha = showCrosshair ? 1f : 0f;
            canvasGroup.alpha = targetAlpha;
            canvasGroup.interactable = showCrosshair;
            canvasGroup.blocksRaycasts = false; // Never block raycasts

            if (showDebugLogs)
            {
                Debug.Log($"[CanvasCrosshair] UpdateVisibility - showCrosshair: {showCrosshair}, alpha: {canvasGroup.alpha}");
            }
        }
        else if (showDebugLogs)
        {
            Debug.LogError("[CanvasCrosshair] UpdateVisibility - CanvasGroup is NULL!");
        }
    }

    #region Editor Helper Methods

    public void EditorSetup(RectTransform top, RectTransform bottom, RectTransform left, RectTransform right, Image dot)
    {
        topLine = top;
        bottomLine = bottom;
        leftLine = left;
        rightLine = right;
        centerDot = dot;

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    #endregion Editor Helper Methods

    #region Debug Helpers

    [ContextMenu("Log Current State")]
    private void LogCurrentState()
    {
        Debug.Log($"[CanvasCrosshair] State:");
        Debug.Log($"  Show Crosshair: {showCrosshair}");
        Debug.Log($"  Current Spread: {currentSpread:F2}");
        Debug.Log($"  Current Weapon: {(currentWeapon ? currentWeapon.gameObject.name : "NULL")}");
        Debug.Log($"  Is Aiming: {(currentWeapon ? currentWeapon.IsAiming().ToString() : "N/A")}");
        Debug.Log($"  Center Dot Active: {(centerDot ? centerDot.gameObject.activeSelf.ToString() : "NULL")}");
        Debug.Log($"  Canvas Alpha: {(canvasGroup ? canvasGroup.alpha.ToString("F2") : "NULL")}");
    }

    [ContextMenu("Show Crosshair")]
    private void ShowCrosshairDebug() => Show();

    [ContextMenu("Hide Crosshair")]
    private void HideCrosshairDebug() => Hide();

    [ContextMenu("Toggle Dot")]
    private void ToggleDotDebug()
    {
        if (centerDot)
        {
            centerDot.gameObject.SetActive(!centerDot.gameObject.activeSelf);
            Debug.Log($"[CanvasCrosshair] Dot toggled to: {centerDot.gameObject.activeSelf}");
        }
    }

    #endregion Debug Helpers
}