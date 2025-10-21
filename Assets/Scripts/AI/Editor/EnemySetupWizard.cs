using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace AI.Editor
{
    /// <summary>
    /// Editor wizard to help designers quickly setup enemy AI
    /// Accessible via Tools > AI > Enemy Setup Wizard
    /// </summary>
    public class EnemySetupWizard : EditorWindow
    {
        private enum SetupStep
        {
            Welcome,
            CreateProfile,
            SetupPrefab,
            AddSpawnPoints,
            ConfigureSpawnManager,
            Complete
        }

        private SetupStep currentStep = SetupStep.Welcome;
        private Vector2 scrollPosition;

        // Step 2: Create Profile
        private string profileName = "NewEnemy_Profile";
        private EnemyType enemyType = EnemyType.Basic;

        // Step 3: Setup Prefab
        private GameObject enemyPrefab;
        private EnemyProfile selectedProfile;

        // Step 4: Spawn Points
        private GameObject roomPrefab;
        private int spawnPointCount = 4;

        [MenuItem("Tools/AI/Enemy Setup Wizard")]
        public static void ShowWindow()
        {
            var window = GetWindow<EnemySetupWizard>("Enemy Setup Wizard");
            window.minSize = new Vector2(500, 600);
            window.Show();
        }

        private void OnGUI()
        {
            DrawHeader();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            switch (currentStep)
            {
                case SetupStep.Welcome:
                    DrawWelcomeStep();
                    break;
                case SetupStep.CreateProfile:
                    DrawCreateProfileStep();
                    break;
                case SetupStep.SetupPrefab:
                    DrawSetupPrefabStep();
                    break;
                case SetupStep.AddSpawnPoints:
                    DrawAddSpawnPointsStep();
                    break;
                case SetupStep.ConfigureSpawnManager:
                    DrawConfigureSpawnManagerStep();
                    break;
                case SetupStep.Complete:
                    DrawCompleteStep();
                    break;
            }

            EditorGUILayout.EndScrollView();

            DrawNavigation();
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(10);
            
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter
            };
            
            EditorGUILayout.LabelField("Enemy AI Setup Wizard", titleStyle);
            EditorGUILayout.LabelField($"Step {(int)currentStep + 1} of 6", EditorStyles.centeredGreyMiniLabel);
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.Space(10);
        }

        private void DrawWelcomeStep()
        {
            EditorGUILayout.HelpBox("Welcome to the Enemy AI Setup Wizard!\n\n" +
                "This wizard will guide you through:\n" +
                "• Creating enemy profiles\n" +
                "• Setting up enemy prefabs\n" +
                "• Adding spawn points to rooms\n" +
                "• Configuring the spawn manager\n\n" +
                "Click 'Next' to begin!", MessageType.Info);

            EditorGUILayout.Space(20);

            EditorGUILayout.LabelField("Quick Links:", EditorStyles.boldLabel);
            if (GUILayout.Button("Open README Documentation"))
            {
                var readme = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Scripts/AI/README.md");
                if (readme)
                    AssetDatabase.OpenAsset(readme);
                else
                    EditorUtility.DisplayDialog("Not Found", "README.md not found at Assets/Scripts/AI/", "OK");
            }
        }

        private void DrawCreateProfileStep()
        {
            EditorGUILayout.HelpBox("Step 1: Create an Enemy Profile\n\n" +
                "Profiles define enemy stats and behavior.", MessageType.Info);

            EditorGUILayout.Space(10);

            profileName = EditorGUILayout.TextField("Profile Name", profileName);
            enemyType = (EnemyType)EditorGUILayout.EnumPopup("Enemy Type", enemyType);

            EditorGUILayout.Space(10);

            if (GUILayout.Button("Create Profile", GUILayout.Height(30)))
            {
                CreateEnemyProfile();
            }

            EditorGUILayout.Space(20);
            EditorGUILayout.LabelField("Or select existing profile:", EditorStyles.boldLabel);
            selectedProfile = (EnemyProfile)EditorGUILayout.ObjectField("Existing Profile", selectedProfile, typeof(EnemyProfile), false);
        }

        private void DrawSetupPrefabStep()
        {
            EditorGUILayout.HelpBox("Step 2: Setup Enemy Prefab\n\n" +
                "Add AI components to your enemy prefab.", MessageType.Info);

            EditorGUILayout.Space(10);

            enemyPrefab = (GameObject)EditorGUILayout.ObjectField("Enemy Prefab", enemyPrefab, typeof(GameObject), false);
            selectedProfile = (EnemyProfile)EditorGUILayout.ObjectField("Enemy Profile", selectedProfile, typeof(EnemyProfile), false);

            if (enemyPrefab == null)
            {
                EditorGUILayout.HelpBox("Please assign an enemy prefab (GameObject with model)", MessageType.Warning);
            }
            else if (selectedProfile == null)
            {
                EditorGUILayout.HelpBox("Please assign an enemy profile (or create one in previous step)", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.Space(10);

                if (GUILayout.Button("Add AI Components to Prefab", GUILayout.Height(30)))
                {
                    SetupEnemyPrefab();
                }

                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("Components to add:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("• NavMeshAgent");
                EditorGUILayout.LabelField("• Animator (if missing)");
                EditorGUILayout.LabelField("• AudioSource (if missing)");
                EditorGUILayout.LabelField("• EnemyHealth");
                EditorGUILayout.LabelField("• EnemyRoomTracker");
                EditorGUILayout.LabelField($"• {enemyType}Enemy");
            }
        }

        private void DrawAddSpawnPointsStep()
        {
            EditorGUILayout.HelpBox("Step 3: Add Spawn Points to Room\n\n" +
                "Spawn points define where enemies appear in rooms.", MessageType.Info);

            EditorGUILayout.Space(10);

            roomPrefab = (GameObject)EditorGUILayout.ObjectField("Room Prefab", roomPrefab, typeof(GameObject), false);
            spawnPointCount = EditorGUILayout.IntSlider("Spawn Point Count", spawnPointCount, 1, 10);

            if (roomPrefab == null)
            {
                EditorGUILayout.HelpBox("Please assign a room prefab", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.Space(10);

                if (GUILayout.Button("Add Spawn Points to Room", GUILayout.Height(30)))
                {
                    AddSpawnPointsToRoom();
                }

                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("Tips:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("• Spawn points will be evenly distributed");
                EditorGUILayout.LabelField("• You can manually reposition them after");
                EditorGUILayout.LabelField("• Blue arrow shows forward direction");
            }
        }

        private void DrawConfigureSpawnManagerStep()
        {
            EditorGUILayout.HelpBox("Step 4: Configure Spawn Manager\n\n" +
                "The spawn manager controls enemy spawning and difficulty scaling.", MessageType.Info);

            EditorGUILayout.Space(10);

            var spawnManager = FindObjectOfType<AI.Spawning.EnemySpawnManager>();

            if (spawnManager == null)
            {
                EditorGUILayout.HelpBox("No EnemySpawnManager found in scene", MessageType.Warning);
                
                if (GUILayout.Button("Create Spawn Manager", GUILayout.Height(30)))
                {
                    CreateSpawnManager();
                }
            }
            else
            {
                EditorGUILayout.LabelField("Spawn Manager Found!", EditorStyles.boldLabel);
                
                if (GUILayout.Button("Select Spawn Manager", GUILayout.Height(25)))
                {
                    Selection.activeGameObject = spawnManager.gameObject;
                }

                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("Next Steps:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("1. Select the Spawn Manager");
                EditorGUILayout.LabelField("2. Add your enemy prefabs to 'Enemy Prefabs' array");
                EditorGUILayout.LabelField("3. Configure weights and spawn conditions");
                EditorGUILayout.LabelField("4. Adjust difficulty scaling settings");
            }
        }

        private void DrawCompleteStep()
        {
            EditorGUILayout.HelpBox("Setup Complete!\n\n" +
                "Your enemy AI system is ready to use.", MessageType.Info);

            EditorGUILayout.Space(20);

            EditorGUILayout.LabelField("Final Checklist:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("? Enemy profiles created");
            EditorGUILayout.LabelField("? Enemy prefabs configured");
            EditorGUILayout.LabelField("? Spawn points added to rooms");
            EditorGUILayout.LabelField("? Spawn manager setup");

            EditorGUILayout.Space(20);

            EditorGUILayout.LabelField("Don't Forget:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("• Bake NavMesh on room floors");
            EditorGUILayout.LabelField("• Setup animator controller with required parameters");
            EditorGUILayout.LabelField("• Assign audio clips to enemy profiles");
            EditorGUILayout.LabelField("• Test in play mode!");

            EditorGUILayout.Space(20);

            if (GUILayout.Button("Open README for More Details", GUILayout.Height(30)))
            {
                var readme = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Scripts/AI/README.md");
                if (readme)
                    AssetDatabase.OpenAsset(readme);
            }

            if (GUILayout.Button("Close Wizard", GUILayout.Height(30)))
            {
                Close();
            }
        }

        private void DrawNavigation()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            
            EditorGUILayout.BeginHorizontal();

            GUI.enabled = currentStep > SetupStep.Welcome;
            if (GUILayout.Button("? Previous", GUILayout.Height(30)))
            {
                currentStep--;
            }
            GUI.enabled = true;

            GUILayout.FlexibleSpace();

            if (currentStep < SetupStep.Complete)
            {
                if (GUILayout.Button("Next ?", GUILayout.Height(30)))
                {
                    currentStep++;
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(10);
        }

        #region Helper Methods

        private void CreateEnemyProfile()
        {
            string path = "Assets/ScriptableObjects/AI/Profiles/";
            if (!AssetDatabase.IsValidFolder(path.TrimEnd('/')))
            {
                AssetDatabase.CreateFolder("Assets/ScriptableObjects/AI", "Profiles");
            }

            EnemyProfile profile = CreateInstance<EnemyProfile>();
            profile.enemyName = profileName.Replace("_Profile", "");
            profile.enemyType = enemyType;

            // Set default values based on type
            switch (enemyType)
            {
                case EnemyType.Basic:
                    profile.maxHealth = 80;
                    profile.chaseSpeed = 3.5f;
                    profile.attackDamage = 20;
                    break;
                case EnemyType.Tough:
                    profile.maxHealth = 150;
                    profile.chaseSpeed = 2.5f;
                    profile.attackDamage = 30;
                    profile.painChance = 0.15f;
                    break;
                case EnemyType.Fast:
                    profile.maxHealth = 60;
                    profile.chaseSpeed = 5f;
                    profile.attackDamage = 15;
                    profile.painChance = 0.4f;
                    break;
            }

            string assetPath = path + profileName + ".asset";
            AssetDatabase.CreateAsset(profile, assetPath);
            AssetDatabase.SaveAssets();

            selectedProfile = profile;
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = profile;

            EditorUtility.DisplayDialog("Success", $"Created enemy profile: {profileName}", "OK");
        }

        private void SetupEnemyPrefab()
        {
            // Ensure we're working with a prefab
            string prefabPath = AssetDatabase.GetAssetPath(enemyPrefab);
            if (string.IsNullOrEmpty(prefabPath))
            {
                EditorUtility.DisplayDialog("Error", "Please assign a prefab, not a scene instance", "OK");
                return;
            }

            GameObject prefab = PrefabUtility.LoadPrefabContents(prefabPath);

            // Add NavMeshAgent
            if (!prefab.GetComponent<UnityEngine.AI.NavMeshAgent>())
                prefab.AddComponent<UnityEngine.AI.NavMeshAgent>();

            // Add Animator
            if (!prefab.GetComponent<Animator>())
                prefab.AddComponent<Animator>();

            // Add AudioSource
            if (!prefab.GetComponent<AudioSource>())
                prefab.AddComponent<AudioSource>();

            // Add EnemyHealth
            if (!prefab.GetComponent<EnemyHealth>())
                prefab.AddComponent<EnemyHealth>();

            // Add EnemyRoomTracker
            if (!prefab.GetComponent<EnemyRoomTracker>())
                prefab.AddComponent<EnemyRoomTracker>();

            // Add appropriate AI component
            switch (selectedProfile.enemyType)
            {
                case EnemyType.Basic:
                    if (!prefab.GetComponent<AI.EnemyTypes.BasicEnemy>())
                    {
                        var ai = prefab.AddComponent<AI.EnemyTypes.BasicEnemy>();
                        SerializedObject so = new SerializedObject(ai);
                        so.FindProperty("profile").objectReferenceValue = selectedProfile;
                        so.ApplyModifiedProperties();
                    }
                    break;
                case EnemyType.Tough:
                    if (!prefab.GetComponent<AI.EnemyTypes.ToughEnemy>())
                    {
                        var ai = prefab.AddComponent<AI.EnemyTypes.ToughEnemy>();
                        SerializedObject so = new SerializedObject(ai);
                        so.FindProperty("profile").objectReferenceValue = selectedProfile;
                        so.ApplyModifiedProperties();
                    }
                    break;
                case EnemyType.Fast:
                    if (!prefab.GetComponent<AI.EnemyTypes.FastEnemy>())
                    {
                        var ai = prefab.AddComponent<AI.EnemyTypes.FastEnemy>();
                        SerializedObject so = new SerializedObject(ai);
                        so.FindProperty("profile").objectReferenceValue = selectedProfile;
                        so.ApplyModifiedProperties();
                    }
                    break;
            }

            PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
            PrefabUtility.UnloadPrefabContents(prefab);

            EditorUtility.DisplayDialog("Success", "Added AI components to prefab!", "OK");
            AssetDatabase.Refresh();
        }

        private void AddSpawnPointsToRoom()
        {
            string prefabPath = AssetDatabase.GetAssetPath(roomPrefab);
            if (string.IsNullOrEmpty(prefabPath))
            {
                EditorUtility.DisplayDialog("Error", "Please assign a prefab", "OK");
                return;
            }

            GameObject prefab = PrefabUtility.LoadPrefabContents(prefabPath);

            // Create spawn points container
            Transform container = prefab.transform.Find("SpawnPoints");
            if (!container)
            {
                GameObject containerObj = new GameObject("SpawnPoints");
                containerObj.transform.SetParent(prefab.transform);
                containerObj.transform.localPosition = Vector3.zero;
                container = containerObj.transform;
            }

            // Add spawn points in a circle pattern
            float radius = 5f;
            for (int i = 0; i < spawnPointCount; i++)
            {
                GameObject spawnPoint = new GameObject($"SpawnPoint_{i + 1}");
                spawnPoint.transform.SetParent(container);

                float angle = (360f / spawnPointCount) * i;
                float x = Mathf.Cos(angle * Mathf.Deg2Rad) * radius;
                float z = Mathf.Sin(angle * Mathf.Deg2Rad) * radius;

                spawnPoint.transform.localPosition = new Vector3(x, 0, z);
                spawnPoint.transform.localRotation = Quaternion.Euler(0, angle + 90, 0);

                var sp = spawnPoint.AddComponent<AI.Spawning.SpawnPoint>();
                // Configure spawn point if needed
            }

            PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
            PrefabUtility.UnloadPrefabContents(prefab);

            EditorUtility.DisplayDialog("Success", $"Added {spawnPointCount} spawn points to room!", "OK");
            AssetDatabase.Refresh();
        }

        private void CreateSpawnManager()
        {
            GameObject managerObj = new GameObject("EnemySpawnManager");
            managerObj.AddComponent<AI.Spawning.EnemySpawnManager>();

            Selection.activeGameObject = managerObj;
            EditorUtility.DisplayDialog("Success", "Created EnemySpawnManager!\n\nConfigure it in the Inspector.", "OK");
        }

        #endregion
    }
}
