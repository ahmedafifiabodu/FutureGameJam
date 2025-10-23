using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple target dummy for testing weapons.
/// Implements IDamageable interface and shows damage feedback.
/// </summary>
public class TargetDummy : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;

    [SerializeField] private bool respawnOnDeath = true;
    [SerializeField] private float respawnDelay = 3f;

    [Header("Visual Feedback")]
    [SerializeField] private Renderer targetRenderer;

    [SerializeField] private Color damageColor = Color.red;
    [SerializeField] private float damageFlashDuration = 0.1f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;

    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioClip destroySound;

    [Header("Effects")]
    [SerializeField] private GameObject hitEffectPrefab;

    [SerializeField] private GameObject destroyEffectPrefab;

    private float currentHealth;
    private Color originalColor;
    private Material materialInstance;
    private bool isFlashing;
    private float flashTimer;

    private void Awake()
    {
        currentHealth = maxHealth;

        if (!targetRenderer)
            targetRenderer = GetComponent<Renderer>();

        if (targetRenderer && targetRenderer.material)
        {
            // Create material instance to avoid modifying shared material
            materialInstance = new Material(targetRenderer.material);
            targetRenderer.material = materialInstance;
            originalColor = materialInstance.color;
        }

        if (!audioSource)
            audioSource = GetComponent<AudioSource>();
    }

    public void TakeDamage(float damage)
    {
        if (currentHealth <= 0) return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);

        Debug.Log($"[TargetDummy] Took {damage} damage. Health: {currentHealth}/{maxHealth}");

        // Visual feedback
        StartDamageFlash();

        // Audio feedback
        if (audioSource && hitSound)
            audioSource.PlayOneShot(hitSound);

        // Check if dead
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void StartDamageFlash()
    {
        isFlashing = true;
        flashTimer = damageFlashDuration;

        if (materialInstance)
            materialInstance.color = damageColor;
    }

    private void Update()
    {
        if (isFlashing)
        {
            flashTimer -= Time.deltaTime;
            if (flashTimer <= 0)
            {
                isFlashing = false;
                if (materialInstance)
                    materialInstance.color = originalColor;
            }
        }
    }

    private void Die()
    {
        Debug.Log($"[TargetDummy] Destroyed!");

        // Play destroy sound
        if (audioSource && destroySound)
            audioSource.PlayOneShot(destroySound);

        // Spawn destroy effect
        if (destroyEffectPrefab)
        {
            GameObject effect = Instantiate(destroyEffectPrefab, transform.position, Quaternion.identity);
        }

        if (respawnOnDeath)
        {
            // Hide and respawn
            gameObject.SetActive(false);
            Invoke(nameof(Respawn), respawnDelay);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Respawn()
    {
        currentHealth = maxHealth;
        gameObject.SetActive(true);

        if (materialInstance)
            materialInstance.color = originalColor;

        Debug.Log($"[TargetDummy] Respawned with {maxHealth} health");
    }

    //// Show health bar in GUI for debugging
    //private void OnGUI()
    //{
    //    if (currentHealth <= 0) return;

    //    if (Camera.main == null) return;

    //    // Get screen position
    //    Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2f);

    //    if (screenPos.z > 0 && screenPos.x > 0 && screenPos.x < Screen.width && screenPos.y > 0 && screenPos.y < Screen.height)
    //    {
    //        // Convert to GUI coordinates (inverted Y)
    //        screenPos.y = Screen.height - screenPos.y;

    //        // Draw health bar
    //        float barWidth = 100f;
    //        float barHeight = 10f;
    //        float barX = screenPos.x - barWidth / 2f;
    //        float barY = screenPos.y;

    //        // Background
    //        GUI.Box(new Rect(barX, barY, barWidth, barHeight), "");

    //        // Health fill
    //        float healthPercent = currentHealth / maxHealth;
    //        Color barColor = healthPercent > 0.5f ? Color.green : (healthPercent > 0.25f ? Color.yellow : Color.red);
    //        GUI.color = barColor;
    //        GUI.Box(new Rect(barX, barY, barWidth * healthPercent, barHeight), "",
    //            new GUIStyle(GUI.skin.box) { normal = { background = Texture2D.whiteTexture } });

    //        // Reset color
    //        GUI.color = Color.white;

    //        // Health text
    //        GUI.Label(new Rect(barX, barY - 15, barWidth, 15), $"{currentHealth:F0}/{maxHealth:F0}",
    //            new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 10 });
    //    }
    //}

    private void OnDestroy()
    {
        // Clean up material instance
        if (materialInstance)
            Destroy(materialInstance);
    }

    // Debug visualization
    private void OnDrawGizmos()
    {
        Gizmos.color = currentHealth > 0 ? Color.green : Color.red;
        Gizmos.DrawWireCube(transform.position, Vector3.one);
    }
}