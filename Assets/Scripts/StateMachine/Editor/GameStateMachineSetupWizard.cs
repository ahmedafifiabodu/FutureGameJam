using UnityEngine;
using UnityEditor;

namespace GameStateMachine.Editor
{
    /// <summary>
    /// Editor window for setting up the Game State Machine system
    /// </summary>
    public class GameStateMachineSetupWizard : EditorWindow
    {
        private bool stateMachineExists = false;
        private bool gameStateManagerExists = false;
        private bool levelGeneratorExists = false;
        private GameObject stateMachineObj;

        [MenuItem("Tools/Game State Machine/Setup Wizard")]
        public static void ShowWindow()
        {
            var window = GetWindow<GameStateMachineSetupWizard>("State Machine Setup");
            window.minSize = new Vector2(400, 500);
            window.Show();
        }

        private void OnEnable()
        {
            CheckExistingSystems();
        }

        private void OnGUI()
        {
            GUILayout.Label("Game State Machine Setup Wizard", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            DrawStatusSection();
            EditorGUILayout.Space();
            DrawSetupSection();
            EditorGUILayout.Space();
            DrawInfoSection();
        }

        private void DrawStatusSection()
        {
            EditorGUILayout.LabelField("System Status", EditorStyles.boldLabel);

            DrawStatusLine("Game State Machine", stateMachineExists);
            DrawStatusLine("GameStateManager", gameStateManagerExists);
            DrawStatusLine("ProceduralLevelGenerator", levelGeneratorExists);

            if (GUILayout.Button("Refresh Status", GUILayout.Height(30)))
            {
                CheckExistingSystems();
            }
        }

        private void DrawStatusLine(string label, bool exists)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(200));

            if (exists)
            {
                GUI.color = Color.green;
                EditorGUILayout.LabelField("✓ Found", EditorStyles.boldLabel);
            }
            else
            {
                GUI.color = Color.red;
                EditorGUILayout.LabelField("✗ Not Found", EditorStyles.boldLabel);
            }
            GUI.color = Color.white;

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSetupSection()
        {
            EditorGUILayout.LabelField("Setup Actions", EditorStyles.boldLabel);

            EditorGUI.BeginDisabledGroup(stateMachineExists);
            if (GUILayout.Button("Create Game State Machine", GUILayout.Height(40)))
            {
                CreateStateMachine();
            }
            EditorGUI.EndDisabledGroup();

            if (stateMachineExists)
            {
                EditorGUILayout.HelpBox("State Machine already exists in scene!", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("Click to create the Game State Machine GameObject", MessageType.Warning);
            }

            EditorGUILayout.Space();

            if (stateMachineExists && stateMachineObj != null)
            {
                if (GUILayout.Button("Select State Machine", GUILayout.Height(30)))
                {
                    Selection.activeGameObject = stateMachineObj;
                    EditorGUIUtility.PingObject(stateMachineObj);
                }
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Open Documentation", GUILayout.Height(30)))
            {
                OpenDocumentation();
            }
        }

        private void DrawInfoSection()
        {
            EditorGUILayout.LabelField("Quick Info", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
            "The Game State Machine provides:\n" +
       "• Pause/Resume functionality (ESC key)\n" +
     "• Complete game restart\n" +
           "• Clean state management\n" +
      "• Event system for state changes\n" +
         "• OOP best practices",
  MessageType.Info
        );

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Required Components:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("• GameStateMachineManager (auto-created)");
            EditorGUILayout.LabelField("• GameStateManager (should exist)");
            EditorGUILayout.LabelField("• ProceduralLevelGenerator (should exist)");

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Next Steps:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("1. Create State Machine (button above)");
            EditorGUILayout.LabelField("2. Test pause with ESC key");
            EditorGUILayout.LabelField("3. Implement Pause Menu UI (optional)");
            EditorGUILayout.LabelField("4. Subscribe to events (optional)");
        }

        private void CheckExistingSystems()
        {
            // Check for state machine
            var stateMachine = FindFirstObjectByType<GameStateMachineManager>();
            stateMachineExists = stateMachine != null;
            if (stateMachineExists)
            {
                stateMachineObj = stateMachine.gameObject;
            }

            // Check for game state manager
            var gameStateManager = FindFirstObjectByType<GameStateManager>();
            gameStateManagerExists = gameStateManager != null;

            // Check for level generator
            var levelGen = FindFirstObjectByType<ProceduralGeneration.ProceduralLevelGenerator>();
            levelGeneratorExists = levelGen != null;
        }

        private void CreateStateMachine()
        {
            GameObject stateMachine = new GameObject("GameStateMachine");
            stateMachine.AddComponent<GameStateMachineManager>();

            // Position it nicely in hierarchy
            stateMachine.transform.SetAsFirstSibling();

            // Select it
            Selection.activeGameObject = stateMachine;
            EditorGUIUtility.PingObject(stateMachine);

            // Mark scene as dirty
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
            );

            Debug.Log("[GameStateMachine] State Machine created successfully!");

            CheckExistingSystems();
        }

        private void OpenDocumentation()
        {
            string path = "Assets/../Documentation/GameStateMachine-Guide.md";
            Application.OpenURL("file://" + System.IO.Path.GetFullPath(path));
        }
    }
}