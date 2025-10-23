using UnityEngine;
using UnityEditor;
using System.Linq;

namespace ProceduralGeneration.Editor
{
    [CustomEditor(typeof(Corridor))]
    public class CorridorEditor : UnityEditor.Editor
    {
        // Corridor-specific properties
        private SerializedProperty corridorNameProp;
        private SerializedProperty enemySpawnDelayProp;
        private SerializedProperty showDebugLogsProp;

        // LevelPiece parent properties
        private SerializedProperty pointAProp;
        private SerializedProperty pointBProp;
        private SerializedProperty entranceDoorProp;
        private SerializedProperty exitDoorProp;
      private SerializedProperty spawnPointsProp;
        private SerializedProperty playerHasEnteredProp;
        private SerializedProperty showEnemyDebugProp;

        // Spawn Configuration properties (from LevelPiece)
    private SerializedProperty baseEnemiesPerPieceProp;
        private SerializedProperty enemiesPerIterationProp;
        private SerializedProperty maxEnemiesPerPieceProp;
        private SerializedProperty spawnChanceProp;
        private SerializedProperty useCustomSpawnSettingsProp;
        private SerializedProperty minIterationProp;
        private SerializedProperty maxIterationProp;

        private void OnEnable()
     {
      // Corridor-specific properties
            corridorNameProp = serializedObject.FindProperty("corridorName");
        enemySpawnDelayProp = serializedObject.FindProperty("enemySpawnDelay");
showDebugLogsProp = serializedObject.FindProperty("showDebugLogs");

    // LevelPiece parent properties
 pointAProp = serializedObject.FindProperty("pointA");
    pointBProp = serializedObject.FindProperty("pointB");
        entranceDoorProp = serializedObject.FindProperty("entranceDoor");
      exitDoorProp = serializedObject.FindProperty("exitDoor");
   spawnPointsProp = serializedObject.FindProperty("spawnPoints");
    playerHasEnteredProp = serializedObject.FindProperty("playerHasEntered");
            showEnemyDebugProp = serializedObject.FindProperty("showEnemyDebug");

       // Spawn Configuration properties (from LevelPiece)
       baseEnemiesPerPieceProp = serializedObject.FindProperty("baseEnemiesPerPiece");
 enemiesPerIterationProp = serializedObject.FindProperty("enemiesPerIteration");
            maxEnemiesPerPieceProp = serializedObject.FindProperty("maxEnemiesPerPiece");
        spawnChanceProp = serializedObject.FindProperty("spawnChance");
  useCustomSpawnSettingsProp = serializedObject.FindProperty("useCustomSpawnSettings");
   minIterationProp = serializedObject.FindProperty("minIteration");
   maxIterationProp = serializedObject.FindProperty("maxIteration");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

      Corridor corridor = (Corridor)target;

  // Header
     EditorGUILayout.Space();
EditorGUILayout.LabelField("Corridor Configuration", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Corridors connect rooms. They typically have fewer enemies than rooms.", MessageType.Info);
            EditorGUILayout.Space();

   // Corridor Settings
         EditorGUILayout.LabelField("Corridor Settings", EditorStyles.boldLabel);
          if (corridorNameProp != null)
                EditorGUILayout.PropertyField(corridorNameProp, new GUIContent("Corridor Name"));

       EditorGUILayout.Space();

         // Connection Points (from LevelPiece)
            EditorGUILayout.LabelField("Connection Points", EditorStyles.boldLabel);
            if (pointAProp != null)
       EditorGUILayout.PropertyField(pointAProp, new GUIContent("Point A (Entrance)"));
       if (pointBProp != null)
           EditorGUILayout.PropertyField(pointBProp, new GUIContent("Point B (Exit)"));

    // Validation
            if (pointAProp != null && pointAProp.objectReferenceValue == null)
        {
          EditorGUILayout.HelpBox("Corridor needs Point A (entrance)!", MessageType.Error);
          }
     if (pointBProp != null && pointBProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Corridor needs Point B (exit)!", MessageType.Error);
}

       EditorGUILayout.Space();

      // Doors (from LevelPiece)
      EditorGUILayout.LabelField("Doors", EditorStyles.boldLabel);
            if (entranceDoorProp != null)
                EditorGUILayout.PropertyField(entranceDoorProp, new GUIContent("Entrance Door", "Door at Point A (entrance)"));
         if (exitDoorProp != null)
             EditorGUILayout.PropertyField(exitDoorProp, new GUIContent("Exit Door", "Door at Point B (exit)"));

     EditorGUILayout.Space();

            // Spawn Points (from LevelPiece)
  EditorGUILayout.LabelField("Spawn Points", EditorStyles.boldLabel);
        if (spawnPointsProp != null)
     {
                EditorGUILayout.PropertyField(spawnPointsProp, new GUIContent("Spawn Points"), true);

       if (spawnPointsProp.arraySize == 0)
   {
         EditorGUILayout.HelpBox("No spawn points assigned. No enemies will spawn in this corridor.", MessageType.Info);
   }
     else
       {
 EditorGUILayout.HelpBox($"{spawnPointsProp.arraySize} spawn points configured.", MessageType.Info);
              }
    }

    EditorGUILayout.Space();

      // Spawn Configuration (from LevelPiece)
       EditorGUILayout.LabelField("Spawn Configuration", EditorStyles.boldLabel);

      if (useCustomSpawnSettingsProp != null)
 {
   EditorGUILayout.PropertyField(useCustomSpawnSettingsProp, new GUIContent("Use Custom Spawn Settings",
         "If enabled, this corridor uses its own spawn settings instead of EnemySpawnManager defaults"));

      if (useCustomSpawnSettingsProp.boolValue)
            {
                    EditorGUI.indentLevel++;

  EditorGUILayout.HelpBox("Custom spawn settings enabled. Configure this corridor's specific enemy spawn behavior below.", MessageType.Info);

      // Iteration Range
        EditorGUILayout.BeginHorizontal();
       if (minIterationProp != null && maxIterationProp != null)
    {
    EditorGUILayout.PropertyField(minIterationProp, new GUIContent("Min Iteration",
       "Minimum room iteration where this piece can spawn"));
         EditorGUILayout.PropertyField(maxIterationProp, new GUIContent("Max Iteration",
     "Maximum room iteration where this piece can spawn (0 = unlimited)"));
        }
     EditorGUILayout.EndHorizontal();

    if (minIterationProp != null && minIterationProp.intValue < 0)
              {
         EditorGUILayout.HelpBox("Min Iteration cannot be negative!", MessageType.Warning);
         }

     if (maxIterationProp != null && minIterationProp != null && 
         maxIterationProp.intValue > 0 && maxIterationProp.intValue < minIterationProp.intValue)
      {
   EditorGUILayout.HelpBox("Max Iteration must be greater than Min Iteration!", MessageType.Error);
           }

      EditorGUILayout.Space(5);

         // Enemy spawn settings
        if (baseEnemiesPerPieceProp != null)
      EditorGUILayout.PropertyField(baseEnemiesPerPieceProp, new GUIContent("Base Enemies",
   "Starting number of enemies for this corridor"));

if (enemiesPerIterationProp != null)
    EditorGUILayout.PropertyField(enemiesPerIterationProp, new GUIContent("Enemies Per Iteration",
            "Additional enemies added per room iteration (difficulty scaling)"));

         if (maxEnemiesPerPieceProp != null)
        EditorGUILayout.PropertyField(maxEnemiesPerPieceProp, new GUIContent("Max Enemies",
           "Maximum number of enemies this corridor can spawn"));

       if (spawnChanceProp != null)
            EditorGUILayout.PropertyField(spawnChanceProp, new GUIContent("Spawn Chance",
    "Probability (0-1) that each spawn point will spawn an enemy"));

          // Show preview of spawn count at different iterations
                 if (baseEnemiesPerPieceProp != null && enemiesPerIterationProp != null && 
   maxEnemiesPerPieceProp != null && minIterationProp != null)
  {
          EditorGUILayout.Space(5);
    EditorGUILayout.LabelField("Preview Enemy Counts:", EditorStyles.miniBoldLabel);

 int baseEnemies = baseEnemiesPerPieceProp.intValue;
            float perIteration = enemiesPerIterationProp.floatValue;
      int maxEnemies = maxEnemiesPerPieceProp.intValue;
      int minIter = minIterationProp.intValue;
      int maxIter = maxIterationProp.intValue > 0 ? maxIterationProp.intValue : 5;
    int displayMax = Mathf.Min(maxIter, 5);

              EditorGUI.indentLevel++;
  for (int iter = minIter; iter <= displayMax; iter++)
         {
     int enemyCount = Mathf.Min(baseEnemies + Mathf.FloorToInt(iter * perIteration), maxEnemies);
     EditorGUILayout.LabelField($"Iteration {iter}:", $"{enemyCount} enemies");
   }
            if (maxIter > 5)
           {
         EditorGUILayout.LabelField("...", "");
             }
     EditorGUI.indentLevel--;
      }

          EditorGUI.indentLevel--;
  }
       else
        {
    EditorGUILayout.HelpBox("Using global EnemySpawnManager settings for this corridor (typically fewer enemies than rooms).", MessageType.Info);
      }
     }

            EditorGUILayout.Space();

   // Enemy Spawning
      EditorGUILayout.LabelField("Enemy Spawning", EditorStyles.boldLabel);
    if (enemySpawnDelayProp != null)
      EditorGUILayout.PropertyField(enemySpawnDelayProp, new GUIContent("Enemy Spawn Delay",
    "Delay before spawning enemies (allows NavMesh to update)"));

      EditorGUILayout.Space();

         // Player Tracking (from LevelPiece)
            EditorGUILayout.LabelField("Player Tracking", EditorStyles.boldLabel);
            if (playerHasEnteredProp != null)
             EditorGUILayout.PropertyField(playerHasEnteredProp, new GUIContent("Player Has Entered"));

    EditorGUILayout.Space();

      // Debug Settings
   EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
            if (showDebugLogsProp != null)
      EditorGUILayout.PropertyField(showDebugLogsProp, new GUIContent("Show Debug Logs (Corridor)"));
            if (showEnemyDebugProp != null)
                EditorGUILayout.PropertyField(showEnemyDebugProp, new GUIContent("Show Enemy Debug (LevelPiece)"));

            EditorGUILayout.Space();

      // Runtime Information
    if (Application.isPlaying)
       {
  EditorGUILayout.LabelField("Runtime Information", EditorStyles.boldLabel);

  EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.LabelField("Corridor Name:", corridor.CorridorName);
          EditorGUILayout.LabelField("Player Has Entered:", corridor.PlayerHasEntered.ToString());
       EditorGUILayout.LabelField("Enemies Spawned:", corridor.EnemiesSpawned.ToString());

    if (corridor.HasEnemies())
     {
              EditorGUILayout.LabelField("Active Enemies:", corridor.GetActiveEnemyCount().ToString());
           }

     // Show spawn point status
   if (corridor.SpawnPoints != null && corridor.SpawnPoints.Length > 0)
         {
   int spawnedCount = corridor.SpawnPoints.Count(sp => sp != null && sp.hasSpawned);
      EditorGUILayout.LabelField("Spawn Points Status:", $"{spawnedCount}/{corridor.SpawnPoints.Length} spawned");
        }

        EditorGUI.EndDisabledGroup();

     EditorGUILayout.Space();

       // Runtime Controls
         EditorGUILayout.LabelField("Runtime Controls", EditorStyles.boldLabel);

    EditorGUILayout.BeginHorizontal();
    if (GUILayout.Button("Force Open Exit Door"))
      {
    corridor.ForceOpenExitDoor();
      }
          if (GUILayout.Button("Force Register Enemies"))
          {
          corridor.ForceRegisterEnemies();
     }
     EditorGUILayout.EndHorizontal();

                // Status messages
     if (corridor.HasEnemies())
     {
        EditorGUILayout.HelpBox($"{corridor.GetActiveEnemyCount()} enemies remaining. Exit door will open when all are defeated.", MessageType.Warning);
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
            if (pointAProp != null && pointAProp.objectReferenceValue == null)
            {
      EditorGUILayout.HelpBox("? Corridor missing Point A (entrance)", MessageType.Error);
  hasErrors = true;
     }
            if (pointBProp != null && pointBProp.objectReferenceValue == null)
{
    EditorGUILayout.HelpBox("? Corridor missing Point B (exit)", MessageType.Error);
      hasErrors = true;
 }

            // Check doors
            if (exitDoorProp != null && exitDoorProp.objectReferenceValue == null)
            {
    EditorGUILayout.HelpBox("?? No exit door assigned", MessageType.Warning);
     hasWarnings = true;
            }
   if (entranceDoorProp != null && entranceDoorProp.objectReferenceValue == null)
            {
          EditorGUILayout.HelpBox("?? No entrance door assigned", MessageType.Warning);
      hasWarnings = true;
            }

   // Check spawn points
       if (spawnPointsProp != null && spawnPointsProp.arraySize == 0)
      {
          EditorGUILayout.HelpBox("?? No spawn points - no enemies will spawn (this is often fine for corridors)", MessageType.Info);
  }

            // Summary message
            if (!hasErrors && !hasWarnings)
       {
       EditorGUILayout.HelpBox("? Corridor configuration looks good!", MessageType.Info);
      }
            else if (hasErrors)
 {
                EditorGUILayout.HelpBox("Fix the errors above before using this corridor.", MessageType.Error);
          }

  serializedObject.ApplyModifiedProperties();
        }
    }
}
