using AI.Enemy;
using AI.Enemy.Configuration;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace AI.Spawning
{
    /// <summary>
    /// Manages enemy and prop spawning across procedurally generated levels
    /// </summary>
    public class EnemySpawnManager : MonoBehaviour
    {
        [Header("Enemy Configurations")]
        [SerializeField] private List<EnemyConfigSO> enemyConfigs = new List<EnemyConfigSO>();

        [Header("Spawn Settings")]
        [Tooltip("Base number of enemies per room")]
        [SerializeField] private int baseEnemiesPerRoom = 3;

        [Tooltip("Additional enemies per room iteration")]
        [SerializeField] private float enemiesPerIteration = 0.5f;

        [Tooltip("Maximum enemies per room")]
        [SerializeField] private int maxEnemiesPerRoom = 10;

        [Tooltip("Base spawn chance for rooms")]
        [Range(0f, 1f)]
        [SerializeField] private float baseRoomSpawnChance = 0.8f;

        [Tooltip("Base spawn chance for corridors")]
        [Range(0f, 1f)]
        [SerializeField] private float baseCorridorSpawnChance = 0.3f;

        [Header("NavMesh Settings")]
        [Tooltip("Maximum distance to search for valid NavMesh position")]
        [SerializeField] private float navMeshSearchRadius = 5f;

        [Tooltip("Maximum attempts to find a valid spawn position")]
        [SerializeField] private int maxSpawnAttempts = 5;

        [Header("Prop Settings")]
        [SerializeField] private GameObject[] propPrefabs;

        [Tooltip("Chance to spawn a prop at a prop spawn point")]
        [Range(0f, 1f)]
        [SerializeField] private float propSpawnChance = 0.5f;

        [Header("Pooling")]
        [SerializeField] private Transform enemyPoolParent;

        [Header("Debug")]
        [SerializeField] private bool showSpawnDebug = true;

        // Runtime data
        private int currentRoomIteration = 0;
        private Dictionary<EnemyConfigSO, Queue<EnemyController>> enemyPools = new Dictionary<EnemyConfigSO, Queue<EnemyController>>();

        private void Awake()
        {
            if (enemyPoolParent == null)
            {
                enemyPoolParent = new GameObject("Enemy Pool").transform;
                enemyPoolParent.SetParent(transform);
            }

            InitializeEnemyPools();
        }

        #region Initialization

        private void InitializeEnemyPools()
        {
            // Pre-instantiate some enemies for pooling
            foreach (var config in enemyConfigs)
            {
                if (config == null || config.enemyPrefab == null)
                    continue;

                enemyPools[config] = new Queue<EnemyController>();
            }
        }

        private EnemyController CreateEnemy(EnemyConfigSO config)
        {
            GameObject enemyObj = Instantiate(config.enemyPrefab, enemyPoolParent);
            enemyObj.SetActive(false);

            if (!enemyObj.TryGetComponent<EnemyController>(out var controller))
                controller = enemyObj.AddComponent<EnemyController>();

            enemyPools[config].Enqueue(controller);
            return controller;
        }

        #endregion Initialization

        #region Public API

        /// <summary>
        /// Set the current room iteration for difficulty scaling
        /// </summary>
        public void SetRoomIteration(int iteration)
        {
            currentRoomIteration = iteration;
        }

        /// <summary>
        /// Get current room iteration
        /// </summary>
        public int GetCurrentIteration()
        {
            return currentRoomIteration;
        }

        /// <summary>
        /// Spawn enemies at the given spawn points
        /// Returns the number of enemies successfully spawned
        /// </summary>
        public int SpawnEnemiesAtPoints(SpawnPoint[] spawnPoints, bool isRoom, int roomIteration)
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                if (showSpawnDebug)
                    Debug.LogWarning("[EnemySpawnManager] No spawn points provided");
                return 0;
            }

            currentRoomIteration = roomIteration;

            // Calculate spawn chance based on room or corridor
            float spawnChance = isRoom ? baseRoomSpawnChance : baseCorridorSpawnChance;

            // Increase spawn chance slightly with iteration
            float iterationBonus = Mathf.Min(roomIteration * 0.05f, 0.2f);
            spawnChance = Mathf.Min(spawnChance + iterationBonus, 1f);

            // Calculate number of enemies to spawn
            int enemiesToSpawn = CalculateEnemyCount(isRoom, roomIteration);

            // Get valid enemy spawn points
            var validPoints = spawnPoints.Where(sp =>
                sp != null &&
                sp.isActive &&
                !sp.hasSpawned &&
                (sp.spawnType == SpawnPoint.SpawnType.Enemy || sp.spawnType == SpawnPoint.SpawnType.Both)
            ).ToList();

            if (validPoints.Count == 0)
            {
                if (showSpawnDebug)
                    Debug.LogWarning("[EnemySpawnManager] No valid enemy spawn points found");
                return 0;
            }

            // Shuffle spawn points
            ShuffleList(validPoints);

            // Spawn enemies
            int spawned = 0;
            int attempts = 0;

            if (showSpawnDebug)
                Debug.Log($"[EnemySpawnManager] Attempting to spawn {enemiesToSpawn} enemies at {validPoints.Count} points");

            foreach (var point in validPoints)
            {
                if (spawned >= enemiesToSpawn) break;

                // Random chance to spawn
                if (Random.value > spawnChance) continue;

                // Select enemy type
                EnemyConfigSO config = SelectEnemyConfig(roomIteration);
                if (config == null) continue;

                // Try to spawn enemy with NavMesh validation
                if (TrySpawnEnemyAtPoint(config, point))
                {
                    point.hasSpawned = true;
                    spawned++;

                    if (showSpawnDebug)
                        Debug.Log($"[EnemySpawnManager] Successfully spawned {config.enemyName} at {point.name}");
                }
                else
                {
                    if (showSpawnDebug)
                        Debug.LogWarning($"[EnemySpawnManager] Failed to spawn {config.enemyName} at {point.name} - NavMesh issue");
                }

                attempts++;
            }

            if (showSpawnDebug)
            {
                Debug.Log($"[EnemySpawnManager] Spawn complete: {spawned}/{enemiesToSpawn} enemies spawned, {attempts} attempts made");

                if (spawned == 0 && enemiesToSpawn > 0)
                {
                    Debug.LogError($"[EnemySpawnManager] CRITICAL: Failed to spawn any enemies! Check NavMesh and spawn point positions.");
                }
            }

            return spawned;
        }

        /// <summary>
        /// Spawn props at spawn points
        /// </summary>
        public void SpawnPropsAtPoints(SpawnPoint[] spawnPoints)
        {
            if (spawnPoints == null || spawnPoints.Length == 0 || propPrefabs == null || propPrefabs.Length == 0)
                return;

            var validPoints = spawnPoints.Where(sp =>
                sp != null &&
                sp.isActive &&
                !sp.hasSpawned &&
                (sp.spawnType == SpawnPoint.SpawnType.Prop || sp.spawnType == SpawnPoint.SpawnType.Both)
            ).ToList();

            foreach (var point in validPoints)
            {
                if (Random.value <= propSpawnChance)
                {
                    GameObject propPrefab = propPrefabs[Random.Range(0, propPrefabs.Length)];
                    Instantiate(propPrefab, point.transform.position, point.transform.rotation);
                    point.hasSpawned = true;
                }
            }
        }

        #endregion Public API

        #region Helper Methods

        private int CalculateEnemyCount(bool isRoom, int iteration)
        {
            if (!isRoom) return Mathf.Min(Random.Range(0, 3), maxEnemiesPerRoom); // Corridors have fewer enemies

            int count = baseEnemiesPerRoom + Mathf.FloorToInt(iteration * enemiesPerIteration);
            return Mathf.Min(count, maxEnemiesPerRoom);
        }

        private EnemyConfigSO SelectEnemyConfig(int roomIteration)
        {
            // Filter configs based on minimum iteration
            var availableConfigs = enemyConfigs.Where(c =>
                c != null &&
                c.enemyPrefab != null &&
                c.minRoomIteration <= roomIteration
            ).ToList();

            if (availableConfigs.Count == 0)
            {
                if (showSpawnDebug)
                    Debug.LogWarning($"[EnemySpawnManager] No enemy configs available for iteration {roomIteration}");
                return null;
            }

            // Calculate total weight
            int totalWeight = availableConfigs.Sum(c => c.spawnWeight);
            if (totalWeight == 0) return availableConfigs[Random.Range(0, availableConfigs.Count)];

            // Weighted random selection
            int randomValue = Random.Range(0, totalWeight);
            int currentWeight = 0;

            foreach (var config in availableConfigs)
            {
                currentWeight += config.spawnWeight;
                if (randomValue < currentWeight)
                {
                    return config;
                }
            }

            return availableConfigs[0];
        }

        /// <summary>
        /// Try to spawn an enemy at a specific point with NavMesh validation
        /// </summary>
        private bool TrySpawnEnemyAtPoint(EnemyConfigSO config, SpawnPoint spawnPoint)
        {
            Vector3 originalPosition = spawnPoint.transform.position;
            Vector3 validPosition;

            // Try to find a valid NavMesh position
            if (FindValidNavMeshPosition(originalPosition, out validPosition))
            {
                EnemyController enemy = SpawnEnemyAtPosition(config, validPosition, spawnPoint.transform.rotation);

                if (enemy != null)
                {
                    // Set the parent to the same parent as the spawn point (usually the room/corridor)
                    if (spawnPoint.transform.parent != null)
                    {
                        enemy.transform.SetParent(spawnPoint.transform.parent);
                    }

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Find a valid position on the NavMesh near the target position
        /// </summary>
        private bool FindValidNavMeshPosition(Vector3 targetPosition, out Vector3 validPosition)
        {
            validPosition = targetPosition;

            // First, try the exact position
            if (IsPositionOnNavMesh(targetPosition))
            {
                validPosition = targetPosition;
                return true;
            }

            // Try to find a nearby valid position
            for (int i = 0; i < maxSpawnAttempts; i++)
            {
                // Generate random position within search radius
                Vector2 randomCircle = Random.insideUnitCircle * navMeshSearchRadius;
                Vector3 testPosition = targetPosition + new Vector3(randomCircle.x, 0, randomCircle.y);

                if (NavMesh.SamplePosition(testPosition, out NavMeshHit hit, navMeshSearchRadius, NavMesh.AllAreas))
                {
                    validPosition = hit.position;

                    if (showSpawnDebug)
                        Debug.Log($"[EnemySpawnManager] Found valid NavMesh position after {i + 1} attempts. Distance from original: {Vector3.Distance(targetPosition, validPosition):F2}");

                    return true;
                }
            }

            if (showSpawnDebug)
                Debug.LogError($"[EnemySpawnManager] Failed to find valid NavMesh position near {targetPosition} after {maxSpawnAttempts} attempts");

            return false;
        }

        /// <summary>
        /// Check if a position is on the NavMesh
        /// </summary>
        private bool IsPositionOnNavMesh(Vector3 position)
        {
            return NavMesh.SamplePosition(position, out NavMeshHit hit, 0.1f, NavMesh.AllAreas);
        }

        private EnemyController SpawnEnemyAtPosition(EnemyConfigSO config, Vector3 position, Quaternion rotation)
        {
            try
            {
                EnemyController enemy = GetEnemyFromPool(config);
                enemy.transform.SetPositionAndRotation(position, rotation);
                enemy.gameObject.SetActive(true);
                return enemy;
            }
            catch (System.Exception e)
            {
                if (showSpawnDebug)
                    Debug.LogError($"[EnemySpawnManager] Failed to spawn enemy: {e.Message}");
                return null;
            }
        }

        private EnemyController GetEnemyFromPool(EnemyConfigSO config)
        {
            if (!enemyPools.ContainsKey(config) || enemyPools[config].Count == 0)
                return CreateEnemy(config);

            return enemyPools[config].Dequeue();
        }

        private void ShuffleList<T>(List<T> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                T temp = list[i];
                int randomIndex = Random.Range(i, list.Count);
                list[i] = list[randomIndex];
                list[randomIndex] = temp;
            }
        }

        #endregion Helper Methods
    }
}