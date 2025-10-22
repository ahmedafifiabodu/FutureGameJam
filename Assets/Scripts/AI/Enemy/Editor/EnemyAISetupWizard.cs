using AI.Enemy.Configuration;
using AI.Spawning;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace AI.Enemy.Editor
{
    /// <summary>
    /// Comprehensive setup wizard for enemy AI system
    /// Helps configure scene, prefabs, and all required components
    /// </summary>
    public class EnemyAISetupWizard : EditorWindow
    {
        private Vector2 scrollPosition;
        private int currentTab = 0;
        private readonly string[] tabs = { "Scene Setup", "Enemy Prefab Setup", "Config Management", "Validation" };

        // Scene Setup
        private EnemySpawnManager spawnManager;

        private GameObject selectedRoom;
        private int spawnPointsToAdd = 5;

        // Enemy Prefab Setup
        private GameObject enemyPrefab;

        private EnemyConfigSO assignedConfig;
        private bool autoSetupComponents = true;
        private bool createAttackCollider = true;

        // Validation
        private readonly List<string> validationErrors = new();

        private readonly List<string> validationWarnings = new();
        private bool validationComplete = false;

        [MenuItem("Tools/AI/Enemy AI Setup Wizard")]
        public static void ShowWindow()
        {
            var window = GetWindow<EnemyAISetupWizard>("Enemy AI Setup Wizard");
            window.minSize = new Vector2(600, 500);
        }

        private void OnEnable()
        {
            // Try to find existing spawn manager
            spawnManager = FindFirstObjectByType<EnemySpawnManager>();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            DrawHeader();
            EditorGUILayout.Space(10);

            // Tab selection
            currentTab = GUILayout.Toolbar(currentTab, tabs, GUILayout.Height(25));
            EditorGUILayout.Space(10);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            switch (currentTab)
            {
                case 0:
                    DrawSceneSetupTab();
                    break;

                case 1:
                    DrawEnemyPrefabSetupTab();
                    break;

                case 2:
                    DrawConfigManagementTab();
                    break;

                case 3:
                    DrawValidationTab();
                    break;
            }

            EditorGUILayout.EndScrollView();
        }

        #region Header

        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Enemy AI Setup Wizard", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Complete setup tool for scene and prefab configuration", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }

        #endregion Header

        #region Scene Setup Tab

        private void DrawSceneSetupTab()
        {
            DrawSection("Scene Setup", "Configure spawn manager and add spawn points to rooms");

            // Spawn Manager Setup
            DrawSubSection("Spawn Manager");

            spawnManager = (EnemySpawnManager)EditorGUILayout.ObjectField(
          "Spawn Manager",
      spawnManager,
                typeof(EnemySpawnManager),
       true
   );

            if (spawnManager == null)
            {
                EditorGUILayout.HelpBox("No EnemySpawnManager found in scene!", MessageType.Error);

                if (GUILayout.Button("Create Spawn Manager", GUILayout.Height(30)))
                {
                    CreateSpawnManager();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("? Spawn Manager found and ready", MessageType.Info);

                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.IntField("Enemy Configs Count", spawnManager.transform.childCount);
                EditorGUI.EndDisabledGroup();

                if (GUILayout.Button("Select Spawn Manager", GUILayout.Height(25)))
                {
                    Selection.activeGameObject = spawnManager.gameObject;
                    EditorGUIUtility.PingObject(spawnManager.gameObject);
                }
            }

            EditorGUILayout.Space(15);

            // Spawn Points Setup
            DrawSubSection("Add Spawn Points to Room/Corridor");

            selectedRoom = (GameObject)EditorGUILayout.ObjectField(
           "Room/Corridor Prefab",
         selectedRoom,
               typeof(GameObject),
                      false
              );

            spawnPointsToAdd = EditorGUILayout.IntSlider("Number of Spawn Points", spawnPointsToAdd, 1, 20);

            EditorGUILayout.Space(5);

            EditorGUI.BeginDisabledGroup(selectedRoom == null);

            if (GUILayout.Button("Add Spawn Points to Selected Room", GUILayout.Height(30)))
            {
                AddSpawnPointsToRoom(selectedRoom, spawnPointsToAdd);
            }

            EditorGUI.EndDisabledGroup();

            if (selectedRoom != null)
            {
                var existingPoints = selectedRoom.GetComponentsInChildren<SpawnPoint>(true);
                EditorGUILayout.HelpBox($"Current spawn points in prefab: {existingPoints.Length}", MessageType.Info);
            }

            EditorGUILayout.Space(15);

            // Quick Actions
            DrawSubSection("Quick Actions");

            if (GUILayout.Button("Find All Rooms in Project", GUILayout.Height(25)))
            {
                FindAndSelectRooms();
            }

            if (GUILayout.Button("Create Template Configs", GUILayout.Height(25)))
            {
                EnemyConfigTemplateCreator.CreateTemplateConfigs();
            }
        }

        #endregion Scene Setup Tab

        #region Enemy Prefab Setup Tab

        private void DrawEnemyPrefabSetupTab()
        {
            DrawSection("Enemy Prefab Setup", "Configure enemy prefabs with all required components");

            // Prefab Selection
            DrawSubSection("Enemy Prefab");

            enemyPrefab = (GameObject)EditorGUILayout.ObjectField(
                   "Enemy Prefab",
              enemyPrefab,
                typeof(GameObject),
                false
                     );

            if (enemyPrefab == null)
            {
                EditorGUILayout.HelpBox("Select an enemy prefab to configure", MessageType.Info);
                return;
            }

            EditorGUILayout.Space(10);

            // Config Assignment
            DrawSubSection("Configuration");

            assignedConfig = (EnemyConfigSO)EditorGUILayout.ObjectField(
        "Enemy Config",
           assignedConfig,
              typeof(EnemyConfigSO),
     false
     );

            if (assignedConfig == null)
            {
                EditorGUILayout.HelpBox("Select a config to assign to this enemy", MessageType.Warning);
            }

            EditorGUILayout.Space(10);

            // Setup Options
            DrawSubSection("Setup Options");

            autoSetupComponents = EditorGUILayout.Toggle("Auto-Setup Components", autoSetupComponents);
            createAttackCollider = EditorGUILayout.Toggle("Create Attack Collider", createAttackCollider);

            EditorGUILayout.Space(10);

            // Component Status
            DrawSubSection("Component Status");

            DrawComponentStatus();

            EditorGUILayout.Space(10);

            // Setup Button
            EditorGUI.BeginDisabledGroup(enemyPrefab == null);

            if (GUILayout.Button("Setup Enemy Prefab", GUILayout.Height(40)))
            {
                SetupEnemyPrefab(enemyPrefab, assignedConfig, autoSetupComponents, createAttackCollider);
            }

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(10);

            // Batch Setup
            DrawSubSection("Batch Setup");

            if (GUILayout.Button("Setup All Selected Prefabs", GUILayout.Height(30)))
            {
                SetupSelectedPrefabs();
            }

            EditorGUILayout.HelpBox("Select multiple enemy prefabs in Project window, then click above", MessageType.Info);
        }

        private void DrawComponentStatus()
        {
            if (enemyPrefab == null) return;

            var controller = enemyPrefab.GetComponent<EnemyController>();
            var agent = enemyPrefab.GetComponent<NavMeshAgent>();
            var animator = enemyPrefab.GetComponent<Animator>();
            var collider = enemyPrefab.GetComponent<Collider>();

            DrawStatusLine("EnemyController", controller != null);
            DrawStatusLine("NavMeshAgent", agent != null);
            DrawStatusLine("Animator", animator != null);
            DrawStatusLine("Collider (Body)", collider != null);

            if (controller != null)
            {
                EditorGUI.indentLevel++;
                DrawStatusLine("Config Assigned", controller.Config != null);
                DrawStatusLine("Attack Collider Set", controller.Agent != null);
                EditorGUI.indentLevel--;
            }
        }

        private void DrawStatusLine(string label, bool status)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(150));

            GUIStyle style = new(EditorStyles.label);
            style.normal.textColor = status ? Color.green : Color.red;
            EditorGUILayout.LabelField(status ? "? Present" : "? Missing", style);

            EditorGUILayout.EndHorizontal();
        }

        #endregion Enemy Prefab Setup Tab

        #region Config Management Tab

        private void DrawConfigManagementTab()
        {
            DrawSection("Config Management", "Manage and create enemy configurations");

            DrawSubSection("Create New Config");

            if (GUILayout.Button("Open Config Creator", GUILayout.Height(30)))
            {
                EnemyConfigEditorWindow.ShowWindow();
            }

            if (GUILayout.Button("Create Template Configs", GUILayout.Height(30)))
            {
                EnemyConfigTemplateCreator.CreateTemplateConfigs();
            }

            EditorGUILayout.Space(15);

            DrawSubSection("Existing Configs");

            var configs = FindAllConfigs();

            if (configs.Length == 0)
            {
                EditorGUILayout.HelpBox("No enemy configs found in project", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox($"Found {configs.Length} enemy config(s)", MessageType.Info);

                foreach (var config in configs)
                {
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

                    EditorGUILayout.LabelField(config.enemyName, EditorStyles.boldLabel, GUILayout.Width(150));
                    EditorGUILayout.LabelField($"Type: {config.enemyType}", GUILayout.Width(100));
                    EditorGUILayout.LabelField($"Weight: {config.spawnWeight}", GUILayout.Width(80));

                    if (GUILayout.Button("Select", GUILayout.Width(60)))
                    {
                        Selection.activeObject = config;
                        EditorGUIUtility.PingObject(config);
                    }

                    if (GUILayout.Button("Edit", GUILayout.Width(60)))
                    {
                        var window = GetWindow<EnemyConfigEditorWindow>("Enemy Config Creator");
                        // Note: Would need to add a method to set the current config in the editor window
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.Space(15);

            DrawSubSection("Quick Config Tools");

            if (GUILayout.Button("Assign Configs to Spawn Manager", GUILayout.Height(30)))
            {
                AssignConfigsToSpawnManager();
            }
        }

        #endregion Config Management Tab

        #region Validation Tab

        private void DrawValidationTab()
        {
            DrawSection("Project Validation", "Validate your enemy AI setup");

            if (GUILayout.Button("Run Full Validation", GUILayout.Height(40)))
            {
                RunValidation();
            }

            EditorGUILayout.Space(10);

            if (validationComplete)
            {
                // Errors
                if (validationErrors.Count > 0)
                {
                    DrawSubSection($"Errors ({validationErrors.Count})");
                    foreach (var error in validationErrors)
                    {
                        EditorGUILayout.HelpBox(error, MessageType.Error);
                    }
                }

                // Warnings
                if (validationWarnings.Count > 0)
                {
                    EditorGUILayout.Space(10);
                    DrawSubSection($"Warnings ({validationWarnings.Count})");
                    foreach (var warning in validationWarnings)
                    {
                        EditorGUILayout.HelpBox(warning, MessageType.Warning);
                    }
                }

                // Success
                if (validationErrors.Count == 0 && validationWarnings.Count == 0)
                {
                    EditorGUILayout.Space(10);
                    EditorGUILayout.HelpBox("? All validation checks passed! Your enemy AI system is properly configured.", MessageType.Info);
                }

                EditorGUILayout.Space(10);

                DrawSubSection("Validation Details");
                EditorGUILayout.LabelField($"Total Configs: {FindAllConfigs().Length}");
                EditorGUILayout.LabelField($"Scene has Spawn Manager: {FindFirstObjectByType<EnemySpawnManager>() != null}");
                EditorGUILayout.LabelField($"Player Tag Exists: {GameObject.FindGameObjectWithTag("Player") != null}");
            }

            EditorGUILayout.Space(15);

            DrawSubSection("Documentation");

            if (GUILayout.Button("Open Quick Setup Guide", GUILayout.Height(25)))
            {
                Application.OpenURL("file://" + Application.dataPath + "/Scripts/AI/QUICK_SETUP_GUIDE.md");
            }

            if (GUILayout.Button("Open Full Documentation", GUILayout.Height(25)))
            {
                Application.OpenURL("file://" + Application.dataPath + "/Scripts/AI/README_ENEMY_AI.md");
            }
        }

        #endregion Validation Tab

        #region Helper Methods

        private void DrawSection(string title, string description)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(description, EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        private void DrawSubSection(string title)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.Space(3);
        }

        private void CreateSpawnManager()
        {
            GameObject managerObj = new("EnemySpawnManager");
            spawnManager = managerObj.AddComponent<EnemySpawnManager>();

            // Create pool parent
            GameObject poolParent = new("Enemy Pool");
            poolParent.transform.SetParent(managerObj.transform);

            Undo.RegisterCreatedObjectUndo(managerObj, "Create Spawn Manager");
            Selection.activeGameObject = managerObj;

            EditorUtility.DisplayDialog("Success", "Spawn Manager created successfully!", "OK");
        }

        private void AddSpawnPointsToRoom(GameObject roomPrefab, int count)
        {
            if (roomPrefab == null) return;

            string prefabPath = AssetDatabase.GetAssetPath(roomPrefab);
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);

            // Create or find spawn points container
            Transform spawnContainer = prefabRoot.transform.Find("SpawnPoints");
            if (spawnContainer == null)
            {
                GameObject container = new("SpawnPoints");
                container.transform.SetParent(prefabRoot.transform);
                container.transform.localPosition = Vector3.zero;
                spawnContainer = container.transform;
            }

            // Add spawn points in a grid pattern
            float spacing = 3f;
            int cols = Mathf.CeilToInt(Mathf.Sqrt(count));

            for (int i = 0; i < count; i++)
            {
                GameObject spawnPoint = new($"SpawnPoint_{i + 1:00}");
                spawnPoint.transform.SetParent(spawnContainer);

                // Position in grid
                int row = i / cols;
                int col = i % cols;
                spawnPoint.transform.SetLocalPositionAndRotation(new Vector3(col * spacing, 0, row * spacing), Quaternion.identity);

                // Add component
                var sp = spawnPoint.AddComponent<SpawnPoint>();
                sp.spawnType = SpawnPoint.SpawnType.Enemy;
                sp.isActive = true;
            }

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            PrefabUtility.UnloadPrefabContents(prefabRoot);

            EditorUtility.DisplayDialog("Success", $"Added {count} spawn points to {roomPrefab.name}", "OK");
        }

        private void SetupEnemyPrefab(GameObject prefab, EnemyConfigSO config, bool autoSetup, bool createAttack)
        {
            if (prefab == null) return;

            string prefabPath = AssetDatabase.GetAssetPath(prefab);
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);

            // Add EnemyController
            var controller = prefabRoot.GetComponent<EnemyController>();
            if (controller == null && autoSetup)
            {
                controller = prefabRoot.AddComponent<EnemyController>();
            }

            // Add NavMeshAgent
            var agent = prefabRoot.GetComponent<NavMeshAgent>();
            if (agent == null && autoSetup)
            {
                agent = prefabRoot.AddComponent<NavMeshAgent>();
                agent.speed = config != null ? config.moveSpeed : 3.5f;
                agent.angularSpeed = 120f;
                agent.acceleration = 8f;
            }

            // Add Animator
            var animator = prefabRoot.GetComponent<Animator>();
            if (animator == null && autoSetup)
            {
                animator = prefabRoot.AddComponent<Animator>();
            }

            // Add body collider if missing
            var collider = prefabRoot.GetComponent<Collider>();
            if (collider == null && autoSetup)
            {
                var capsule = prefabRoot.AddComponent<CapsuleCollider>();
                capsule.height = 2f;
                capsule.radius = 0.5f;
                capsule.center = new Vector3(0, 1, 0);
            }

            // Create attack collider
            if (createAttack && autoSetup)
            {
                Transform attackTrans = prefabRoot.transform.Find("AttackCollider");
                if (attackTrans == null)
                {
                    GameObject attackObj = new("AttackCollider");
                    attackObj.transform.SetParent(prefabRoot.transform);
                    attackObj.transform.localPosition = new Vector3(0, 1, 0.8f);

                    var attackCollider = attackObj.AddComponent<BoxCollider>();
                    attackCollider.size = new Vector3(0.8f, 1f, 1f);
                    attackCollider.isTrigger = true;
                    attackCollider.enabled = false;

                    // Assign to controller
                    if (controller != null)
                    {
                        SerializedObject so = new(controller);
                        so.FindProperty("attackCollider").objectReferenceValue = attackCollider;
                        so.ApplyModifiedPropertiesWithoutUndo();
                    }
                }
            }

            // Assign config if provided
            if (controller != null && config != null)
            {
                SerializedObject so = new(controller);
                so.FindProperty("config").objectReferenceValue = config;
                so.FindProperty("agent").objectReferenceValue = agent;
                so.FindProperty("animator").objectReferenceValue = animator;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            PrefabUtility.UnloadPrefabContents(prefabRoot);

            EditorUtility.DisplayDialog("Success", $"Enemy prefab {prefab.name} configured successfully!", "OK");
        }

        private void SetupSelectedPrefabs()
        {
            var selectedObjects = Selection.objects;
            int count = 0;

            foreach (var obj in selectedObjects)
            {
                if (obj is GameObject go && AssetDatabase.Contains(go))
                {
                    SetupEnemyPrefab(go, null, autoSetupComponents, createAttackCollider);
                    count++;
                }
            }

            EditorUtility.DisplayDialog("Batch Setup Complete", $"Configured {count} enemy prefab(s)", "OK");
        }

        private void FindAndSelectRooms()
        {
            var guids = AssetDatabase.FindAssets("t:GameObject", new[] { "Assets" });
            List<GameObject> rooms = new();

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (obj != null)
                {
                    // Check if it's a room/corridor
                    if (obj.GetComponent<ProceduralGeneration.Room>() != null || obj.GetComponent<ProceduralGeneration.Corridor>() != null)
                        rooms.Add(obj);
                }
            }

            if (rooms.Count > 0)
            {
                Selection.objects = rooms.ToArray();
                EditorUtility.DisplayDialog("Rooms Found", $"Found and selected {rooms.Count} room/corridor prefab(s)", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("No Rooms Found", "No room or corridor prefabs found in project", "OK");
            }
        }

        private EnemyConfigSO[] FindAllConfigs()
        {
            var guids = AssetDatabase.FindAssets("t:EnemyConfigSO");
            List<EnemyConfigSO> configs = new();

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var config = AssetDatabase.LoadAssetAtPath<EnemyConfigSO>(path);
                if (config != null)
                    configs.Add(config);
            }

            return configs.ToArray();
        }

        private void AssignConfigsToSpawnManager()
        {
            if (spawnManager == null)
            {
                EditorUtility.DisplayDialog("Error", "No spawn manager found!", "OK");
                return;
            }

            var configs = FindAllConfigs();

            SerializedObject so = new(spawnManager);
            SerializedProperty configsProp = so.FindProperty("enemyConfigs");

            configsProp.ClearArray();
            for (int i = 0; i < configs.Length; i++)
            {
                configsProp.InsertArrayElementAtIndex(i);
                configsProp.GetArrayElementAtIndex(i).objectReferenceValue = configs[i];
            }

            so.ApplyModifiedProperties();

            EditorUtility.DisplayDialog("Success", $"Assigned {configs.Length} config(s) to spawn manager", "OK");
        }

        private void RunValidation()
        {
            validationErrors.Clear();
            validationWarnings.Clear();
            validationComplete = false;

            // Check spawn manager
            var sm = FindFirstObjectByType<EnemySpawnManager>();
            if (sm == null)
            {
                validationErrors.Add("No EnemySpawnManager found in scene");
            }
            else
            {
                SerializedObject so = new(sm);
                var configsProp = so.FindProperty("enemyConfigs");
                if (configsProp.arraySize == 0)
                {
                    validationWarnings.Add("Spawn Manager has no enemy configs assigned");
                }
            }

            // Check configs
            var configs = FindAllConfigs();
            if (configs.Length == 0)
            {
                validationErrors.Add("No enemy configs found in project");
            }
            else
            {
                foreach (var config in configs)
                {
                    if (config.enemyPrefab == null)
                    {
                        validationWarnings.Add($"Config '{config.enemyName}' has no enemy prefab assigned");
                    }
                }
            }

            // Check player tag
            try
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player == null)
                {
                    validationWarnings.Add("No GameObject with 'Player' tag found in scene");
                }
            }
            catch
            {
                validationErrors.Add("'Player' tag does not exist in project");
            }

            // Check NavMesh
            var navMeshData = UnityEngine.AI.NavMesh.CalculateTriangulation();
            if (navMeshData.vertices.Length == 0)
            {
                validationWarnings.Add("No NavMesh baked in scene. Enemies need NavMesh to pathfind. Bake NavMesh from Window > AI > Navigation");
            }

            validationComplete = true;
        }

        #endregion Helper Methods
    }
}