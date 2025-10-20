using UnityEngine;
using UnityEditor;
using System.IO;

namespace ProceduralGeneration.Editor
{
    public class ProceduralSetupWizard : EditorWindow
    {
        private string prefabPath = "Assets/Prefabs/ProceduralGeneration";
        private bool createExamplePrefabs = true;
        
        [MenuItem("Tools/Procedural Generation/Setup Wizard")]
        public static void ShowWindow()
        {
            GetWindow<ProceduralSetupWizard>("Procedural Setup");
        }

        private void OnGUI()
        {
            GUILayout.Label("Procedural Generation Setup Wizard", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox("This wizard will help you set up the procedural generation system.", MessageType.Info);
            EditorGUILayout.Space();

            // Step 1: Folder Structure
            EditorGUILayout.LabelField("Step 1: Folder Structure", EditorStyles.boldLabel);
            prefabPath = EditorGUILayout.TextField("Prefab Root Path", prefabPath);
            
            if (GUILayout.Button("Create Folder Structure"))
            {
                CreateFolderStructure();
            }
            
            EditorGUILayout.Space();

            // Step 2: Example Prefabs
            EditorGUILayout.LabelField("Step 2: Example Prefabs", EditorStyles.boldLabel);
            createExamplePrefabs = EditorGUILayout.Toggle("Create Example Prefabs", createExamplePrefabs);
            
            if (GUILayout.Button("Generate Example Prefabs"))
            {
                if (createExamplePrefabs)
                    CreateExamplePrefabs();
            }
            
            EditorGUILayout.Space();

            // Step 3: Scene Setup
            EditorGUILayout.LabelField("Step 3: Scene Setup", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Add Generator to Scene"))
            {
                CreateGeneratorInScene();
            }
            
            EditorGUILayout.Space();

            // Documentation
            EditorGUILayout.LabelField("Documentation", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Open README"))
            {
                string readmePath = "Assets/Scripts/ProceduralGeneration/README.md";
                if (File.Exists(readmePath))
                {
                    AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<TextAsset>(readmePath));
                }
                else
                {
                    EditorUtility.DisplayDialog("README Not Found", 
                        "README.md not found at: " + readmePath, "OK");
                }
            }
        }

        private void CreateFolderStructure()
        {
            string[] folders = new string[]
            {
                prefabPath,
                prefabPath + "/Rooms",
                prefabPath + "/Corridors",
                prefabPath + "/StartingRooms"
            };

            foreach (string folder in folders)
            {
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    string parentFolder = Path.GetDirectoryName(folder).Replace("\\", "/");
                    string newFolder = Path.GetFileName(folder);
                    
                    if (!AssetDatabase.IsValidFolder(parentFolder))
                    {
                        Directory.CreateDirectory(parentFolder);
                    }
                    
                    AssetDatabase.CreateFolder(parentFolder, newFolder);
                    Debug.Log($"[Setup] Created folder: {folder}");
                }
            }

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Folder Structure", "Folder structure created successfully!", "OK");
        }

        private void CreateExamplePrefabs()
        {
            int createdCount = 0;
            
            // Create starting room
            CreateExampleStartingRoom();
            createdCount++;
            
            // Create diverse room prefabs
            CreateSquareRoom("Room_Small_Square", 6f, 6f);
            createdCount++;
            
            CreateRectangularRoom("Room_Medium_Rectangular", 10f, 7f);
            createdCount++;
            
            CreateLargeRoom("Room_Large_Square", 15f, 15f);
            createdCount++;
            
            CreateLShapedRoom("Room_L_Shaped", 12f, 12f);
            createdCount++;
            
            CreateNarrowRoom("Room_Narrow_Long", 5f, 12f);
            createdCount++;
            
            CreateWideRoom("Room_Wide_Hall", 15f, 8f);
            createdCount++;
            
            CreateArenaRoom("Room_Arena_Round", 12f);
            createdCount++;
            
            // Create diverse corridor prefabs
            CreateStraightCorridor("Corridor_Short_Narrow", 5f, 3f);
            createdCount++;
            
            CreateStraightCorridor("Corridor_Medium", 8f, 3f);
            createdCount++;
            
            CreateStraightCorridor("Corridor_Long", 12f, 3f);
            createdCount++;
            
            CreateWideCorridor("Corridor_Wide_Short", 6f, 5f);
            createdCount++;
            
            CreateWideCorridor("Corridor_Wide_Long", 10f, 5f);
            createdCount++;
            
            CreateZigZagCorridor("Corridor_ZigZag", 12f);
            createdCount++;

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Example Prefabs Created", 
                $"Successfully created {createdCount} prefabs!\n\n" +
                "Includes:\n" +
                "- 1 Starting Room\n" +
                "- 7 Varied Room Layouts\n" +
                "- 6 Different Corridors\n\n" +
                "Check: " + prefabPath, "OK");
        }

