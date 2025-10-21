using UnityEngine;

namespace AI.EnemyTypes
{
    /// <summary>
    /// Fast Enemy: Walks towards player when close or very far.
    /// When in medium range, predicts where player will be and jumps there.
    /// When in melee range, quickly stabs the player.
    /// </summary>
    public class FastEnemy : EnemyAI
    {
        [Header("Fast Enemy Settings")]
        [SerializeField] private float mediumRangeMin = 5f; // Min distance for jump attack

        [SerializeField] private float mediumRangeMax = 12f; // Max distance for jump attack
        [SerializeField] private float jumpHeight = 3f; // Height of jump arc
        [SerializeField] private float jumpDuration = 0.8f; // Time of jump
        [SerializeField] private float jumpPredictionMultiplier = 1.2f; // How far ahead to predict
        [SerializeField] private float jumpCooldown = 3f; // Cooldown between jumps
        [SerializeField] private float stabWindupTime = 0.2f; // Quick stab windup
        [SerializeField] private float stabRange = 1.5f; // Melee range for stab

        private bool isJumping;
        private float jumpTimer;
        private Vector3 jumpStartPosition;
        private Vector3 jumpTargetPosition;
        private float lastJumpTime = -999f;

        private float stabTimer;

        protected override void UpdateChasing()
        {
            if (!player) return;

            // Don't allow state transitions if currently attacking
            if (isAttacking)
            {
                if (enableDebugLogs)
                    Debug.Log($"[FastEnemy] Still attacking, skipping chase update");
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

            // Handle jumping state
            if (isJumping)
            {
                UpdateJump();
                return;
            }

            float distanceToPlayer = Vector3.Distance(transform.position, player.position);

            // Melee range - quick stab (ONLY if we can attack - respects cooldown AND stopping distance)
            float effectiveStabRange = Mathf.Max(stabRange, navAgent.stoppingDistance);
            bool hasStoppedMoving = navAgent.velocity.magnitude < 0.1f;
            
            // Require minimum chase time before attacking
            float minChaseTime = 0.3f;
            bool hasBeenChasingLongEnough = (Time.time - chaseStartTime) >= minChaseTime;
            
            if (distanceToPlayer <= effectiveStabRange && hasStoppedMoving && hasBeenChasingLongEnough && CanAttack())
            {
                ChangeState(EnemyState.Attacking);
                return;
            }

            // Medium range - jump attack
            if (distanceToPlayer >= mediumRangeMin && distanceToPlayer <= mediumRangeMax)
            {
                if (CanJump())
                {
                    StartJumpAttack();
                    return;
                }
            }

            // Close or far range - walk normally
            // Only update destination if not already close to it
            if (navAgent && navAgent.isActiveAndEnabled && navAgent.isOnNavMesh)
            {
                float distanceToDestination = Vector3.Distance(transform.position, player.position);
                
                if (distanceToDestination > navAgent.stoppingDistance + 0.5f)
                {
                    navAgent.SetDestination(player.position);
                }
            }

            // Check if in attack range for stab (respects stopping distance)
            float effectiveAttackRange = Mathf.Max(profile.attackRange, navAgent.stoppingDistance);
            if (distanceToPlayer <= effectiveAttackRange && hasStoppedMoving && hasBeenChasingLongEnough && CanAttack())
            {
                ChangeState(EnemyState.Attacking);
            }
        }

        protected override void UpdateAttacking()
        {
            // Stop movement
            navAgent.isStopped = true;

            // Face player
            if (player)
            {
                Vector3 directionToPlayer = (player.position - transform.position).normalized;
                directionToPlayer.y = 0;

                if (directionToPlayer != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * profile.rotationSpeed * 2f); // Faster rotation
                }
            }

            // Quick stab windup
            stabTimer += Time.deltaTime;

            if (stabTimer >= stabWindupTime && stabTimer < stabWindupTime + 0.1f)
            {
                // Execute stab
                PerformStab();
            }

            // End attack after duration
            if (stabTimer >= profile.attackDuration)
            {
                stabTimer = 0f;
                EndAttack();
            }
        }

