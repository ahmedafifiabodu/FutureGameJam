using AI.Enemy.Configuration;
using UnityEditor;
using UnityEngine;

namespace AI.Enemy.Editor
{
    /// <summary>
    /// Simple config editor - For complete setup use Enemy AI Setup Wizard
    /// </summary>
    public class EnemyConfigEditorWindow : EditorWindow
    {
        [MenuItem("Tools/AI/Enemy Config Creator (Legacy)")]
        public static void ShowWindow()
        {
            if (EditorUtility.DisplayDialog(
    "Use Enemy AI Setup Wizard Instead?",
     "The new Enemy AI Setup Wizard provides complete scene and prefab configuration.\n\n" +
       "Do you want to open it instead?",
                "Open Setup Wizard",
    "Use Legacy Editor"))
            {
                EnemyAISetupWizard.ShowWindow();
            }
            else
            {
                var window = GetWindow<EnemyConfigEditorWindow>("Config Editor");
                window.minSize = new Vector2(400, 200);
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox("This is the legacy config editor. For complete setup including scene and prefab configuration, use:", MessageType.Info);

            if (GUILayout.Button("Open Enemy AI Setup Wizard", GUILayout.Height(40)))
            {
                EnemyAISetupWizard.ShowWindow();
                Close();
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Quick Config Actions:", EditorStyles.boldLabel);

            if (GUILayout.Button("Create New Config", GUILayout.Height(30)))
            {
                CreateNewConfig();
            }

            if (GUILayout.Button("Create Template Configs", GUILayout.Height(30)))
            {
                EnemyConfigTemplateCreator.CreateTemplateConfigs();
            }
        }

        private void CreateNewConfig()
        {
            string path = EditorUtility.SaveFilePanelInProject(
               "Create New Enemy Config",
         "NewEnemyConfig",
         "asset",
               "Choose where to save the new enemy config"
                 );

            if (!string.IsNullOrEmpty(path))
            {
                EnemyConfigSO newConfig = CreateInstance<EnemyConfigSO>();
                newConfig.enemyName = "New Enemy";

                AssetDatabase.CreateAsset(newConfig, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                EditorGUIUtility.PingObject(newConfig);
                Selection.activeObject = newConfig;
            }
        }
    }

    /// <summary>
    /// Custom inspector for EnemyConfigSO
    /// </summary>
    [CustomEditor(typeof(EnemyConfigSO))]
    public class EnemyConfigSOEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(10);

            if (GUILayout.Button("Open in Setup Wizard", GUILayout.Height(30)))
            {
                EnemyAISetupWizard.ShowWindow();
            }

            EnemyConfigSO config = (EnemyConfigSO)target;

            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox($"Type: {config.enemyType} | Weight: {config.spawnWeight} | Min Iteration: {config.minRoomIteration}", MessageType.Info);
        }
    }
}