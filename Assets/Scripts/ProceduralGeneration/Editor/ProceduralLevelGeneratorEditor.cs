using UnityEngine;
using UnityEditor;

namespace ProceduralGeneration.Editor
{
    [CustomEditor(typeof(ProceduralLevelGenerator))]
    public class ProceduralLevelGeneratorEditor : UnityEditor.Editor
    {
        private SerializedProperty roomPrefabsProp;
        private SerializedProperty corridorPrefabsProp;
        private SerializedProperty startingRoomProp;
        private SerializedProperty playerProp;
        private SerializedProperty proximityProp;
        private SerializedProperty debugLogsProp;

        private void OnEnable()
        {
            roomPrefabsProp = serializedObject.FindProperty("roomPrefabs");
            corridorPrefabsProp = serializedObject.FindProperty("corridorPrefabs");
            startingRoomProp = serializedObject.FindProperty("startingRoomPrefab");
            playerProp = serializedObject.FindProperty("player");
            proximityProp = serializedObject.FindProperty("proximityCheckDistance");
            debugLogsProp = serializedObject.FindProperty("enableDebugLogs");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            ProceduralLevelGenerator generator = (ProceduralLevelGenerator)target;

            // Header
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Procedural Level Generator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Configure room and corridor prefabs with weighted randomness. " +
                "Higher weights = more likely to spawn.", MessageType.Info);
            EditorGUILayout.Space();

            // Starting Room
            EditorGUILayout.LabelField("Starting Room", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(startingRoomProp, new GUIContent("Starting Room Prefab"));
            
            if (startingRoomProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Assign a starting room prefab!", MessageType.Warning);
            }
            
            EditorGUILayout.Space();

            // Room Prefabs
            EditorGUILayout.LabelField("Room Prefabs (Weighted)", EditorStyles.boldLabel);
            DrawWeightedPrefabArray(roomPrefabsProp, "Room");
            
            if (GUILayout.Button("+ Add Room Prefab"))
            {
                roomPrefabsProp.arraySize++;
                serializedObject.ApplyModifiedProperties();
            }

            EditorGUILayout.Space();

            // Corridor Prefabs
            EditorGUILayout.LabelField("Corridor Prefabs (Weighted)", EditorStyles.boldLabel);
            DrawWeightedPrefabArray(corridorPrefabsProp, "Corridor");
            
            if (GUILayout.Button("+ Add Corridor Prefab"))
            {
                corridorPrefabsProp.arraySize++;
                serializedObject.ApplyModifiedProperties();
            }

            EditorGUILayout.Space();

            // Player Settings
            EditorGUILayout.LabelField("Player Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(playerProp, new GUIContent("Player Transform"));
            EditorGUILayout.PropertyField(proximityProp, new GUIContent("Proximity Distance"));
            
            if (playerProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Player will be auto-detected by 'Player' tag if not assigned.", MessageType.Info);
            }

            EditorGUILayout.Space();

            // Debug
            EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(debugLogsProp, new GUIContent("Enable Debug Logs"));

            EditorGUILayout.Space();

            // Statistics
            if (Application.isPlaying)
            {
                EditorGUILayout.LabelField("Runtime Statistics", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Check console for generation logs when 'Enable Debug Logs' is on.", MessageType.Info);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawWeightedPrefabArray(SerializedProperty arrayProp, string prefabType)
        {
            for (int i = 0; i < arrayProp.arraySize; i++)
            {
                SerializedProperty element = arrayProp.GetArrayElementAtIndex(i);
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

                EditorGUILayout.PropertyField(prefabProp, new GUIContent("Prefab"));
                EditorGUILayout.PropertyField(weightProp, new GUIContent("Weight (1-100)"));
                EditorGUILayout.PropertyField(descProp, new GUIContent("Description"));

                // Validation
                if (prefabProp.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox("Prefab not assigned!", MessageType.Error);
                }
                else
                {
                    GameObject prefab = prefabProp.objectReferenceValue as GameObject;
                    bool hasComponent = prefabType == "Room" 
                        ? prefab.GetComponent<Room>() != null 
                        : prefab.GetComponent<Corridor>() != null;

                    if (!hasComponent)
                    {
                        EditorGUILayout.HelpBox($"Prefab is missing {prefabType} component!", MessageType.Error);
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
