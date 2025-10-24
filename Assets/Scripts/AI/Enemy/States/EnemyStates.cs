using UnityEngine;

namespace AI.Enemy.States
{
    /// <summary>
    /// Idle state - enemy is not aggro'd
    /// </summary>
    public class EnemyIdleStateNew : IEnemyState
    {
        public void EnterState(EnemyController enemy)
        {
            // Safety check before modifying agent
            if (enemy.Agent != null && enemy.Agent.isOnNavMesh && enemy.Agent.enabled)
                enemy.Agent.isStopped = true;

            if (enemy.Animator != null && enemy.Config.hasCustomAnimations)
            {
                // Set IsChasing to false
                enemy.Animator.SetBool(enemy.Config.chaseAnimation, false);
                // Set IsMoving to false for idle
                enemy.Animator.SetBool(enemy.Config.patrolAnimation, false);
            }
        }

        public void UpdateState(EnemyController enemy)
        {
            // Aggro system handles transition to chase
        }

        public void ExitState(EnemyController enemy)
        {
        }
    }

    /// <summary>
    /// Chase state - enemy pursues the player
    /// </summary>
    public class EnemyChaseStateNew : IEnemyState
    {
        private Vector3 lastKnownPlayerPosition;

        private float memoryTimer = 0f;
        private float pathUpdateTimer = 0f;
        private float stateEntryTimer = 0f;

        private const float PATH_UPDATE_INTERVAL = 0.2f;
        private const float ATTACK_CHECK_DELAY = 0.5f;

        public void EnterState(EnemyController enemy)
        {
            // Safety check before modifying agent
            if (enemy.Agent == null || !enemy.Agent.isOnNavMesh || !enemy.Agent.enabled)
                return;

            enemy.Agent.isStopped = false;
            enemy.Agent.speed = enemy.Config.chaseSpeed;
            enemy.Agent.stoppingDistance = enemy.Config.attackRange * 0.7f;

            if (enemy.Animator != null && enemy.Config.hasCustomAnimations)
            {
                // IsChasing (boolean) - set to true
                enemy.Animator.SetBool(enemy.Config.chaseAnimation, true);
                // IsMoving (boolean) - set to true
                enemy.Animator.SetBool(enemy.Config.patrolAnimation, true);
            }

            lastKnownPlayerPosition = enemy.LastKnownPlayerPosition;
            pathUpdateTimer = 0f;
            stateEntryTimer = 0f;
        }

        public void UpdateState(EnemyController enemy)
        {
            if (enemy.Player == null) return;

            pathUpdateTimer += Time.deltaTime;
            stateEntryTimer += Time.deltaTime;

            // Update last known position from _controller
            enemy.UpdateLastKnownPosition();
            lastKnownPlayerPosition = enemy.LastKnownPlayerPosition;

            // CRITICAL: Check if game mode allows attack state transition
            // Wait before checking attack range to prevent instant loop
            if (stateEntryTimer >= ATTACK_CHECK_DELAY)
            {
                // Check if can attack AND game mode allows attack
                // Only transition to attack if BOTH conditions are met:
                // 1. Player is in attack range and enemy can attack
                // 2. Game mode allows attacking (not in Parasite mode with no host)
                if (enemy.IsPlayerInAttackRange() && enemy.CanAttack() && enemy.CanTransitionToAttackState())
                {
                    enemy.ChangeState(new EnemyAttackState());
                    return;
                }
            }

            // Handle movement based on enemy type
            switch (enemy.Config.enemyType)
            {
                case Configuration.EnemyType.Basic:
                    UpdateBasicChase(enemy);
                    break;

                case Configuration.EnemyType.Tough:
                    UpdateToughChase(enemy);
                    break;

                case Configuration.EnemyType.Fast:
                    UpdateFastChase(enemy);
                    break;
            }
        }

        public void ExitState(EnemyController enemy)
        {
            pathUpdateTimer = 0f;
            stateEntryTimer = 0f;
        }