        // ROOM CREATION METHODS

        private void CreateExampleStartingRoom()
        {
            GameObject roomObj = new GameObject("StartingRoom");
            Room room = roomObj.AddComponent<Room>();
            
            // Create floor
            CreateFloor(roomObj.transform, 10f, 10f, Color.gray);
            
            // Create walls
            CreateWalls(roomObj.transform, 10f, 10f, 3f);
            
            // Create exit point (Point B only for starting room)
            GameObject exitPoint = new GameObject("ExitPoint");
            exitPoint.transform.parent = roomObj.transform;
            exitPoint.transform.localPosition = new Vector3(0f, 1f, 5f); // At one edge
            exitPoint.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            exitPoint.AddComponent<ConnectionPoint>();
            
            // Create entrance trigger at exit
            GameObject trigger = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trigger.name = "ExitTrigger";
            trigger.transform.parent = roomObj.transform;
            trigger.transform.localPosition = new Vector3(0f, 1f, 4f);
            trigger.transform.localScale = new Vector3(3f, 3f, 1f);
            DestroyImmediate(trigger.GetComponent<MeshRenderer>());
            trigger.GetComponent<BoxCollider>().isTrigger = true;
            
            // Save prefab
            string path = prefabPath + "/StartingRooms/StartingRoom.prefab";
            PrefabUtility.SaveAsPrefabAsset(roomObj, path);
            DestroyImmediate(roomObj);
            
            Debug.Log($"[Setup] Created starting room: {path}");
        }

        private void CreateSquareRoom(string name, float size, float height)
        {
            GameObject roomObj = new GameObject(name);
            Room room = roomObj.AddComponent<Room>();
            
            // Create floor
            CreateFloor(roomObj.transform, size, size, Color.gray);
            
            // Create walls
            CreateWalls(roomObj.transform, size, size, 3f);
            
            // Add some decorative pillars in corners
            CreatePillar(roomObj.transform, new Vector3(size/2f - 1f, 1.5f, size/2f - 1f), 0.5f, 3f);
            CreatePillar(roomObj.transform, new Vector3(-size/2f + 1f, 1.5f, size/2f - 1f), 0.5f, 3f);
            CreatePillar(roomObj.transform, new Vector3(size/2f - 1f, 1.5f, -size/2f + 1f), 0.5f, 3f);
            CreatePillar(roomObj.transform, new Vector3(-size/2f + 1f, 1.5f, -size/2f + 1f), 0.5f, 3f);
            
            // Create connection points
            CreateRoomConnectionPoints(roomObj.transform, size);
            
            // Create entrance trigger
            CreateEntranceTrigger(roomObj.transform, size);
            
            // Save prefab
            SaveRoomPrefab(roomObj, name);
        }

        private void CreateRectangularRoom(string name, float width, float depth)
        {
            GameObject roomObj = new GameObject(name);
            Room room = roomObj.AddComponent<Room>();
            
            // Create floor
            CreateFloor(roomObj.transform, width, depth, Color.gray);
            
            // Create walls
            CreateWalls(roomObj.transform, width, depth, 3f);
            
            // Add side alcoves
            CreateAlcove(roomObj.transform, new Vector3(width/2f, 1.5f, 0f), 1f, 2f, 3f, Quaternion.Euler(0, 90, 0));
            CreateAlcove(roomObj.transform, new Vector3(-width/2f, 1.5f, 0f), 1f, 2f, 3f, Quaternion.Euler(0, -90, 0));
            
            // Create connection points
            CreateRoomConnectionPoints(roomObj.transform, depth);
            
            // Create entrance trigger
            CreateEntranceTrigger(roomObj.transform, depth);
            
            // Save prefab
            SaveRoomPrefab(roomObj, name);
        }

