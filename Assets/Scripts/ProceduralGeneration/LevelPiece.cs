using UnityEngine;
using AI.Spawning;

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
        [SerializeField] protected GameObject[] doors;

        [Header("Spawn Points")]
        [SerializeField] protected SpawnPoint[] spawnPoints;

        [Header("Player Tracking")]
        [SerializeField] protected bool playerHasEntered = false;

        public ConnectionPoint PointA => pointA;
        public ConnectionPoint PointB => pointB;
        public SpawnPoint[] SpawnPoints => spawnPoints;
        public bool PlayerHasEntered => playerHasEntered;

        protected virtual void Awake()
        {
            // Auto-find connection points if not assigned
            if (pointA == null || pointB == null)
            {
                var points = GetComponentsInChildren<ConnectionPoint>();
                foreach (var point in points)
                {
                    if (point.Type == ConnectionPoint.PointType.A)
                        pointA = point;
                    else if (point.Type == ConnectionPoint.PointType.B)
                        pointB = point;
                }
            }

            if (pointA == null)
                Debug.LogWarning($"[{gameObject.name}] Point A (Entrance) not found!");
            if (pointB == null)
                Debug.LogWarning($"[{gameObject.name}] Point B (Exit) not found!");

            // Auto-find spawn points if not assigned
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                spawnPoints = GetComponentsInChildren<SpawnPoint>();
            }
        }

        public virtual void CloseDoors()
        {
            if (doors == null) return;
            
            foreach (var door in doors)
            {
                if (door != null)
                    door.SetActive(true);
            }
        }

        public virtual void OpenDoors()
        {
            if (doors == null) return;
            
            foreach (var door in doors)
            {
                if (door != null)
                    door.SetActive(false);
            }
        }

        /// <summary>
        /// Called when player enters this level piece through the entrance
        /// </summary>
        public virtual void OnPlayerEntered()
        {
            playerHasEntered = true;
            Debug.Log($"[LevelPiece] Player entered {gameObject.name}");
        }

        /// <summary>
        /// Called when this level piece is spawned
        /// Override to handle spawn-time initialization
        /// </summary>
        public virtual void OnSpawned(int roomIteration)
        {
            // Override in derived classes
        }
    }
}
