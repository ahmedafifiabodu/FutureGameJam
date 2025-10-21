using UnityEngine;
using UnityEngine.AI;

namespace AI.Spawning
{
    /// <summary>
    /// Spawn point for enemies or props
    /// Can be placed in rooms/corridors and activated by EnemySpawnManager
    /// </summary>
    public class SpawnPoint : MonoBehaviour
    {
        [Header("Spawn Type")]
        [SerializeField] private SpawnPointType spawnType = SpawnPointType.Enemy;

        [Header("Enemy Settings")]
        [Tooltip("If empty, uses spawn manager's enemy pool")]
        [SerializeField] private GameObject[] specificEnemyPrefabs;
        [SerializeField] private bool spawnOnStart = false;
        [SerializeField] private bool spawnOnlyOnce = true;
        
        [Header("NavMesh Settings")]
        [Tooltip("Auto-align enemies to nearest NavMesh surface")]
        [SerializeField] private bool autoAlignToNavMesh = true;
        [Tooltip("Max distance to search for NavMesh (units)")]
        [SerializeField] private float navMeshSearchDistance = 5f;

        [Header("Prop Settings")]
        [Tooltip("Prop prefab to spawn (for prop spawn points)")]
        [SerializeField] private GameObject propPrefab;

        [Header("Debug")]
        [SerializeField] private Color gizmoColor = Color.green;
        [SerializeField] private float gizmoSize = 0.5f;

        private bool hasSpawned = false;
        private GameObject spawnedObject;

        public SpawnPointType Type => spawnType;
        public bool HasSpawned => hasSpawned;
        public bool CanSpawn => !spawnOnlyOnce || !hasSpawned;

        private void Start()
        {
            if (spawnOnStart)
            {
                Spawn();
            }
        }

        /// <summary>
        /// Spawn enemy or prop at this point
        /// </summary>
        public GameObject Spawn()
        {
            if (!CanSpawn)
            {
                Debug.LogWarning($"[SpawnPoint] {gameObject.name} cannot spawn (already spawned once)");
                return null;
            }

            GameObject prefabToSpawn = null;

            if (spawnType == SpawnPointType.Enemy)
            {
                // Get enemy prefab
                if (specificEnemyPrefabs != null && specificEnemyPrefabs.Length > 0)
                {
                    prefabToSpawn = specificEnemyPrefabs[Random.Range(0, specificEnemyPrefabs.Length)];
                }
                else
                {
                    Debug.LogWarning($"[SpawnPoint] No enemy prefabs assigned to {gameObject.name}");
                    return null;
                }
            }
            else if (spawnType == SpawnPointType.Prop)
            {
                prefabToSpawn = propPrefab;
            }

            if (prefabToSpawn == null)
            {
                Debug.LogWarning($"[SpawnPoint] No prefab to spawn at {gameObject.name}");
                return null;
            }

            // Get spawn position (align to NavMesh if enemy)
            Vector3 spawnPosition = GetSpawnPosition(spawnType == SpawnPointType.Enemy);
            
            // Spawn object
            spawnedObject = Instantiate(prefabToSpawn, spawnPosition, transform.rotation);
            hasSpawned = true;

            Debug.Log($"[SpawnPoint] Spawned {prefabToSpawn.name} at {gameObject.name} (Position: {spawnPosition})");

            return spawnedObject;
        }

        /// <summary>
        /// Spawn specific prefab (called by EnemySpawnManager)
        /// </summary>
        public GameObject Spawn(GameObject prefab)
        {
            if (!CanSpawn)
            {
                Debug.LogWarning($"[SpawnPoint] {gameObject.name} cannot spawn (already spawned once)");
                return null;
            }

            if (prefab == null)
            {
                Debug.LogWarning($"[SpawnPoint] Null prefab provided to {gameObject.name}");
                return null;
            }

            // Check if spawning an enemy (has NavMeshAgent component)
            bool isEnemy = prefab.GetComponent<UnityEngine.AI.NavMeshAgent>() != null;
            
            // Get spawn position (align to NavMesh if enemy)
            Vector3 spawnPosition = GetSpawnPosition(isEnemy);

            spawnedObject = Instantiate(prefab, spawnPosition, transform.rotation);
            hasSpawned = true;

            // If enemy, assign current room/corridor
            if (isEnemy)
            {
                var roomTracker = spawnedObject.GetComponent<AI.EnemyRoomTracker>();
                if (roomTracker != null)
                {
                    // Find parent room or corridor
                    var levelPiece = GetComponentInParent<ProceduralGeneration.LevelPiece>();
                    if (levelPiece != null)
                    {
                        roomTracker.SetCurrentLevelPiece(levelPiece);
                        Debug.Log($"[SpawnPoint] Assigned {spawnedObject.name} to {levelPiece.name}");
                    }
                }
            }

            Debug.Log($"[SpawnPoint] Spawned {prefab.name} at {gameObject.name} (Position: {spawnPosition}, OnNavMesh: {isEnemy && autoAlignToNavMesh})");

            return spawnedObject;
        }

        /// <summary>
        /// Get spawn position, optionally aligned to NavMesh
        /// </summary>
        private Vector3 GetSpawnPosition(bool alignToNavMesh)
        {
            Vector3 spawnPos = transform.position;

            if (alignToNavMesh && autoAlignToNavMesh)
            {
                // Try to find nearest point on NavMesh
                if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, navMeshSearchDistance, NavMesh.AllAreas))
                {
                    spawnPos = hit.position;
                    Debug.Log($"[SpawnPoint] Aligned to NavMesh: {transform.position} -> {spawnPos} (distance: {Vector3.Distance(transform.position, spawnPos):F2})");
                }
                else
                {
                    Debug.LogWarning($"[SpawnPoint] {gameObject.name} - No NavMesh found within {navMeshSearchDistance} units! Enemy may not move.");
                }
            }

            return spawnPos;
        }

        /// <summary>
        /// Reset spawn point (for reuse)
        /// </summary>
        public void Reset()
        {
            hasSpawned = false;
            
            if (spawnedObject != null)
            {
                Destroy(spawnedObject);
                spawnedObject = null;
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireSphere(transform.position, gizmoSize);

            // Draw direction arrow
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, transform.forward * 1f);
            
            // Draw NavMesh search area (for enemies)
            if (spawnType == SpawnPointType.Enemy && autoAlignToNavMesh)
            {
                Gizmos.color = new Color(0, 1, 1, 0.2f); // Cyan transparent
                Gizmos.DrawWireSphere(transform.position, navMeshSearchDistance);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = gizmoColor;
            Gizmos.DrawSphere(transform.position, gizmoSize);

            // Draw spawn type text
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up, $"{spawnType} Spawn Point");
            
            // Show NavMesh alignment info for enemies
            if (spawnType == SpawnPointType.Enemy && autoAlignToNavMesh)
            {
                // Check if on NavMesh in editor
                if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, navMeshSearchDistance, NavMesh.AllAreas))
                {
                    // Draw line to nearest NavMesh point
                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(transform.position, hit.position);
                    Gizmos.DrawSphere(hit.position, 0.2f);
                    
                    float distance = Vector3.Distance(transform.position, hit.position);
                    UnityEditor.Handles.Label(hit.position + Vector3.up * 0.5f, $"NavMesh: {distance:F2}m");
                }
                else
                {
                    // No NavMesh found - draw warning
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireSphere(transform.position, navMeshSearchDistance);
                    UnityEditor.Handles.Label(transform.position + Vector3.up * 1.5f, "? NO NAVMESH!");
                }
            }
            #endif
        }
    }

    public enum SpawnPointType
    {
        Enemy,
        Prop
    }
}
