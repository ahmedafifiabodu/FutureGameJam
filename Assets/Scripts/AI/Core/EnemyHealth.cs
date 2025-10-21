using UnityEngine;
using UnityEngine.Events;

namespace AI
{
    /// <summary>
    /// Handles enemy health, damage, death, and pain/stagger system
    /// Implements IDamageable for weapon system integration
    /// </summary>
    public class EnemyHealth : MonoBehaviour, IDamageable
    {
        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth;

        [Header("Pain/Stagger System")]
        [SerializeField, Range(0f, 1f)] private float painChance = 0.3f;
        [SerializeField] private float staggerDuration = 0.5f;
        [SerializeField] private float staggerCooldown = 2f; // Prevent stagger spam
        private float lastStaggerTime = -999f;

        [Header("Death Settings")]
        [SerializeField] private bool useRagdoll = true;
        [SerializeField] private float ragdollLifetime = 5f;
        [SerializeField] private GameObject deathEffectPrefab;

        [Header("Events")]
        public UnityEvent<float> OnDamaged; // Passes damage amount
        public UnityEvent OnStaggered;
        public UnityEvent OnDeath;

        public bool IsDead { get; private set; }
        public bool IsStaggered { get; private set; }
        public float HealthPercentage => currentHealth / maxHealth;

        private Animator animator;
        private EnemyAI enemyAI;
        private Collider[] colliders;
        private Rigidbody[] rigidbodies;

        // Animation parameter hashes
        private int staggerHash;
        private int deathHash;

        private void Awake()
        {
            currentHealth = maxHealth;
            animator = GetComponent<Animator>();
            enemyAI = GetComponent<EnemyAI>();

            // Cache colliders and rigidbodies for ragdoll
            colliders = GetComponentsInChildren<Collider>();
            rigidbodies = GetComponentsInChildren<Rigidbody>();

            // Initialize rigidbodies as kinematic (non-ragdoll)
            SetRagdollState(false);

            // Cache animation hashes using GameConstant
            staggerHash = Animator.StringToHash(GameConstant.AnimationParameters.Stagger);
            deathHash = Animator.StringToHash(GameConstant.AnimationParameters.Death);
        }

        public void TakeDamage(float damage)
        {
            if (IsDead) return;

            currentHealth -= damage;
            currentHealth = Mathf.Max(0, currentHealth);

            OnDamaged?.Invoke(damage);

            Debug.Log($"[EnemyHealth] {gameObject.name} took {damage} damage. Health: {currentHealth}/{maxHealth}");

            // Check for stagger (pain chance)
            if (!IsStaggered && Time.time >= lastStaggerTime + staggerCooldown)
            {
                if (Random.value <= painChance)
                {
                    TriggerStagger();
                }
            }

            // Check for death
            if (currentHealth <= 0)
            {
                Die();
            }
        }

        private void TriggerStagger()
        {
            IsStaggered = true;
            lastStaggerTime = Time.time;

            // Notify AI to stop current action
            if (enemyAI)
                enemyAI.OnStaggered();

            // Play stagger animation
            if (animator)
                animator.SetTrigger(staggerHash);

            OnStaggered?.Invoke();

            // End stagger after duration
            Invoke(nameof(EndStagger), staggerDuration);

            Debug.Log($"[EnemyHealth] {gameObject.name} staggered!");
        }

        private void EndStagger()
        {
            IsStaggered = false;

            // Notify AI to resume
            if (enemyAI)
                enemyAI.OnStaggerEnded();
        }

        private void Die()
        {
            if (IsDead) return;

            IsDead = true;
            OnDeath?.Invoke();

            // Notify AI
            if (enemyAI)
                enemyAI.OnDeath();

            // Spawn death effect
            if (deathEffectPrefab)
            {
                Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
            }

            // Enable ragdoll or play death animation
            if (useRagdoll)
            {
                EnableRagdoll();
            }
            else if (animator)
            {
                animator.SetTrigger(deathHash);
            }

            // Destroy after ragdoll lifetime
            Destroy(gameObject, ragdollLifetime);

            Debug.Log($"[EnemyHealth] {gameObject.name} died!");
        }

        private void EnableRagdoll()
        {
            // Disable animator
            if (animator)
                animator.enabled = false;

            // Disable AI components
            if (enemyAI)
                enemyAI.enabled = false;

            // Enable physics on all rigidbodies
            SetRagdollState(true);

            // Optional: Add force to ragdoll for dramatic effect
            // foreach (var rb in rigidbodies)
            // {
            //     rb.AddForce(Vector3.up * 200f, ForceMode.Impulse);
            // }
        }

        private void SetRagdollState(bool enabled)
        {
            foreach (var rb in rigidbodies)
            {
                if (rb != null && rb.gameObject != gameObject) // Skip root rigidbody if exists
                {
                    rb.isKinematic = !enabled;
                    rb.useGravity = enabled;
                }
            }

            // Disable character collider, enable ragdoll colliders
            foreach (var col in colliders)
            {
                if (col != null && col.gameObject != gameObject)
                {
                    col.enabled = enabled;
                }
            }

            // Main collider (if character controller or capsule)
            var mainCollider = GetComponent<Collider>();
            if (mainCollider)
                mainCollider.enabled = !enabled;
        }

        /// <summary>
        /// Heal the enemy (for testing or mechanics)
        /// </summary>
        public void Heal(float amount)
        {
            if (IsDead) return;

            currentHealth += amount;
            currentHealth = Mathf.Min(currentHealth, maxHealth);
        }

        /// <summary>
        /// Set max health (useful for difficulty scaling)
        /// </summary>
        public void SetMaxHealth(float newMaxHealth)
        {
            maxHealth = newMaxHealth;
            currentHealth = maxHealth;
        }

        /// <summary>
        /// Set pain chance (useful for difficulty scaling)
        /// </summary>
        public void SetPainChance(float chance)
        {
            painChance = Mathf.Clamp01(chance);
        }

        private void OnDrawGizmosSelected()
        {
            // Draw health bar above enemy
            if (Application.isPlaying)
            {
                Gizmos.color = Color.green;
                Vector3 healthBarStart = transform.position + Vector3.up * 2.5f;
                Vector3 healthBarEnd = healthBarStart + Vector3.right * (HealthPercentage * 2f);
                Gizmos.DrawLine(healthBarStart, healthBarEnd);
            }
        }
    }
}