        private void UpdateBasicChase(EnemyController enemy)
        {
            memoryTimer += Time.deltaTime;

            if (memoryTimer >= enemy.Config.memoryDelay)
            {
                lastKnownPlayerPosition = enemy.LastKnownPlayerPosition;
                memoryTimer = 0f;
            }

            if (pathUpdateTimer >= PATH_UPDATE_INTERVAL)
            {
                if (enemy.Agent.isOnNavMesh && enemy.Agent.enabled)
                    enemy.Agent.SetDestination(lastKnownPlayerPosition);

                pathUpdateTimer = 0f;
            }
        }

        private void UpdateToughChase(EnemyController enemy)
        {
            if (pathUpdateTimer >= PATH_UPDATE_INTERVAL)
            {
                if (enemy.Agent.isOnNavMesh && enemy.Agent.enabled)
                    enemy.Agent.SetDestination(enemy.LastKnownPlayerPosition);

                pathUpdateTimer = 0f;
            }
        }

        private void UpdateFastChase(EnemyController enemy)
        {
            float distance = Vector3.Distance(enemy.transform.position, enemy.Player.position);

            if (distance >= enemy.Config.mediumRangeMin && distance <= enemy.Config.mediumRangeMax)
            {
                // Check if can attack AND game mode allows attack
                if (enemy.CanAttack() && enemy.CanTransitionToAttackState())
                {
                    enemy.ChangeState(new EnemyFastJumpAttackState());
                    return;
                }
            }

            if (pathUpdateTimer >= PATH_UPDATE_INTERVAL)
            {
                if (enemy.Agent.isOnNavMesh && enemy.Agent.enabled)
                    enemy.Agent.SetDestination(enemy.LastKnownPlayerPosition);

                pathUpdateTimer = 0f;
            }
        }
    }

    /// <summary>
    /// Attack state - keeps attacking while player is in range, returns to patrol when out of range.
    /// </summary>
    public class EnemyAttackState : IEnemyState
    {
        private float nextAttackTime;

        public void EnterState(EnemyController enemy)
        {
            // Safety check before modifying agent
            if (enemy.Agent != null && enemy.Agent.isOnNavMesh && enemy.Agent.enabled)
            {
                enemy.Agent.isStopped = true;
            }

            if (enemy.Animator != null && enemy.Config.hasCustomAnimations)
            {
                // Stop movement animations (both IsMoving and IsChasing are booleans)
                enemy.Animator.SetBool(enemy.Config.patrolAnimation, false);
                enemy.Animator.SetBool(enemy.Config.chaseAnimation, false);
            }

            nextAttackTime = Time.time;
        }

        public void UpdateState(EnemyController enemy)
        {
            if (enemy.Player == null)
                return;

            float distanceToPlayer = Vector3.Distance(enemy.transform.position, enemy.Player.position);

            // If player left attack range → go back to chase (not patrol!)
            if (distanceToPlayer > enemy.Config.attackRange)
            {
                enemy.ChangeState(new EnemyChaseStateNew());
                return;
            }

            // 🌀 Face the player smoothly
            Vector3 direction = (enemy.Player.position - enemy.transform.position).normalized;
            direction.y = 0;
            if (direction != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(direction);
                enemy.transform.rotation = Quaternion.Slerp(enemy.transform.rotation, lookRotation, Time.deltaTime * 5f);
            }

            // ⚔️ Attack repeatedly while in range
            if (Time.time >= nextAttackTime && enemy.CanAttack())
            {
                enemy.PerformAttack();

                // Trigger attack animation based on enemy type and whether ranged
                if (enemy.Animator != null && enemy.Config.hasCustomAnimations)
                {
                    if (enemy.Config.isRanged)
                        enemy.Animator.SetTrigger(enemy.Config.projectileAttackAnimation);
                    else
                        enemy.Animator.SetTrigger(enemy.Config.meleeAttackAnimation);
                }

                nextAttackTime = Time.time + enemy.Config.attackCooldown;
            }
        }

