using UnityEngine;

/// <summary>
/// DEPRECATED: This class is replaced by CanvasCrosshair for Canvas-based UI.
/// Kept for backward compatibility. Consider migrating to CanvasCrosshair.
/// Simple crosshair system for ranged weapons.
/// Displays dynamic crosshair that expands when moving/shooting.
/// </summary>
[System.Obsolete("Use CanvasCrosshair instead for better performance and Canvas integration")]
public class CrosshairController : MonoBehaviour
{
    [Header("Migration Notice")]
    [SerializeField] private bool useCanvasCrosshair = true;

    [SerializeField] private CanvasCrosshair canvasCrosshair;

    [Header("Legacy OnGUI Settings")]
    [SerializeField] private bool showCrosshair = true;

    [SerializeField] private Color crosshairColor = Color.white;
    [SerializeField] private float crosshairSize = 10f;
    [SerializeField] private float crosshairThickness = 2f;
    [SerializeField] private float crosshairGap = 5f;

    [Header("Dynamic Crosshair")]
    [SerializeField] private bool dynamicCrosshair = true;

    [SerializeField] private float maxSpread = 30f;
    [SerializeField] private float spreadSpeed = 10f;
    [SerializeField] private float spreadRecoverySpeed = 5f;

    [Header("References")]
    [SerializeField] private RangedWeapon rangedWeapon;

    private float currentSpread;
    private InputManager inputManager;

    private void Start()
    {
        inputManager = ServiceLocator.Instance.GetService<InputManager>();

        if (!rangedWeapon)
            rangedWeapon = GetComponent<RangedWeapon>();

        // Try to find CanvasCrosshair if not assigned
        if (useCanvasCrosshair && !canvasCrosshair)
        {
            canvasCrosshair = FindFirstObjectByType<CanvasCrosshair>();

            if (canvasCrosshair)
            {
                Debug.Log("[CrosshairController] Automatically found CanvasCrosshair. Consider assigning it in the Inspector for better performance.");
            }
            else
            {
                Debug.LogWarning("[CrosshairController] CanvasCrosshair not found. Falling back to OnGUI rendering. Consider running the Gameplay HUD Setup Wizard.");
                useCanvasCrosshair = false;
            }
        }

        // Setup canvas crosshair if using it
        if (useCanvasCrosshair && canvasCrosshair)
        {
            canvasCrosshair.SetWeapon(rangedWeapon);
            canvasCrosshair.Show();
        }
    }

    private void Update()
    {
        if (!dynamicCrosshair) return;

        UpdateCrosshairSpread();
    }

    private void UpdateCrosshairSpread()
    {
        if (inputManager == null) return;

        // Increase spread when moving
        Vector2 moveInput = inputManager.PlayerActions.Move.ReadValue<Vector2>();
        bool isMoving = moveInput.magnitude > 0.1f;
        bool isSprinting = inputManager.PlayerActions.Sprint.IsPressed();

        float targetSpread = 0f;

        if (isMoving)
        {
            targetSpread += 10f;
            if (isSprinting)
                targetSpread += 10f;
        }

        // Increase spread when shooting
        if (rangedWeapon != null && inputManager.PlayerActions.Attack.IsPressed())
        {
            targetSpread += 15f;
        }

        // Interpolate to target spread
        if (targetSpread > currentSpread)
        {
            currentSpread = Mathf.Lerp(currentSpread, targetSpread, spreadSpeed * Time.deltaTime);
        }
        else
        {
            currentSpread = Mathf.Lerp(currentSpread, targetSpread, spreadRecoverySpeed * Time.deltaTime);
        }

        currentSpread = Mathf.Clamp(currentSpread, 0f, maxSpread);
    }

    //private void OnGUI()
    //{
    //    // Only render OnGUI if not using Canvas crosshair
    //    if (useCanvasCrosshair || !showCrosshair) return;

    //    float centerX = Screen.width / 2f;
    //    float centerY = Screen.height / 2f;

    //    float spread = dynamicCrosshair ? currentSpread : 0f;
    //    float gap = crosshairGap + spread;
    //    float size = crosshairSize;

    //    // Set crosshair color
    //    GUI.color = crosshairColor;

    //    // Top line
    //    GUI.DrawTexture(new Rect(centerX - crosshairThickness / 2f, centerY - gap - size, crosshairThickness, size), Texture2D.whiteTexture);

    //    // Bottom line
    //    GUI.DrawTexture(new Rect(centerX - crosshairThickness / 2f, centerY + gap, crosshairThickness, size), Texture2D.whiteTexture);

    //    // Left line
    //    GUI.DrawTexture(new Rect(centerX - gap - size, centerY - crosshairThickness / 2f, size, crosshairThickness), Texture2D.whiteTexture);

    //    // Right line
    //    GUI.DrawTexture(new Rect(centerX + gap, centerY - crosshairThickness / 2f, size, crosshairThickness), Texture2D.whiteTexture);

    //    // Reset color
    //    GUI.color = Color.white;
    //}

    // Public methods to control crosshair
    public void Show()
    {
        showCrosshair = true;
        if (useCanvasCrosshair && canvasCrosshair)
            canvasCrosshair.Show();
    }

    public void Hide()
    {
        showCrosshair = false;
        if (useCanvasCrosshair && canvasCrosshair)
            canvasCrosshair.Hide();
    }

    public void SetColor(Color color)
    {
        crosshairColor = color;
        if (useCanvasCrosshair && canvasCrosshair)
            canvasCrosshair.SetColor(color);
    }

    /// <summary>
    /// Migrate to Canvas-based crosshair system
    /// </summary>
    [ContextMenu("Migrate to Canvas Crosshair")]
    public void MigrateToCanvasCrosshair()
    {
        useCanvasCrosshair = true;

        if (!canvasCrosshair)
        {
            canvasCrosshair = FindFirstObjectByType<CanvasCrosshair>();
        }

        if (canvasCrosshair)
        {
            Debug.Log("[CrosshairController] Successfully migrated to CanvasCrosshair!");
        }
        else
        {
            Debug.LogWarning("[CrosshairController] CanvasCrosshair not found in scene. Please run 'Tools/Gameplay/Setup Gameplay HUD' to create it.");
        }
    }
}