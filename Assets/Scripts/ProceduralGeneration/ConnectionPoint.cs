using UnityEngine;

namespace ProceduralGeneration
{
    /// <summary>
    /// Represents a connection point for rooms and corridors.
    /// Point A = Entrance, Point B = Exit
    /// </summary>
    public class ConnectionPoint : MonoBehaviour
    {
        public enum PointType
        {
            A, // Entrance
            B  // Exit
        }

        [Header("Connection Settings")]
        [SerializeField] private PointType pointType = PointType.A;
        [SerializeField] private float detectionRadius = 2f;
        [SerializeField] private bool showGizmos = true;

        public PointType Type => pointType;
        public float DetectionRadius => detectionRadius;

        private void OnDrawGizmos()
        {
            if (!showGizmos) return;

            Gizmos.color = pointType == PointType.A ? Color.green : Color.red;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
            
            // Draw direction arrow
            Gizmos.color = pointType == PointType.A ? Color.cyan : Color.yellow;
            Vector3 direction = transform.forward;
            Gizmos.DrawRay(transform.position, direction * 1.5f);
            
            // Draw label
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, 
                $"Point {pointType}\n{gameObject.name}");
            #endif
        }

        private void OnDrawGizmosSelected()
        {
            if (!showGizmos) return;

            Gizmos.color = pointType == PointType.A ? new Color(0, 1, 0, 0.3f) : new Color(1, 0, 0, 0.3f);
            Gizmos.DrawSphere(transform.position, detectionRadius);
        }
    }
}
