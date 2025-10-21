using UnityEngine;
using ProceduralGeneration;

namespace AI
{
    /// <summary>
    /// Tracks which room/corridor an enemy is in
    /// Used to restrict enemy aggro to current room/corridor only
    /// </summary>
    public class EnemyRoomTracker : MonoBehaviour
    {
        [Header("Current Location")]
        [SerializeField] private LevelPiece currentLevelPiece;

        [SerializeField] private bool isInRoom;
        [SerializeField] private bool isInCorridor;

        [Header("Detection Settings")]
        [Tooltip("Retry detection if failed on first attempt")]
        [SerializeField] private bool retryDetection = true;

        [Tooltip("Max retry attempts")]
        [SerializeField] private int maxRetries = 5;

        [Tooltip("Delay between retries (seconds)")]
        [SerializeField] private float retryDelay = 0.5f;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = false;

        private int retryCount = 0;

        private void Start()
        {
            // Try to detect level piece immediately
            DetectCurrentLevelPiece();

            // If failed and retry enabled, try again after delay
            if (currentLevelPiece == null && retryDetection)
            {
                InvokeRepeating(nameof(RetryDetection), retryDelay, retryDelay);
            }
        }

        private void RetryDetection()
        {
            if (currentLevelPiece != null)
            {
                // Successfully detected, stop retrying
                CancelInvoke(nameof(RetryDetection));
                return;
            }

            retryCount++;

            if (enableDebugLogs)
                Debug.Log($"[EnemyRoomTracker] Retry {retryCount}/{maxRetries} - Detecting level piece for {gameObject.name}");

            DetectCurrentLevelPiece();

            if (retryCount >= maxRetries)
            {
                CancelInvoke(nameof(RetryDetection));

                if (currentLevelPiece == null)
                {
                    Debug.LogWarning($"[EnemyRoomTracker] {gameObject.name} failed to detect room/corridor after {maxRetries} attempts. " +
                                     "Enemy will not have room-based aggro restrictions.");
                }
            }
        }

        private void DetectCurrentLevelPiece()
        {
            // Find all rooms and corridors in scene
            var rooms = FindObjectsOfType<Room>();
            var corridors = FindObjectsOfType<Corridor>();

            if (enableDebugLogs)
                Debug.Log($"[EnemyRoomTracker] Found {rooms.Length} rooms and {corridors.Length} corridors in scene");

            // Check which room contains this enemy
            foreach (var room in rooms)
            {
                if (IsInsideBounds(room.transform))
                {
                    currentLevelPiece = room;
                    isInRoom = true;
                    isInCorridor = false;

                    if (enableDebugLogs)
                        Debug.Log($"[EnemyRoomTracker] {gameObject.name} is in room: {room.RoomName}");

                    return;
                }
            }

            // Check which corridor contains this enemy
            foreach (var corridor in corridors)
            {
                if (IsInsideBounds(corridor.transform))
                {
                    currentLevelPiece = corridor;
                    isInRoom = false;
                    isInCorridor = true;

                    if (enableDebugLogs)
                        Debug.Log($"[EnemyRoomTracker] {gameObject.name} is in corridor: {corridor.CorridorName}");

                    return;
                }
            }

            // Not found - will retry if enabled
            if (enableDebugLogs && retryCount == 0)
                Debug.LogWarning($"[EnemyRoomTracker] {gameObject.name} is not in any room or corridor! (Will retry)");
        }

        /// <summary>
        /// Check if enemy is inside a level piece's bounds
        /// </summary>
        private bool IsInsideBounds(Transform levelPieceTransform)
        {
            // Get all colliders in the level piece
            var colliders = levelPieceTransform.GetComponentsInChildren<Collider>();

            foreach (var col in colliders)
            {
                // Skip trigger colliders (like doors)
                if (col.isTrigger) continue;

                // Use bounds.Contains for precise check
                if (col.bounds.Contains(transform.position))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Check if player is in the same room or corridor as this enemy
        /// </summary>
        public bool IsPlayerInSameRoomOrCorridor()
        {
            // Check if player has entered this level piece first
            if (currentLevelPiece.PlayerHasEntered)
                return true;
            else
            {
                if (enableDebugLogs && Time.frameCount % 120 == 0)
                    Debug.Log($"[EnemyRoomTracker] {gameObject.name} - Player NOT in same room ({currentLevelPiece.name})");
                return false;
            }
        }

        /// <summary>
        /// Manually set current level piece (called by spawn system if needed)
        /// </summary>
        public void SetCurrentLevelPiece(LevelPiece newLevelPiece)
        {
            currentLevelPiece = newLevelPiece;

            isInRoom = newLevelPiece is Room;
            isInCorridor = newLevelPiece is Corridor;

            // Stop retrying if manually set
            CancelInvoke(nameof(RetryDetection));

            if (enableDebugLogs)
                Debug.Log($"[EnemyRoomTracker] {gameObject.name} manually assigned to {newLevelPiece.name}");
        }

        public LevelPiece GetCurrentLevelPiece() => currentLevelPiece;

        public bool IsInRoom() => isInRoom;

        public bool IsInCorridor() => isInCorridor;
    }
}