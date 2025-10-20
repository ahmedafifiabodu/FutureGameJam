using UnityEngine;
using UnityEditor;

namespace ProceduralGeneration.Editor
{
    /// <summary>
    /// Helper tool to assign materials to all generated prefabs
    /// </summary>
    public class PrefabMaterialAssigner : EditorWindow
    {
        private Material floorMaterial;
        private Material wallMaterial;
        private Material pillarMaterial;
        private string prefabPath = "Assets/Prefabs/ProceduralGeneration";
        
        [MenuItem("Tools/Procedural Generation/Assign Materials to Prefabs")]
        public static void ShowWindow()
        {
            GetWindow<PrefabMaterialAssigner>("Material Assigner");
        }

        private void OnGUI()
        {
            GUILayout.Label("Assign Materials to Generated Prefabs", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox("Assign materials to automatically apply them to all room and corridor prefabs.", MessageType.Info);
            EditorGUILayout.Space();

            // Material fields
            floorMaterial = (Material)EditorGUILayout.ObjectField("Floor Material", floorMaterial, typeof(Material), false);
            wallMaterial = (Material)EditorGUILayout.ObjectField("Wall Material", wallMaterial, typeof(Material), false);
            pillarMaterial = (Material)EditorGUILayout.ObjectField("Pillar Material", pillarMaterial, typeof(Material), false);
            
            EditorGUILayout.Space();
            prefabPath = EditorGUILayout.TextField("Prefab Root Path", prefabPath);
            
            EditorGUILayout.Space();

            if (GUILayout.Button("Create Default Materials"))
            {
                CreateDefaultMaterials();
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Apply Materials to All Prefabs"))
            {
                ApplyMaterialsToPrefabs();
            }
        }

        private void CreateDefaultMaterials()
        {
            string materialPath = "Assets/Materials";
            
            // Create Materials folder if it doesn't exist
            if (!AssetDatabase.IsValidFolder(materialPath))
            {
                AssetDatabase.CreateFolder("Assets", "Materials");
            }

            // Create Floor Material (gray)
            if (floorMaterial == null)
            {
                Material floor = new Material(Shader.Find("Standard"));
                floor.color = new Color(0.4f, 0.4f, 0.4f); // Dark gray
                AssetDatabase.CreateAsset(floor, materialPath + "/Floor_Material.mat");
                floorMaterial = floor;
                Debug.Log("[MaterialAssigner] Created Floor_Material.mat");
            }

            // Create Wall Material (lighter gray)
            if (wallMaterial == null)
            {
                Material wall = new Material(Shader.Find("Standard"));
                wall.color = new Color(0.6f, 0.6f, 0.6f); // Light gray
                AssetDatabase.CreateAsset(wall, materialPath + "/Wall_Material.mat");
                wallMaterial = wall;
                Debug.Log("[MaterialAssigner] Created Wall_Material.mat");
            }

            // Create Pillar Material (brown)
            if (pillarMaterial == null)
            {
                Material pillar = new Material(Shader.Find("Standard"));
                pillar.color = new Color(0.5f, 0.35f, 0.2f); // Brown
                AssetDatabase.CreateAsset(pillar, materialPath + "/Pillar_Material.mat");
                pillarMaterial = pillar;
                Debug.Log("[MaterialAssigner] Created Pillar_Material.mat");
            }

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Materials Created", 
                "Default materials created in Assets/Materials/\n\n" +
                "- Floor_Material (dark gray)\n" +
                "- Wall_Material (light gray)\n" +
                "- Pillar_Material (brown)", "OK");
        }

        private void ApplyMaterialsToPrefabs()
        {
            if (floorMaterial == null || wallMaterial == null)
            {
                EditorUtility.DisplayDialog("Missing Materials", 
                    "Please assign or create materials first!", "OK");
                return;
            }

            int roomsProcessed = 0;
            int corridorsProcessed = 0;

            // Process Rooms
            string roomPath = prefabPath + "/Rooms";
            if (AssetDatabase.IsValidFolder(roomPath))
            {
                string[] roomGuids = AssetDatabase.FindAssets("t:Prefab", new[] { roomPath });
                foreach (string guid in roomGuids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (ApplyMaterialsToPrefab(prefab, path))
                        roomsProcessed++;
                }
            }

            // Process Corridors
            string corridorPath = prefabPath + "/Corridors";
            if (AssetDatabase.IsValidFolder(corridorPath))
            {
                string[] corridorGuids = AssetDatabase.FindAssets("t:Prefab", new[] { corridorPath });
                foreach (string guid in corridorGuids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (ApplyMaterialsToPrefab(prefab, path))
                        corridorsProcessed++;
                }
            }

            // Process Starting Rooms
            string startPath = prefabPath + "/StartingRooms";
            if (AssetDatabase.IsValidFolder(startPath))
            {
                string[] startGuids = AssetDatabase.FindAssets("t:Prefab", new[] { startPath });
                foreach (string guid in startGuids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    ApplyMaterialsToPrefab(prefab, path);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Materials Applied", 
                $"Materials applied successfully!\n\n" +
                $"Rooms: {roomsProcessed}\n" +
                $"Corridors: {corridorsProcessed}", "OK");
        }

        private bool ApplyMaterialsToPrefab(GameObject prefab, string prefabPath)
        {
            if (prefab == null) return false;

            // Load prefab for editing
            GameObject prefabInstance = PrefabUtility.LoadPrefabContents(prefabPath);
            bool modified = false;

            // Find all renderers
            Renderer[] renderers = prefabInstance.GetComponentsInChildren<Renderer>();
            
            foreach (Renderer renderer in renderers)
            {
                string objName = renderer.gameObject.name.ToLower();
                
                if (objName.Contains("floor"))
                {
                    renderer.material = floorMaterial;
                    modified = true;
                }
                else if (objName.Contains("wall"))
                {
                    renderer.material = wallMaterial;
                    modified = true;
                }
                else if (objName.Contains("pillar") || objName.Contains("alcove") || objName.Contains("platform"))
                {
                    renderer.material = pillarMaterial != null ? pillarMaterial : wallMaterial;
                    modified = true;
                }
            }

            if (modified)
            {
                PrefabUtility.SaveAsPrefabAsset(prefabInstance, prefabPath);
                Debug.Log($"[MaterialAssigner] Applied materials to: {prefab.name}");
            }

            PrefabUtility.UnloadPrefabContents(prefabInstance);
            return modified;
        }
    }
}
