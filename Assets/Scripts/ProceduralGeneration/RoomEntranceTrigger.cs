using UnityEngine;

namespace ProceduralGeneration
{
    /// <summary>
    /// Detects when player enters a new room and triggers cleanup
    /// Attach this to each room's entrance collider
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class RoomEntranceTrigger : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private Room parentRoom;
        [SerializeField] private bool debugLogs = true;

        private ProceduralLevelGenerator levelGenerator;
        private bool hasTriggered = false;

        private void Awake()
        {
            // Auto-find parent room if not assigned
            if (parentRoom == null)
                parentRoom = GetComponentInParent<Room>();

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
            levelGenerator = FindObjectOfType<ProceduralLevelGenerator>();
            
            if (levelGenerator == null)
            {
                Debug.LogError("[RoomEntranceTrigger] ProceduralLevelGenerator not found in scene!");
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (hasTriggered) return;

            // Check if it's the player
            if (other.CompareTag("Player") || other.GetComponent<FirstPersonZoneController>() != null)
            {
                hasTriggered = true;
                
                if (debugLogs)
                    Debug.Log($"[RoomEntranceTrigger] Player entered room: {parentRoom?.RoomName ?? "Unknown"}");

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
