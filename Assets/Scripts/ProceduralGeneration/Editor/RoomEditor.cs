using UnityEngine;
using UnityEditor;
using System.Linq;

namespace ProceduralGeneration.Editor
{
    [CustomEditor(typeof(Room))]
    public class RoomEditor : UnityEditor.Editor
    {
        // Room-specific properties
        private SerializedProperty roomNameProp;
        private SerializedProperty startingRoomProp;
        private SerializedProperty enemySpawnDelayProp;
        private SerializedProperty startingRoomDoorDelayProp;
        private SerializedProperty showDebugLogsProp;

        // LevelPiece parent properties
        private SerializedProperty pointAProp;
        private SerializedProperty pointBProp;
        private SerializedProperty entranceDoorProp;
        private SerializedProperty exitDoorProp;
        private SerializedProperty spawnPointsProp;
        private SerializedProperty playerHasEnteredProp;
        private SerializedProperty showEnemyDebugProp;

        private void OnEnable()
        {
            // Room-specific properties
            roomNameProp = serializedObject.FindProperty("roomName");
            startingRoomProp = serializedObject.FindProperty("startingRoom");
            enemySpawnDelayProp = serializedObject.FindProperty("enemySpawnDelay");
            startingRoomDoorDelayProp = serializedObject.FindProperty("startingRoomDoorDelay");
            showDebugLogsProp = serializedObject.FindProperty("showDebugLogs");

            // LevelPiece parent properties
            pointAProp = serializedObject.FindProperty("pointA");
            pointBProp = serializedObject.FindProperty("pointB");
            entranceDoorProp = serializedObject.FindProperty("entranceDoor");
            exitDoorProp = serializedObject.FindProperty("exitDoor");
            spawnPointsProp = serializedObject.FindProperty("spawnPoints");
            playerHasEnteredProp = serializedObject.FindProperty("playerHasEntered");
            showEnemyDebugProp = serializedObject.FindProperty("showEnemyDebug");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            Room room = (Room)target;

            // Header
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Room Configuration", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Room Settings
            EditorGUILayout.LabelField("Room Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(roomNameProp, new GUIContent("Room Name"));
            EditorGUILayout.PropertyField(startingRoomProp, new GUIContent("Starting Room"));

            if (startingRoomProp.boolValue)
            {
                EditorGUILayout.HelpBox("Starting Room: Only has exit door (Point B). Next area generation is triggered by ConnectionPoint when player enters Point B trigger area.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("Regular Room: Has entrance (Point A) and exit (Point B). Next area generation is triggered when all enemies are defeated.", MessageType.Info);
            }

            EditorGUILayout.Space();

            // Connection Points (from LevelPiece)
            EditorGUILayout.LabelField("Connection Points", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(pointAProp, new GUIContent("Point A (Entrance)"));
            EditorGUILayout.PropertyField(pointBProp, new GUIContent("Point B (Exit)"));

            // Show connection point validation inline
            if (room.IsStartingRoom)
            {
                if (pointAProp.objectReferenceValue != null)
                {
                    EditorGUILayout.HelpBox("Point A will be automatically disabled for starting rooms.", MessageType.Warning);
                }
                if (pointBProp.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox("Starting room needs Point B (exit) with ConnectionPoint trigger!", MessageType.Error);
                }
                else
                {
                    // Check if Point B has ConnectionPoint
                    ConnectionPoint pointB = pointBProp.objectReferenceValue as ConnectionPoint;
                    if (pointB != null)
                    {
                        EditorGUILayout.HelpBox($"✅ Point B ConnectionPoint trigger radius: {pointB.DetectionRadius}m", MessageType.Info);
                    }
                }
            }
            else
            {
                if (pointAProp.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox("Regular room needs Point A (entrance)!", MessageType.Error);
                }
                if (pointBProp.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox("Regular room needs Point B (exit)!", MessageType.Error);
                }
            }

            EditorGUILayout.Space();

            // Doors (from LevelPiece)
            EditorGUILayout.LabelField("Doors", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(entranceDoorProp, new GUIContent("Entrance Door", "Door at Point A (entrance)"));
            EditorGUILayout.PropertyField(exitDoorProp, new GUIContent("Exit Door", "Door at Point B (exit)"));

            EditorGUILayout.Space();

            // Spawn Points (from LevelPiece)
            EditorGUILayout.LabelField("Spawn Points", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(spawnPointsProp, new GUIContent("Spawn Points"), true);

            if (spawnPointsProp.arraySize == 0)
            {
                EditorGUILayout.HelpBox("No spawn points assigned. Enemies won't spawn in this room.", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox($"{spawnPointsProp.arraySize} spawn points configured.", MessageType.Info);
            }

            EditorGUILayout.Space();

            // Enemy Spawning (only for non-starting rooms)
            if (!startingRoomProp.boolValue)
            {
                EditorGUILayout.LabelField("Enemy Spawning", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(enemySpawnDelayProp, new GUIContent("Enemy Spawn Delay", "Delay before spawning enemies (allows NavMesh to update)"));
                EditorGUILayout.Space();
            }

            // Starting Room Settings (only for starting rooms)
            if (startingRoomProp.boolValue)
            {
                EditorGUILayout.LabelField("Starting Room Generation", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(startingRoomDoorDelayProp, new GUIContent("Door Open Delay", "Time to wait after generation before opening door"));

                EditorGUILayout.HelpBox("Starting room exit generation is triggered by ConnectionPoint (Point B) when player enters its trigger area. Adjust the Detection Radius on the ConnectionPoint component.", MessageType.Info);
                EditorGUILayout.Space();
            }

            // Player Tracking (from LevelPiece)
            EditorGUILayout.LabelField("Player Tracking", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(playerHasEnteredProp, new GUIContent("Player Has Entered"));

            EditorGUILayout.Space();

            // Debug Settings
            EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(showDebugLogsProp, new GUIContent("Show Debug Logs (Room)"));
            EditorGUILayout.PropertyField(showEnemyDebugProp, new GUIContent("Show Enemy Debug (LevelPiece)"));

            EditorGUILayout.Space();

            // Runtime Information
            if (Application.isPlaying)
            {
                EditorGUILayout.LabelField("Runtime Information", EditorStyles.boldLabel);

                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.LabelField("Room Name:", room.RoomName);
                EditorGUILayout.LabelField("Is Starting Room:", room.IsStartingRoom.ToString());
                EditorGUILayout.LabelField("Player Has Entered:", room.PlayerHasEntered.ToString());
                EditorGUILayout.LabelField("Enemies Spawned:", room.EnemiesSpawned.ToString());

                if (room.HasEnemies())
                {
                    EditorGUILayout.LabelField("Active Enemies:", room.GetActiveEnemyCount().ToString());
                }

                // Show spawn point status
                if (room.SpawnPoints != null && room.SpawnPoints.Length > 0)
                {
                    int spawnedCount = room.SpawnPoints.Count(sp => sp != null && sp.hasSpawned);
                    EditorGUILayout.LabelField("Spawn Points Status:", $"{spawnedCount}/{room.SpawnPoints.Length} spawned");
                }

                EditorGUI.EndDisabledGroup();

                EditorGUILayout.Space();

                // Runtime Controls
                EditorGUILayout.LabelField("Runtime Controls", EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Force Open Exit Door"))
                {
                    room.ForceOpenExitDoor();
                }
                if (GUILayout.Button("Force Register Enemies"))
                {
                    room.ForceRegisterEnemies();
                }
                EditorGUILayout.EndHorizontal();

                // Status messages
                if (room.IsStartingRoom)
                {
                    EditorGUILayout.HelpBox("Starting Room: Exit door opens automatically when player approaches Point B.", MessageType.Info);
                }
                else if (room.HasEnemies())
                {
                    EditorGUILayout.HelpBox($"{room.GetActiveEnemyCount()} enemies remaining. Exit door will open when all are defeated.", MessageType.Warning);
                }
                else
                {
                    EditorGUILayout.HelpBox("No enemies detected. Exit door should be open.", MessageType.Info);
                }
            }

            // Validation Summary
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Validation Summary", EditorStyles.boldLabel);

            bool hasErrors = false;
            bool hasWarnings = false;

            // Check connection points
            if (room.IsStartingRoom)
            {
                if (pointBProp.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox("❌ Starting room missing Point B (exit)", MessageType.Error);
                    hasErrors = true;
                }
                else
                {
                    // Check if Point B has proper ConnectionPoint setup
                    ConnectionPoint pointB = pointBProp.objectReferenceValue as ConnectionPoint;
                    if (pointB != null && pointB.Type != ConnectionPoint.PointType.B)
                    {
                        EditorGUILayout.HelpBox("❌ Point B ConnectionPoint should be type B (Exit)", MessageType.Error);
                        hasErrors = true;
                    }
                }

                if (pointAProp.objectReferenceValue != null)
                {
                    EditorGUILayout.HelpBox("⚠️ Starting room has Point A (will be disabled)", MessageType.Warning);
                    hasWarnings = true;
                }
            }
            else
            {
                if (pointAProp.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox("❌ Regular room missing Point A (entrance)", MessageType.Error);
                    hasErrors = true;
                }
                if (pointBProp.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox("❌ Regular room missing Point B (exit)", MessageType.Error);
                    hasErrors = true;
                }
            }

            // Check doors
            if (exitDoorProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("⚠️ No exit door assigned", MessageType.Warning);
                hasWarnings = true;
            }
            if (!room.IsStartingRoom && entranceDoorProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("⚠️ No entrance door assigned", MessageType.Warning);
                hasWarnings = true;
            }

            // Check spawn points for non-starting rooms
            if (!room.IsStartingRoom && spawnPointsProp.arraySize == 0)
            {
                EditorGUILayout.HelpBox("⚠️ No spawn points - no enemies will spawn", MessageType.Warning);
                hasWarnings = true;
            }

            // Summary message
            if (!hasErrors && !hasWarnings)
            {
                EditorGUILayout.HelpBox("✅ Room configuration looks good!", MessageType.Info);
            }
            else if (hasErrors)
            {
                EditorGUILayout.HelpBox("Fix the errors above before using this room.", MessageType.Error);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}