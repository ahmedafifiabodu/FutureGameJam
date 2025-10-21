using UnityEngine;

namespace AI.EnemyTypes
{
    /// <summary>
    /// Basic Enemy: Sees where player was a second ago and goes there.
    /// When reaches close range, begins a bayonet thrust.
    /// Cannot move while performing attack.
    /// After attack, takes a moment to rest before moving again.
    /// </summary>
    public class BasicEnemy : EnemyAI
    {
        [Header("Basic Enemy Settings")]
        [SerializeField] private float delayedPositionTime = 1f; // How old the position we're moving to

        [SerializeField] private float attackWindupTime = 0.3f; // Time before thrust
        [SerializeField] private float thrustDistance = 2f; // How far the bayonet reaches

        private Vector3 delayedPlayerPosition;
        private float attackTimer;
        private float positionUpdateTimer;

        protected override void Start()
        {
            base.Start();

            // Initialize to current position (will be updated when player is detected)
            if (player)
                delayedPlayerPosition = player.position;
            else
                delayedPlayerPosition = transform.position;
        }

        protected override void UpdateChasing()
        {
            if (!player) return;

            // Don't allow state transitions if currently attacking
            if (isAttacking)
            {
                if (enableDebugLogs)
                    Debug.Log($"[BasicEnemy] Still attacking, skipping chase update");
                return;
            }

            // Check if player is still in valid range (room-based containment)
            if (roomTracker && !roomTracker.IsPlayerInSameRoomOrCorridor())
            {
                LoseAggro();
                return;
            }

            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
          
            // CLOSE PROXIMITY CHECK - If player is very close, always target their actual position
            // This prevents enemy from running past player to old delayed position
            float closeProximityRange = 3f;
            bool playerIsVeryClose = distanceToPlayer <= closeProximityRange;

            // Update last known position if player is visible OR very close
            bool canSeePlayerNow = CanSeePlayer();
            if (canSeePlayerNow || playerIsVeryClose)
            {
                lastSeenPlayerTime = Time.time;

                // Update the delayed position on a timer to simulate slower reaction
                // BUT if player is very close, update immediately
                if (playerIsVeryClose)
                {
                    // When player is very close, track them more accurately
                    delayedPlayerPosition = player.position;
                    positionUpdateTimer = 0f;
        
                    if (enableDebugLogs && Time.frameCount % 60 == 0)
                        Debug.Log($"[BasicEnemy] Player VERY CLOSE - tracking actual position: {delayedPlayerPosition}");
                }
                else
                {
                    positionUpdateTimer += Time.deltaTime;
                    if (positionUpdateTimer >= delayedPositionTime)
                    {
                        delayedPlayerPosition = player.position;
                        positionUpdateTimer = 0f;

                        if (enableDebugLogs)
                            Debug.Log($"[BasicEnemy] Updated delayed position to {delayedPlayerPosition}");
                    }
                }
            }
            else
            {
                // Lose aggro if player hasn't been seen for too long
                if (Time.time - lastSeenPlayerTime > profile.loseAggroTime)
                {
                    LoseAggro();
                    return;
                }
           
                // If we can't see player, don't update delayed position - just move to last known spot
                if (enableDebugLogs && Time.frameCount % 60 == 0)
                    Debug.Log($"[BasicEnemy] Can't see player, moving to last delayed position");
            }

            // Move towards delayed position (where player was)
            if (navAgent && navAgent.isActiveAndEnabled && navAgent.isOnNavMesh)
            {
                // Only update destination if not already close to it
                float distanceToDestination = Vector3.Distance(transform.position, delayedPlayerPosition);
           
                if (distanceToDestination > navAgent.stoppingDistance + 0.5f)
                {
                    navAgent.SetDestination(delayedPlayerPosition);
                }
                else if (!canSeePlayerNow && !playerIsVeryClose && distanceToDestination <= navAgent.stoppingDistance)
                {
                    // We reached the delayed position but can't see player AND player is not close - lose aggro
                    if (enableDebugLogs)
                        Debug.Log($"[BasicEnemy] Reached delayed position, player not visible and not close");
                    LoseAggro();
                    return;
                }

                if (enableDebugLogs && Time.frameCount % 60 == 0)
                    Debug.DrawLine(transform.position, delayedPlayerPosition, Color.blue, 0.1f);
            }

            // Check if in attack range - Use stopping distance OR attack range, whichever is larger
            float effectiveAttackRange = Mathf.Max(profile.attackRange, navAgent.stoppingDistance);
            
            // Only attack if within range AND we've stopped moving (velocity near zero)
            bool hasStoppedMoving = navAgent.velocity.magnitude < 0.1f;
        
            // Require minimum chase time before attacking (prevents instant attacks on aggro)
            float minChaseTime = 0.5f;
            bool hasBeenChasingLongEnough = (Time.time - chaseStartTime) >= minChaseTime;
    
            // Attack if: in range, stopped, chased long enough, AND (can see player OR player is very close)
            // This allows attacking even if player moved behind us at close range
            if (distanceToPlayer <= effectiveAttackRange && 
                hasStoppedMoving && 
                hasBeenChasingLongEnough && 
                (canSeePlayerNow || playerIsVeryClose) && 
                CanAttack())
            {
                ChangeState(EnemyState.Attacking);
             }
        }

