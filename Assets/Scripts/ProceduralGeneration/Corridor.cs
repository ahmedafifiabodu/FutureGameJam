using UnityEngine;

namespace ProceduralGeneration
{
    /// <summary>
    /// Represents a corridor piece in the procedural level
    /// </summary>
    public class Corridor : LevelPiece
    {
        [Header("Corridor Settings")]
        [SerializeField] private string corridorName = "Corridor";

        [Header("NavMesh Settings")]
        [Tooltip("Delay before spawning enemies (allows NavMesh to update)")]
        [SerializeField] private float enemySpawnDelay = 0.5f;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;

        // Track spawning state
        private bool enemiesSpawnInitiated = false;

        private bool enemySpawningCompleted = false;
        private int expectedEnemyCount = 0;
        private int actuallySpawnedCount = 0;

        public string CorridorName => corridorName;

        public override void OnSpawned(int roomIteration)
        {
            base.OnSpawned(roomIteration);

            if (showDebugLogs)
                Debug.Log($"[Corridor] {corridorName} spawned, initiating enemy spawn process...");

            // Ensure exit door is closed until enemies are dealt with
            CloseExitDoor();

            // Delay enemy spawning to allow NavMesh to carve/update
            enemiesSpawnInitiated = true;
            Invoke(nameof(SpawnEnemiesDelayed), enemySpawnDelay);
        }

        public override void OnPlayerEntered()
        {
            base.OnPlayerEntered();

            // If enemy spawning hasn't completed yet, wait for it
            if (enemiesSpawnInitiated && !enemySpawningCompleted)
            {
                if (showDebugLogs)
                    Debug.Log($"[Corridor] {corridorName} - Player entered but enemy spawning not complete, waiting...");
                return;
            }

            // If spawning is complete, register enemies normally
            if (enemySpawningCompleted && !enemiesSpawned)
            {
                RegisterSpawnedEnemies();
            }
        }

        private void SpawnEnemiesDelayed()
        {
            if (showDebugLogs)
                Debug.Log($"[Corridor] {corridorName} - Starting delayed enemy spawn...");

            // Spawn enemies and props if spawn manager exists
            var spawnManager = FindFirstObjectByType<AI.Spawning.EnemySpawnManager>();
            if (spawnManager && spawnPoints != null && spawnPoints.Length > 0)
            {
                // Get current iteration from spawn manager
                int currentIteration = spawnManager.GetCurrentIteration();

                if (showDebugLogs)
                    Debug.Log($"[Corridor] {corridorName} - Spawning enemies at {spawnPoints.Length} spawn points for iteration {currentIteration}");

                // Count expected enemies (corridors typically have fewer enemies)
                expectedEnemyCount = EstimateExpectedEnemyCount(currentIteration);

                // Attempt to spawn enemies and get actual count
                actuallySpawnedCount = spawnManager.SpawnEnemiesAtPoints(spawnPoints, false, currentIteration); // false for corridor
                spawnManager.SpawnPropsAtPoints(spawnPoints);

                if (showDebugLogs)
                {
                    Debug.Log($"[Corridor] {corridorName} - Enemy spawning attempted: {actuallySpawnedCount}/{expectedEnemyCount} enemies spawned");

                    if (actuallySpawnedCount == 0 && expectedEnemyCount > 0)
                    {
                        Debug.LogError($"[Corridor] {corridorName} - CRITICAL: No enemies spawned despite expecting {expectedEnemyCount}! Check NavMesh setup.");
                    }
                }

                // Register enemies after spawning with a slight delay to ensure they're all created
                Invoke(nameof(CompleteEnemySpawning), 0.3f); // Increased delay for more reliable spawning
            }
            else
            {
                if (showDebugLogs)
                    Debug.Log($"[Corridor] {corridorName} - No spawn manager or spawn points found, completing spawning immediately");

                // No enemies to spawn, complete immediately
                expectedEnemyCount = 0;
                actuallySpawnedCount = 0;
                CompleteEnemySpawning();
            }
        }

        private int EstimateExpectedEnemyCount(int iteration)
        {
            // Corridors typically have fewer enemies than rooms
            // Random range 0-2 for corridors based on EnemySpawnManager logic
            return Random.Range(0, 3);
        }

