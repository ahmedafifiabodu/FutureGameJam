using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace AI.Spawning
{
    /// <summary>
    /// Weighted enemy prefab with spawn conditions
    /// Designer-friendly spawn configuration
    /// </summary>
    [System.Serializable]
    public class WeightedEnemyPrefab
    {
        [Tooltip("Enemy prefab to spawn")]
        public GameObject enemyPrefab;

        [Tooltip("Enemy profile (automatically detected from prefab if not set)")]
        public EnemyProfile profile;

        [Tooltip("Spawn weight (higher = more common) - overrides profile if > 0")]
        [Range(0, 100)]
        public int weight = 0; // 0 = use profile weight

        public bool IsValid()
        {
            if (enemyPrefab == null) return false;
            
            // Try to get profile from prefab if not assigned
            if (profile == null)
            {
                var enemyAI = enemyPrefab.GetComponent<EnemyAI>();
                if (enemyAI != null)
                {
                    // Access profile through reflection or make it public
                    // For now, we'll require manual assignment
                    return false;
                }
            }
            
            return profile != null;
        }

        public bool CanSpawnAtIteration(int currentIteration)
        {
            return profile != null && currentIteration >= profile.minRoomIteration;
        }

        public int GetWeight()
        {
            // Use override weight if set, otherwise use profile weight
            return weight > 0 ? weight : (profile != null ? profile.spawnWeight : 0);
        }

        public int GetMaxPerRoom()
        {
            return profile != null ? profile.maxPerRoom : 1;
        }
    }

    /// <summary>
    /// Manages enemy spawning in rooms and corridors
    /// Integrates with ProceduralLevelGenerator for difficulty scaling
    /// </summary>
    public class EnemySpawnManager : MonoBehaviour
    {
        [Header("Enemy Pool")]
        [SerializeField] private WeightedEnemyPrefab[] enemyPrefabs;

        [Header("Spawn Settings")]
        [Tooltip("Base number of enemies per room")]
        [SerializeField] private int baseEnemiesPerRoom = 2;

        [Tooltip("Additional enemies per room iteration")]
        [SerializeField] private float enemiesPerIteration = 0.5f;

        [Tooltip("Maximum enemies per room")]
        [SerializeField] private int maxEnemiesPerRoom = 8;

        [Tooltip("Chance to spawn enemy at a spawn point (0-1)")]
        [Range(0f, 1f)]
        [SerializeField] private float enemySpawnChance = 0.7f;

        [Tooltip("Increase spawn chance per iteration")]
        [Range(0f, 0.1f)]
        [SerializeField] private float spawnChanceIncreasePerIteration = 0.05f;

        [Header("Corridor Settings")]
        [Tooltip("Chance to spawn enemies in corridors")]
        [Range(0f, 1f)]
        [SerializeField] private float corridorSpawnChance = 0.3f;

        [Tooltip("Maximum enemies in corridors")]
        [SerializeField] private int maxEnemiesPerCorridor = 2;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;

        // Track current iteration (updated by ProceduralLevelGenerator)
        private int currentRoomIteration = 0;
        private Dictionary<EnemyProfile, int> spawnedEnemyCount = new Dictionary<EnemyProfile, int>();

        private void Awake()
        {
            ValidateEnemyPrefabs();
        }

        private void ValidateEnemyPrefabs()
        {
            if (enemyPrefabs == null || enemyPrefabs.Length == 0)
            {
                Debug.LogError("[EnemySpawnManager] No enemy prefabs assigned!");
                return;
            }

            int validCount = 0;
            for (int i = 0; i < enemyPrefabs.Length; i++)
            {
                var prefab = enemyPrefabs[i];
                if (prefab.IsValid())
                {
                    validCount++;
                    
                    if (enableDebugLogs)
                    {
                        Debug.Log($"[EnemySpawnManager] Valid: {prefab.enemyPrefab.name} " +
                                  $"(Profile: {prefab.profile.enemyName}, Weight: {prefab.GetWeight()}, " +
                                  $"Min Iteration: {prefab.profile.minRoomIteration}, Max Per Room: {prefab.GetMaxPerRoom()})");
                    }
                }
                else
                {
                    string reason = "";
                    if (prefab.enemyPrefab == null)
                        reason = "Enemy prefab is null";
                    else if (prefab.profile == null)
                        reason = "Profile is null";
                    
                    Debug.LogWarning($"[EnemySpawnManager] Invalid prefab at index {i}: {reason}");
                }
            }
            
            if (enableDebugLogs)
                Debug.Log($"[EnemySpawnManager] Validated {validCount}/{enemyPrefabs.Length} enemy prefabs");
        }

        /// <summary>
        /// Spawn enemies at spawn points in a room/corridor
        /// Called by ProceduralLevelGenerator when spawning a new level piece
        /// </summary>
        public void SpawnEnemiesAtPoints(SpawnPoint[] spawnPoints, bool isRoom, int roomIteration)
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                if (enableDebugLogs)
                    Debug.Log("[EnemySpawnManager] No spawn points provided");
                return;
            }

            currentRoomIteration = roomIteration;
            spawnedEnemyCount.Clear();

            // Get enemy spawn points only
            var enemySpawnPoints = spawnPoints.Where(sp => sp.Type == SpawnPointType.Enemy).ToArray();
            
            if (enemySpawnPoints.Length == 0)
            {
                if (enableDebugLogs)
                    Debug.Log("[EnemySpawnManager] No enemy spawn points found");
                return;
            }

            // Calculate how many enemies to spawn
            int targetEnemyCount = CalculateEnemyCount(isRoom, roomIteration);
            int maxEnemies = isRoom ? maxEnemiesPerRoom : maxEnemiesPerCorridor;
            targetEnemyCount = Mathf.Min(targetEnemyCount, maxEnemies, enemySpawnPoints.Length);

            // Calculate current spawn chance
            float currentSpawnChance = enemySpawnChance + (roomIteration * spawnChanceIncreasePerIteration);
            currentSpawnChance = Mathf.Clamp01(currentSpawnChance);

            if (enableDebugLogs)
                Debug.Log($"[EnemySpawnManager] Attempting to spawn {targetEnemyCount} enemies " +
                          $"(iteration {roomIteration}, spawn chance {currentSpawnChance:P0}, " +
                          $"spawn points: {enemySpawnPoints.Length})");

            // Shuffle spawn points for randomness
            var shuffledPoints = enemySpawnPoints.OrderBy(x => Random.value).ToArray();

            int spawnedCount = 0;
            int attemptCount = 0;

            foreach (var spawnPoint in shuffledPoints)
            {
                if (spawnedCount >= targetEnemyCount)
                    break;

                attemptCount++;

                // Probability check
                if (Random.value > currentSpawnChance)
                {
                    if (enableDebugLogs)
                        Debug.Log($"[EnemySpawnManager] Attempt {attemptCount}: Spawn chance failed");
                    continue;
                }

                // Get random enemy prefab based on weights and constraints
                var enemyPrefab = GetRandomEnemyPrefab(roomIteration);
                
                if (enemyPrefab != null)
                {
                    var spawned = spawnPoint.Spawn(enemyPrefab.enemyPrefab);
                    
                    if (spawned != null)
                    {
                        spawnedCount++;
                        
                        // Track spawned count per type
                        if (!spawnedEnemyCount.ContainsKey(enemyPrefab.profile))
                            spawnedEnemyCount[enemyPrefab.profile] = 0;
                        
                        spawnedEnemyCount[enemyPrefab.profile]++;

                        if (enableDebugLogs)
                            Debug.Log($"[EnemySpawnManager] Spawned {enemyPrefab.profile.enemyName} at {spawnPoint.gameObject.name}");
                    }
                }
                else
                {
                    if (enableDebugLogs)
                        Debug.Log($"[EnemySpawnManager] Attempt {attemptCount}: No valid prefab returned");
                }
            }

            if (enableDebugLogs)
                Debug.Log($"[EnemySpawnManager] Spawned {spawnedCount}/{targetEnemyCount} enemies " +
                          $"({attemptCount} attempts, {enemySpawnPoints.Length} spawn points)");
        }

        private int CalculateEnemyCount(bool isRoom, int iteration)
        {
            if (!isRoom)
            {
                // Corridors use simpler logic
                return Random.value < corridorSpawnChance ? Random.Range(1, maxEnemiesPerCorridor + 1) : 0;
            }

            // Rooms scale with iteration
            int count = baseEnemiesPerRoom + Mathf.FloorToInt(iteration * enemiesPerIteration);
            return count;
        }

        private WeightedEnemyPrefab GetRandomEnemyPrefab(int currentIteration)
        {
            // Filter valid prefabs for current iteration
            var validPrefabs = enemyPrefabs
                .Where(e => e.IsValid() && e.CanSpawnAtIteration(currentIteration))
                .ToList();

            if (validPrefabs.Count == 0)
            {
                Debug.LogWarning($"[EnemySpawnManager] No valid enemy prefabs for iteration {currentIteration}");
                return null;
            }

            // Further filter by max per room constraint
            validPrefabs = validPrefabs
                .Where(e => !spawnedEnemyCount.ContainsKey(e.profile) || spawnedEnemyCount[e.profile] < e.GetMaxPerRoom())
                .ToList();

            if (validPrefabs.Count == 0)
            {
                if (enableDebugLogs)
                    Debug.Log($"[EnemySpawnManager] All enemy types reached max per room limit");
                return null;
            }

            // Weighted random selection
            int totalWeight = validPrefabs.Sum(e => e.GetWeight());
            if (totalWeight == 0)
            {
                Debug.LogWarning($"[EnemySpawnManager] Total weight is 0! Check enemy profile weights.");
                return null;
            }

            int randomValue = Random.Range(0, totalWeight);
            int currentWeight = 0;

            foreach (var prefab in validPrefabs)
            {
                currentWeight += prefab.GetWeight();
                if (randomValue < currentWeight)
                {
                    return prefab;
                }
            }

            return validPrefabs[0]; // Fallback
        }

        /// <summary>
        /// Spawn props at prop spawn points
        /// </summary>
        public void SpawnPropsAtPoints(SpawnPoint[] spawnPoints)
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
                return;

            var propSpawnPoints = spawnPoints.Where(sp => sp.Type == SpawnPointType.Prop).ToArray();

            foreach (var spawnPoint in propSpawnPoints)
            {
                spawnPoint.Spawn();
            }

            if (enableDebugLogs)
                Debug.Log($"[EnemySpawnManager] Spawned props at {propSpawnPoints.Length} spawn points");
        }

        /// <summary>
        /// Set current room iteration (called by ProceduralLevelGenerator)
        /// </summary>
        public void SetRoomIteration(int iteration)
        {
            currentRoomIteration = iteration;
        }

        public int GetCurrentIteration() => currentRoomIteration;
    }
}
