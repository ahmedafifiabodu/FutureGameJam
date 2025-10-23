using UnityEngine;
using UnityEditor;

namespace GameStateMachine.Editor
{
    /// <summary>
    /// Custom inspector for GameStateMachineManager with real-time state display
    /// </summary>
    [CustomEditor(typeof(GameStateMachineManager))]
    public class GameStateMachineEditor : UnityEditor.Editor
    {
        private bool showStateInfo = true;
        private bool showDependencies = true;
private bool showEvents = false;

     public override void OnInspectorGUI()
   {
       var stateMachine = (GameStateMachineManager)target;

         // Header
   EditorGUILayout.Space();
 EditorGUILayout.LabelField("Game State Machine Manager", EditorStyles.boldLabel);
     EditorGUILayout.HelpBox("Manages game states: Parasite, Host, Paused, Game Over", MessageType.Info);
      EditorGUILayout.Space();

      // Draw default inspector
       DrawDefaultInspector();

      EditorGUILayout.Space();
   DrawSeparator();

      // Runtime info
            if (Application.isPlaying)
{
 DrawRuntimeInfo(stateMachine);
     }
   else
    {
     DrawEditorInfo();
            }

            EditorGUILayout.Space();
  DrawSeparator();

    // Dependencies
   showDependencies = EditorGUILayout.Foldout(showDependencies, "Dependencies", true);
      if (showDependencies)
      {
          DrawDependencies(stateMachine);
      }

EditorGUILayout.Space();
     DrawSeparator();

            // Events
 showEvents = EditorGUILayout.Foldout(showEvents, "Available Events", true);
       if (showEvents)
       {
    DrawEvents();
          }

       EditorGUILayout.Space();
       DrawSeparator();

  // Buttons
DrawButtons(stateMachine);
        }

     private void DrawRuntimeInfo(GameStateMachineManager stateMachine)
        {
 showStateInfo = EditorGUILayout.Foldout(showStateInfo, "Runtime State Info", true);
    if (!showStateInfo) return;

     EditorGUI.indentLevel++;

       // Current state
     EditorGUILayout.BeginHorizontal();
       EditorGUILayout.LabelField("Current State:", GUILayout.Width(120));
   GUI.color = Color.cyan;
       EditorGUILayout.LabelField(stateMachine.CurrentState?.StateName ?? "None", EditorStyles.boldLabel);
            GUI.color = Color.white;
  EditorGUILayout.EndHorizontal();

        // Paused status
       EditorGUILayout.BeginHorizontal();
       EditorGUILayout.LabelField("Is Paused:", GUILayout.Width(120));
 GUI.color = stateMachine.IsPaused ? Color.yellow : Color.green;
       EditorGUILayout.LabelField(stateMachine.IsPaused ? "Yes" : "No", EditorStyles.boldLabel);
     GUI.color = Color.white;
     EditorGUILayout.EndHorizontal();

      // Time scale
   EditorGUILayout.BeginHorizontal();
  EditorGUILayout.LabelField("Time Scale:", GUILayout.Width(120));
       GUI.color = Time.timeScale == 0 ? Color.red : Color.green;
   EditorGUILayout.LabelField(Time.timeScale.ToString("F2"), EditorStyles.boldLabel);
       GUI.color = Color.white;
    EditorGUILayout.EndHorizontal();

       EditorGUI.indentLevel--;

  // Force repaint for live updates
        Repaint();
        }

   private void DrawEditorInfo()
     {
       EditorGUILayout.HelpBox("State machine info will appear here during Play Mode", MessageType.Info);
   }

 private void DrawDependencies(GameStateMachineManager stateMachine)
    {
     EditorGUI.indentLevel++;

            if (Application.isPlaying)
    {
   DrawDependencyStatus("GameStateManager", stateMachine.GetGameStateManager() != null);
     DrawDependencyStatus("InputManager", stateMachine.GetInputManager() != null);
       DrawDependencyStatus("LevelGenerator", stateMachine.GetLevelGenerator() != null);
  }
       else
{
        var gsm = FindFirstObjectByType<GameStateManager>();
     var im = FindFirstObjectByType<InputManager>();
          var lg = FindFirstObjectByType<ProceduralGeneration.ProceduralLevelGenerator>();

     DrawDependencyStatus("GameStateManager", gsm != null);
     DrawDependencyStatus("InputManager", im != null);
   DrawDependencyStatus("LevelGenerator", lg != null);
       }

       EditorGUI.indentLevel--;
        }

        private void DrawDependencyStatus(string name, bool exists)
   {
       EditorGUILayout.BeginHorizontal();
     EditorGUILayout.LabelField(name, GUILayout.Width(150));
     
 if (exists)
    {
       GUI.color = Color.green;
  EditorGUILayout.LabelField("? Connected", EditorStyles.boldLabel);
       }
   else
   {
   GUI.color = Color.red;
          EditorGUILayout.LabelField("? Missing", EditorStyles.boldLabel);
       }
  GUI.color = Color.white;

     EditorGUILayout.EndHorizontal();
      }

        private void DrawEvents()
     {
            EditorGUI.indentLevel++;

       EditorGUILayout.LabelField("• OnGamePaused");
            EditorGUILayout.LabelField("• OnGameResumed");
   EditorGUILayout.LabelField("• OnGameRestarted");
       EditorGUILayout.LabelField("• OnGameOver(int hosts, float time)");

         EditorGUILayout.Space();
         EditorGUILayout.HelpBox("Subscribe to these events to react to state changes", MessageType.Info);

       EditorGUI.indentLevel--;
        }

        private void DrawButtons(GameStateMachineManager stateMachine)
   {
     EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);

  EditorGUI.BeginDisabledGroup(!Application.isPlaying);

    EditorGUILayout.BeginHorizontal();

     if (stateMachine.IsPaused)
   {
    if (GUILayout.Button("Resume Game", GUILayout.Height(30)))
      {
stateMachine.ResumeGame();
     }
            }
   else
        {
     if (GUILayout.Button("Pause Game", GUILayout.Height(30)))
{
    stateMachine.PauseGame();
          }
       }

       if (GUILayout.Button("Restart Game", GUILayout.Height(30)))
     {
         stateMachine.RestartGame();
     }

            EditorGUILayout.EndHorizontal();

      EditorGUI.EndDisabledGroup();

    if (!Application.isPlaying)
  {
                EditorGUILayout.HelpBox("Enter Play Mode to use quick actions", MessageType.Info);
       }

   EditorGUILayout.Space();

       if (GUILayout.Button("Open Documentation", GUILayout.Height(25)))
    {
        Application.OpenURL("file://" + System.IO.Path.GetFullPath("Documentation/GameStateMachine-Guide.md"));
   }
     }

        private void DrawSeparator()
        {
  EditorGUILayout.Space(5);
       EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
       EditorGUILayout.Space(5);
     }
    }
}