        protected override void OnStateEnter(EnemyState state)
        {
            base.OnStateEnter(state);

            if (state == EnemyState.Attacking)
            {
                stabTimer = 0f;
                TriggerAttack();
            }
        }

        private bool CanJump()
        {
            return Time.time >= lastJumpTime + jumpCooldown && !isJumping;
        }

        private void StartJumpAttack()
        {
            isJumping = true;
            jumpTimer = 0f;
            jumpStartPosition = transform.position;

            // Predict where player will be
            Vector3 playerVelocity = Vector3.zero;

            // Try to get player's velocity (simplified prediction)
            var playerController = player.GetComponent<FirstPersonZoneController>();
            if (playerController)
            {
                // Estimate velocity based on player's movement direction
                // (You might need to add a GetVelocity() method to FirstPersonZoneController)
                playerVelocity = (player.position - lastKnownPlayerPosition) / Time.deltaTime;
            }

            // Predict future position
            jumpTargetPosition = player.position + playerVelocity * jumpPredictionMultiplier;

            // Make sure target is on ground (same Y level roughly)
            jumpTargetPosition.y = transform.position.y;

            lastJumpTime = Time.time;

            // Disable NavMeshAgent during jump
            navAgent.enabled = false;

            // Play jump animation using GameConstant
            if (animator)
                animator.SetTrigger(Animator.StringToHash(GameConstant.AnimationParameters.Jump));

            if (enableDebugLogs)
                Debug.Log($"[FastEnemy] Starting jump attack to predicted position!");
        }

        private void UpdateJump()
        {
            jumpTimer += Time.deltaTime;
            float normalizedTime = jumpTimer / jumpDuration;

            if (normalizedTime >= 1f)
            {
                // End jump
                EndJump();
                return;
            }

            // Calculate arc position (parabolic jump)
            Vector3 currentPosition = Vector3.Lerp(jumpStartPosition, jumpTargetPosition, normalizedTime);

            // Add height using sine wave for smooth arc
            float heightOffset = Mathf.Sin(normalizedTime * Mathf.PI) * jumpHeight;
            currentPosition.y += heightOffset;

            // Move to position
            transform.position = currentPosition;

            // Face direction of movement
            Vector3 direction = (jumpTargetPosition - jumpStartPosition).normalized;
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }

        private void EndJump()
        {
            isJumping = false;
            jumpTimer = 0f;

            // Re-enable NavMeshAgent
            navAgent.enabled = true;

            // Check if we landed near player and can attack
            if (player)
            {
                float distanceToPlayer = Vector3.Distance(transform.position, player.position);
                if (distanceToPlayer <= stabRange && CanAttack())
                {
                    ChangeState(EnemyState.Attacking);
                }
            }

            if (enableDebugLogs)
                Debug.Log($"[FastEnemy] Jump attack ended!");
        }

        private void PerformStab()
        {
            // Quick raycast forward for stab
            Ray stabRay = new Ray(transform.position + Vector3.up, transform.forward);

            if (Physics.Raycast(stabRay, out RaycastHit hit, stabRange))
            {
                var damageable = hit.collider.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    DealDamage();

                    if (enableDebugLogs)
                        Debug.Log($"[FastEnemy] Stab hit {hit.collider.name}!");
                }
            }

            if (enableDebugLogs)
                Debug.DrawRay(stabRay.origin, stabRay.direction * stabRange, Color.red, 1f);
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();

            if (!drawGizmos) return;

            // Draw medium range zone (jump zone)
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, mediumRangeMin);
            Gizmos.DrawWireSphere(transform.position, mediumRangeMax);

            // Draw stab range
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position + Vector3.up, transform.forward * stabRange);

            // Draw jump arc preview
            if (Application.isPlaying && isJumping)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(jumpStartPosition, jumpTargetPosition);
                Gizmos.DrawSphere(jumpTargetPosition, 0.5f);
            }
        }
    }
}