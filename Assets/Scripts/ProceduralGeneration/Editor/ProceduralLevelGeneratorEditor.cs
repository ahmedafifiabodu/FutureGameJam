using UnityEngine;
using UnityEditor;
using System.Linq; // Added for Any() extension method

namespace ProceduralGeneration.Editor
{
    [CustomEditor(typeof(ProceduralLevelGenerator))]
    public class ProceduralLevelGeneratorEditor : UnityEditor.Editor
    {
        private SerializedProperty roomPrefabsProp;
        private SerializedProperty corridorPrefabsProp;
        private SerializedProperty startingRoomProp;
        private SerializedProperty debugLogsProp;
        private SerializedProperty currentRoomIterationProp;

        private void OnEnable()
        {
            roomPrefabsProp = serializedObject.FindProperty("roomPrefabs");
            corridorPrefabsProp = serializedObject.FindProperty("corridorPrefabs");
            startingRoomProp = serializedObject.FindProperty("startingRoomPrefab");
            debugLogsProp = serializedObject.FindProperty("enableDebugLogs");
            currentRoomIterationProp = serializedObject.FindProperty("currentRoomIteration");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            ProceduralLevelGenerator generator = (ProceduralLevelGenerator)target;

            // Header
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Procedural Level Generator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Configure room and corridor prefabs with weighted randomness. " +
                "Higher weights = more likely to spawn. Generation is now triggered by Door opening instead of proximity.", MessageType.Info);
            EditorGUILayout.Space();

            // Starting Room
            EditorGUILayout.LabelField("Starting Room", EditorStyles.boldLabel);
            if (startingRoomProp != null)
            {
                EditorGUILayout.PropertyField(startingRoomProp, new GUIContent("Starting Room Prefab"));

                if (startingRoomProp.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox("Assign a starting room prefab with Room component!", MessageType.Warning);
                }
                else
                {
                    // Validate starting room has Room component
                    GameObject startingRoom = startingRoomProp.objectReferenceValue as GameObject;
                    if (startingRoom != null && startingRoom.GetComponent<Room>() == null)
                    {
                        EditorGUILayout.HelpBox("Starting room prefab is missing Room component!", MessageType.Error);
                    }
                }
            }

            EditorGUILayout.Space();

            // Room Prefabs
            EditorGUILayout.LabelField("Room Prefabs (Weighted)", EditorStyles.boldLabel);
            if (roomPrefabsProp != null)
            {
                DrawWeightedPrefabArray(roomPrefabsProp, "Room");

                if (GUILayout.Button("+ Add Room Prefab"))
                {
                    roomPrefabsProp.arraySize++;
                    serializedObject.ApplyModifiedProperties();
                }
            }

            EditorGUILayout.Space();

            // Corridor Prefabs
            EditorGUILayout.LabelField("Corridor Prefabs (Weighted)", EditorStyles.boldLabel);
            if (corridorPrefabsProp != null)
            {
                DrawWeightedPrefabArray(corridorPrefabsProp, "Corridor");

                if (GUILayout.Button("+ Add Corridor Prefab"))
                {
                    corridorPrefabsProp.arraySize++;
                    serializedObject.ApplyModifiedProperties();
                }
            }

            EditorGUILayout.Space();

