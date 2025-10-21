using UnityEngine;

namespace AI
{
    /// <summary>
    /// ScriptableObject defining enemy stats and behavior parameters
    /// Allows designers to easily create and balance different enemy types
    /// </summary>
    [CreateAssetMenu(fileName = "New Enemy Profile", menuName = "AI/Enemy Profile")]
    public class EnemyProfile : ScriptableObject
    {
        [Header("Basic Info")]
        public string enemyName = "Enemy";
        public EnemyType enemyType = EnemyType.Basic;

        [Header("Health & Defense")]
        [Tooltip("Maximum health points")]
        public float maxHealth = 100f;
        
        [Tooltip("Chance to get staggered when hit (0-1)")]
        [Range(0f, 1f)] public float painChance = 0.3f;
        
        [Tooltip("Duration of stagger state in seconds")]
        public float staggerDuration = 0.5f;

        [Header("Movement")]
        [Tooltip("How fast the enemy moves while patrolling")]
        public float patrolSpeed = 2f;
        
        [Tooltip("How fast the enemy moves while chasing")]
        public float chaseSpeed = 4f;
        
        [Tooltip("How fast the enemy rotates (degrees per second)")]
        public float rotationSpeed = 120f;
        
        [Tooltip("How close to waypoint before considering it reached")]
        public float waypointReachDistance = 0.5f;

        [Header("Detection & Aggro")]
        [Tooltip("Range at which enemy instantly starts chasing")]
        public float instantAggroRange = 8f;
        
        [Tooltip("Range at which enemy starts chasing after delay")]
        public float delayedAggroRange = 15f;
        
        [Tooltip("Delay before starting chase in delayed aggro range")]
        public float aggroDelay = 1.5f;
        
        [Tooltip("Angle of vision cone (degrees)")]
        [Range(0f, 360f)] public float visionAngle = 120f;
        
        [Tooltip("Time to lose aggro if player leaves sight")]
        public float loseAggroTime = 3f;

        [Header("Attack")]
        [Tooltip("Damage dealt per attack")]
        public float attackDamage = 20f;
        
        [Tooltip("Range at which enemy can attack")]
        public float attackRange = 2f;
        
        [Tooltip("Cooldown between attacks")]
        public float attackCooldown = 1.5f;
        
        [Tooltip("Duration of attack animation/action")]
        public float attackDuration = 0.8f;
        
        [Tooltip("Time after attack before enemy can move again")]
        public float attackRecoveryTime = 0.5f;

        [Header("Spawning")]
        [Tooltip("Relative spawn weight (higher = more common)")]
        [Range(1, 100)] public int spawnWeight = 50;
        
        [Tooltip("Minimum room iteration before this enemy can spawn")]
        public int minRoomIteration = 0;
        
        [Tooltip("Maximum number of this enemy type per room")]
        public int maxPerRoom = 3;

        [Header("Audio")]
        public AudioClip idleSound;
        public AudioClip chaseSound;
        public AudioClip attackSound;
        public AudioClip hurtSound;
        public AudioClip deathSound;

        [Header("VFX")]
        public GameObject attackEffectPrefab;
        public GameObject deathEffectPrefab;
    }

    public enum EnemyType
    {
        Basic,      // Melee enemy with delayed movement and bayonet thrust
        Tough,      // Tanky enemy with shotgun
        Fast        // Agile enemy with jump attack and quick stab
    }
}
