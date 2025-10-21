using UnityEngine;
using AI.Spawning;

namespace ProceduralGeneration
{
    /// <summary>
    /// Represents a room piece in the procedural level
    /// </summary>
    public class Room : LevelPiece
    {
        [Header("Room Settings")]
        [SerializeField] private string roomName = "Room";

        [SerializeField] private bool startingRoom = false;

        [Header("NavMesh Settings")]
        [Tooltip("Delay before spawning enemies (allows NavMesh to update)")]
        [SerializeField] private float enemySpawnDelay = 0.5f;

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
                Debug.Log($"[Room] Starting room {roomName} - marked as player entered");
            }
        }

        public override void OnSpawned(int roomIteration)
        {
            base.OnSpawned(roomIteration);

            // Delay enemy spawning to allow NavMesh to carve/update
            // Unity's NavMesh updates automatically for Navigation Static objects
            Invoke(nameof(SpawnEnemiesDelayed), enemySpawnDelay);
        }

        private void SpawnEnemiesDelayed()
        {
            // Spawn enemies and props if spawn manager exists
            var spawnManager = FindObjectOfType<AI.Spawning.EnemySpawnManager>();
            if (spawnManager && spawnPoints != null && spawnPoints.Length > 0)
            {
                // Get current iteration from spawn manager
                int currentIteration = spawnManager.GetCurrentIteration();

                spawnManager.SpawnEnemiesAtPoints(spawnPoints, true, currentIteration);
                spawnManager.SpawnPropsAtPoints(spawnPoints);
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = startingRoom ? Color.yellow : Color.blue;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 2f);
        }
    }
}