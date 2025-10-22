using UnityEngine;

namespace ProceduralGeneration
{
    /// <summary>
    /// Detects when player enters a new room/corridor and triggers cleanup
    /// Attach this to each room/corridor's entrance collider
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class RoomEntranceTrigger : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private LevelPiece parentLevelPiece;

        [SerializeField] private bool debugLogs = true;

        private ProceduralLevelGenerator levelGenerator;
        private bool hasTriggered = false;

        private void Awake()
        {
            // Auto-find parent level piece (Room or Corridor) if not assigned
            if (parentLevelPiece == null)
                parentLevelPiece = GetComponentInParent<LevelPiece>();

            // Ensure this is a trigger
            Collider col = GetComponent<Collider>();
            if (!col.isTrigger)
            {
                col.isTrigger = true;
                if (debugLogs)
                    Debug.Log($"[RoomEntranceTrigger] Set {gameObject.name} as trigger");
            }
        }

        private void Start()
        {
            levelGenerator = FindFirstObjectByType<ProceduralLevelGenerator>();

            if (levelGenerator == null)
            {
                Debug.LogError("[RoomEntranceTrigger] ProceduralLevelGenerator not found in scene!");
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (hasTriggered) return;

            // Check if it's the player
            if (other.GetComponent<FirstPersonZoneController>() != null)
            {
                hasTriggered = true;

                string levelPieceName = "Unknown";
                if (parentLevelPiece is Room room)
                    levelPieceName = room.RoomName;
                else if (parentLevelPiece is Corridor corridor)
                    levelPieceName = corridor.CorridorName;

                if (debugLogs)
                    Debug.Log($"[RoomEntranceTrigger] Player entered: {levelPieceName}");

                // Notify parent level piece that player has entered
                if (parentLevelPiece != null)
                {
                    parentLevelPiece.OnPlayerEntered();
                }

                if (levelGenerator != null)
                    levelGenerator.OnPlayerEnteredNewRoom();
            }
        }

        private void OnDrawGizmos()
        {
            Collider col = GetComponent<Collider>();
            if (col != null && col.isTrigger)
            {
                Gizmos.color = new Color(0, 1, 0, 0.3f);
                Gizmos.matrix = transform.localToWorldMatrix;

                if (col is BoxCollider box)
                    Gizmos.DrawCube(box.center, box.size);
                else if (col is SphereCollider sphere)
                    Gizmos.DrawSphere(sphere.center, sphere.radius);
            }
        }
    }
}