        public void ExitState(EnemyController enemy)
        {
            // Safety check before modifying agent
            if (enemy.Agent != null && enemy.Agent.isOnNavMesh && enemy.Agent.enabled)
                enemy.Agent.isStopped = false;
        }
    }

    /// <summary>
    /// Special jump attack state for Fast enemy
    /// </summary>
    public class EnemyFastJumpAttackState : IEnemyState
    {
        private Vector3 targetPosition;
        private bool isJumping = false;

        public void EnterState(EnemyController enemy)
        {
            // Safety check before modifying agent
            if (enemy.Agent != null && enemy.Agent.isOnNavMesh && enemy.Agent.enabled)
                enemy.Agent.isStopped = true;

            // Predict where player will be
            Vector3 playerVelocity = Vector3.zero;
            if (enemy.Player != null && enemy.Player.TryGetComponent<Rigidbody>(out var rb))
                playerVelocity = rb.linearVelocity;

            // Calculate jump target
            float jumpTime = 0.5f; // Time to reach target
            targetPosition = enemy.Player.position + playerVelocity * jumpTime;

            // Trigger jump animation (Jump is a trigger)
            if (enemy.Animator != null && enemy.Config.hasCustomAnimations)
                enemy.Animator.SetTrigger(enemy.Config.jumpAnimation);

            isJumping = true;
        }

        public void UpdateState(EnemyController enemy)
        {
            if (!isJumping) return;

            // Move towards target (simplified - in production, use proper arc)
            enemy.transform.position = Vector3.MoveTowards(enemy.transform.position, targetPosition, enemy.Config.moveSpeed * 2f * Time.deltaTime);

            // Check if reached target
            float distance = Vector3.Distance(enemy.transform.position, targetPosition);
            if (distance < 0.5f)
            {
                isJumping = false;

                // Quick stab attack
                if (enemy.IsPlayerInAttackRange())
                {
                    enemy.ChangeState(new EnemyAttackState());
                }
                else
                {
                    enemy.StartAttackCooldown();
                    enemy.ChangeState(new EnemyChaseStateNew());
                }
            }
        }

        public void ExitState(EnemyController enemy)
        {
            // Safety check before modifying agent
            if (enemy.Agent != null && enemy.Agent.isOnNavMesh && enemy.Agent.enabled)
                enemy.Agent.isStopped = false;
        }
    }

    /// <summary>
    /// Stagger state - enemy is stunned
    /// </summary>
    public class EnemyStaggerStateNew : IEnemyState
    {
        public void EnterState(EnemyController enemy)
        {
            // Safety check before modifying agent
            if (enemy.Agent != null && enemy.Agent.isOnNavMesh && enemy.Agent.enabled)
            {
                enemy.Agent.isStopped = true;
            }

            // Trigger stagger animation (Stagger is a trigger)
            if (enemy.Animator != null && enemy.Config.hasCustomAnimations)
            {
                enemy.Animator.SetTrigger(enemy.Config.staggerAnimation);
            }
        }

        public void UpdateState(EnemyController enemy)
        {
            // Stagger timer is handled in EnemyController
        }

        public void ExitState(EnemyController enemy)
        {
        }
    }

    /// <summary>
    /// Death state - enemy is dead
    /// </summary>
    public class EnemyDeadState : IEnemyState
    {
        public void EnterState(EnemyController enemy)
        {
            // Safety check before modifying agent
            if (enemy.Agent != null && enemy.Agent.isOnNavMesh && enemy.Agent.enabled)
                enemy.Agent.isStopped = true;

            // Trigger death animation (Death is a trigger)
            if (enemy.Animator != null && enemy.Config.hasCustomAnimations)
                enemy.Animator.SetTrigger(enemy.Config.deathAnimation);
        }

        public void UpdateState(EnemyController enemy)
        {
            // Nothing to update
        }

        public void ExitState(EnemyController enemy)
        {
        }
    }
}