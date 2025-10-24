using UnityEngine;

namespace ProceduralGeneration
{
    /// <summary>
    /// Represents a room piece in the procedural level
    /// </summary>
    public class Room : LevelPiece
    {
        [Header("Room Settings")]
        [SerializeField] private string roomName = "Room";

        [Header("NavMesh Settings")]
        [Tooltip("Delay before spawning enemies (allows NavMesh to update)")]
        [SerializeField] private float enemySpawnDelay = 0.5f;

        [Header("Starting Room Generation")]
        [SerializeField] private bool startingRoom = false;

        [Tooltip("Time to wait after generation before opening door (starting room only)")]
        [SerializeField] private float startingRoomDoorDelay = 1.5f;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;

        // Track spawning state
        private bool enemiesSpawnInitiated = false;

        private bool enemySpawningCompleted = false;
        private int expectedEnemyCount = 0;
        private int actuallySpawnedCount = 0;

        // Starting room generation tracking
        private bool startingRoomGenerationTriggered = false;

        private ProceduralLevelGenerator levelGenerator;

        public string RoomName => roomName;
        public bool IsStartingRoom => startingRoom;

        protected override void Awake()
        {
            base.Awake();

            // Starting room only has Point B (exit), no entrance
            if (startingRoom && pointA != null)
            {
                pointA.gameObject.SetActive(false);
            }

            // Player spawns in starting room, so mark as entered
            if (startingRoom)
            {
                playerHasEntered = true;
                if (showDebugLogs)
                    Debug.Log($"[Room] Starting room {roomName} - marked as player entered");

                // Find level generator for starting room generation
                levelGenerator = FindFirstObjectByType<ProceduralLevelGenerator>();
            }
        }

        /// <summary>
        /// Called by ConnectionPoint when player approaches starting room exit (Point B)
        /// </summary>
        public void OnStartingRoomExitTriggered()
        {
            if (!startingRoom || startingRoomGenerationTriggered)
                return;

            if (showDebugLogs)
                Debug.Log($"[Room] {roomName} - Starting room exit triggered via ConnectionPoint");

            startingRoomGenerationTriggered = true;
            TriggerStartingRoomGeneration();
        }

        private void TriggerStartingRoomGeneration()
        {
            if (levelGenerator != null && pointB != null)
            {
                if (showDebugLogs)
                    Debug.Log($"[Room] {roomName} - Triggering next area generation for starting room");

                // Trigger generation through the level generator
                levelGenerator.TriggerNextAreaGeneration(pointB);

                // Wait for generation to complete, then open door
                Invoke(nameof(OpenStartingRoomExitDoor), startingRoomDoorDelay);
            }
            else
            {
                if (showDebugLogs)
                    Debug.LogWarning($"[Room] {roomName} - Cannot trigger generation: missing levelGenerator or pointB");

                // Fallback: just open the door
                Invoke(nameof(OpenStartingRoomExitDoor), 0.5f);
            }
        }

        private void OpenStartingRoomExitDoor()
        {
            if (showDebugLogs)
                Debug.Log($"[Room] {roomName} - Opening starting room exit door after generation");

            OpenExitDoor();
        }

        public override void OnSpawned(int roomIteration)
        {
            base.OnSpawned(roomIteration);

            if (showDebugLogs)
                Debug.Log($"[Room] {roomName} spawned, initiating enemy spawn process...");

            // Starting rooms don't spawn enemies, so handle differently
            if (startingRoom)
            {
                if (showDebugLogs)
                    Debug.Log($"[Room] {roomName} - Starting room, skipping enemy spawn process");

                // No enemies to spawn, but don't auto-open door - wait for ConnectionPoint trigger
                enemySpawningCompleted = true;
                expectedEnemyCount = 0;
                actuallySpawnedCount = 0;
                return;
            }

            // Regular room behavior - ensure exit door is closed until enemies are dealt with
            CloseExitDoor();

            // Delay enemy spawning to allow NavMesh to carve/update
            // Unity's NavMesh updates automatically for Navigation Static objects
            enemiesSpawnInitiated = true;
            Invoke(nameof(SpawnEnemiesDelayed), enemySpawnDelay);
        }

