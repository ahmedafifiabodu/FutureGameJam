using UnityEngine;

namespace AI
{
    /// <summary>
    /// Enemy AI states
    /// </summary>
    public enum EnemyState
    {
        Idle,           // Standing still, playing idle animation
        Patrol,         // Walking between waypoints
        Chasing,        // Moving towards player
        Attacking,      // Performing attack
        Staggered,      // Stunned from pain chance
        Dead            // Died
    }

    /// <summary>
    /// Base enemy AI controller using state machine pattern
    /// Handles pathfinding, aggro, and state management
    /// Specific attack behaviors are implemented in derived classes
    /// </summary>
    [RequireComponent(typeof(UnityEngine.AI.NavMeshAgent))]
    [RequireComponent(typeof(EnemyHealth))]
    public abstract class EnemyAI : MonoBehaviour
    {
        [Header("Profile")]
        [SerializeField] protected EnemyProfile profile;

        [Header("References")]
        [SerializeField] protected Transform player;

        [SerializeField] protected Animator animator;
        [SerializeField] protected AudioSource audioSource;

        [Header("Debug")]
        [SerializeField] protected bool enableDebugLogs = false;

        [SerializeField] protected bool drawGizmos = true;

        // Components
        protected UnityEngine.AI.NavMeshAgent navAgent;

        protected EnemyHealth health;
        protected EnemyRoomTracker roomTracker;

        // State
        protected EnemyState currentState = EnemyState.Idle;

        protected EnemyState previousState;

        // Aggro tracking
        protected bool hasAggro = false;

        protected float aggroTimer = 0f;
        protected float lastSeenPlayerTime = -999f;
        protected Vector3 lastKnownPlayerPosition;

        // Attack tracking
        protected float lastAttackTime = -999f;

        protected bool isAttacking = false;
        protected float chaseStartTime = -999f; // Track when chase state started

        // Animation parameter hashes (cached for performance)
        protected int isMovingHash;

        protected int isChasingHash;
        protected int attackTriggerHash;
        protected int staggerTriggerHash;
        protected int deathTriggerHash;

        protected virtual void Awake()
        {
            navAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            health = GetComponent<EnemyHealth>();
            roomTracker = GetComponent<EnemyRoomTracker>();

            if (!animator)
                animator = GetComponent<Animator>();

            if (!audioSource)
                audioSource = GetComponent<AudioSource>();

            // Cache animation parameter hashes using GameConstant
            isMovingHash = Animator.StringToHash(GameConstant.AnimationParameters.IsMoving);
            isChasingHash = Animator.StringToHash(GameConstant.AnimationParameters.IsChasing);
            attackTriggerHash = Animator.StringToHash(GameConstant.AnimationParameters.Attack);
            staggerTriggerHash = Animator.StringToHash(GameConstant.AnimationParameters.Stagger);
            deathTriggerHash = Animator.StringToHash(GameConstant.AnimationParameters.Death);

            // Subscribe to health events
            if (health)
            {
                health.OnStaggered.AddListener(OnStaggered);
                health.OnDeath.AddListener(OnDeath);
            }
        }

        protected virtual void Start()
        {
            if (!profile)
            {
                Debug.LogError($"[EnemyAI] No profile assigned to {gameObject.name}!");
                enabled = false;
                return;
            }

            // Apply profile settings to health
            if (health)
            {
                health.SetMaxHealth(profile.maxHealth);
                health.SetPainChance(profile.painChance);
            }

            // Configure NavMeshAgent - ensure it's properly initialized
            if (navAgent)
            {
                // Wait for NavMeshAgent to be placed on NavMesh if needed
                if (!navAgent.isOnNavMesh)
                {
                    Debug.LogWarning($"[EnemyAI] {gameObject.name} NavMeshAgent not on NavMesh at start! Position: {transform.position}");

                    // Try to warp to nearest NavMesh position
                    if (UnityEngine.AI.NavMesh.SamplePosition(transform.position, out UnityEngine.AI.NavMeshHit hit, 5f, UnityEngine.AI.NavMesh.AllAreas))
                    {
                        transform.position = hit.position;
                        Debug.Log($"[EnemyAI] {gameObject.name} warped to NavMesh at {hit.position}");
                    }
                }

                navAgent.speed = profile.patrolSpeed;
                navAgent.angularSpeed = profile.rotationSpeed;
                navAgent.stoppingDistance = profile.attackRange * 0.8f;

                // Enable avoidance to prevent enemies from pushing each other
                navAgent.obstacleAvoidanceType = UnityEngine.AI.ObstacleAvoidanceType.HighQualityObstacleAvoidance;
                navAgent.avoidancePriority = Random.Range(30, 70); // Randomize priority so enemies don't all defer to each other
                navAgent.radius = 0.5f; // Ensure proper spacing

                navAgent.enabled = true;

                if (enableDebugLogs)
                    Debug.Log($"[EnemyAI] {gameObject.name} NavMeshAgent configured - OnNavMesh: {navAgent.isOnNavMesh}, Speed: {navAgent.speed}, Stopping Distance: {navAgent.stoppingDistance}, Enabled: {navAgent.enabled}");
            }
            else
            {
                Debug.LogError($"[EnemyAI] {gameObject.name} has no NavMeshAgent component!");
            }

            // Find player if not assigned
            if (!player)
            {
                var parasiteController = ServiceLocator.Instance.GetService<ParasiteController>();
                if (parasiteController != null)
                {
                    player = parasiteController.transform;

                    if (enableDebugLogs)
                        Debug.Log($"[EnemyAI] {gameObject.name} found player at {player.position}");
                }
                else
                {
                    Debug.LogWarning($"[EnemyAI] {gameObject.name} could not find player!");
                }
            }

            // Start in idle state
            ChangeState(EnemyState.Idle);
        }

