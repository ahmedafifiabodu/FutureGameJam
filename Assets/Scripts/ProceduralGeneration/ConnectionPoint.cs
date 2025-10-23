using UnityEngine;

namespace ProceduralGeneration
{
    /// <summary>
    /// Represents a connection point for rooms and corridors.
    /// Point A = Entrance, Point B = Exit
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
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

        [Header("Player Detection")]
        [SerializeField] private LayerMask playerLayer = 1 << 6; // Default to layer 6 (Player)

        [SerializeField] private bool debugMode = true;

        [Header("Door Management")]
        [SerializeField] private float doorCloseDelay = 2f;

        [Header("Next Area Generation")]
        [SerializeField] private bool disableProximityGeneration = true; // NEW: Disable proximity-based generation

        private LevelPiece parentLevelPiece;
        private bool playerDetected = false;
        private bool playerInside = false;
        private SphereCollider triggerCollider;

        private ProceduralLevelGenerator levelGenerator;

        public PointType Type => pointType;
        public float DetectionRadius => detectionRadius;
        public bool PlayerInside => playerInside;

        private void Awake()
        {
            // Get parent LevelPiece
            parentLevelPiece = GetComponentInParent<LevelPiece>();
            if (parentLevelPiece == null)
            {
                Debug.LogWarning($"[ConnectionPoint] {gameObject.name} has no LevelPiece parent!");
            }

            // Setup sphere collider for trigger detection
            triggerCollider = GetComponent<SphereCollider>();
            if (triggerCollider == null)
            {
                triggerCollider = gameObject.AddComponent<SphereCollider>();
            }

            triggerCollider.isTrigger = true;
            triggerCollider.radius = detectionRadius;

            if (debugMode)
            {
                Debug.Log($"[ConnectionPoint] {gameObject.name} initialized. PointType: {pointType}, DetectionRadius: {detectionRadius}, PlayerLayer: {playerLayer.value}");
            }
        }

        private void Start()
        {
            levelGenerator = FindFirstObjectByType<ProceduralLevelGenerator>();
        }

        private void OnTriggerEnter(Collider other)
        {
            // Check if the collider is on the player layer
            if (((1 << other.gameObject.layer) & playerLayer) != 0)
            {
                if (debugMode)
                {
                    Debug.Log($"[ConnectionPoint] Player entered trigger at {gameObject.name}. Player: {other.gameObject.name} on layer {other.gameObject.layer}");
                }

                playerInside = true;

                // Handle Point A (entrance) logic
                if (pointType == PointType.A && !playerDetected)
                {
                    OnPlayerDetected();
                    playerDetected = true;
                }
                // Handle Point B (exit) logic for starting rooms
                else if (pointType == PointType.B && !playerDetected)
                {
                    OnExitPointTriggered();
                    playerDetected = true;
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            // Check if the collider is on the player layer
            if (((1 << other.gameObject.layer) & playerLayer) != 0)
            {
                if (debugMode)
                {
                    Debug.Log($"[ConnectionPoint] Player exited trigger at {gameObject.name}. Player: {other.gameObject.name}");
                }

                playerInside = false;

                // If this is an entrance point and player has passed through, close the door after a delay
                if (pointType == PointType.A && playerDetected && parentLevelPiece != null)
                {
                    // Cancel any existing door close invoke
                    CancelInvoke(nameof(DelayedCloseDoor));
                    Invoke(nameof(DelayedCloseDoor), doorCloseDelay);
                }
            }
        }

        private void DelayedCloseDoor()
        {
            if (parentLevelPiece != null && !playerInside)
            {
                parentLevelPiece.CloseEntranceDoor();
                if (debugMode)
                {
                    Debug.Log($"[ConnectionPoint] Delayed door close triggered for {gameObject.name}");
                }

                // CRITICAL: Clean up previous room/corridor AFTER the door closes
                // This ensures the previous room stays visible while the door is open
                if (levelGenerator != null && pointType == PointType.A)
                {
                    if (debugMode)
                    {
                        Debug.Log($"[ConnectionPoint] Triggering previous room cleanup after door close");
                    }
                    levelGenerator.OnPlayerEnteredNewRoom();
                }
            }
        }

        private void OnPlayerDetected()
        {
            Debug.Log($"[ConnectionPoint] Player detected at {pointType} of {gameObject.name}");

            if (parentLevelPiece != null)
            {
                // Notify the level piece that player has entered
                parentLevelPiece.OnPlayerEntered();
            }

            // REMOVED: Don't cleanup previous room immediately when entering
            // The cleanup now happens when the entrance door closes (in DelayedCloseDoor)
        }

        /// <summary>
        /// Handle Point B (exit) trigger - specifically for starting rooms
        /// </summary>
        private void OnExitPointTriggered()
        {
            if (debugMode)
                Debug.Log($"[ConnectionPoint] Exit point triggered at {gameObject.name}");

            // Check if this is a starting room
            Room parentRoom = parentLevelPiece as Room;
            if (parentRoom != null && parentRoom.IsStartingRoom)
            {
                if (debugMode)
                    Debug.Log($"[ConnectionPoint] Triggering starting room exit for {parentRoom.RoomName}");

                // Notify the starting room that its exit was triggered
                parentRoom.OnStartingRoomExitTriggered();
            }
            else
            {
                if (debugMode)
                    Debug.Log($"[ConnectionPoint] Exit point triggered but not a starting room - ignoring");

                // For regular rooms, Point B triggers are handled by Door system when enemies are defeated
                // No action needed here for regular rooms
            }
        }

        /// <summary>
        /// Reset detection state (useful when reusing level pieces)
        /// </summary>
        public void ResetDetection()
        {
            playerDetected = false;
            playerInside = false;
            CancelInvoke(nameof(DelayedCloseDoor));
        }

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
            string label = $"Point {pointType}\n{gameObject.name}";

            if (pointType == PointType.B)
            {
                // Check if this is a starting room exit
                Room parentRoom = GetComponentInParent<Room>();
                if (parentRoom != null && parentRoom.IsStartingRoom)
                {
                    label += "\n(Starting Room Exit)";
                }
                else if (disableProximityGeneration)
                {
                    label += "\n(Door-triggered)";
                }
            }

            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, label);
#endif
        }

        private void OnDrawGizmosSelected()
        {
            if (!showGizmos) return;

            Gizmos.color = pointType == PointType.A ? new Color(0, 1, 0, 0.3f) : new Color(1, 0, 0, 0.3f);
            Gizmos.DrawSphere(transform.position, detectionRadius);
        }

        private void OnValidate()
        {
            // Update sphere collider radius when detection radius changes
            if (TryGetComponent<SphereCollider>(out var sphereCol))
            {
                sphereCol.radius = detectionRadius;
                sphereCol.isTrigger = true;
            }

            // Log layer mask value for debugging
            if (debugMode && Application.isPlaying)
            {
                Debug.Log($"[ConnectionPoint] {gameObject.name} - Player Layer Mask Value: {playerLayer.value}");
            }
        }
    }
}