        public override void OnPlayerEntered()
        {
            base.OnPlayerEntered();

            // Starting rooms handle their own door logic via ConnectionPoint trigger
            if (startingRoom)
            {
                if (showDebugLogs)
                    Debug.Log($"[Room] {roomName} - Starting room player entered, ConnectionPoint will handle exit trigger");
                return;
            }

            // Regular room behavior
            // If enemy spawning hasn't completed yet, wait for it
            if (enemiesSpawnInitiated && !enemySpawningCompleted)
            {
                if (showDebugLogs)
                    Debug.Log($"[Room] {roomName} - Player entered but enemy spawning not complete, waiting...");
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
                Debug.Log($"[Room] {roomName} - Starting delayed enemy spawn...");

            // Spawn enemies and props if spawn manager exists
            var spawnManager = FindFirstObjectByType<AI.Spawning.EnemySpawnManager>();
            if (spawnManager && spawnPoints != null && spawnPoints.Length > 0)
            {
                // Get current iteration from spawn manager
                int currentIteration = spawnManager.GetCurrentIteration();

                if (showDebugLogs)
                    Debug.Log($"[Room] {roomName} - Spawning enemies at {spawnPoints.Length} spawn points for iteration {currentIteration}");

                // Count expected enemies based on this piece's settings or manager defaults
                expectedEnemyCount = EstimateExpectedEnemyCount(currentIteration, spawnManager);

                // Attempt to spawn enemies and get actual count - pass 'this' for per-piece configuration
                actuallySpawnedCount = spawnManager.SpawnEnemiesAtPoints(spawnPoints, true, currentIteration, this);
                spawnManager.SpawnPropsAtPoints(spawnPoints);

                if (showDebugLogs)
                {
                    Debug.Log($"[Room] {roomName} - Enemy spawning attempted: {actuallySpawnedCount}/{expectedEnemyCount} enemies spawned");

                    if (actuallySpawnedCount == 0 && expectedEnemyCount > 0)
                    {
                        Debug.LogError($"[Room] {roomName} - CRITICAL: No enemies spawned despite expecting {expectedEnemyCount}! Check NavMesh setup.");
                    }
                }

                // Register enemies after spawning with a slight delay to ensure they're all created
                Invoke(nameof(CompleteEnemySpawning), 0.3f); // Increased delay for more reliable spawning
            }
            else
            {
                if (showDebugLogs)
                    Debug.Log($"[Room] {roomName} - No spawn manager or spawn points found, completing spawning immediately");

                // No enemies to spawn, complete immediately
                expectedEnemyCount = 0;
                actuallySpawnedCount = 0;
                CompleteEnemySpawning();
            }
        }

        private int EstimateExpectedEnemyCount(int iteration, AI.Spawning.EnemySpawnManager manager)
        {
            // Use this piece's custom settings if enabled, otherwise use manager defaults
            if (useCustomSpawnSettings)
            {
                int estimated = baseEnemiesPerPiece + Mathf.FloorToInt(iteration * enemiesPerIteration);
                return Mathf.Min(estimated, maxEnemiesPerPiece);
            }
            else
            {
                // Fallback to manager defaults
                int baseEnemies = 3;
                float perIteration = 0.5f;
                int maxEnemies = 10;

                int estimated = baseEnemies + Mathf.FloorToInt(iteration * perIteration);
                return Mathf.Min(estimated, maxEnemies);
            }
        }

        private void CompleteEnemySpawning()
        {
            enemySpawningCompleted = true;

            if (showDebugLogs)
            {
                Debug.Log($"[Room] {roomName} - Enemy spawning completed. Expected: {expectedEnemyCount}, Actually spawned: {actuallySpawnedCount}");
            }

            // Force register enemies now that spawning is complete
            ForceRegisterEnemies();

            // If player has already entered while we were spawning, handle enemy registration
            if (playerHasEntered && !enemiesSpawned)
            {
                if (showDebugLogs)
                    Debug.Log($"[Room] {roomName} - Player already entered, registering enemies now");
                RegisterSpawnedEnemies();
            }
        }

        /// <summary>
        /// Override to add additional debug logging and validation
        /// </summary>
        protected override void RegisterSpawnedEnemies()
        {
            if (showDebugLogs)
                Debug.Log($"[Room] {roomName} - Registering spawned enemies...");

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
                Debug.Log($"[Room] {roomName} - Found {aliveEnemies} alive enemies in room (Expected: {expectedEnemyCount}, Spawned: {actuallySpawnedCount})");
            }
        }

