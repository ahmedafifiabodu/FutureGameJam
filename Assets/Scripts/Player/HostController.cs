using UnityEngine;

/// <summary>
/// Controls a host body that the parasite can attach to.
/// Manages host health/timer and death.
/// </summary>
public class HostController : MonoBehaviour
{
    [Header("Host Stats")]
    [SerializeField] private float hostLifetime = 30f; // How long the host survives
    [SerializeField] private bool decreaseLifetimeEachHost = true;
    [SerializeField] private float lifetimeDecreasePerHost = 5f;
    [SerializeField] private float minLifetime = 5f;

    [Header("References")]
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private FirstPersonZoneController hostMovementController;

    [Header("Death")]
    [SerializeField] private GameObject deathEffect;
    [SerializeField] private float ragdollDuration = 3f;

    private bool isControlled = false;
    private float remainingLifetime;
    private float timeSinceAttached;
    private ParasiteController attachedParasite;
    private Camera hostCamera;

    private static int hostCount = 0; // Track number of hosts used

    private void Awake()
    {
        if (!hostMovementController)
            hostMovementController = GetComponent<FirstPersonZoneController>();

        if (!cameraPivot)
            Debug.LogWarning($"[Host] CameraPivot not assigned on {gameObject.name}");

        // Get the camera in the CameraPivot (host's camera)
        if (cameraPivot)
        {
            hostCamera = cameraPivot.GetComponentInChildren<Camera>();
            if (hostCamera)
            {
                // Disable host camera by default (parasite isn't attached yet)
                hostCamera.enabled = false;
                Debug.Log($"[Host] Found and disabled host camera: {hostCamera.name}");
            }
            else
            {
                Debug.LogWarning($"[Host] No camera found in CameraPivot on {gameObject.name}");
            }
        }
    }

    private void Start()
    {
        // Initially disable host movement
        if (hostMovementController)
            hostMovementController.enabled = false;

        // Calculate lifetime based on host count
        remainingLifetime = hostLifetime;
        if (decreaseLifetimeEachHost && hostCount > 0)
        {
            remainingLifetime = Mathf.Max(minLifetime, hostLifetime - (hostCount * lifetimeDecreasePerHost));
        }
    }

    private void Update()
    {
        if (!isControlled) return;

        // Count down lifetime
        timeSinceAttached += Time.deltaTime;
        remainingLifetime -= Time.deltaTime;

        if (remainingLifetime <= 0f)
        {
            Die();
        }
    }

    public void OnParasiteAttached(ParasiteController parasite)
    {
        attachedParasite = parasite;
        isControlled = true;
        hostCount++;

        // Enable host movement controller
        if (hostMovementController)
            hostMovementController.enabled = true;

        // Enable host camera
        if (hostCamera)
        {
            hostCamera.enabled = true;
            Debug.Log($"[Host] Enabled host camera for control");
        }

        // Disable the parasite object visually
        if (parasite != null)
            parasite.gameObject.SetActive(false);

        Debug.Log($"[Host] Parasite attached! Lifetime: {remainingLifetime:F1}s");
    }

    public void OnParasiteDetached()
    {
        isControlled = false;

        // Disable host camera when parasite leaves
        if (hostCamera)
        {
            hostCamera.enabled = false;
            Debug.Log($"[Host] Disabled host camera - parasite detached");
        }

        // Disable host movement controller
        if (hostMovementController)
            hostMovementController.enabled = false;

        Debug.Log($"[Host] Parasite detached");
    }

    private void Die()
    {
        Debug.Log($"[Host] Host died! Survived: {timeSinceAttached:F1}s");

        // Disable camera
        if (hostCamera)
            hostCamera.enabled = false;

        // Disable movement
        if (hostMovementController)
            hostMovementController.enabled = false;

        // Spawn death effect
        if (deathEffect)
            Instantiate(deathEffect, transform.position, Quaternion.identity);

        // Notify game manager to switch back to parasite mode
        GameStateManager.Instance?.OnHostDied(attachedParasite);

        // Optional: Enable ragdoll or death animation
        EnableRagdoll();

        // Destroy host after ragdoll duration
        Destroy(gameObject, ragdollDuration);
    }

    private void EnableRagdoll()
    {
        // Disable character controller
        var cc = GetComponent<CharacterController>();
        if (cc) cc.enabled = false;

        // Enable ragdoll physics on all rigidbodies
        foreach (var rb in GetComponentsInChildren<Rigidbody>())
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }
    }

    public Transform GetCameraPivot() => cameraPivot;

    public float GetRemainingLifetime() => remainingLifetime;

    public float GetLifetimePercentage() => remainingLifetime / hostLifetime;

    public bool IsControlled() => isControlled;

    private void OnGUI()
    {
        if (!isControlled) return;

        // Display lifetime warning
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;

        GUI.Label(new Rect(screenWidth - 220, 8, 200, 30),
            $"Host Time: {remainingLifetime:F1}s",
            new GUIStyle(GUI.skin.label) { fontSize = 18, normal = { textColor = remainingLifetime < 10f ? Color.red : Color.white } });

        // Lifetime bar
        float barWidth = 200f;
        float barHeight = 20f;
        float barX = screenWidth - barWidth - 10f;
        float barY = 40f;

        GUI.Box(new Rect(barX, barY, barWidth, barHeight), "");
        GUI.Box(new Rect(barX, barY, barWidth * GetLifetimePercentage(), barHeight), "",
            new GUIStyle(GUI.skin.box) { normal = { background = Texture2D.whiteTexture } });
    }

    public static void ResetHostCount()
    {
        hostCount = 0;
    }
}