        private void CompleteEnemySpawning()
        {
            enemySpawningCompleted = true;

            if (showDebugLogs)
            {
                Debug.Log($"[Corridor] {corridorName} - Enemy spawning completed. Expected: {expectedEnemyCount}, Actually spawned: {actuallySpawnedCount}");
            }

            // Force register enemies now that spawning is complete
            ForceRegisterEnemies();

            // If player has already entered while we were spawning, handle enemy registration
            if (playerHasEntered && !enemiesSpawned)
            {
                if (showDebugLogs)
                    Debug.Log($"[Corridor] {corridorName} - Player already entered, registering enemies now");
                RegisterSpawnedEnemies();
            }
        }

        /// <summary>
        /// Override to add additional debug logging and validation
        /// </summary>
        protected override void RegisterSpawnedEnemies()
        {
            if (showDebugLogs)
                Debug.Log($"[Corridor] {corridorName} - Registering spawned enemies...");

            // Call base implementation
            base.RegisterSpawnedEnemies();

            // Additional validation - count actual enemy controllers
            var enemyControllers = GetComponentsInChildren<AI.Enemy.EnemyController>();
            int aliveEnemies = 0;

            foreach (var enemy in enemyControllers)
            {
                if (enemy != null && !enemy.IsDead)
                {
                    aliveEnemies++;
                    // Set the enemy's current room for better tracking
                    enemy.SetCurrentRoom(transform);
                }
            }

            if (showDebugLogs)
            {
                Debug.Log($"[Corridor] {corridorName} - Found {aliveEnemies} alive enemies in corridor (Expected: {expectedEnemyCount}, Spawned: {actuallySpawnedCount})");
            }
        }

        /// <summary>
        /// Override to handle spawn failure detection
        /// </summary>
        protected override void HandleNoEnemiesFound()
        {
            if (showDebugLogs)
                Debug.Log($"[Corridor] {corridorName} - HandleNoEnemiesFound called. Expected: {expectedEnemyCount}, Actual: {actuallySpawnedCount}");

            // Check if this is a spawn failure vs intentionally empty corridor
            // Corridors are more likely to be empty by design, so be less strict
            if (expectedEnemyCount > 1 && actuallySpawnedCount == 0)
            {
                // This might be a spawn failure - keep door closed for safety
                if (showDebugLogs)
                    Debug.LogError($"[Corridor] {corridorName} - POTENTIAL SPAWN FAILURE! Expected {expectedEnemyCount} enemies but none spawned. Exit door will remain CLOSED.");

                // Don't open the door - this forces manual inspection/fix
                return;
            }
            else
            {
                // Legitimately empty corridor or minor spawn expectations
                if (showDebugLogs)
                    Debug.Log($"[Corridor] {corridorName} - Empty corridor (acceptable), opening exit door");

                OpenExitDoor();
            }
        }

        /// <summary>
        /// Override to add debug logging
        /// </summary>
        public override void OpenExitDoor()
        {
            if (showDebugLogs)
                Debug.Log($"[Corridor] {corridorName} - Opening exit door");

            base.OpenExitDoor();
        }

        /// <summary>
        /// Override to add debug logging
        /// </summary>
        public override void CloseExitDoor()
        {
            if (showDebugLogs)
                Debug.Log($"[Corridor] {corridorName} - Closing exit door");

            base.CloseExitDoor();
        }

        /// <summary>
        /// Force open the exit door (for debugging or manual override)
        /// </summary>
        [ContextMenu("Force Open Exit Door")]
        public void ForceOpenExitDoor()
        {
            if (showDebugLogs)
                Debug.Log($"[Corridor] {corridorName} - FORCING exit door open (manual override)");

            OpenExitDoor();
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.magenta;

            if (pointA != null && pointB != null)
            {
                Gizmos.DrawLine(pointA.transform.position, pointB.transform.position);
            }

            // Draw spawn points
            if (spawnPoints != null)
            {
                foreach (var spawnPoint in spawnPoints)
                {
                    if (spawnPoint != null)
                    {
                        // Color code spawn points based on spawn status
                        // Red = failed/not spawned, Green = spawned successfully
                        Gizmos.color = spawnPoint.hasSpawned ? Color.green : Color.red;
                        Gizmos.DrawWireSphere(spawnPoint.transform.position, 0.3f); // Smaller for corridors

                        // Show additional info for spawn failures
                        if (!spawnPoint.hasSpawned && expectedEnemyCount > 0)
                        {
                            Gizmos.color = Color.yellow;
                            Gizmos.DrawWireCube(spawnPoint.transform.position, Vector3.one * 0.2f);
                        }
                    }
                }
            }
        }
    }
}