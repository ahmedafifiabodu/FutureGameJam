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

        public void EnterState(EnemyController enemy)
        {
            enemy.Agent.isStopped = false;

            if (enemy.Animator != null)
            {
                enemy.Animator.SetBool(GameConstant.AnimationParameters.IsMoving, true);
                enemy.Animator.SetBool(GameConstant.AnimationParameters.IsChasing, false);
            }

            // Start moving to first patrol point or wander
            if (enemy.Config.usePatrolPoints && enemy.PatrolPoints != null && enemy.PatrolPoints.Length > 0)
            {
                Debug.Log($"[PATROL] Using patrol points. Total points: {enemy.PatrolPoints.Length}");
                MoveToNextPatrolPoint(enemy);
            }
            else
            {
                Debug.Log($"[PATROL] No patrol points, using wander mode");
                SetRandomWanderTarget(enemy);
            }
        }

        public void UpdateState(EnemyController enemy)
        {
            // Vision system will handle transition to chase

            if (isWaiting)
            {
                waitTimer -= Time.deltaTime;
                if (waitTimer <= 0f)
                {
                    isWaiting = false;
                    Debug.Log($"[PATROL] Wait finished, moving to next point");

                    if (enemy.Config.usePatrolPoints && enemy.PatrolPoints != null && enemy.PatrolPoints.Length > 0)
                    {
                        MoveToNextPatrolPoint(enemy);
                    }
                    else
                    {
                        SetRandomWanderTarget(enemy);
                    }
                }
            }
            else
            {
                // Check if reached destination
                if (!enemy.Agent.pathPending && enemy.Agent.remainingDistance <= enemy.Agent.stoppingDistance)
                {
                    // Start waiting
                    Debug.Log($"[PATROL] Reached patrol point, starting wait ({enemy.Config.patrolWaitTime}s)");
                    isWaiting = true;
                    waitTimer = enemy.Config.patrolWaitTime;
                    enemy.Agent.isStopped = true;
                }
            }
        }

        public void ExitState(EnemyController enemy)
        {
            Debug.Log($"[PATROL] {enemy.Config.enemyName} EXITING Patrol State");
            isWaiting = false;
            waitTimer = 0f;
        }

        private void MoveToNextPatrolPoint(EnemyController enemy)
        {
            if (enemy.PatrolPoints == null || enemy.PatrolPoints.Length == 0) return;

            currentPatrolIndex = (currentPatrolIndex + 1) % enemy.PatrolPoints.Length;

            if (enemy.PatrolPoints[currentPatrolIndex] != null)
            {
                Vector3 targetPos = enemy.PatrolPoints[currentPatrolIndex].position;
                Debug.Log($"[PATROL] Moving to patrol point {currentPatrolIndex}: {targetPos}");

                enemy.Agent.isStopped = false;
                enemy.Agent.SetDestination(targetPos);
            }
            else
            {
                Debug.LogWarning($"[PATROL] Patrol point {currentPatrolIndex} is NULL!");
            }
        }

        private void SetRandomWanderTarget(EnemyController enemy)
        {
            // Generate random point within wander radius
            Vector2 randomCircle = Random.insideUnitCircle * 10f;
            wanderTarget = enemy.transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);

            // Sample NavMesh to find valid position
            UnityEngine.AI.NavMeshHit hit;
            if (UnityEngine.AI.NavMesh.SamplePosition(wanderTarget, out hit, 10f, UnityEngine.AI.NavMesh.AllAreas))
            {
                Debug.Log($"[PATROL] Wander target set to: {hit.position}");
                enemy.Agent.isStopped = false;
                enemy.Agent.SetDestination(hit.position);
            }
            else
            {
                Debug.LogWarning($"[PATROL] Failed to find valid wander position near {wanderTarget}");
            }
        }
    }
}