        protected virtual void Update()
        {
            if (health && health.IsDead) return;

            // Update state machine
            switch (currentState)
            {
                case EnemyState.Idle:
                    UpdateIdle();
                    break;

                case EnemyState.Patrol:
                    UpdatePatrol();
                    break;

                case EnemyState.Chasing:
                    UpdateChasing();
                    break;

                case EnemyState.Attacking:
                    UpdateAttacking();
                    break;

                case EnemyState.Staggered:
                    UpdateStaggered();
                    break;
            }

            // Update animations
            UpdateAnimations();
        }

        #region State Machine

        protected virtual void ChangeState(EnemyState newState)
        {
            if (currentState == newState) return;

            // Exit current state
            OnStateExit(currentState);

            previousState = currentState;
            currentState = newState;

            // Enter new state
            OnStateEnter(newState);

            if (enableDebugLogs)
                Debug.Log($"[EnemyAI] {gameObject.name} changed state: {previousState} -> {currentState}");
        }

        protected virtual void OnStateEnter(EnemyState state)
        {
            switch (state)
            {
                case EnemyState.Idle:
                    if (navAgent && navAgent.isActiveAndEnabled && navAgent.isOnNavMesh)
                        navAgent.isStopped = true;
                    PlaySound(profile.idleSound);
                    break;

                case EnemyState.Patrol:
                    if (navAgent && navAgent.isActiveAndEnabled && navAgent.isOnNavMesh)
                    {
                        navAgent.isStopped = false;
                        navAgent.speed = profile.patrolSpeed;
                    }
                    break;

                case EnemyState.Chasing:
                    if (navAgent && navAgent.isActiveAndEnabled && navAgent.isOnNavMesh)
                    {
                        navAgent.isStopped = false;
                        navAgent.speed = profile.chaseSpeed;

                        // Track when chase started
                        chaseStartTime = Time.time;

                        if (enableDebugLogs)
                            Debug.Log($"[EnemyAI] {gameObject.name} entering chase state - NavAgent enabled: {navAgent.enabled}, isStopped: {navAgent.isStopped}, speed: {navAgent.speed}");
                    }
                    else
                    {
                        Debug.LogWarning($"[EnemyAI] {gameObject.name} cannot chase - NavAgent issues: enabled={navAgent?.enabled}, isOnNavMesh={navAgent?.isOnNavMesh}");
                    }
                    PlaySound(profile.chaseSound);
                    break;

                case EnemyState.Attacking:
                    if (navAgent && navAgent.isActiveAndEnabled && navAgent.isOnNavMesh)
                        navAgent.isStopped = true;
                    isAttacking = true;
                    break;

                case EnemyState.Staggered:
                    if (navAgent && navAgent.isActiveAndEnabled && navAgent.isOnNavMesh)
                        navAgent.isStopped = true;
                    break;
            }
        }

        protected virtual void OnStateExit(EnemyState state)
        {
            switch (state)
            {
                case EnemyState.Attacking:
                    // Ensure attacking flag is cleared when leaving attack state
                    isAttacking = false;
                    break;
            }
        }

        #endregion State Machine

        #region State Updates

