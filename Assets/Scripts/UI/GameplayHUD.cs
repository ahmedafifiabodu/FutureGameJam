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

    [SerializeField] private TextMeshProUGUI ammoText;
    [SerializeField] private TextMeshProUGUI reserveAmmoText;
    [SerializeField] private TextMeshProUGUI aimingText;
    [SerializeField] private Image reloadProgressBar;
    [SerializeField] private TextMeshProUGUI reloadingText;

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
        {
            Debug.LogWarning("[GameplayHUD] ParasiteController not found in ServiceLocator! Trying FindFirstObjectByType...");
            parasiteController = FindFirstObjectByType<ParasiteController>();

            if (!parasiteController)
            {
                Debug.LogError("[GameplayHUD] ParasiteController not found! Lifetime slider will not update.");
            }
            else
            {
                Debug.Log("[GameplayHUD] ParasiteController found via FindFirstObjectByType");
            }
        }
        else
        {
            Debug.Log("[GameplayHUD] ParasiteController found via ServiceLocator");
        }

        // Start with parasite UI
        ShowParasiteUI();

        // Log slider state
        if (parasiteLifetimeSlider)
        {
            Debug.Log($"[GameplayHUD] Parasite lifetime slider assigned: {parasiteLifetimeSlider.gameObject.name}");
            Debug.Log($"[GameplayHUD] Initial slider progress: {parasiteLifetimeSlider.GetProgress()}");
        }
        else
        {
            Debug.LogError("[GameplayHUD] Parasite lifetime slider not assigned!");
        }
    }

    private void Update()
    {
        if (parasitePanel.activeSelf)
            UpdateParasiteUI();
        else if (hostPanel.activeSelf)
            UpdateHostUI();

        if (weaponPanel.activeSelf)
            UpdateWeaponUI();
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
        // If weapon already equipped, keep panel and crosshair visible
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
                $"Slider Progress: {parasiteLifetimeSlider?.GetProgress():F2}";
            parasiteDebugText.color = lifetimeColor;
        }
    }

    #endregion Parasite UI Updates

    #region Host UI Updates

    private void UpdateHostUI()
    {
        if (!currentHost) return;

        float remainingLifetime = currentHost.GetLifetimePercentage() * 100f; // Convert to percentage
        float lifetimeSeconds = remainingLifetime; // Assuming GetLifetimePercentage returns actual seconds

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
                hostExitHintText.text = "Release J to Exit";
                hostExitHintText.color = successColor;
            }
            else
            {
                hostExitHintText.text = "Hold J to Show Exit Trajectory";
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
                Debug.Log($"[GameplayHUD] SetCurrentWeapon - Showing crosshair for weapon: {weapon.gameObject.name}");
                crosshair.SetWeapon(weapon);
                crosshair.Show();
            }
            else
            {
                Debug.LogError("[GameplayHUD] SetCurrentWeapon - Crosshair is NULL!");
            }
        }
        else
        {
            // No weapon - hide UI
            weaponPanel.SetActive(false);
            
            if (crosshair)
            {
                Debug.Log("[GameplayHUD] SetCurrentWeapon - Hiding crosshair (weapon cleared)");
                crosshair.SetWeapon(null);
                crosshair.Hide();
            }
        }
    }

    private void UpdateWeaponUI()
    {
        if (!currentWeapon) return;

        // Update ammo display
        if (ammoText)
        {
            int currentAmmo = currentWeapon.GetCurrentAmmo();
            int magazineSize = currentWeapon.GetMagazineSize();
            ammoText.text = $"{currentAmmo}/{magazineSize}";
            ammoText.color = currentAmmo <= magazineSize * 0.2f ? dangerColor : normalColor;
        }

        if (reserveAmmoText)
        {
            reserveAmmoText.text = $"Reserve: {currentWeapon.GetReserveAmmo()}";
            reserveAmmoText.color = Color.gray;
        }

        // Update aiming indicator
        if (aimingText)
        {
            aimingText.gameObject.SetActive(currentWeapon.IsAiming());
            aimingText.text = "AIMING";
            aimingText.color = warningColor;
        }

        // Update reload progress
        bool isReloading = currentWeapon.IsReloading();

        if (reloadProgressBar)
            reloadProgressBar.gameObject.SetActive(isReloading);

        if (reloadingText)
            reloadingText.gameObject.SetActive(isReloading);

        if (isReloading)
        {
            float progress = currentWeapon.GetReloadProgress();
            if (reloadProgressBar)
                reloadProgressBar.fillAmount = progress;

            if (reloadingText)
            {
                reloadingText.text = "RELOADING...";
                reloadingText.color = warningColor;
            }
        }
    }

    #endregion Weapon UI Updates

    #region Public Interface

    public void ShowCrosshair() => crosshair?.Show();

    public void HideCrosshair() => crosshair?.Hide();

    public void SetCrosshairColor(Color color) => crosshair?.SetColor(color);

    public DualProgressSlider GetParasiteLifetimeSlider() => parasiteLifetimeSlider;

    public DualProgressSlider GetHostLifetimeSlider() => hostLifetimeSlider;

    #endregion Public Interface
}