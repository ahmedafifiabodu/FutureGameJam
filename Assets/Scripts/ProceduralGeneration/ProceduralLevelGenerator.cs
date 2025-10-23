using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace ProceduralGeneration
{
    /// <summary>
    /// Main procedural level generator that manages room and corridor spawning
    /// </summary>
    public class ProceduralLevelGenerator : MonoBehaviour
    {
        [Header("Prefab Pools")]
        [SerializeField] private WeightedRoomPrefab[] roomPrefabs;

        [SerializeField] private WeightedCorridorPrefab[] corridorPrefabs;

        [Header("Starting Room")]
        [SerializeField] private GameObject startingRoomPrefab;

        [Header("Difficulty Scaling")]
        [SerializeField] private int currentRoomIteration = 0; // Track progression

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;

        private Transform player;
        private Room currentRoom;
        private Corridor currentCorridor;
        private Room previousRoom;
        private Corridor previousCorridor;

        // Track if we're generating to prevent multiple generations
        private bool isGenerating = false;

        private readonly HashSet<ConnectionPoint> processedPoints = new();

        // Reference to spawn manager
        private AI.Spawning.EnemySpawnManager spawnManager;

        private void Start()
        {
            // Find or create spawn manager
            spawnManager = FindFirstObjectByType<AI.Spawning.EnemySpawnManager>();

            ValidatePrefabs();
            SpawnStartingRoom();
        }

        private void ValidatePrefabs()
        {
            if (roomPrefabs == null || roomPrefabs.Length == 0)
            {
                Debug.LogError("[ProceduralLevelGenerator] No room prefabs assigned!");
                return;
            }

            if (corridorPrefabs == null || corridorPrefabs.Length == 0)
            {
                Debug.LogError("[ProceduralLevelGenerator] No corridor prefabs assigned!");
                return;
            }

            int validRooms = roomPrefabs.Count(r => r.IsValid());
            int validCorridors = corridorPrefabs.Count(c => c.IsValid());

            if (enableDebugLogs)
                Debug.Log($"[ProceduralLevelGenerator] Validated {validRooms}/{roomPrefabs.Length} rooms and {validCorridors}/{corridorPrefabs.Length} corridors");
        }

        private void SpawnStartingRoom()
        {
            if (startingRoomPrefab == null)
            {
                Debug.LogError("[ProceduralLevelGenerator] No starting room prefab assigned!");
                return;
            }

            GameObject roomObj = Instantiate(startingRoomPrefab, Vector3.zero, Quaternion.identity);
            currentRoom = roomObj.GetComponent<Room>();

            if (currentRoom == null)
            {
                Debug.LogError("[ProceduralLevelGenerator] Starting room prefab doesn't have Room component!");
                Destroy(roomObj);
                return;
            }

            if (enableDebugLogs)
                Debug.Log($"[ProceduralLevelGenerator] Spawned starting room: {currentRoom.RoomName}");

            // Find player if not assigned
            if (player == null)
            {
                player = ServiceLocator.Instance.GetService<ParasiteController>().transform;
            }
        }

        /// <summary>
        /// Public method to trigger next area generation from external sources (like Door class)
        /// </summary>
        public void TriggerNextAreaGeneration(ConnectionPoint exitPoint)
        {
            if (exitPoint == null)
            {
                Debug.LogWarning("[ProceduralLevelGenerator] TriggerNextAreaGeneration called with null exitPoint");
                return;
            }

            if (isGenerating)
            {
                Debug.Log("[ProceduralLevelGenerator] Already generating, ignoring trigger");
                return;
            }

            if (processedPoints.Contains(exitPoint))
            {
                Debug.Log("[ProceduralLevelGenerator] Exit point already processed, ignoring trigger");
                return;
            }

            Debug.Log($"[ProceduralLevelGenerator] Next area generation triggered externally for exit point");
            processedPoints.Add(exitPoint);
            GenerateNextSection(exitPoint);
        }

        /// <summary>
        /// Get the current room's exit point (Point B) for external access
        /// </summary>
        public ConnectionPoint GetCurrentRoomExitPoint() => currentRoom.PointB;

        /// <summary>
        /// Check if next area generation is currently in progress
        /// </summary>
        public bool IsGenerating => isGenerating;

        private void GenerateNextSection(ConnectionPoint roomExitPoint)
        {
            if (isGenerating) return;
            isGenerating = true;

            // Increment room iteration for difficulty scaling
            currentRoomIteration++;

            // Update spawn manager with current iteration
            if (spawnManager)
                spawnManager.SetRoomIteration(currentRoomIteration);

            // Step 1: Spawn corridor with its entrance (Point B) connected to room's exit (Point B)
            Corridor newCorridor = SpawnCorridor(roomExitPoint);

            if (newCorridor == null)
            {
                Debug.LogError("[ProceduralLevelGenerator] Failed to spawn corridor!");
                isGenerating = false;
                return;
            }

            // FIXED: Wait a frame to ensure corridor's points are initialized
            // Then spawn room aligned with corridor's exit (Point A)
            if (newCorridor.PointA == null)
            {
                Debug.LogError("[ProceduralLevelGenerator] Corridor Point A is null after spawning!");
                Destroy(newCorridor.gameObject);
                isGenerating = false;
                return;
            }

            // Step 2: Spawn room with its entrance (Point A) connected to corridor's exit (Point A)
            Room newRoom = SpawnRoom(newCorridor.PointA);

            if (newRoom == null)
            {
                Debug.LogError("[ProceduralLevelGenerator] Failed to spawn room!");
                Destroy(newCorridor.gameObject);
                isGenerating = false;
                return;
            }

            // Step 3: Update references
            previousRoom = currentRoom;
            previousCorridor = currentCorridor;
            currentCorridor = newCorridor;
            currentRoom = newRoom;

            if (enableDebugLogs)
                Debug.Log($"[ProceduralLevelGenerator] Generated: {newCorridor.CorridorName} -> {newRoom.RoomName} (Iteration {currentRoomIteration})");

            isGenerating = false;
        }

        private Corridor SpawnCorridor(ConnectionPoint roomExitPoint)
        {
            GameObject corridorPrefab = GetRandomCorridorPrefab();
            if (corridorPrefab == null) return null;

            // Instantiate corridor
            GameObject corridorObj = Instantiate(corridorPrefab);
            Corridor corridor = corridorObj.GetComponent<Corridor>();

            if (corridor == null || corridor.PointB == null)
            {
                Debug.LogError("[ProceduralLevelGenerator] Corridor prefab missing component or Point B!");
                Destroy(corridorObj);
                return null;
            }

            AlignConnectionPoints(corridorObj.transform, corridor.PointB, roomExitPoint, true);
            corridor.OnSpawned(currentRoomIteration);

            return corridor;
        }

        private Room SpawnRoom(ConnectionPoint corridorExitPoint)
        {
            GameObject roomPrefab = GetRandomRoomPrefab();
            if (roomPrefab == null) return null;

            // Instantiate room
            GameObject roomObj = Instantiate(roomPrefab);
            Room room = roomObj.GetComponent<Room>();

            if (room == null || room.PointA == null)
            {
                Debug.LogError("[ProceduralLevelGenerator] Room prefab missing component or Point A!");
                Destroy(roomObj);
                return null;
            }

            AlignConnectionPoints(roomObj.transform, room.PointA, corridorExitPoint, true);
            room.OnSpawned(currentRoomIteration);

            return room;
        }

        private void AlignConnectionPoints(Transform objectTransform, ConnectionPoint sourcePoint, ConnectionPoint targetPoint, bool faceOpposite = true)
        {
            if (sourcePoint == null || targetPoint == null)
            {
                Debug.LogError("[ProceduralLevelGenerator] Cannot align null connection points!");
                return;
            }

            // Step 1: Calculate rotation first (before moving)
            Quaternion targetRotation = targetPoint.transform.rotation;
            Quaternion sourceRotation = sourcePoint.transform.rotation;

            // Calculate the rotation needed to align source to target
            // If faceOpposite is true, add 180 degrees so they face each other
            Quaternion rotationDifference = targetRotation * Quaternion.Inverse(sourceRotation);
            if (faceOpposite)
            {
                rotationDifference *= Quaternion.Euler(0, 180, 0);
            }

            // Apply rotation to the object
            objectTransform.rotation = rotationDifference * objectTransform.rotation;

            // Step 2: Calculate position offset AFTER rotation
            // Now that the object is rotated, calculate where the source point is
            Vector3 sourceWorldPos = sourcePoint.transform.position;
            Vector3 targetWorldPos = targetPoint.transform.position;

            // Move the object so source point matches target point
            Vector3 offset = targetWorldPos - sourceWorldPos;
            objectTransform.position += offset;

            if (enableDebugLogs)
            {
                Debug.Log($"[ProceduralLevelGenerator] Aligned {objectTransform.name} - " +
                         $"Source: {sourcePoint.Type}, Target: {targetPoint.Type}, " +
                         $"Offset: {offset}, FaceOpposite: {faceOpposite}");
            }
        }

        public void OnPlayerEnteredNewRoom()
        {
            if (enableDebugLogs)
                Debug.Log("[ProceduralLevelGenerator] Player entered new room. Closing doors and cleaning up...");

            // Delete previous room and corridor
            if (previousRoom != null)
            {
                if (enableDebugLogs)
                    Debug.Log($"[ProceduralLevelGenerator] Destroying previous room: {previousRoom.RoomName}");

                Destroy(previousRoom.gameObject);
                previousRoom = null;
            }

            if (previousCorridor != null)
            {
                if (enableDebugLogs)
                    Debug.Log($"[ProceduralLevelGenerator] Destroying previous corridor: {previousCorridor.CorridorName}");

                Destroy(previousCorridor.gameObject);
                previousCorridor = null;
            }
        }

        private GameObject GetRandomRoomPrefab()
        {
            var validPrefabs = roomPrefabs.Where(r => r.IsValid()).ToArray();
            if (validPrefabs.Length == 0)
            {
                Debug.LogError("[ProceduralLevelGenerator] No valid room prefabs!");
                return null;
            }

            return GetWeightedRandomPrefab(validPrefabs.Select(r => (r.RoomPrefab, r.Weight)).ToArray());
        }

        private GameObject GetRandomCorridorPrefab()
        {
            var validPrefabs = corridorPrefabs.Where(c => c.IsValid()).ToArray();
            if (validPrefabs.Length == 0)
            {
                Debug.LogError("[ProceduralLevelGenerator] No valid corridor prefabs!");
                return null;
            }

            return GetWeightedRandomPrefab(validPrefabs.Select(c => (c.CorridorPrefab, c.Weight)).ToArray());
        }

        private GameObject GetWeightedRandomPrefab((GameObject prefab, int weight)[] weightedPrefabs)
        {
            int totalWeight = weightedPrefabs.Sum(p => p.weight);
            int randomValue = Random.Range(0, totalWeight);
            int currentWeight = 0;

            foreach (var (prefab, weight) in weightedPrefabs)
            {
                currentWeight += weight;
                if (randomValue < currentWeight)
                    return prefab;
            }

            return weightedPrefabs[0].prefab;
        }

        // Optional: Add a new debug method for current room/corridor state
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;

            // Draw current room boundaries
            if (currentRoom != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireCube(currentRoom.transform.position, Vector3.one * 5f);

                // Draw current room's exit point
                if (currentRoom.PointB != null)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireSphere(currentRoom.PointB.transform.position, 1f);
                }
            }

            // Draw current corridor
            if (currentCorridor != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(currentCorridor.transform.position, Vector3.one * 3f);
            }
        }

        /// <summary>
        /// Reset the level to initial state (called during game restart)
        /// </summary>
        public void ResetLevel()
        {
            if (enableDebugLogs)
                Debug.Log("[ProceduralLevelGenerator] Resetting level to starting room...");

            // Destroy ALL rooms and corridors in the scene (not just tracked ones)
            Room[] allRooms = FindObjectsByType<Room>(FindObjectsSortMode.None);
            foreach (var room in allRooms)
            {
                if (enableDebugLogs)
                    Debug.Log($"[ProceduralLevelGenerator] Destroying room: {room.RoomName}");
                Destroy(room.gameObject);
            }

            Corridor[] allCorridors = FindObjectsByType<Corridor>(FindObjectsSortMode.None);
            foreach (var corridor in allCorridors)
            {
                if (enableDebugLogs)
                    Debug.Log($"[ProceduralLevelGenerator] Destroying corridor: {corridor.CorridorName}");
                Destroy(corridor.gameObject);
            }

            // Clear all references
            currentRoom = null;
            currentCorridor = null;
            previousRoom = null;
            previousCorridor = null;

            // Clear processed connection points
            processedPoints.Clear();

            // Reset iteration counter
            currentRoomIteration = 0;

            // Update spawn manager
            if (spawnManager)
                spawnManager.SetRoomIteration(0);

            // Reset generation flag
            isGenerating = false;

            // Spawn fresh starting room
            SpawnStartingRoom();

            // Reposition player to starting room after a frame delay
            // (to ensure room is fully spawned and positioned)
            StartCoroutine(RepositionPlayerDelayed());

            if (enableDebugLogs)
                Debug.Log("[ProceduralLevelGenerator] Level reset complete - fresh starting room spawned");
        }

        /// <summary>
        /// Reposition player to starting room spawn point with a delay
        /// </summary>
        private System.Collections.IEnumerator RepositionPlayerDelayed()
        {
            // Wait for room to be fully spawned and positioned
            yield return null;
            yield return null;

            if (currentRoom != null)
            {
                if (player != null)
                {
                    // Position player at starting room center + slightly above ground
                    Vector3 spawnPosition = currentRoom.transform.position + Vector3.up * 1.5f;

                    // Disable CharacterController temporarily to prevent physics issues during teleport
                    CharacterController cc = player.GetComponent<CharacterController>();
                    if (cc != null && cc.enabled)
                    {
                        cc.enabled = false;
                        player.position = spawnPosition;
                        yield return null; // Wait a frame
                        cc.enabled = true;
                    }
                    else
                    {
                        player.position = spawnPosition;
                    }

                    if (enableDebugLogs)
                        Debug.Log($"[ProceduralLevelGenerator] Repositioned player to starting room at {spawnPosition}");
                }
                else
                {
                    if (enableDebugLogs)
                        Debug.LogWarning("[ProceduralLevelGenerator] Could not find player to reposition!");
                }
            }
        }
    }
}