        protected virtual void UpdateIdle()
        {
            // Check for player aggro
            CheckPlayerDetection();

            // Transition to patrol if no aggro
            // (Override in derived classes for custom patrol behavior)
        }

        protected virtual void UpdatePatrol()
        {
            // Check for player aggro
            CheckPlayerDetection();

            // Override in derived classes for waypoint patrol
        }

        protected virtual void UpdateChasing()
        {
            if (!player) return;

            // Don't allow state transitions if currently attacking
            if (isAttacking)
            {
                if (enableDebugLogs && Time.frameCount % 60 == 0)
                    Debug.Log($"[EnemyAI] {gameObject.name} still in attack, skipping chase update");
                return;
            }

            // Check if player is still in valid range (room-based containment)
            if (roomTracker && !roomTracker.IsPlayerInSameRoomOrCorridor())
            {
                LoseAggro();
                return;
            }

            // Update last known position if player is visible
            if (CanSeePlayer())
            {
                lastSeenPlayerTime = Time.time;
                lastKnownPlayerPosition = player.position;
            }
            else
            {
                // Lose aggro if player hasn't been seen for too long
                if (Time.time - lastSeenPlayerTime > profile.loseAggroTime)
                {
                    LoseAggro();
                    return;
                }
            }

            // Move towards last known position
            if (navAgent && navAgent.isActiveAndEnabled && navAgent.isOnNavMesh)
            {
                // Only update destination if not already close to it
                // This prevents NavAgent from constantly recalculating path
                float distanceToDestination = Vector3.Distance(transform.position, lastKnownPlayerPosition);

                if (distanceToDestination > navAgent.stoppingDistance + 0.5f)
                {
                    navAgent.SetDestination(lastKnownPlayerPosition);
                }

                if (enableDebugLogs && Time.frameCount % 60 == 0) // Log once per second (at 60fps)
                {
                    Debug.Log($"[EnemyAI] {gameObject.name} chasing - Destination: {lastKnownPlayerPosition}, Distance: {Vector3.Distance(transform.position, lastKnownPlayerPosition):F2}, Velocity: {navAgent.velocity.magnitude:F2}");
                }
            }
            else if (enableDebugLogs && Time.frameCount % 60 == 0)
            {
                Debug.LogWarning($"[EnemyAI] {gameObject.name} cannot move - NavAgent: {(navAgent == null ? "null" : $"enabled={navAgent.enabled}, onNavMesh={navAgent.isOnNavMesh}")}");
            }

            // Check if in attack range AND respects stopping distance
            // Only attack if we're within stopping distance (not trying to get closer)
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);

            // Use stopping distance OR attack range, whichever is larger
            float effectiveAttackRange = Mathf.Max(profile.attackRange, navAgent.stoppingDistance);

            // Only attack if within range AND we've stopped moving (velocity near zero)
            bool hasStoppedMoving = navAgent.velocity.magnitude < 0.1f;

            // Require minimum chase time before attacking (prevents instant attacks on aggro)
            float minChaseTime = 0.3f; // At least 0.3 seconds of chasing before attack
            bool hasBeenChasingLongEnough = (Time.time - chaseStartTime) >= minChaseTime;

            if (distanceToPlayer <= effectiveAttackRange && hasStoppedMoving && hasBeenChasingLongEnough && CanAttack())
            {
                ChangeState(EnemyState.Attacking);
            }
        }

        protected virtual void UpdateAttacking()
        {
            // Face player during attack
            if (player)
            {
                Vector3 directionToPlayer = (player.position - transform.position).normalized;
                directionToPlayer.y = 0; // Keep rotation on XZ plane

                if (directionToPlayer != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * profile.rotationSpeed);
                }
            }

