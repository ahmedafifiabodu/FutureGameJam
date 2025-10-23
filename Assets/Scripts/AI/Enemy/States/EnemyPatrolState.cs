using UnityEngine;

namespace AI.Enemy.States
{
    /// <summary>
    /// Patrol state - enemy patrols between points or wanders
    /// </summary>
    public class EnemyPatrolState : IEnemyState
    {
        private int currentPatrolIndex = 0;
        private float waitTimer = 0f;
        private bool isWaiting = false;
        private Vector3 wanderTarget;
        private bool hasStartedMoving = false;
        private Vector3 currentDestination;
        private const float PATROL_STOPPING_DISTANCE = 0.5f; // Fixed stopping distance for patrol
        private float originalStoppingDistance; // Store original stopping distance to restore later

        public void EnterState(EnemyController enemy)
        {
            // Store original stopping distance and set patrol-specific stopping distance
            originalStoppingDistance = enemy.Agent.stoppingDistance;
            enemy.Agent.stoppingDistance = PATROL_STOPPING_DISTANCE;

            enemy.Agent.isStopped = false;
            hasStartedMoving = false;

            if (enemy.Animator != null && enemy.Config.hasCustomAnimations)
            {
                enemy.Animator.SetBool(enemy.Config.patrolAnimation, true);
                enemy.Animator.SetBool(enemy.Config.chaseAnimation, false);
                enemy.Animator.SetBool(enemy.Config.idleAnimation, false);
            }

            // Start moving to first patrol point or wander
            if (enemy.Config.usePatrolPoints && enemy.PatrolPoints != null && enemy.PatrolPoints.Length > 0)
                MoveToNextPatrolPoint(enemy);
            else
                SetRandomWanderTarget(enemy);
        }

        public void UpdateState(EnemyController enemy)
        {
            // CRITICAL: Check if player has been spotted and transition to chase
            if (enemy.HasSeenPlayer && enemy.Player != null)
            {
                // Player has been spotted, switch to chase state immediately
                enemy.Agent.speed = enemy.Config.chaseSpeed;
                enemy.ChangeState(new EnemyChaseStateNew());
                return;
            }

            // Check if player is in attack range AND game mode allows attack state
            // This prevents entering attack state in Parasite mode
            if (enemy.IsPlayerInAttackRange() && enemy.CanAttack() && enemy.CanTransitionToAttackState())
            {
                enemy.ChangeState(new EnemyAttackState());
                return;
            }

            if (isWaiting)
            {
                waitTimer -= Time.deltaTime;
                if (waitTimer <= 0f)
                {
                    isWaiting = false;
                    hasStartedMoving = false;

                    if (enemy.Animator != null && enemy.Config.hasCustomAnimations)
                    {
                        enemy.Animator.SetBool(enemy.Config.idleAnimation, false);
                        enemy.Animator.SetBool(enemy.Config.patrolAnimation, true);
                    }

                    if (enemy.Config.usePatrolPoints && enemy.PatrolPoints != null && enemy.PatrolPoints.Length > 0)
                        MoveToNextPatrolPoint(enemy);
                    else
                        SetRandomWanderTarget(enemy);
                }
            }
            else
            {
                // Check if agent has started moving
                if (!hasStartedMoving && enemy.Agent.velocity.magnitude > 0.1f)
                    hasStartedMoving = true;

                // Only check for arrival if we've actually started moving
                if (hasStartedMoving && !enemy.Agent.pathPending && enemy.Agent.hasPath)
                {
                    // Use actual distance to destination
                    float actualDistance = Vector3.Distance(enemy.transform.position, currentDestination);

                    // Use patrol stopping distance with small buffer
                    float arrivalThreshold = PATROL_STOPPING_DISTANCE + 0.3f; // 0.8m total

                    // Check if we're close enough and have slowed down
                    bool isCloseEnough = actualDistance <= arrivalThreshold;
                    bool isSettled = enemy.Agent.velocity.magnitude < 0.2f;

                    if (isCloseEnough && isSettled)
                    {
                        isWaiting = true;
                        hasStartedMoving = false;
                        waitTimer = enemy.Config.patrolWaitTime;
                        enemy.Agent.isStopped = true;

                        if (enemy.Animator != null && enemy.Config.hasCustomAnimations)
                        {
                            enemy.Animator.SetBool(enemy.Config.idleAnimation, true);
                            enemy.Animator.SetBool(enemy.Config.patrolAnimation, false);
                        }
                    }
                }
            }
        }

        public void ExitState(EnemyController enemy)
        {
            // Restore original stopping distance when exiting patrol
            enemy.Agent.stoppingDistance = originalStoppingDistance;

            isWaiting = false;
            waitTimer = 0f;
            hasStartedMoving = false;
        }

        private void MoveToNextPatrolPoint(EnemyController enemy)
        {
            if (enemy.PatrolPoints == null || enemy.PatrolPoints.Length == 0)
                return;

            currentPatrolIndex = (currentPatrolIndex + 1) % enemy.PatrolPoints.Length;

            if (enemy.PatrolPoints[currentPatrolIndex] != null)
            {
                currentDestination = enemy.PatrolPoints[currentPatrolIndex].position;

                enemy.Agent.isStopped = false;
                enemy.Agent.SetDestination(currentDestination);
                hasStartedMoving = false;
            }
        }

        private void SetRandomWanderTarget(EnemyController enemy)
        {
            int maxAttempts = 10;
            float minWanderDistance = 4f;
            float maxWanderDistance = 10f;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                float randomDistance = Random.Range(minWanderDistance, maxWanderDistance);
                Vector2 randomDirection = Random.insideUnitCircle.normalized;
                Vector3 randomOffset = new Vector3(randomDirection.x, 0, randomDirection.y) * randomDistance;
                wanderTarget = enemy.transform.position + randomOffset;

                if (UnityEngine.AI.NavMesh.SamplePosition(wanderTarget, out UnityEngine.AI.NavMeshHit hit, maxWanderDistance, UnityEngine.AI.NavMesh.AllAreas))
                {
                    float actualDistance = Vector3.Distance(enemy.transform.position, hit.position);
                    if (actualDistance >= minWanderDistance)
                    {
                        UnityEngine.AI.NavMeshPath testPath = new UnityEngine.AI.NavMeshPath();
                        if (enemy.Agent.CalculatePath(hit.position, testPath) && testPath.status == UnityEngine.AI.NavMeshPathStatus.PathComplete)
                        {
                            currentDestination = hit.position;

                            enemy.Agent.isStopped = false;
                            enemy.Agent.SetDestination(currentDestination);
                            hasStartedMoving = false;
                            return;
                        }
                    }
                }
            }
        }
    }
}