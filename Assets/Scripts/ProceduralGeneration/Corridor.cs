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

        public string CorridorName => corridorName;

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
