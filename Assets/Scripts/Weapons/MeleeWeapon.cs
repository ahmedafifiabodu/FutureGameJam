using UnityEngine;

/// <summary>
/// Placeholder for melee weapon functionality.
/// Will be implemented later with swing mechanics, hitboxes, and combos.
/// </summary>
public class MeleeWeapon : WeaponBase
{
    [Header("Melee Settings")]
    [SerializeField] private float attackRange = 2f;

    [SerializeField] private float attackSpeed = 0.5f;
    [SerializeField] private LayerMask hitLayers = ~0;

    [Header("Visual Effects")]
    [SerializeField] private TrailRenderer swingTrail;

    private float nextAttackTime;
    private int meleeAttackHash;

    private void Awake()
    {
        // Cache animation parameter hash
        meleeAttackHash = Animator.StringToHash(GameConstant.AnimationParameters.MeleeAttack);
    }

    public override void Update()
    {
        if (!isEquipped) return;

        // TODO: Implement melee attack logic
        if (inputManager != null && inputManager.PlayerActions.Attack.triggered)
        {
            if (Time.time >= nextAttackTime)
            {
                Attack();
                nextAttackTime = Time.time + attackSpeed;
            }
        }
    }

    public override void Attack()
    {
        Debug.Log("[MeleeWeapon] Melee attack - TO BE IMPLEMENTED");

        // Placeholder: Simple sphere cast for hit detection
        Collider[] hits = Physics.OverlapSphere(transform.position + transform.forward * attackRange, 1f, hitLayers);
        foreach (var hit in hits)
        {
            if (hit.transform != transform.root) // Don't hit ourselves
            {
                if (hit.TryGetComponent<IDamageable>(out var damageable))
                {
                    damageable.TakeDamage(damage);
                    Debug.Log($"[MeleeWeapon] Hit {hit.name} for {damage} damage");
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Visualize attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + transform.forward * attackRange, 1f);
    }
}