            // Player Settings - Removed since it's now auto-detected
            EditorGUILayout.LabelField("Player Settings", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Player is automatically detected from ParasiteController via ServiceLocator at runtime.", MessageType.Info);

            EditorGUILayout.Space();

            // Difficulty Scaling
            EditorGUILayout.LabelField("Difficulty Scaling", EditorStyles.boldLabel);
            if (currentRoomIterationProp != null)
            {
                EditorGUILayout.PropertyField(currentRoomIterationProp, new GUIContent("Current Room Iteration"));
                EditorGUILayout.HelpBox("This tracks the current difficulty level. Higher iterations spawn more enemies.", MessageType.Info);
            }

            EditorGUILayout.Space();

            // Debug
            EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
            if (debugLogsProp != null)
            {
                EditorGUILayout.PropertyField(debugLogsProp, new GUIContent("Enable Debug Logs"));
            }

            EditorGUILayout.Space();

            // Generation Info
            EditorGUILayout.LabelField("Generation System", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("• Next areas are now generated when exit doors open (not by proximity)\n" +
                "• This prevents skybox visibility when doors open\n" +
                "• Door system handles timing and generation triggers", MessageType.Info);

            // Statistics
            if (Application.isPlaying)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Runtime Statistics", EditorStyles.boldLabel);

                // Show generation status
                bool isGenerating = generator.IsGenerating;
                EditorGUILayout.LabelField("Generation Status:", isGenerating ? "Generating..." : "Idle");

                // Show current room info
                var currentExit = generator.GetCurrentRoomExitPoint();
                EditorGUILayout.LabelField("Current Room Exit:", currentExit != null ? "Available" : "None");

                EditorGUILayout.HelpBox("Check console for generation logs when 'Enable Debug Logs' is on.", MessageType.Info);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawWeightedPrefabArray(SerializedProperty arrayProp, string prefabType)
        {
            if (arrayProp == null)
            {
                EditorGUILayout.HelpBox($"{prefabType} array property not found!", MessageType.Error);
                return;
            }

            for (int i = 0; i < arrayProp.arraySize; i++)
            {
                SerializedProperty element = arrayProp.GetArrayElementAtIndex(i);
                if (element == null) continue;

                SerializedProperty prefabProp = element.FindPropertyRelative(prefabType.ToLower() + "Prefab");
                SerializedProperty weightProp = element.FindPropertyRelative("weight");
                SerializedProperty descProp = element.FindPropertyRelative("description");

                EditorGUILayout.BeginVertical("box");

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"{prefabType} #{i + 1}", EditorStyles.boldLabel, GUILayout.Width(80));

                if (GUILayout.Button("Remove", GUILayout.Width(70)))
                {
                    arrayProp.DeleteArrayElementAtIndex(i);
                    serializedObject.ApplyModifiedProperties();
                    return;
                }
                EditorGUILayout.EndHorizontal();

                if (prefabProp != null)
                    EditorGUILayout.PropertyField(prefabProp, new GUIContent("Prefab"));
                if (weightProp != null)
                    EditorGUILayout.PropertyField(weightProp, new GUIContent("Weight (1-100)"));
                if (descProp != null)
                    EditorGUILayout.PropertyField(descProp, new GUIContent("Description"));

                // Validation
                if (prefabProp != null && prefabProp.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox("Prefab not assigned!", MessageType.Error);
                }
                else if (prefabProp != null && prefabProp.objectReferenceValue != null)
                {
                    GameObject prefab = prefabProp.objectReferenceValue as GameObject;
                    if (prefab != null)
                    {
                        bool hasComponent = prefabType == "Room"
                            ? prefab.GetComponent<Room>() != null
                            : prefab.GetComponent<Corridor>() != null;

                        if (!hasComponent)
                        {
                            EditorGUILayout.HelpBox($"Prefab is missing {prefabType} component!", MessageType.Error);
                        }
                        else
                        {
                            // Additional validation for connection points
                            if (prefab.TryGetComponent<LevelPiece>(out var levelPiece))
                            {
                                // Check for connection points
                                var connectionPoints = prefab.GetComponentsInChildren<ConnectionPoint>();
                                bool hasPointA = connectionPoints.Any(cp => cp.Type == ConnectionPoint.PointType.A);
                                bool hasPointB = connectionPoints.Any(cp => cp.Type == ConnectionPoint.PointType.B);

                                if (prefabType == "Room" && (!hasPointA || !hasPointB))
                                {
                                    EditorGUILayout.HelpBox("Room should have both Point A (entrance) and Point B (exit)!", MessageType.Warning);
                                }
                                else if (prefabType == "Corridor" && (!hasPointA || !hasPointB))
                                {
                                    EditorGUILayout.HelpBox("Corridor should have both Point A and Point B!", MessageType.Warning);
                                }
                            }
                        }
                    }
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(5);
            }

            if (arrayProp.arraySize == 0)
            {
                EditorGUILayout.HelpBox($"No {prefabType.ToLower()} prefabs assigned!", MessageType.Warning);
            }
        }
    }
}