            // Derived classes implement specific attack logic
        }

        protected virtual void UpdateStaggered()
        {
            // Wait for stagger to end (handled by EnemyHealth)
        }

        #endregion State Updates

        #region Detection & Aggro

        protected virtual void CheckPlayerDetection()
        {
            if (!player)
            {
                if (enableDebugLogs && Time.frameCount % 120 == 0)
                    Debug.LogWarning($"[EnemyAI] {gameObject.name} - No player reference!");
                return;
            }

            // Check if player is in same room/corridor
            if (roomTracker && !roomTracker.IsPlayerInSameRoomOrCorridor())
            {
                if (enableDebugLogs && Time.frameCount % 120 == 0)
                    Debug.Log($"[EnemyAI] {gameObject.name} - Player not in same room/corridor");
                return; // Player is in different room
            }

            float distanceToPlayer = Vector3.Distance(transform.position, player.position);

            if (enableDebugLogs && Time.frameCount % 120 == 0)
            {
                Debug.Log($"[EnemyAI] {gameObject.name} - Distance to player: {distanceToPlayer:F2}, Instant aggro range: {profile.instantAggroRange}, Can see: {CanSeePlayer()}");
            }

            // CLOSE PROXIMITY DETECTION - Player very close regardless of vision (simulates hearing/awareness)
            float closeProximityRange = 2.5f; // Player is extremely close
            if (distanceToPlayer <= closeProximityRange)
            {
                if (enableDebugLogs && Time.frameCount % 120 == 0)
                    Debug.Log($"[EnemyAI] {gameObject.name} - Player in CLOSE PROXIMITY! Gaining aggro regardless of vision");
                GainAggro();
                return;
            }

            // Instant aggro range (requires vision)
            if (distanceToPlayer <= profile.instantAggroRange)
            {
                if (CanSeePlayer())
                {
                    GainAggro();
                }
                else if (enableDebugLogs && Time.frameCount % 120 == 0)
                {
                    Debug.Log($"[EnemyAI] {gameObject.name} - Player in instant aggro range but not visible");
                }
            }
            // Delayed aggro range (requires vision)
            else if (distanceToPlayer <= profile.delayedAggroRange)
            {
                if (CanSeePlayer())
                {
                    aggroTimer += Time.deltaTime;
                    if (aggroTimer >= profile.aggroDelay)
                    {
                        GainAggro();
                    }
                    else if (enableDebugLogs && Time.frameCount % 120 == 0)
                    {
                        Debug.Log($"[EnemyAI] {gameObject.name} - Building aggro: {aggroTimer:F2}/{profile.aggroDelay}");
                    }
                }
                else
                {
                    aggroTimer = 0f;
                }
            }
            else
            {
                aggroTimer = 0f;
            }
        }

        protected virtual bool CanSeePlayer()
        {
            if (!player) return false;

            Vector3 directionToPlayer = player.position - transform.position;
            float angle = Vector3.Angle(transform.forward, directionToPlayer);

            // Check if within vision cone
            if (angle > profile.visionAngle * 0.5f)
            {
                if (enableDebugLogs && Time.frameCount % 120 == 0)
                    Debug.Log($"[EnemyAI] {gameObject.name} - Player outside vision cone (angle: {angle:F1}° > {profile.visionAngle * 0.5f:F1}°)");
                return false;
            }

            // Raycast to check line of sight
            // Use position at enemy's center and aim towards player's center (adjusted for small parasite)
            Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;
            Vector3 targetPosition = player.position + Vector3.up * 0.25f; // Lower target for parasite
            Vector3 rayDirection = (targetPosition - rayOrigin).normalized;
            float rayDistance = Vector3.Distance(rayOrigin, targetPosition);

            if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, rayDistance))
            {
                // Check if we hit the player or any of its children
                if (hit.transform == player ||
                    hit.transform.IsChildOf(player) ||
                    hit.transform.root == player.root ||
                    hit.collider.CompareTag(GameConstant.Tags.Player))
                {
                    if (enableDebugLogs && Time.frameCount % 120 == 0)
                        Debug.Log($"[EnemyAI] {gameObject.name} - CAN SEE PLAYER (hit: {hit.collider.name})");
                    return true;
                }

                // Debug: Log what we hit instead
                if (enableDebugLogs && Time.frameCount % 120 == 0)
                    Debug.Log($"[EnemyAI] {gameObject.name} - Vision blocked by {hit.collider.name} (tag: {hit.collider.tag}, distance: {hit.distance:F2})");

                return false;
            }
            else
            {
                // No hit = clear line of sight (player is visible)
                // This handles cases where player has no collider in raycast path
                if (enableDebugLogs && Time.frameCount % 120 == 0)
                    Debug.Log($"[EnemyAI] {gameObject.name} - CAN SEE PLAYER (no raycast hit)");
                return true;
            }
        }

        protected virtual void GainAggro()
        {
            if (hasAggro) return;

            hasAggro = true;
            lastSeenPlayerTime = Time.time;

            if (player)
            {
                lastKnownPlayerPosition = player.position;
            }

            ChangeState(EnemyState.Chasing);

            if (enableDebugLogs)
                Debug.Log($"[EnemyAI] {gameObject.name} gained aggro on player at position {lastKnownPlayerPosition}!");
        }

        protected virtual void LoseAggro()
        {
            if (!hasAggro) return;

            hasAggro = false;
            aggroTimer = 0f;

            ChangeState(EnemyState.Idle);

            if (enableDebugLogs)
                Debug.Log($"[EnemyAI] {gameObject.name} lost aggro!");
        }

        #endregion Detection & Aggro

        #region Attack

        protected virtual bool CanAttack()
        {
            return Time.time >= lastAttackTime + profile.attackCooldown && !isAttacking;
        }

        /// <summary>
        /// Called by derived classes to trigger attack
        /// </summary>
        protected virtual void TriggerAttack()
        {
            lastAttackTime = Time.time;

            if (animator)
                animator.SetTrigger(attackTriggerHash);

            PlaySound(profile.attackSound);

            // Spawn attack effect
            if (profile.attackEffectPrefab)
            {
                Instantiate(profile.attackEffectPrefab, transform.position + transform.forward, Quaternion.identity);
            }
        }

        /// <summary>
        /// Called by animation event or timer to deal damage
        /// </summary>
        protected virtual void DealDamage()
        {
            if (!player) return;

            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            if (distanceToPlayer <= profile.attackRange)
            {
                // Try to damage player
                if (player.TryGetComponent<IDamageable>(out var playerHealth))
                {
                    playerHealth.TakeDamage(profile.attackDamage);

                    if (enableDebugLogs)
                        Debug.Log($"[EnemyAI] {gameObject.name} dealt {profile.attackDamage} damage to player!");
                }
            }
        }

        /// <summary>
        /// Called when attack animation finishes
        /// </summary>
        protected virtual void EndAttack()
        {
            isAttacking = false;

            // Return to chasing after attack recovery
            Invoke(nameof(ReturnToChase), profile.attackRecoveryTime);
        }

        protected virtual void ReturnToChase()
        {
            // Only return to chase if we're still in attacking state and have aggro
            if (currentState == EnemyState.Attacking && hasAggro)
            {
                isAttacking = false; // Clear attacking flag before state change
                ChangeState(EnemyState.Chasing);
            }
            else
            {
                // Just clear the flag if state already changed
                isAttacking = false;
            }
        }

        #endregion Attack

        #region Health Callbacks

        public virtual void OnStaggered()
        {
            if (currentState != EnemyState.Staggered && currentState != EnemyState.Dead)
            {
                ChangeState(EnemyState.Staggered);
                PlaySound(profile.hurtSound);
            }
        }

        public virtual void OnStaggerEnded()
        {
            if (currentState == EnemyState.Staggered)
            {
                // Return to previous state (usually chasing or idle)
                if (hasAggro)
                    ChangeState(EnemyState.Chasing);
                else
                    ChangeState(EnemyState.Idle);
            }
        }

        public virtual void OnDeath()
        {
            ChangeState(EnemyState.Dead);
            PlaySound(profile.deathSound);

            if (animator)
                animator.SetTrigger(deathTriggerHash);

            // Disable NavMeshAgent
            if (navAgent)
                navAgent.enabled = false;

            // Disable this script
            enabled = false;
        }

        #endregion Health Callbacks

        #region Animations

        protected virtual void UpdateAnimations()
        {
            if (!animator) return;

            bool isMoving = navAgent.velocity.magnitude > 0.1f;
            bool isChasing = currentState == EnemyState.Chasing;

            animator.SetBool(isMovingHash, isMoving);
            animator.SetBool(isChasingHash, isChasing);
        }

        #endregion Animations

        #region Audio

        protected virtual void PlaySound(AudioClip clip)
        {
            if (audioSource && clip)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        #endregion Audio

        #region Gizmos

        protected virtual void OnDrawGizmosSelected()
        {
            if (!drawGizmos || !profile) return;

            // Draw aggro ranges
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, profile.instantAggroRange);

            Gizmos.color = Color.orange;
            Gizmos.DrawWireSphere(transform.position, profile.delayedAggroRange);

            // Draw attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, profile.attackRange);

            // Draw vision cone
            if (Application.isPlaying && player)
            {
                Gizmos.color = CanSeePlayer() ? Color.green : Color.red;
                Vector3 directionToPlayer = (player.position - transform.position).normalized;
                Gizmos.DrawLine(transform.position, transform.position + directionToPlayer * 5f);
            }

            // Draw last known player position
            if (Application.isPlaying && hasAggro)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(lastKnownPlayerPosition, 0.5f);
            }
        }

        #endregion Gizmos
    }
}