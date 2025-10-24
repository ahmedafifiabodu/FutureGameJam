using AI.Enemy.Configuration;
using AI.Enemy.States;
using UnityEngine;
using UnityEngine.AI;

namespace AI.Enemy
{
    /// <summary>
    /// Main _controller for enemy behavior using state machine pattern
    /// Replaces the old Enemy and EnemyStateManager classes
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Animator))]
    public class EnemyController : MonoBehaviour, IDamageable
    {
        [Header("Configuration")]
        [SerializeField] private EnemyConfigSO config;

        [Header("References")]
        [SerializeField] private NavMeshAgent agent;

        [SerializeField] private Animator animator;
        [SerializeField] private Transform projectileSpawnPoint;
        [SerializeField] private GameObject deathEffect;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip deathSound;

        [Header("Patrol Points")]
        [SerializeField] private Transform[] patrolPoints;

        [Header("Raycast Attack Settings")]
        [SerializeField] private bool debugRaycast = true;

        [SerializeField] private float raycastOriginHeight = 1.5f;

        [Header("Debug")]
        [SerializeField] private bool showDebugTargeting = true;

        // State management
        private States.IEnemyState currentState;

        private EnemyPatrolState patrolState;
        private EnemyChaseStateNew chaseState;
        private EnemyAttackState attackState;
        private EnemyStaggerStateNew staggerState;
        private EnemyDeadState deadState;

        // Runtime data
        private float currentHealth;

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

        // Player targeting system
        private GameStateManager gameStateManager;

        private ParasiteController parasiteController;
        private Transform parasiteTransform;
        private Transform hostTransform;
        private GameStateManager.GameMode lastKnownGameMode;

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

            UpdatePlayerTarget(); // Update player target based on game mode
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

            // Get game state manager for player targeting
            gameStateManager = ServiceLocator.Instance.GetService<GameStateManager>();

            // Find parasite _controller
            parasiteController = ServiceLocator.Instance.GetService<ParasiteController>();
            if (parasiteController != null)
            {
                parasiteTransform = parasiteController.transform;
            }

            // Initialize tracking variables
            lastKnownGameMode = GameStateManager.GameMode.Parasite;

            // Initialize player target
            UpdatePlayerTarget();
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

        #region Player Targeting System

        /// <summary>
        /// Updates the player target based on current game mode
        /// In Parasite mode: target the parasite
        /// In Host mode: target the possessed host
        /// </summary>
        private void UpdatePlayerTarget()
        {
            if (gameStateManager == null) return;

            GameStateManager.GameMode currentMode = gameStateManager.GetCurrentMode();

            // Only update if game mode has changed
            if (currentMode != lastKnownGameMode)
            {
                OnGameModeChanged(currentMode);
                lastKnownGameMode = currentMode;
            }

            // Ensure player reference is valid
            if (player == null)
            {
                SetPlayerTarget(currentMode);
            }
        }

        /// <summary>
        /// Called when game mode changes
        /// </summary>
        private void OnGameModeChanged(GameStateManager.GameMode newMode)
        {
            if (showDebugTargeting)
            {
                Debug.Log($"[EnemyController] {config.enemyName} switching target due to mode change: {lastKnownGameMode} -> {newMode}");
            }

            SetPlayerTarget(newMode);

            // If we had spotted the player before, we should re-evaluate
            if (hasSeenPlayer)
            {
                // Reset vision state to allow re-acquisition of new target
                hasSeenPlayer = false;
                timeSinceLastSaw = 0f;

                // Return to patrol to start fresh target acquisition
                agent.speed = config.patrolSpeed;
                ChangeState(patrolState);

                if (showDebugTargeting)
                {
                    Debug.Log($"[EnemyController] {config.enemyName} reset vision state for new target");
                }
            }
        }

        /// <summary>
        /// Sets the player target based on game mode
        /// </summary>
        private void SetPlayerTarget(GameStateManager.GameMode mode)
        {
            Transform previousTarget = player;

            switch (mode)
            {
                case GameStateManager.GameMode.Parasite:
                    // Target the parasite
                    if (parasiteTransform != null && parasiteTransform.gameObject.activeInHierarchy)
                    {
                        player = parasiteTransform;
                        if (showDebugTargeting)
                        {
                            Debug.Log($"[EnemyController] {config.enemyName} now targeting parasite: {player.name}");
                        }
                    }
                    else
                    {
                        // Try to find parasite if reference is lost
                        if (parasiteController == null)
                        {
                            parasiteController = ServiceLocator.Instance.GetService<ParasiteController>();
                        }

                        if (parasiteController != null)
                        {
                            parasiteTransform = parasiteController.transform;
                            player = parasiteTransform;
                            if (showDebugTargeting)
                            {
                                Debug.Log($"[EnemyController] {config.enemyName} found and targeting parasite: {player.name}");
                            }
                        }
                        else
                        {
                            player = null;
                            if (showDebugTargeting)
                            {
                                Debug.LogWarning($"[EnemyController] {config.enemyName} could not find parasite to target!");
                            }
                        }
                    }
                    break;

                case GameStateManager.GameMode.Host:
                    // Target the possessed host
                    // Use GameStateManager's CurrentHost property for more efficient access
                    if (gameStateManager != null && gameStateManager.CurrentHost != null)
                    {
                        hostTransform = gameStateManager.CurrentHost.transform;
                        player = hostTransform;
                        if (showDebugTargeting)
                        {
                            Debug.Log($"[EnemyController] {config.enemyName} now targeting host: {player.name}");
                        }
                    }
                    else
                    {
                        player = null;
                        if (showDebugTargeting)
                        {
                            Debug.LogWarning($"[EnemyController] {config.enemyName} could not find active host to target!");
                        }
                    }
                    break;
            }

            // If target changed, update last known position
            if (player != previousTarget && player != null)
            {
                lastKnownPlayerPosition = player.position;
            }
        }

        #endregion Player Targeting System

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

            // Check if there are obstacles blocking vision
            // Use RaycastAll to get all hits, then filter out the player/host
            Vector3 rayOrigin = transform.position + Vector3.up * 1.5f; // Eye level
            RaycastHit[] hits = Physics.RaycastAll(rayOrigin, directionToPlayer, distanceToPlayer, config.visionObstacleMask);

            // Filter out hits on the player/host - we only care about obstacles
            foreach (RaycastHit hit in hits)
            {
                // Skip if the hit object is the player/host we're looking for
                if (hit.transform == player || hit.transform.IsChildOf(player))
                    continue;

                // Skip if hit is on this enemy itself
                if (hit.transform == transform || hit.transform.IsChildOf(transform))
                    continue;

                // We hit something that's NOT the player - vision is blocked!
                if (showDebugTargeting)
                {
                    Debug.Log($"[EnemyController] {config.enemyName} vision blocked by {hit.collider.name}");
                }
                return;
            }

            // No obstacles blocking vision - player spotted!
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

            if (showDebugTargeting)
            {
                string targetName = player != null ? player.name : "unknown";
                Debug.Log($"[EnemyController] {config.enemyName} spotted target: {targetName}!");
            }
        }

        public void UpdateLastKnownPosition()
        {
            if (player == null) return;

            // Check if we can still see the player
            Vector3 directionToPlayer = (player.position - transform.position).normalized;
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);

            // Check sight range and obstacles
            bool canSeePlayer = false;

            if (distanceToPlayer <= config.sightRange)
            {
                // Use RaycastAll to check for obstacles, but ignore the player
                Vector3 rayOrigin = transform.position + Vector3.up;
                RaycastHit[] hits = Physics.RaycastAll(rayOrigin, directionToPlayer, distanceToPlayer, config.visionObstacleMask);

                bool visionBlocked = false;

                // Filter out hits on the player/host
                foreach (RaycastHit hit in hits)
                {
                    // Skip if the hit object is the player/host
                    if (hit.transform == player || hit.transform.IsChildOf(player))
                        continue;

                    // Skip if hit is on this enemy itself
                    if (hit.transform == transform || hit.transform.IsChildOf(transform))
                        continue;

                    // We hit an obstacle
                    visionBlocked = true;
                    break;
                }

                canSeePlayer = !visionBlocked;
            }

            if (canSeePlayer)
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

        public bool CanTransitionToAttackState()
        {
            // Don't allow attack state transition in Parasite mode
            if (gameStateManager != null && gameStateManager.GetCurrentMode() == GameStateManager.GameMode.Parasite)
            {
                return false;
            }

            // Allow attack state transition in Host mode or if gameStateManager is null (fail-safe)
            return true;
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
            if (player == null)
            {
                Debug.LogWarning($"[EnemyController] {config.enemyName} attempted to attack but player reference is null!");
                return;
            }

            if (gameStateManager == null)
            {
                Debug.LogWarning($"[EnemyController] {config.enemyName} cannot attack - GameStateManager is null!");
                return;
            }

            // Get current game mode
            GameStateManager.GameMode currentMode = gameStateManager.GetCurrentMode();

            // If in Parasite mode, exit attack state and return to patrol
            if (currentMode == GameStateManager.GameMode.Parasite)
            {
                if (showDebugTargeting)
                {
                    Debug.Log($"[EnemyController] {config.enemyName} detected Parasite mode - exiting attack state, returning to patrol");
                }

                // Reset aggro and return to patrol
                hasSeenPlayer = false;
                timeSinceLastSaw = 0f;
                agent.speed = config.patrolSpeed;
                ChangeState(patrolState);
                return;
            }

            // Only proceed with attack if in Host mode
            if (currentMode != GameStateManager.GameMode.Host)
            {
                Debug.LogWarning($"[EnemyController] {config.enemyName} in unknown game mode, cannot attack!");
                return;
            }

            // Perform raycast attack (Host mode only from here)
            Vector3 rayOrigin = projectileSpawnPoint != null ? projectileSpawnPoint.position : transform.position + Vector3.up * raycastOriginHeight;

            Vector3 directionToTarget = (player.position - rayOrigin).normalized;
            float distanceToTarget = Vector3.Distance(rayOrigin, player.position);

            // Perform the raycast using visionObstacleMask (same as vision detection)
            if (Physics.Raycast(rayOrigin, directionToTarget, out RaycastHit hit, config.attackRange, config.visionObstacleMask))
            {
                if (debugRaycast)
                {
                    Debug.DrawRay(rayOrigin, directionToTarget * hit.distance, Color.red, 1f);
                    Debug.Log($"[EnemyController] {config.enemyName} raycast hit: {hit.collider.name} at distance {hit.distance:F2}");
                }

                // Check if the hit object or its parents have IDamageable
                IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();
                if (damageable != null)
                {
                    // In Host mode: Only damage the current possessed host
                    HostController currentHost = gameStateManager.CurrentHostController;
                    HostController hitHost = hit.collider.GetComponentInParent<HostController>();

                    if (hitHost != null && hitHost == currentHost)
                    {
                        hitHost.TakeDamage(config.attackDamage);
                        Debug.Log($"[EnemyController] {config.enemyName} successfully damaged possessed host for {config.attackDamage} damage!");
                    }
                    else if (debugRaycast)
                    {
                        Debug.Log($"[EnemyController] {config.enemyName} hit IDamageable but it's not the current possessed host. Ignoring.");
                    }
                }
                else
                {
                    if (debugRaycast)
                    {
                        Debug.Log($"[EnemyController] {config.enemyName} hit {hit.collider.name} but it's not IDamageable (checked parents too).");
                    }
                }
            }
            else
            {
                // Raycast didn't hit anything
                if (debugRaycast)
                {
                    Debug.DrawRay(rayOrigin, directionToTarget * config.attackRange, Color.yellow, 1f);
                    Debug.Log($"[EnemyController] {config.enemyName} raycast attack missed!");
                }
            }
        }

        /// <summary>
        /// Apply damage to enemy with pain chance system
        /// </summary>
        public void TakeDamage(float damage)
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
                OnPlayerSpotted();
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
            audioSource.PlayOneShot(deathSound);

            ChangeState(deadState);

            if (TryGetComponent<Collider>(out var collider))
            {
                collider.enabled = false;
            }

            if (deathEffect)
                Instantiate(deathEffect, transform.position, Quaternion.identity);
            // TODO: Implement ragdoll or death animation
            // For now, just destroy
            Destroy(gameObject);
        }

        #endregion Combat System

        #region Inspector Debug Functions

        /// <summary>
        /// Take damage function that can be called from inspector for testing
        /// </summary>
        [ContextMenu("Take 10 Damage")]
        public void TakeDamageFromInspector()
        {
            TakeDamageFromInspector(10f);
        }

        /// <summary>
        /// Take custom damage amount - can be called from inspector or code
        /// </summary>
        [ContextMenu("Take 25 Damage")]
        public void TakeDamage25FromInspector()
        {
            TakeDamageFromInspector(25f);
        }

        /// <summary>
        /// Take half health damage - can be called from inspector
        /// </summary>
        [ContextMenu("Take Half Health Damage")]
        public void TakeHalfHealthDamage()
        {
            float halfHealth = Mathf.Max(1, currentHealth / 2);
            TakeDamageFromInspector(halfHealth);
        }

        /// <summary>
        /// Kill enemy instantly - can be called from inspector
        /// </summary>
        [ContextMenu("Kill Enemy")]
        public void KillEnemyFromInspector()
        {
            TakeDamageFromInspector(currentHealth);
        }

        /// <summary>
        /// Internal function to handle inspector damage calls
        /// </summary>
        private void TakeDamageFromInspector(float damage)
        {
            if (isDead)
            {
                Debug.LogWarning($"[EnemyController] {config.enemyName ?? gameObject.name} is already dead!");
                return;
            }

            Debug.Log($"[EnemyController] {config.enemyName ?? gameObject.name} taking {damage} damage from inspector. Health: {currentHealth} -> {currentHealth - damage}");

            // Use the existing TakeDamage function with enemy's current position as hit point
            TakeDamage(damage);
        }

        /// <summary>
        /// Get current health - useful for inspector debugging
        /// </summary>
        [ContextMenu("Show Current Health")]
        public void ShowCurrentHealth()
        {
            if (config != null)
            {
                Debug.Log($"[EnemyController] {config.enemyName} Health: {currentHealth}/{config.maxHealth} ({(float)currentHealth / config.maxHealth * 100:F1}%)");
            }
            else
            {
                Debug.Log($"[EnemyController] {gameObject.name} Health: {currentHealth} (no config found)");
            }
        }

        /// <summary>
        /// Reset enemy to full health - useful for testing
        /// </summary>
        [ContextMenu("Reset to Full Health")]
        public void ResetToFullHealth()
        {
            if (config != null)
            {
                currentHealth = config.maxHealth;
                isDead = false;
                isStaggered = false;

                // Re-enable components if they were disabled
                if (agent != null) agent.enabled = true;

                Debug.Log($"[EnemyController] {config.enemyName} health reset to {currentHealth}");
            }
            else
            {
                Debug.LogWarning($"[EnemyController] Cannot reset health - no config found!");
            }
        }

        #endregion Inspector Debug Functions

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

            // Draw line to current target
            if (player != null && showDebugTargeting)
            {
                Gizmos.color = hasSeenPlayer ? Color.red : Color.gray;
                Gizmos.DrawLine(transform.position, player.position);
            }
        }

        #endregion Gizmos
    }
}