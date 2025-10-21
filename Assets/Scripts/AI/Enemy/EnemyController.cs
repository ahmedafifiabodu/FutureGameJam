using AI.Enemy.Configuration;
using AI.Enemy.States;
using UnityEngine;
using UnityEngine.AI;

namespace AI.Enemy
{
    /// <summary>
    /// Main controller for enemy behavior using state machine pattern
    /// Replaces the old Enemy and EnemyStateManager classes
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Animator))]
    public class EnemyController : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private EnemyConfigSO config;

        [Header("References")]
        [SerializeField] private NavMeshAgent agent;

        [SerializeField] private Animator animator;
        [SerializeField] private Collider attackCollider;
        [SerializeField] private Transform projectileSpawnPoint;

        [Header("Patrol Points")]
        [SerializeField] private Transform[] patrolPoints;

        // State management
        private States.IEnemyState currentState;

        private EnemyPatrolState patrolState;
        private EnemyChaseStateNew chaseState;
        private EnemyAttackState attackState;
        private EnemyStaggerStateNew staggerState;
        private EnemyDeadState deadState;

        // Runtime data
        private int currentHealth;

        private bool isDead = false;
        private bool isStaggered = false;
        private float staggerTimer = 0f;
        private float attackCooldownTimer = 0f;

        // Vision/Detection system
        private Transform player;

        private bool hasSeenPlayer = false;
        private float visionCheckTimer = 0f;
        private Vector3 lastKnownPlayerPosition;
        private float timeSinceLastSaw = 0f;

        // Room tracking
        private Transform currentRoom;

        public EnemyConfigSO Config => config;
        public NavMeshAgent Agent => agent;
        public Animator Animator => animator;
        public Transform Player => player;
        public bool HasSeenPlayer => hasSeenPlayer;
        public bool IsDead => isDead;
        public bool IsStaggered => isStaggered;
        public Transform CurrentRoom => currentRoom;
        public Transform ProjectileSpawnPoint => projectileSpawnPoint;
        public Transform[] PatrolPoints => patrolPoints;
        public Vector3 LastKnownPlayerPosition => lastKnownPlayerPosition;

        private void Awake()
        {
            if (agent == null) agent = GetComponent<NavMeshAgent>();
            if (animator == null) animator = GetComponent<Animator>();

            ValidateConfiguration();
        }

        private void Start()
        {
            InitializeEnemy();
            InitializeStates();
            ChangeState(patrolState);
        }

        private void Update()
        {
            if (isDead) return;

            UpdateStagger();
            UpdateVision();
            UpdateAttackCooldown();

            currentState?.UpdateState(this);
        }

        #region Initialization

        private void ValidateConfiguration()
        {
            if (config == null)
            {
                Debug.LogError($"[EnemyController] {gameObject.name} is missing EnemyConfigSO!", this);
                enabled = false;
                return;
            }

            if (config.enemyPrefab == null)
            {
                Debug.LogWarning($"[EnemyController] {config.enemyName} config is missing enemy prefab!", this);
            }
        }

        private void InitializeEnemy()
        {
            currentHealth = config.maxHealth;
            agent.speed = config.patrolSpeed;
            agent.stoppingDistance = config.attackRange * 0.7f;
            agent.acceleration = 8f;
            agent.angularSpeed = 200f;
            agent.autoBraking = true;
            agent.obstacleAvoidanceType = UnityEngine.AI.ObstacleAvoidanceType.HighQualityObstacleAvoidance;

            // Find player
            player = ServiceLocator.Instance.GetService<ParasiteController>().transform;

            // Auto-find patrol points if not assigned
            if (patrolPoints == null || patrolPoints.Length == 0)
            {
                FindPatrolPoints();
            }

            // Disable attack collider initially
            if (attackCollider != null)
            {
                attackCollider.enabled = false;
            }
        }

        private void FindPatrolPoints()
        {
            // Try to find patrol points in parent room/corridor
            if (transform.parent != null)
            {
                Transform patrolContainer = transform.parent.Find("PatrolPoints");
                if (patrolContainer != null)
                {
                    int childCount = patrolContainer.childCount;
                    patrolPoints = new Transform[childCount];
                    for (int i = 0; i < childCount; i++)
                    {
                        patrolPoints[i] = patrolContainer.GetChild(i);
                    }
                }
            }
        }

        private void InitializeStates()
        {
            patrolState = new EnemyPatrolState();
            chaseState = new EnemyChaseStateNew();
            attackState = new EnemyAttackState();
            staggerState = new EnemyStaggerStateNew();
            deadState = new EnemyDeadState();
        }

        #endregion Initialization

        #region State Management

        public void ChangeState(States.IEnemyState newState)
        {
            currentState?.ExitState(this);
            currentState = newState;
            currentState?.EnterState(this);
        }

        #endregion State Management

        #region Vision/Detection System

        private void UpdateVision()
        {
            if (player == null) return;

            visionCheckTimer += Time.deltaTime;

            if (visionCheckTimer >= config.visionCheckInterval)
            {
                visionCheckTimer = 0f;
                CheckForPlayer();
            }
        }

        private void CheckForPlayer()
        {
            if (player == null) return;

            Vector3 directionToPlayer = (player.position - transform.position).normalized;
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);

            // Check if player is within sight range
            if (distanceToPlayer > config.sightRange)
                return;

            // Check if player is within field of view
            float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);
            if (angleToPlayer > config.fieldOfViewAngle / 2f)
                return;

            // Check if there are obstacles blocking vision (raycast should NOT hit anything to see the player)
            Vector3 rayOrigin = transform.position + Vector3.up * 1.5f; // Eye level
            if (Physics.Raycast(rayOrigin, directionToPlayer, distanceToPlayer, config.visionObstacleMask))
            {
                // Something is blocking the view
                return;
            }

            // Player spotted!
            OnPlayerSpotted();
        }

        private void OnPlayerSpotted()
        {
            if (hasSeenPlayer) return;

            hasSeenPlayer = true;
            lastKnownPlayerPosition = player.position;
            timeSinceLastSaw = 0f;

            // Switch to chase state
            agent.speed = config.chaseSpeed;
            ChangeState(chaseState);

            Debug.Log($"[EnemyController] {config.enemyName} spotted player!");
        }

        public void UpdateLastKnownPosition()
        {
            if (player == null) return;

            // Check if we can still see the player
            Vector3 directionToPlayer = (player.position - transform.position).normalized;
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);

            if (distanceToPlayer <= config.sightRange &&
                !Physics.Raycast(transform.position + Vector3.up, directionToPlayer, distanceToPlayer, config.visionObstacleMask))
            {
                lastKnownPlayerPosition = player.position;
                timeSinceLastSaw = 0f;
            }
            else
            {
                timeSinceLastSaw += Time.deltaTime;

                // Lost sight of player for too long
                if (timeSinceLastSaw >= config.chaseLostSightTime)
                {
                    LosePlayer();
                }
            }
        }

        private void LosePlayer()
        {
            hasSeenPlayer = false;
            timeSinceLastSaw = 0f;

            // Force navmesh to resume properly
            agent.isStopped = false;

            // Return to patrol
            agent.speed = config.patrolSpeed;
            ChangeState(patrolState);

            Debug.Log($"[EnemyController] {config.enemyName} lost sight of player, returning to patrol");
        }

        #endregion Vision/Detection System

        #region Combat System

        /// <summary>
        /// Check if player is within attack range
        /// </summary>
        public bool IsPlayerInAttackRange()
        {
            if (player == null) return false;
            float distance = Vector3.Distance(transform.position, player.position);
            return distance <= config.attackRange;
        }

        /// <summary>
        /// Check if enemy can attack (cooldown ready)
        /// </summary>
        public bool CanAttack()
        {
            return attackCooldownTimer <= 0f && !isStaggered;
        }

        private void UpdateAttackCooldown()
        {
            if (attackCooldownTimer > 0f)
            {
                attackCooldownTimer -= Time.deltaTime;
            }
        }

        public void StartAttackCooldown()
        {
            attackCooldownTimer = config.attackCooldown;
        }

        public void PerformAttack()
        {
            // Your attack logic here — deal damage, play sound, etc.
            Debug.Log($"[EnemyController] {config.enemyName} performed an attack!");
        }

        /// <summary>
        /// Apply damage to enemy with pain chance system
        /// </summary>
        public void TakeDamage(int damage, Vector3 hitPosition)
        {
            if (isDead) return;

            currentHealth -= damage;

            // Check for death
            if (currentHealth <= 0)
            {
                Die();
                return;
            }

            // Pain chance / Stagger system
            float randomRoll = Random.value;
            if (randomRoll <= config.staggerChance)
            {
                TriggerStagger();
            }

            // Immediately spot player if hit
            if (!hasSeenPlayer && player != null)
            {
                OnPlayerSpotted();
            }
        }

        /// <summary>
        /// Trigger stagger effect
        /// </summary>
        private void TriggerStagger()
        {
            if (isStaggered || isDead) return;

            isStaggered = true;
            staggerTimer = config.staggerDuration;
            ChangeState(staggerState);
        }

        private void UpdateStagger()
        {
            if (!isStaggered) return;

            staggerTimer -= Time.deltaTime;

            if (staggerTimer <= 0f)
            {
                isStaggered = false;

                // Return to appropriate state
                if (hasSeenPlayer && player != null)
                {
                    if (IsPlayerInAttackRange() && CanAttack())
                    {
                        ChangeState(attackState);
                    }
                    else
                    {
                        ChangeState(chaseState);
                    }
                }
                else
                {
                    ChangeState(patrolState);
                }
            }
        }

        /// <summary>
        /// Handle enemy death
        /// </summary>
        private void Die()
        {
            if (isDead) return;

            isDead = true;
            agent.isStopped = true;
            agent.enabled = false;

            ChangeState(deadState);

            // Disable colliders
            if (attackCollider != null)
            {
                attackCollider.enabled = false;
            }

            if (TryGetComponent<Collider>(out var collider))
            {
                collider.enabled = false;
            }

            // TODO: Implement ragdoll or death animation
            // For now, just destroy after a delay
            Destroy(gameObject, 3f);
        }

        /// <summary>
        /// Enable attack collider (called from animation event)
        /// </summary>
        public void EnableAttackCollider()
        {
            if (attackCollider != null)
            {
                attackCollider.enabled = true;
            }
        }

        /// <summary>
        /// Disable attack collider (called from animation event)
        /// </summary>
        public void DisableAttackCollider()
        {
            if (attackCollider != null)
            {
                attackCollider.enabled = false;
            }
        }

        #endregion Combat System

        #region Room Management

        public void SetCurrentRoom(Transform room)
        {
            currentRoom = room;
        }

        #endregion Room Management

        #region Gizmos

        private void OnDrawGizmosSelected()
        {
            if (config == null) return;

            // Draw sight range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, config.sightRange);

            // Draw attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, config.attackRange);

            // Draw field of view
            Vector3 leftBoundary = Quaternion.Euler(0, -config.fieldOfViewAngle / 2f, 0) * transform.forward * config.sightRange;
            Vector3 rightBoundary = Quaternion.Euler(0, config.fieldOfViewAngle / 2f, 0) * transform.forward * config.sightRange;

            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, transform.position + leftBoundary);
            Gizmos.DrawLine(transform.position, transform.position + rightBoundary);

            // Draw patrol points connections
            if (patrolPoints != null && patrolPoints.Length > 0)
            {
                Gizmos.color = Color.green;
                for (int i = 0; i < patrolPoints.Length; i++)
                {
                    if (patrolPoints[i] != null)
                    {
                        Gizmos.DrawWireSphere(patrolPoints[i].position, 0.5f);
                        if (i < patrolPoints.Length - 1 && patrolPoints[i + 1] != null)
                        {
                            Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[i + 1].position);
                        }
                    }
                }
            }
        }

        #endregion Gizmos
    }
}