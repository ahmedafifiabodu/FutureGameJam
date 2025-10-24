using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Main gameplay HUD controller that displays all UI elements during gameplay.
/// Replaces OnGUI with proper Canvas-based UI.
/// </summary>
public class GameplayHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Canvas hudCanvas;

    [Header("Parasite UI")]
    [SerializeField] private GameObject parasitePanel;

    [SerializeField] private TextMeshProUGUI parasiteModeText;
    [SerializeField] private TextMeshProUGUI parasiteStatusText;
    [SerializeField] private TextMeshProUGUI parasiteCooldownText;
    [SerializeField] private TextMeshProUGUI parasiteDebugText;
    [SerializeField] private DualProgressSlider parasiteLifetimeSlider;

    [Header("Host UI")]
    [SerializeField] private GameObject hostPanel;

    [SerializeField] private TextMeshProUGUI hostLifetimeText;
    [SerializeField] private TextMeshProUGUI hostExitHintText;
    [SerializeField] private DualProgressSlider hostLifetimeSlider;

    [Header("Weapon UI")]
    [SerializeField] private GameObject weaponPanel;

    [Header("Crosshair")]
    [SerializeField] private CanvasCrosshair crosshair;

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.white;

    [SerializeField] private Color warningColor = Color.yellow;
    [SerializeField] private Color dangerColor = Color.red;
    [SerializeField] private Color successColor = Color.green;

    private ParasiteController parasiteController;
    private HostController currentHost;
    private RangedWeapon currentWeapon;
    private InputManager inputManager;

    private void Awake()
    {
        if (!hudCanvas)
            hudCanvas = GetComponent<Canvas>();

        // Register as service for easy access
        ServiceLocator.Instance.RegisterService(this, false);
    }

    private void Start()
    {
        inputManager = ServiceLocator.Instance.GetService<InputManager>();
        parasiteController = ServiceLocator.Instance.GetService<ParasiteController>();

        if (!parasiteController)
            parasiteController = FindFirstObjectByType<ParasiteController>();

        // Start with parasite UI
        ShowParasiteUI();
    }

    private void Update()
    {
        if (parasitePanel.activeSelf)
            UpdateParasiteUI();
        else if (hostPanel.activeSelf)
            UpdateHostUI();
    }

    #region Panel Management

    public void ShowParasiteUI()
    {
        parasitePanel.SetActive(true);
        hostPanel.SetActive(false);
        weaponPanel.SetActive(false);

        if (crosshair)
            crosshair.Hide();
    }

    public void ShowHostUI(HostController host)
    {
        currentHost = host;
        parasitePanel.SetActive(false);
        hostPanel.SetActive(true);

        // Don't show weapon panel yet - wait for weapon to be equipped
        // Only hide if no weapon is equipped
        if (currentWeapon == null)
        {
            weaponPanel.SetActive(false);

            // Don't show crosshair yet - wait for weapon to be equipped
            if (crosshair)
                crosshair.Hide();
        }
    }

    public void HideAllPanels()
    {
        parasitePanel.SetActive(false);
        hostPanel.SetActive(false);
        weaponPanel.SetActive(false);

        if (crosshair)
            crosshair.Hide();
    }

    #endregion Panel Management

    #region Parasite UI Updates

    private void UpdateParasiteUI()
    {
        if (!parasiteController || !inputManager) return;

        // Update lifetime slider
        if (parasiteLifetimeSlider)
        {
            float percentage = parasiteController.GetLifetimePercentage();
            parasiteLifetimeSlider.SetProgress(percentage);

            // Update slider color based on lifetime
            if (percentage < 0.25f)
                parasiteLifetimeSlider.SetColor(dangerColor);
            else if (percentage < 0.5f)
                parasiteLifetimeSlider.SetColor(warningColor);
            else
                parasiteLifetimeSlider.SetColor(successColor);
        }

        // Update status text
        float remainingLifetime = parasiteController.GetRemainingLifetime();
        Color lifetimeColor = remainingLifetime <= 15f ? dangerColor : warningColor;

        if (parasiteModeText)
        {
            parasiteModeText.text = "Parasite Mode";
            parasiteModeText.color = normalColor;
        }

        // Update debug info (can be disabled in production)
        if (parasiteDebugText)
        {
            Vector2 moveInput = inputManager.ParasiteActions.Move.ReadValue<Vector2>();
            bool isGrounded = parasiteController.GetComponent<CharacterController>().isGrounded;

            parasiteDebugText.text = $"Lifetime: {remainingLifetime:F1}s\n" +
                $"Move: ({moveInput.x:F1}, {moveInput.y:F1})\n" +
                $"Grounded: {isGrounded}\n" +
                $"Slider Progress: {parasiteLifetimeSlider.GetProgress():F2}";
            parasiteDebugText.color = lifetimeColor;
        }
    }

    #endregion Parasite UI Updates

    #region Host UI Updates

    private void UpdateHostUI()
    {
        if (!currentHost) return;

        float remainingLifetime = currentHost.GetLifetimePercentage() * 100f; // Convert to percentage

        // Update lifetime slider
        if (hostLifetimeSlider)
        {
            float percentage = currentHost.GetLifetimePercentage();
            hostLifetimeSlider.SetProgress(percentage);

            // Update slider color based on lifetime
            if (percentage < 0.33f)
                hostLifetimeSlider.SetColor(dangerColor);
            else if (percentage < 0.66f)
                hostLifetimeSlider.SetColor(warningColor);
            else
                hostLifetimeSlider.SetColor(successColor);
        }

        // Update lifetime text
        if (hostLifetimeText)
        {
            Color textColor = remainingLifetime < 10f ? dangerColor : normalColor;
            hostLifetimeText.text = $"Host Time: {remainingLifetime:F1}s";
            hostLifetimeText.color = textColor;
        }

        // Update exit hint (dynamically based on input)
        if (hostExitHintText && inputManager)
        {
            bool exitButtonHeld = inputManager.ParasiteActions.ExitForHost.IsPressed();

            if (exitButtonHeld)
            {
                hostExitHintText.text = "Release RMB to Exit";
                hostExitHintText.color = successColor;
            }
            else
            {
                hostExitHintText.text = "Hold RMB to Show Exit Trajectory";
                hostExitHintText.color = warningColor;
            }
        }
    }

    #endregion Host UI Updates

    #region Weapon UI Updates

    public void SetCurrentWeapon(RangedWeapon weapon)
    {
        currentWeapon = weapon;

        // Show/hide weapon panel and crosshair based on weapon
        if (weapon != null)
        {
            // Weapon equipped - show UI
            weaponPanel.SetActive(true);

            if (crosshair)
            {
                crosshair.SetWeapon(weapon);
                crosshair.Show();
            }
        }
        else
        {
            // No weapon - hide UI
            // Only hide weapon panel, but keep crosshair visible if in Host mode
            // The crosshair will be hidden when switching back to Parasite mode
            weaponPanel.SetActive(false);

            if (crosshair)
            {
                crosshair.SetWeapon(null);
                // Don't automatically hide crosshair - let ShowParasiteUI/ShowHostUI handle visibility
                // crosshair.Hide(); // REMOVED - this was causing the issue
            }
        }
    }

    #endregion Weapon UI Updates
}