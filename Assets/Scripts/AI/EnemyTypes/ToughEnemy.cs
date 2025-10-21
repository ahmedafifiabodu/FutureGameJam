using UnityEngine;

namespace AI.EnemyTypes
{
    /// <summary>
    /// Tough Enemy: Walks directly towards the player.
    /// When reaching melee range, starts aiming shotgun (can rotate to face player while doing so).
    /// After a moment, fires it in a short-range cone (not projectiles).
    /// </summary>
    public class ToughEnemy : EnemyAI
    {
        [Header("Tough Enemy Settings")]
        [SerializeField] private float shotgunAimTime = 0.8f; // Time to aim before firing

        [SerializeField] private float shotgunConeAngle = 30f; // Cone angle in degrees
        [SerializeField] private float shotgunRange = 8f; // Shotgun effective range
        [SerializeField] private int shotgunPelletCount = 8; // Number of raycasts for spread
        [SerializeField] private float shotgunDamagePerPellet = 5f; // Damage per pellet
        [SerializeField] private GameObject shotgunMuzzleFlash;

        private float aimTimer;
        private bool hasAimed;

        protected override void UpdateChasing()
        {
            if (!player) return;

            // Don't allow state transitions if currently attacking
            if (isAttacking)
            {
                if (enableDebugLogs)
                    Debug.Log($"[ToughEnemy] Still attacking, skipping chase update");
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

            // Walk DIRECTLY towards player (not delayed position)
            if (navAgent && navAgent.isActiveAndEnabled && navAgent.isOnNavMesh)
            {
                // Only update destination if not already close to it
                float distanceToDestination = Vector3.Distance(transform.position, player.position);
                
                if (distanceToDestination > navAgent.stoppingDistance + 0.5f)
                {
                    navAgent.SetDestination(player.position);
                }
            }

            // Check if in attack range (respects stopping distance)
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            float effectiveAttackRange = Mathf.Max(profile.attackRange, navAgent.stoppingDistance);
            bool hasStoppedMoving = navAgent.velocity.magnitude < 0.1f;
            
            // Require minimum chase time before attacking
            float minChaseTime = 0.3f;
            bool hasBeenChasingLongEnough = (Time.time - chaseStartTime) >= minChaseTime;
            
            if (distanceToPlayer <= effectiveAttackRange && hasStoppedMoving && hasBeenChasingLongEnough && CanAttack())
            {
                ChangeState(EnemyState.Attacking);
            }
        }

        protected override void UpdateAttacking()
        {
            // Stop movement but CAN rotate
            navAgent.isStopped = true;

            // Continuously face player while aiming
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

            // Aim phase
            aimTimer += Time.deltaTime;

            if (aimTimer >= shotgunAimTime && !hasAimed)
            {
                // Fire shotgun
                FireShotgun();
                hasAimed = true;
            }

            // End attack after full duration
            if (aimTimer >= profile.attackDuration)
            {
                aimTimer = 0f;
                hasAimed = false;
                EndAttack();
            }
        }

        protected override void OnStateEnter(EnemyState state)
        {
            base.OnStateEnter(state);

            if (state == EnemyState.Attacking)
            {
                aimTimer = 0f;
                hasAimed = false;
                TriggerAttack();
            }
        }

        private void FireShotgun()
        {
            if (!player) return;

            // Play muzzle flash
            if (shotgunMuzzleFlash)
            {
                shotgunMuzzleFlash.SetActive(false);
                shotgunMuzzleFlash.SetActive(true);
            }

            // Fire multiple raycasts in a cone
            Vector3 directionToPlayer = (player.position - transform.position).normalized;
            int pelletHits = 0;

            for (int i = 0; i < shotgunPelletCount; i++)
            {
                // Add random spread within cone
                Vector3 spreadDirection = ApplyConeSpread(directionToPlayer, shotgunConeAngle);

                Ray pelletRay = new Ray(transform.position + Vector3.up, spreadDirection);

                if (Physics.Raycast(pelletRay, out RaycastHit hit, shotgunRange))
                {
                    var damageable = hit.collider.GetComponent<IDamageable>();
                    if (damageable != null)
                    {
                        // Deal damage per pellet
                        damageable.TakeDamage(shotgunDamagePerPellet);
                        pelletHits++;
                    }

                    // Spawn impact effect
                    if (profile.attackEffectPrefab)
                    {
                        Instantiate(profile.attackEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                    }
                }

                if (enableDebugLogs)
                    Debug.DrawRay(pelletRay.origin, pelletRay.direction * shotgunRange, Color.yellow, 1f);
            }

            if (enableDebugLogs)
                Debug.Log($"[ToughEnemy] Shotgun fired! {pelletHits}/{shotgunPelletCount} pellets hit.");
        }

        private Vector3 ApplyConeSpread(Vector3 direction, float coneAngle)
        {
            // Random angle within cone
            float randomAngle = Random.Range(-coneAngle * 0.5f, coneAngle * 0.5f);
            float randomRotation = Random.Range(0f, 360f);

            // Create rotation around the forward direction
            Quaternion spread = Quaternion.AngleAxis(randomAngle, Vector3.up) *
                                Quaternion.AngleAxis(randomRotation, direction);

            return spread * direction;
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();

            if (!drawGizmos) return;

            // Draw shotgun cone
            if (currentState == EnemyState.Attacking || !Application.isPlaying)
            {
                Gizmos.color = Color.yellow;

                // Draw cone edges
                Vector3 forward = transform.forward;
                Vector3 right = Quaternion.AngleAxis(shotgunConeAngle * 0.5f, Vector3.up) * forward;
                Vector3 left = Quaternion.AngleAxis(-shotgunConeAngle * 0.5f, Vector3.up) * forward;

                Gizmos.DrawRay(transform.position + Vector3.up, right * shotgunRange);
                Gizmos.DrawRay(transform.position + Vector3.up, left * shotgunRange);
                Gizmos.DrawRay(transform.position + Vector3.up, forward * shotgunRange);
            }
        }
    }
}