using UnityEngine;

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

        public ConnectionPoint PointA => pointA;
        public ConnectionPoint PointB => pointB;

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
    }
}
