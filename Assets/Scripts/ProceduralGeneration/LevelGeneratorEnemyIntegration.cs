using UnityEngine;
using AI.Spawning;

namespace ProceduralGeneration
{
    /// <summary>
    /// Helper component to integrate enhanced enemy AI with procedural level generation
    /// Attach this to your ProceduralLevelGenerator GameObject
    /// </summary>
    [RequireComponent(typeof(ProceduralLevelGenerator))]
    public class LevelGeneratorEnemyIntegration : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Reference to the enemy spawn manager")]
        [SerializeField] private EnemySpawnManager spawnManager;

        [Header("Settings")]
        [Tooltip("Should enemies despawn when room is destroyed?")]
        [SerializeField] private bool despawnEnemiesWithRoom = true;

        [Tooltip("Enable debug logs for enemy spawning")]
        [SerializeField] private bool enableDebugLogs = false;

        private ProceduralLevelGenerator levelGenerator;

        private void Awake()
        {
            levelGenerator = GetComponent<ProceduralLevelGenerator>();

            // Find spawn manager if not assigned
            if (spawnManager == null)
            {
                spawnManager = FindObjectOfType<EnemySpawnManager>();

                if (spawnManager == null)
                {
                    Debug.LogError("[LevelGeneratorEnemyIntegration] No EnemySpawnManager found in scene! Please add one.");
                    enabled = false;
                }
            }
        }

        private void OnEnable()
        {
            // Subscribe to level generation events if available
            // This is a placeholder - implement events in ProceduralLevelGenerator if needed
        }

        private void OnDisable()
        {
            // Unsubscribe from events
        }

        /// <summary>
        /// Called by ProceduralLevelGenerator when a new room is spawned
        /// </summary>
        public void OnRoomSpawned(Room room, int roomIteration)
        {
            if (room == null || spawnManager == null) return;

            if (enableDebugLogs)
            {
                Debug.Log($"[LevelGeneratorEnemyIntegration] Room spawned: {room.RoomName}, Iteration: {roomIteration}");
            }

            // Room handles its own spawning via Room.OnSpawned()
            // This method can be used for additional logic if needed
        }

        /// <summary>
        /// Called by ProceduralLevelGenerator when a new corridor is spawned
        /// </summary>
        public void OnCorridorSpawned(Corridor corridor, int roomIteration)
        {
            if (corridor == null || spawnManager == null) return;

            if (enableDebugLogs)
            {
                Debug.Log($"[LevelGeneratorEnemyIntegration] Corridor spawned: {corridor.CorridorName}, Iteration: {roomIteration}");
            }

            // Corridor handles its own spawning via Corridor.OnSpawned()
            // This method can be used for additional logic if needed
        }

        /// <summary>
        /// Called when a room is about to be destroyed
        /// </summary>
        public void OnRoomDestroyed(Room room)
        {
            if (room == null) return;

            if (despawnEnemiesWithRoom)
            {
                DespawnEnemiesInRoom(room.transform);
            }
        }

        /// <summary>
        /// Despawn all enemies that are children of the given room transform
        /// </summary>
        private void DespawnEnemiesInRoom(Transform roomTransform)
        {
            var enemies = roomTransform.GetComponentsInChildren<AI.Enemy.EnemyController>();

            foreach (var enemy in enemies)
            {
                if (enemy != null && !enemy.IsDead)
                {
                    // Deactivate enemy (would be better to return to pool)
                    enemy.gameObject.SetActive(false);

                    if (enableDebugLogs)
                    {
                        Debug.Log($"[LevelGeneratorEnemyIntegration] Despawned enemy: {enemy.name}");
                    }
                }
            }
        }

        /// <summary>
        /// Get current difficulty based on room iteration
        /// </summary>
        public float GetDifficultyMultiplier(int roomIteration)
        {
            // Simple linear scaling, can be made more complex
            return 1f + (roomIteration * 0.1f);
        }

        /// <summary>
        /// Check if player should be overwhelmed (too many enemies nearby)
        /// </summary>
        public bool IsPlayerOverwhelmed(Transform player, float radius = 10f)
        {
            if (player == null) return false;

            var enemies = FindObjectsOfType<AI.Enemy.EnemyController>();
            int nearbyEnemies = 0;

            foreach (var enemy in enemies)
            {
                if (enemy.IsDead || !enemy.HasSeenPlayer) continue;

                float distance = Vector3.Distance(player.position, enemy.transform.position);
                if (distance <= radius)
                {
                    nearbyEnemies++;
                }
            }

            // Consider player overwhelmed if more than 5 enemies within radius
            return nearbyEnemies > 5;
        }

        #region Debug Visualization

        private void OnDrawGizmos()
        {
            if (!enableDebugLogs || !Application.isPlaying) return;

            // Draw lines between spawn manager and current rooms
            if (spawnManager != null && levelGenerator != null)
            {
                Gizmos.color = Color.cyan;
                // Add visualization if needed
            }
        }

        #endregion Debug Visualization
    }
}