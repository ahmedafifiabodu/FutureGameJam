using UnityEngine;
using AI.Spawning;
using System.Collections.Generic;

namespace ProceduralGeneration
{
    /// <summary>
    /// Base class for room and corridor pieces
    /// </summary>
    public abstract class LevelPiece : MonoBehaviour
    {
        [Header("Connection Points")]
        [SerializeField] protected ConnectionPoint pointA; // Entrance

        [SerializeField] protected ConnectionPoint pointB; // Exit

        [Header("Doors")]
        [Tooltip("Door at Point A (entrance)")]
        [SerializeField] protected Door entranceDoor;

        [Tooltip("Door at Point B (exit)")]
        [SerializeField] protected Door exitDoor;

        [Header("Spawn Points")]
        [SerializeField] protected SpawnPoint[] spawnPoints;

        [Header("Player Tracking")]
        [SerializeField] protected bool playerHasEntered = false;

        [Header("Debug")]
        [SerializeField] protected bool showEnemyDebug = false;

        // Enemy tracking
        private List<AI.Enemy.EnemyController> activeEnemies = new List<AI.Enemy.EnemyController>();

        protected bool enemiesSpawned = false;
        private bool exitDoorOpened = false;

        public ConnectionPoint PointA => pointA;
        public ConnectionPoint PointB => pointB;
        public SpawnPoint[] SpawnPoints => spawnPoints;
        public bool PlayerHasEntered => playerHasEntered;
        public bool EnemiesSpawned => enemiesSpawned;

        protected virtual void Awake()
        {
            // Auto-find spawn points if not assigned
            if (spawnPoints == null || spawnPoints.Length == 0)
                spawnPoints = GetComponentsInChildren<SpawnPoint>();
        }

        /// <summary>
        /// Open only the entrance door (Point A)
        /// </summary>
        public virtual void OpenEntranceDoor()
        {
            if (entranceDoor != null)
            {
                entranceDoor.Open();
                Debug.Log($"[LevelPiece] Opened entrance door in {gameObject.name}");
            }
        }

        /// <summary>
        /// Open only the exit door (Point B)
        /// </summary>
        public virtual void OpenExitDoor()
        {
            if (exitDoor != null && !exitDoorOpened)
            {
                exitDoor.Open();
                exitDoorOpened = true;
                Debug.Log($"[LevelPiece] Opened exit door in {gameObject.name}");
            }
        }

        /// <summary>
        /// Close the entrance door
        /// </summary>
        public virtual void CloseEntranceDoor()
        {
            if (entranceDoor != null)
            {
                entranceDoor.Close();
            }
        }

        /// <summary>
        /// Close the exit door
        /// </summary>
        public virtual void CloseExitDoor()
        {
            if (exitDoor != null && exitDoorOpened)
            {
                exitDoor.Close();
                exitDoorOpened = false;
            }
        }

        /// <summary>
        /// Called when player enters this level piece through the entrance
        /// </summary>
        public virtual void OnPlayerEntered()
        {
            playerHasEntered = true;
            Debug.Log($"[LevelPiece] Player entered {gameObject.name}");

            // Open entrance door when player approaches
            OpenEntranceDoor();

            // Note: Door closing is now handled by ConnectionPoint when player exits the trigger area

            // Only proceed with enemy registration if enemies have been spawned
            // This prevents premature door opening
            if (enemiesSpawned)
            {
                RegisterSpawnedEnemies();
            }
            else
            {
                if (showEnemyDebug)
                    Debug.Log($"[LevelPiece] {gameObject.name} - Player entered but enemies not yet spawned, waiting...");
            }
        }

        /// <summary>
        /// Called when this level piece is spawned
        /// Override to handle spawn-time initialization
        /// </summary>
        public virtual void OnSpawned(int roomIteration)
        {
            // Override in derived classes
        }

