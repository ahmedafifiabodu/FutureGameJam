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

        public string CorridorName => corridorName;

        public override void OnSpawned(int roomIteration)
        {
            base.OnSpawned(roomIteration);

            // Delay enemy spawning to allow NavMesh to carve/update
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

                spawnManager.SpawnEnemiesAtPoints(spawnPoints, false, currentIteration);
                spawnManager.SpawnPropsAtPoints(spawnPoints);
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.magenta;

            if (pointA != null && pointB != null)
            {
                Gizmos.DrawLine(pointA.transform.position, pointB.transform.position);
            }
        }
    }
}