        private void CreateLargeRoom(string name, float size, float height)
        {
            GameObject roomObj = new GameObject(name);
            Room room = roomObj.AddComponent<Room>();
            
            // Create floor
            CreateFloor(roomObj.transform, size, size, Color.gray);
            
            // Create walls with higher ceiling
            CreateWalls(roomObj.transform, size, size, 5f);
            
            // Add central platform
            GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platform.name = "CentralPlatform";
            platform.transform.parent = roomObj.transform;
            platform.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            platform.transform.localScale = new Vector3(4f, 1f, 4f);
            
            // Add corner pillars
            CreatePillar(roomObj.transform, new Vector3(size/2f - 1.5f, 2.5f, size/2f - 1.5f), 0.8f, 5f);
            CreatePillar(roomObj.transform, new Vector3(-size/2f + 1.5f, 2.5f, size/2f - 1.5f), 0.8f, 5f);
            CreatePillar(roomObj.transform, new Vector3(size/2f - 1.5f, 2.5f, -size/2f + 1.5f), 0.8f, 5f);
            CreatePillar(roomObj.transform, new Vector3(-size/2f + 1.5f, 2.5f, -size/2f + 1.5f), 0.8f, 5f);
            
            // Create connection points
            CreateRoomConnectionPoints(roomObj.transform, size);
            
            // Create entrance trigger
            CreateEntranceTrigger(roomObj.transform, size);
            
            // Save prefab
            SaveRoomPrefab(roomObj, name);
        }

        private void CreateLShapedRoom(string name, float size, float depth)
        {
            GameObject roomObj = new GameObject(name);
            Room room = roomObj.AddComponent<Room>();
            
            // Create main section floor
            CreateFloor(roomObj.transform, size * 0.6f, depth, Color.gray, new Vector3(-size * 0.2f, 0f, 0f));
            
            // Create L extension floor
            CreateFloor(roomObj.transform, size * 0.4f, depth * 0.5f, Color.gray, new Vector3(size * 0.3f, 0f, -depth * 0.25f));
            
            // Create walls for main section
            CreatePartialWalls(roomObj.transform, size * 0.6f, depth, 3f, new Vector3(-size * 0.2f, 0f, 0f));
            
            // Create walls for L extension
            CreatePartialWalls(roomObj.transform, size * 0.4f, depth * 0.5f, 3f, new Vector3(size * 0.3f, 0f, -depth * 0.25f));
            
            // Create connection points
            CreateRoomConnectionPoints(roomObj.transform, depth);
            
            // Create entrance trigger
            CreateEntranceTrigger(roomObj.transform, depth);
            
            // Save prefab
            SaveRoomPrefab(roomObj, name);
        }

        private void CreateNarrowRoom(string name, float width, float depth)
        {
            GameObject roomObj = new GameObject(name);
            Room room = roomObj.AddComponent<Room>();
            
            // Create floor
            CreateFloor(roomObj.transform, width, depth, Color.gray);
            
            // Create walls
            CreateWalls(roomObj.transform, width, depth, 3f);
            
            // Add pillars along the length
            for (float z = -depth/2f + 2f; z < depth/2f; z += 3f)
            {
                CreatePillar(roomObj.transform, new Vector3(width/2f - 0.7f, 1.5f, z), 0.4f, 3f);
                CreatePillar(roomObj.transform, new Vector3(-width/2f + 0.7f, 1.5f, z), 0.4f, 3f);
            }
            
            // Create connection points
            CreateRoomConnectionPoints(roomObj.transform, depth);
            
            // Create entrance trigger
            CreateEntranceTrigger(roomObj.transform, depth);
            
            // Save prefab
            SaveRoomPrefab(roomObj, name);
        }

        private void CreateWideRoom(string name, float width, float depth)
        {
            GameObject roomObj = new GameObject(name);
            Room room = roomObj.AddComponent<Room>();
            
            // Create floor
            CreateFloor(roomObj.transform, width, depth, Color.gray);
            
            // Create walls
            CreateWalls(roomObj.transform, width, depth, 3f);
            
            // Add columns in a grid
            for (float x = -width/2f + 3f; x < width/2f; x += 4f)
            {
                for (float z = -depth/2f + 3f; z < depth/2f; z += 4f)
                {
                    CreatePillar(roomObj.transform, new Vector3(x, 1.5f, z), 0.5f, 3f);
                }
            }
            
            // Create connection points
            CreateRoomConnectionPoints(roomObj.transform, depth);
            
            // Create entrance trigger
            CreateEntranceTrigger(roomObj.transform, depth);
            
            // Save prefab
            SaveRoomPrefab(roomObj, name);
        }

