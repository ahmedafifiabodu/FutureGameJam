using AI.Enemy;
using AI.Enemy.Configuration;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

        [Header("Prop Settings")]
        [SerializeField] private GameObject[] propPrefabs;

        [Tooltip("Chance to spawn a prop at a prop spawn point")]
        [Range(0f, 1f)]
        [SerializeField] private float propSpawnChance = 0.5f;

        [Header("Pooling")]
        [SerializeField] private Transform enemyPoolParent;

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
        /// </summary>
        public void SpawnEnemiesAtPoints(SpawnPoint[] spawnPoints, bool isRoom, int roomIteration)
        {
            if (spawnPoints == null || spawnPoints.Length == 0) return;

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

            if (validPoints.Count == 0) return;

            // Shuffle spawn points
            ShuffleList(validPoints);

            // Spawn enemies
            int spawned = 0;
            foreach (var point in validPoints)
            {
                if (spawned >= enemiesToSpawn) break;

                // Random chance to spawn
                if (Random.value > spawnChance) continue;

                // Select enemy type
                EnemyConfigSO config = SelectEnemyConfig(roomIteration);
                if (config == null) continue;

                // Spawn enemy
                SpawnEnemy(config, point.transform.position, point.transform.rotation);
                point.hasSpawned = true;
                spawned++;
            }
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

            if (availableConfigs.Count == 0) return null;

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

        private EnemyController SpawnEnemy(EnemyConfigSO config, Vector3 position, Quaternion rotation)
        {
            EnemyController enemy = GetEnemyFromPool(config);

            enemy.transform.SetPositionAndRotation(position, rotation);
            enemy.gameObject.SetActive(true);

            return enemy;
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