        /// <summary>
        /// Override to handle spawn failure detection
        /// </summary>
        protected override void HandleNoEnemiesFound()
        {
            if (showDebugLogs)
                Debug.Log($"[Room] {roomName} - HandleNoEnemiesFound called. Expected: {expectedEnemyCount}, Actual: {actuallySpawnedCount}");

            // Check if this is a spawn failure vs intentionally empty room
            if (expectedEnemyCount > 0 && actuallySpawnedCount == 0)
            {
                // This is a spawn failure - keep door closed for safety
                if (showDebugLogs)
                    Debug.LogError($"[Room] {roomName} - SPAWN FAILURE DETECTED! Expected {expectedEnemyCount} enemies but none spawned. Exit door will remain CLOSED.");

                // Don't open the door - this forces manual inspection/fix
                return;
            }
            else
            {
                // Legitimately empty room or all enemies spawned and died immediately
                if (showDebugLogs)
                    Debug.Log($"[Room] {roomName} - Legitimate empty room, opening exit door");

                OpenExitDoor();
            }
        }

        /// <summary>
        /// Override to add debug logging
        /// </summary>
        public override void OpenExitDoor()
        {
            if (showDebugLogs)
                Debug.Log($"[Room] {roomName} - Opening exit door");

            base.OpenExitDoor();
        }

        /// <summary>
        /// Override to add debug logging
        /// </summary>
        public override void CloseExitDoor()
        {
            if (showDebugLogs)
                Debug.Log($"[Room] {roomName} - Closing exit door");

            base.CloseExitDoor();
        }

        /// <summary>
        /// Force open the exit door (for debugging or manual override)
        /// </summary>
        [ContextMenu("Force Open Exit Door")]
        public void ForceOpenExitDoor()
        {
            if (showDebugLogs)
                Debug.Log($"[Room] {roomName} - FORCING exit door open (manual override)");

            OpenExitDoor();
        }

        /// <summary>
        /// Called when an enemy in this room is destroyed (either by death or possession)
        /// Forces an immediate check of enemy status instead of waiting for periodic check
        /// </summary>
        public void OnEnemyDestroyed()
        {
            if (!enemiesSpawned)
            {
                if (showDebugLogs)
                    Debug.Log($"[Room] {roomName} - OnEnemyDestroyed called but enemies not yet spawned, ignoring");
                return;
            }

            // The base class OnEnemyPossessed already handles this properly
            // No need for additional logic here
            if (showDebugLogs)
                Debug.Log($"[Room] {roomName} - Enemy destroyed, room status will be checked by base class");
        }

        /// <summary>
        /// Override to handle enemy possession - base class handles the door opening logic
        /// </summary>
        public override void OnEnemyPossessed(AI.Enemy.EnemyController enemy)
        {
            if (showDebugLogs)
                Debug.Log($"[Room] {roomName} - Enemy {enemy.name} was possessed by parasite");

            // Call base implementation which handles removal from activeEnemies list
            // and opens the door if this was the last enemy
            base.OnEnemyPossessed(enemy);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = startingRoom ? Color.yellow : Color.blue;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 2f);

            // Draw starting room info
            if (startingRoom && pointB != null)
            {
                Gizmos.color = startingRoomGenerationTriggered ? Color.green : Color.cyan;

                // Draw connection to Point B
                Gizmos.DrawLine(transform.position, pointB.transform.position);

                // Draw label
#if UNITY_EDITOR
                UnityEditor.Handles.Label(pointB.transform.position + Vector3.up * 2f,
$"Starting Room Exit\nTriggered: {startingRoomGenerationTriggered}");
#endif
            }

            // Draw spawn points
            if (spawnPoints != null)
            {
                // Color code spawn points based on spawn status
                foreach (var spawnPoint in spawnPoints)
                {
                    if (spawnPoint != null)
                    {
                        // Red = failed/not spawned, Green = spawned successfully
                        Gizmos.color = spawnPoint.hasSpawned ? Color.green : Color.red;
                        Gizmos.DrawWireSphere(spawnPoint.transform.position, 0.5f);

                        // Show additional info for spawn failures
                        if (!spawnPoint.hasSpawned && expectedEnemyCount > 0)
                        {
                            Gizmos.color = Color.yellow;
                            Gizmos.DrawWireCube(spawnPoint.transform.position, Vector3.one * 0.3f);
                        }
                    }
                }
            }
        }
    }
}