        protected override void OnStateEnter(EnemyState state)
        {
            base.OnStateEnter(state);

            if (state == EnemyState.Chasing)
            {
                // Immediately set delayed position to current player position when starting chase
                if (player)
                {
                    delayedPlayerPosition = player.position;
                    positionUpdateTimer = 0f;

                    if (enableDebugLogs)
                        Debug.Log($"[BasicEnemy] Started chasing! Initial target: {delayedPlayerPosition}");
                }
            }
            else if (state == EnemyState.Attacking)
            {
                attackTimer = 0f;
                TriggerAttack();
            }
        }

        protected override void UpdateAttacking()
        {
            // Stop movement
            if (navAgent && navAgent.isActiveAndEnabled)
                navAgent.isStopped = true;

            // Face player
            if (player)
            {
                Vector3 directionToPlayer = (player.position - transform.position).normalized;
                directionToPlayer.y = 0;

                if (directionToPlayer != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * profile.rotationSpeed);
                }
            }

            // Windup phase
            attackTimer += Time.deltaTime;

            if (attackTimer >= attackWindupTime && attackTimer < attackWindupTime + 0.1f)
            {
                // Execute bayonet thrust
                PerformBayonetThrust();
            }

            // End attack after full duration
            if (attackTimer >= profile.attackDuration)
            {
                attackTimer = 0f;
                EndAttack();
            }
        }

        private void PerformBayonetThrust()
        {
            // Raycast forward to check for player hit
            Ray thrustRay = new Ray(transform.position + Vector3.up, transform.forward);

            if (Physics.Raycast(thrustRay, out RaycastHit hit, thrustDistance))
            {
                var damageable = hit.collider.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    DealDamage();

                    if (enableDebugLogs)
                        Debug.Log($"[BasicEnemy] Bayonet thrust hit {hit.collider.name}!");
                }
            }

            if (enableDebugLogs)
                Debug.DrawRay(thrustRay.origin, thrustRay.direction * thrustDistance, Color.red, 1f);
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();

            if (!drawGizmos) return;

            // Draw thrust range
            Gizmos.color = Color.magenta;
            Gizmos.DrawRay(transform.position + Vector3.up, transform.forward * thrustDistance);

            // Draw delayed player position
            if (Application.isPlaying && hasAggro)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(delayedPlayerPosition, 0.3f);
                Gizmos.DrawLine(transform.position, delayedPlayerPosition);
            }
        }
    }
}