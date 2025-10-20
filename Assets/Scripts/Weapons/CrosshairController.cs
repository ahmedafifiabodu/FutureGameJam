using UnityEngine;

/// <summary>
/// Simple crosshair system for ranged weapons.
/// Displays dynamic crosshair that expands when moving/shooting.
/// </summary>
public class CrosshairController : MonoBehaviour
{
    [Header("Crosshair Settings")]
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

    private void OnGUI()
    {
        if (!showCrosshair) return;

        float centerX = Screen.width / 2f;
        float centerY = Screen.height / 2f;

        float spread = dynamicCrosshair ? currentSpread : 0f;
        float gap = crosshairGap + spread;
        float size = crosshairSize;

        // Set crosshair color
        GUI.color = crosshairColor;

        // Top line
        GUI.DrawTexture(new Rect(centerX - crosshairThickness / 2f, centerY - gap - size, crosshairThickness, size), Texture2D.whiteTexture);

        // Bottom line
        GUI.DrawTexture(new Rect(centerX - crosshairThickness / 2f, centerY + gap, crosshairThickness, size), Texture2D.whiteTexture);

        // Left line
        GUI.DrawTexture(new Rect(centerX - gap - size, centerY - crosshairThickness / 2f, size, crosshairThickness), Texture2D.whiteTexture);

        // Right line
        GUI.DrawTexture(new Rect(centerX + gap, centerY - crosshairThickness / 2f, size, crosshairThickness), Texture2D.whiteTexture);

        // Optional: Center dot
        //GUI.DrawTexture(new Rect(centerX - 1, centerY - 1, 2, 2), Texture2D.whiteTexture);

        // Reset color
        GUI.color = Color.white;
    }

    // Public methods to control crosshair
    public void Show() => showCrosshair = true;

    public void Hide() => showCrosshair = false;

    public void SetColor(Color color) => crosshairColor = color;
}