        /// <summary>
        /// Register enemies spawned in this level piece
        /// </summary>
        protected virtual void RegisterSpawnedEnemies()
        {
            if (enemiesSpawned)
            {
                if (showEnemyDebug)
                    Debug.Log($"[LevelPiece] {gameObject.name} - Enemies already registered, skipping...");
                return;
            }

            enemiesSpawned = true;
            activeEnemies.Clear();

            // Find all enemies that are children of this level piece
            var enemies = GetComponentsInChildren<AI.Enemy.EnemyController>();

            if (showEnemyDebug)
                Debug.Log($"[LevelPiece] {gameObject.name} - Found {enemies.Length} enemy controllers in children");

            foreach (var enemy in enemies)
            {
                if (enemy != null && !enemy.IsDead)
                {
                    activeEnemies.Add(enemy);
                    if (showEnemyDebug)
                        Debug.Log($"[LevelPiece] {gameObject.name} - Added enemy: {enemy.name}");
                }
            }

            Debug.Log($"[LevelPiece] Registered {activeEnemies.Count} enemies in {gameObject.name}");

            // Handle door logic based on enemy count
            if (activeEnemies.Count > 0)
            {
                // Enemies found - keep exit door closed
                CloseExitDoor();
                if (showEnemyDebug)
                    Debug.Log($"[LevelPiece] {gameObject.name} - {activeEnemies.Count} enemies present, exit door closed");

                // Start checking for enemy deaths
                InvokeRepeating(nameof(CheckEnemyStatus), 1f, 0.5f);
            }
            else
            {
                // No enemies found - check if this is expected or a spawn failure
                // Let derived classes (Room/Corridor) handle this decision
                HandleNoEnemiesFound();
            }
        }

        /// <summary>
        /// Handle the case when no enemies are found after spawning
        /// Can be overridden by derived classes for custom logic
        /// </summary>
        protected virtual void HandleNoEnemiesFound()
        {
            // Default behavior: open exit door if no enemies
            if (showEnemyDebug)
                Debug.Log($"[LevelPiece] {gameObject.name} - No enemies found, opening exit door (default behavior)");
            OpenExitDoor();
        }

        /// <summary>
        /// Periodically check if all enemies are dead
        /// </summary>
        private void CheckEnemyStatus()
        {
            if (!enemiesSpawned || !playerHasEntered)
            {
                if (showEnemyDebug)
                    Debug.Log($"[LevelPiece] {gameObject.name} - CheckEnemyStatus: not ready (spawned:{enemiesSpawned}, entered:{playerHasEntered})");
                return;
            }

            // Count living enemies
            int originalCount = activeEnemies.Count;

            // Remove null or dead enemies from the list
            activeEnemies.RemoveAll(e => e == null || e.IsDead);

            if (showEnemyDebug && activeEnemies.Count != originalCount)
            {
                Debug.Log($"[LevelPiece] {gameObject.name} - Enemy count changed: {originalCount} -> {activeEnemies.Count}");
            }

            // If all enemies are dead, open exit door
            if (activeEnemies.Count == 0 && !exitDoorOpened)
            {
                OpenExitDoor();
                CancelInvoke(nameof(CheckEnemyStatus));
                Debug.Log($"[LevelPiece] All enemies defeated in {gameObject.name}, opening exit door");
            }
        }

        /// <summary>
        /// Force register enemies (called after spawning delay)
        /// </summary>
        public void ForceRegisterEnemies()
        {
            if (showEnemyDebug)
                Debug.Log($"[LevelPiece] {gameObject.name} - ForceRegisterEnemies called");

            RegisterSpawnedEnemies();
        }

        /// <summary>
        /// Get count of active (alive) enemies
        /// </summary>
        public int GetActiveEnemyCount()
        {
            if (!enemiesSpawned) return 0;

            // Clean up the list first
            activeEnemies.RemoveAll(e => e == null || e.IsDead);
            return activeEnemies.Count;
        }

        /// <summary>
        /// Check if this level piece has any enemies
        /// </summary>
        public bool HasEnemies()
        {
            return GetActiveEnemyCount() > 0;
        }
    }
}