        private void CreateArenaRoom(string name, float radius)
        {
            GameObject roomObj = new GameObject(name);
            Room room = roomObj.AddComponent<Room>();
            
            // Create octagonal floor approximation
            float size = radius * 2f;
            CreateFloor(roomObj.transform, size, size, Color.gray);
            
            // Create circular walls (octagonal approximation)
            CreateOctagonalWalls(roomObj.transform, radius, 3f);
            
            // Add central pillar/pedestal
            CreatePillar(roomObj.transform, new Vector3(0f, 0.5f, 0f), 1.5f, 1f);
            
            // Create connection points
            CreateRoomConnectionPoints(roomObj.transform, size);
            
            // Create entrance trigger
            CreateEntranceTrigger(roomObj.transform, size);
            
            // Save prefab
            SaveRoomPrefab(roomObj, name);
        }

        // CORRIDOR CREATION METHODS

        private void CreateStraightCorridor(string name, float length, float width)
        {
            GameObject corridorObj = new GameObject(name);
            Corridor corridor = corridorObj.AddComponent<Corridor>();
            
            // Create floor
            CreateFloor(corridorObj.transform, width, length, Color.gray);
            
            // Create walls
            CreateCorridorWalls(corridorObj.transform, length, width, 3f);
            
            // Create connection points
            CreateCorridorConnectionPoints(corridorObj.transform, length);
            
            // Save prefab
            SaveCorridorPrefab(corridorObj, name);
        }

        private void CreateWideCorridor(string name, float length, float width)
        {
            GameObject corridorObj = new GameObject(name);
            Corridor corridor = corridorObj.AddComponent<Corridor>();
            
            // Create floor
            CreateFloor(corridorObj.transform, width, length, Color.gray);
            
            // Create walls
            CreateCorridorWalls(corridorObj.transform, length, width, 3f);
            
            // Add pillars along sides
            for (float z = -length/2f + 2f; z < length/2f; z += 3f)
            {
                CreatePillar(corridorObj.transform, new Vector3(width/2f - 0.7f, 1.5f, z), 0.4f, 3f);
                CreatePillar(corridorObj.transform, new Vector3(-width/2f + 0.7f, 1.5f, z), 0.4f, 3f);
            }
            
            // Create connection points
            CreateCorridorConnectionPoints(corridorObj.transform, length);
            
            // Save prefab
            SaveCorridorPrefab(corridorObj, name);
        }

        private void CreateZigZagCorridor(string name, float totalLength)
        {
            GameObject corridorObj = new GameObject(name);
            Corridor corridor = corridorObj.AddComponent<Corridor>();
            
            float segmentLength = totalLength / 3f;
            float width = 3f;
            float offset = 2f;
            
            // Create three segments in a zig-zag pattern
            // Segment 1
            CreateFloor(corridorObj.transform, width, segmentLength, Color.gray, 
                new Vector3(-offset, 0f, -totalLength/2f + segmentLength/2f));
            CreateCorridorWalls(corridorObj.transform, segmentLength, width, 3f, 
                new Vector3(-offset, 0f, -totalLength/2f + segmentLength/2f));
            
            // Segment 2 (center)
            CreateFloor(corridorObj.transform, width, segmentLength, Color.gray, 
                new Vector3(0f, 0f, 0f));
            CreateCorridorWalls(corridorObj.transform, segmentLength, width, 3f, 
                new Vector3(0f, 0f, 0f));
            
            // Segment 3
            CreateFloor(corridorObj.transform, width, segmentLength, Color.gray, 
                new Vector3(offset, 0f, totalLength/2f - segmentLength/2f));
            CreateCorridorWalls(corridorObj.transform, segmentLength, width, 3f, 
                new Vector3(offset, 0f, totalLength/2f - segmentLength/2f));
            
            // Create connection points at ends
            CreateCorridorConnectionPoints(corridorObj.transform, totalLength);
            
            // Save prefab
            SaveCorridorPrefab(corridorObj, name);
        }

        // HELPER METHODS

