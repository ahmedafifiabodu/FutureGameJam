using UnityEngine;

namespace AI.Enemy.Configuration
{
    /// <summary>
    /// ScriptableObject configuration for enemy types
    /// Allows designers to create different enemy configurations easily
    /// </summary>
    [CreateAssetMenu(fileName = "New Enemy Config", menuName = "AI/Enemy Configuration", order = 0)]
    public class EnemyConfigSO : ScriptableObject
    {
        [Header("Enemy Identity")]
        [Tooltip("Name of this enemy type")]
        public string enemyName = "New Enemy";

        [Tooltip("Enemy type determines behavior pattern")]
        public EnemyType enemyType = EnemyType.Basic;

        [Header("Prefab Reference")]
        [Tooltip("The enemy prefab to spawn")]
        public GameObject enemyPrefab;

        [Header("Stats")]
        [Tooltip("Maximum health of the enemy")]
        [Range(1, 1000)]
        public float maxHealth = 100f;

        [Tooltip("Damage dealt to player")]
        [Range(1, 100)]
        public int attackDamage = 10;

        [Tooltip("Movement speed")]
        [Range(0.5f, 10f)]
        public float moveSpeed = 3.5f;

        [Header("Patrol Settings")]
        [Tooltip("Speed while patrolling (usually slower than chase)")]
        [Range(0.5f, 10f)]
        public float patrolSpeed = 2f;

        [Tooltip("Time to wait at each patrol point")]
        [Range(0f, 10f)]
        public float patrolWaitTime = 2f;

        [Tooltip("Should patrol between assigned points or wander randomly")]
        public bool usePatrolPoints = true;

        [Header("Vision/Detection Settings")]
        [Tooltip("Maximum distance enemy can see the player")]
        [Range(5f, 50f)]
        public float sightRange = 15f;

        [Tooltip("Field of view angle (180 = can see in front, 360 = all around)")]
        [Range(30f, 360f)]
        public float fieldOfViewAngle = 90f;

        [Tooltip("How often to check for player visibility (seconds)")]
        [Range(0.1f, 2f)]
        public float visionCheckInterval = 0.5f;

        [Tooltip("Layer mask for vision obstruction and attack detection (walls, obstacles). Player/host is automatically excluded from blocking vision.")]
        public LayerMask visionObstacleMask;

        [Header("Combat Settings")]
        [Tooltip("Distance at which enemy can attack")]
        [Range(0.5f, 20f)]
        public float attackRange = 2f;

        [Tooltip("Cooldown between attacks")]
        [Range(0.1f, 10f)]
        public float attackCooldown = 1.5f;

        [Header("Chase Settings")]
        [Tooltip("Speed while chasing player")]
        [Range(1f, 15f)]
        public float chaseSpeed = 5f;

        [Tooltip("Time to keep chasing after losing sight of player")]
        [Range(0f, 10f)]
        public float chaseLostSightTime = 3f;

        [Header("Stagger Settings")]
        [Tooltip("Chance to get staggered when hit (0-1)")]
        [Range(0f, 1f)]
        public float staggerChance = 0.3f;

        [Tooltip("Duration of stagger effect")]
        [Range(0.1f, 5f)]
        public float staggerDuration = 0.5f;

        [Header("Spawning Settings")]
        [Tooltip("Weight for random spawning (higher = more common)")]
        [Range(1, 100)]
        public int spawnWeight = 10;

        [Tooltip("Minimum room iteration to spawn this enemy")]
        [Range(0, 50)]
        public int minRoomIteration = 0;

        [Header("Type-Specific Settings")]
        [Tooltip("Basic Enemy: Memory delay (sees player position from X seconds ago)")]
        [Range(0.1f, 5f)]
        public float memoryDelay = 1f;

        [Tooltip("Tough Enemy: Shotgun cone angle")]
        [Range(10f, 90f)]
        public float shotgunConeAngle = 30f;

        [Tooltip("Tough Enemy: Shotgun range")]
        [Range(1f, 20f)]
        public float shotgunRange = 8f;

        [Tooltip("Tough Enemy: Aim duration before shooting")]
        [Range(0.1f, 5f)]
        public float aimDuration = 1.5f;

        [Tooltip("Fast Enemy: Jump distance")]
        [Range(2f, 20f)]
        public float jumpDistance = 10f;

        [Tooltip("Fast Enemy: Jump attack cooldown")]
        [Range(0.5f, 10f)]
        public float jumpCooldown = 3f;

        [Tooltip("Fast Enemy: Medium range min distance")]
        [Range(2f, 15f)]
        public float mediumRangeMin = 5f;

        [Tooltip("Fast Enemy: Medium range max distance")]
        [Range(5f, 20f)]
        public float mediumRangeMax = 12f;

        [Header("Projectile Settings (if ranged)")]
        public bool isRanged = false;

        [Header("Animation Settings")]
        [Tooltip("Enable if this enemy has specific animations")]
        public bool hasCustomAnimations = false;

        [Tooltip("Idle animation - typically uses IsMoving = false")]
        public string idleAnimation = "Idle";

        [Tooltip("Patrol animation - uses IsMoving boolean parameter")]
        public string patrolAnimation = GameConstant.AnimationParameters.IsMoving;

        [Tooltip("Chase animation - uses IsChasing boolean parameter")]
        public string chaseAnimation = GameConstant.AnimationParameters.IsChasing;

        [Tooltip("Melee attack animation - uses meeleAttack trigger parameter")]
        public string meleeAttackAnimation = GameConstant.AnimationParameters.MeleeAttack;

        [Tooltip("Projectile attack animation - uses ProjectileAttack trigger parameter")]
        public string projectileAttackAnimation = GameConstant.AnimationParameters.AnimationProjectileName;

        [Tooltip("Stagger animation - uses Stagger trigger parameter")]
        public string staggerAnimation = GameConstant.AnimationParameters.Stagger;

        [Tooltip("Death animation - uses Death trigger parameter")]
        public string deathAnimation = GameConstant.AnimationParameters.Death;

        [Tooltip("Jump animation - uses Jump trigger parameter")]
        public string jumpAnimation = GameConstant.AnimationParameters.Jump;
    }

    /// <summary>
    /// Defines the behavior pattern of the enemy
    /// </summary>
    public enum EnemyType
    {
        Basic,   // Melee enemy with delayed memory, bayonet thrust attack
        Tough,  // Shotgun enemy with aiming mechanic
        Fast     // Jump attack enemy with prediction
    }
}