        private void CreateFloor(Transform parent, float width, float depth, Color color, Vector3 offset = default)
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.parent = parent;
            floor.transform.localPosition = offset;
            floor.transform.localScale = new Vector3(width, 0.2f, depth);
            
            // Optional: Set material color (for visual distinction)
            if (floor.GetComponent<Renderer>() != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = color;
                floor.GetComponent<Renderer>().material = mat;
            }
        }

        private void CreatePillar(Transform parent, Vector3 position, float radius, float height)
        {
            GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pillar.name = "Pillar";
            pillar.transform.parent = parent;
            pillar.transform.localPosition = position;
            pillar.transform.localScale = new Vector3(radius, height / 2f, radius);
        }

        private void CreateAlcove(Transform parent, Vector3 position, float depth, float width, float height, Quaternion rotation)
        {
            GameObject alcove = GameObject.CreatePrimitive(PrimitiveType.Cube);
            alcove.name = "Alcove";
            alcove.transform.parent = parent;
            alcove.transform.localPosition = position;
            alcove.transform.localRotation = rotation;
            alcove.transform.localScale = new Vector3(width, height, depth);
        }

        private void CreatePartialWalls(Transform parent, float width, float depth, float height, Vector3 offset)
        {
            float wallThickness = 0.2f;
            
            // North wall
            CreateWall(parent, "WallNorth", offset + new Vector3(0f, height/2f, depth/2f), 
                new Vector3(width + wallThickness, height, wallThickness));
            
            // South wall
            CreateWall(parent, "WallSouth", offset + new Vector3(0f, height/2f, -depth/2f), 
                new Vector3(width + wallThickness, height, wallThickness));
            
            // East wall
            CreateWall(parent, "WallEast", offset + new Vector3(width/2f, height/2f, 0f), 
                new Vector3(wallThickness, height, depth));
            
            // West wall
            CreateWall(parent, "WallWest", offset + new Vector3(-width/2f, height/2f, 0f), 
                new Vector3(wallThickness, height, depth));
        }

        private void CreateOctagonalWalls(Transform parent, float radius, float height)
        {
            int sides = 8;
            float angleStep = 360f / sides;
            float wallThickness = 0.3f;
            
            for (int i = 0; i < sides; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                float nextAngle = (i + 1) * angleStep * Mathf.Deg2Rad;
                
                Vector3 pos1 = new Vector3(Mathf.Cos(angle) * radius, height/2f, Mathf.Sin(angle) * radius);
                Vector3 pos2 = new Vector3(Mathf.Cos(nextAngle) * radius, height/2f, Mathf.Sin(nextAngle) * radius);
                Vector3 wallPos = (pos1 + pos2) / 2f;
                
                float wallLength = Vector3.Distance(pos1, pos2);
                float wallAngle = Mathf.Atan2(pos2.z - pos1.z, pos2.x - pos1.x) * Mathf.Rad2Deg;
                
                GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.name = $"Wall_{i}";
                wall.transform.parent = parent;
                wall.transform.localPosition = wallPos;
                wall.transform.localRotation = Quaternion.Euler(0f, wallAngle - 90f, 0f);
                wall.transform.localScale = new Vector3(wallThickness, height, wallLength);
            }
        }

        private void CreateRoomConnectionPoints(Transform parent, float depth)
        {
            // Create entrance point (Point A)
            GameObject entrancePoint = new GameObject("EntrancePoint");
            entrancePoint.transform.parent = parent;
            entrancePoint.transform.localPosition = new Vector3(0f, 1f, -depth/2f);
            entrancePoint.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            entrancePoint.AddComponent<ConnectionPoint>();
            
            // Create exit point (Point B)
            GameObject exitPoint = new GameObject("ExitPoint");
            exitPoint.transform.parent = parent;
            exitPoint.transform.localPosition = new Vector3(0f, 1f, depth/2f);
            exitPoint.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            exitPoint.AddComponent<ConnectionPoint>();
        }

        private void CreateCorridorConnectionPoints(Transform parent, float length)
        {
            // Create entrance point (Point A)
            GameObject entrancePoint = new GameObject("EntrancePoint");
            entrancePoint.transform.parent = parent;
            entrancePoint.transform.localPosition = new Vector3(0f, 1f, -length/2f);
            entrancePoint.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            entrancePoint.AddComponent<ConnectionPoint>();
            
            // Create exit point (Point B)
            GameObject exitPoint = new GameObject("ExitPoint");
            exitPoint.transform.parent = parent;
            exitPoint.transform.localPosition = new Vector3(0f, 1f, length/2f);
            exitPoint.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            exitPoint.AddComponent<ConnectionPoint>();
        }

        private void CreateEntranceTrigger(Transform parent, float depth)
        {
            GameObject trigger = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trigger.name = "EntranceTrigger";
            trigger.transform.parent = parent;
            trigger.transform.localPosition = new Vector3(0f, 1f, -depth/2f + 1.5f);
            trigger.transform.localScale = new Vector3(4f, 3f, 2f);
            DestroyImmediate(trigger.GetComponent<MeshRenderer>());
            trigger.GetComponent<BoxCollider>().isTrigger = true;
            trigger.AddComponent<RoomEntranceTrigger>();
        }

        private void SaveRoomPrefab(GameObject roomObj, string name)
        {
            string path = prefabPath + "/Rooms/" + name + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(roomObj, path);
            DestroyImmediate(roomObj);
            Debug.Log($"[Setup] Created room: {path}");
        }

        private void SaveCorridorPrefab(GameObject corridorObj, string name)
        {
            string path = prefabPath + "/Corridors/" + name + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(corridorObj, path);
            DestroyImmediate(corridorObj);
            Debug.Log($"[Setup] Created corridor: {path}");
        }

        private void CreateWalls(Transform parent, float width, float depth, float height)
        {
            float wallThickness = 0.2f;
            
            // North wall
            CreateWall(parent, "WallNorth", new Vector3(0f, height/2f, depth/2f), 
                new Vector3(width + wallThickness, height, wallThickness));
            
            // South wall
            CreateWall(parent, "WallSouth", new Vector3(0f, height/2f, -depth/2f), 
                new Vector3(width + wallThickness, height, wallThickness));
            
            // East wall
            CreateWall(parent, "WallEast", new Vector3(width/2f, height/2f, 0f), 
                new Vector3(wallThickness, height, depth));
            
            // West wall
            CreateWall(parent, "WallWest", new Vector3(-width/2f, height/2f, 0f), 
                new Vector3(wallThickness, height, depth));
        }

        private void CreateCorridorWalls(Transform parent, float length, float width, float height, Vector3 offset = default)
        {
            float wallThickness = 0.2f;
            
            // North wall
            CreateWall(parent, "WallNorth", offset + new Vector3(0f, height/2f, length/2f), 
                new Vector3(width + wallThickness, height, wallThickness));
            
            // South wall
            CreateWall(parent, "WallSouth", offset + new Vector3(0f, height/2f, -length/2f), 
                new Vector3(width + wallThickness, height, wallThickness));
            
            // East wall
            CreateWall(parent, "WallEast", offset + new Vector3(width/2f, height/2f, 0f), 
                new Vector3(wallThickness, height, length));
            
            // West wall
            CreateWall(parent, "WallWest", offset + new Vector3(-width/2f, height/2f, 0f), 
                new Vector3(wallThickness, height, length));
        }

        private void CreateWall(Transform parent, string name, Vector3 position, Vector3 scale)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.parent = parent;
            wall.transform.localPosition = position;
            wall.transform.localScale = scale;
        }

        private void CreateGeneratorInScene()
        {
            // Check if already exists
            ProceduralLevelGenerator existing = FindObjectOfType<ProceduralLevelGenerator>();
            if (existing != null)
            {
                EditorUtility.DisplayDialog("Generator Exists", 
                    "ProceduralLevelGenerator already exists in scene!", "OK");
                Selection.activeGameObject = existing.gameObject;
                return;
            }

            // Create generator
            GameObject generatorObj = new GameObject("ProceduralLevelGenerator");
            ProceduralLevelGenerator generator = generatorObj.AddComponent<ProceduralLevelGenerator>();
            
            // Try to find player
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            
            Selection.activeGameObject = generatorObj;
            
            EditorUtility.DisplayDialog("Generator Created", 
                "ProceduralLevelGenerator added to scene!\n\n" +
                "Remember to assign:\n" +
                "- Starting Room Prefab\n" +
                "- Room Prefabs (with weights)\n" +
                "- Corridor Prefabs (with weights)\n" +
                (player == null ? "- Player Transform" : ""), "OK");
        }
    }
}
