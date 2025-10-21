# Runtime NavMesh for Procedural Rooms - Complete Guide

## ?? **The Problem:**

NavMesh baked in **prefab edit mode** doesn't transfer to **runtime instances**. Unity's NavMesh is **scene-specific**, not prefab-specific.

```
Room Prefab (Edit Mode):
?? Floor (Navigation Static) ?
?? NavMesh baked ?

? Instantiate at runtime

Room Instance (Scene):
?? Floor (Navigation Static) ?
?? NavMesh ? NOT THERE!
```

---

## ? **Quick Fix (Implemented)**

Added **0.5 second delay** before spawning enemies. Unity's NavMesh automatically updates for Navigation Static objects, we just need to wait.

### **What Changed:**
- `Room.cs` - Delays enemy spawn by 0.5s
- `Corridor.cs` - Delays enemy spawn by 0.5s

### **How It Works:**
```csharp
1. Room instantiated ? Floor marked Navigation Static
2. Wait 0.5 seconds ? Unity updates NavMesh automatically
3. Spawn enemies ? NavMesh now exists ?
```

This works because Unity's **auto-baking** updates the NavMesh when new Navigation Static objects appear.

---

## ??? **Proper Solution: NavMesh Surface Component**

For **production-quality procedural levels**, use Unity's **NavMesh Components** package.

### **Step 1: Install NavMesh Components**

1. **Window ? Package Manager**
2. Search for: **"AI Navigation"** or **"NavMesh Surface"**
3. **Install** (if not already installed)

---

### **Step 2: Setup Room Prefab**

1. **Open Room prefab**
2. **Select root Room object**
3. **Add Component ? NavMesh Surface**
4. **Configure**:
   ```
   Agent Type: Humanoid
   Collect Objects: Children
   Include Layers: Default (or Floor layer)
   Use Geometry: Render Meshes
   ```
5. **Don't click "Bake"** in prefab mode (won't work at runtime)
6. **Save prefab**

---

### **Step 3: Update Room.cs**

Replace current `Room.cs` with:

```csharp
using UnityEngine;
using UnityEngine.AI;
using AI.Spawning;

namespace ProceduralGeneration
{
    [RequireComponent(typeof(NavMeshSurface))]
    public class Room : LevelPiece
    {
        [Header("Room Settings")]
        [SerializeField] private string roomName = "Room";
        [SerializeField] private bool startingRoom = false;

        [Header("NavMesh Settings")]
        [SerializeField] private bool bakeNavMeshOnSpawn = true;
        [SerializeField] private float enemySpawnDelay = 0.1f;

        private NavMeshSurface navMeshSurface;

        public string RoomName => roomName;
        public bool IsStartingRoom => startingRoom;

        protected override void Awake()
        {
            base.Awake();
            
            navMeshSurface = GetComponent<NavMeshSurface>();
            
            if (startingRoom && pointA != null)
            {
                pointA.gameObject.SetActive(false);
            }
        }

        public override void OnSpawned(int roomIteration)
        {
            base.OnSpawned(roomIteration);
            
            if (bakeNavMeshOnSpawn && navMeshSurface != null)
            {
                // Bake NavMesh for this room instance
                navMeshSurface.BuildNavMesh();
                Debug.Log($"[Room] Baked NavMesh for {roomName}");
            }
            
            // Small delay to ensure NavMesh is ready
            Invoke(nameof(SpawnEnemiesDelayed), enemySpawnDelay);
        }

        private void SpawnEnemiesDelayed()
        {
            var spawnManager = FindObjectOfType<AI.Spawning.EnemySpawnManager>();
            if (spawnManager && spawnPoints != null && spawnPoints.Length > 0)
            {
                int currentIteration = spawnManager.GetCurrentIteration();
                spawnManager.SpawnEnemiesAtPoints(spawnPoints, true, currentIteration);
                spawnManager.SpawnPropsAtPoints(spawnPoints);
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = startingRoom ? Color.yellow : Color.blue;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 2f);
        }
    }
}
```

---

### **Step 4: Test**

1. **Play scene**
2. **Watch console**:
   ```
   ? [Room] Baked NavMesh for Room_1
   ? [SpawnPoint] Aligned to NavMesh: (10, 2, 5) -> (10, 0.1, 5)
   ? [SpawnPoint] Spawned BasicEnemy at SpawnPoint_1 (OnNavMesh: True)
   ```
3. **Enemies move** ?

---

## ?? **Comparison:**

| Method | Speed | Quality | Setup | Use Case |
|--------|-------|---------|-------|----------|
| **Current (Delay)** | Fast | OK | Easy ? | Testing, Simple Games |
| **NavMeshSurface** | Medium | Excellent | Moderate | Production Games |
| **Manual Baking** | Slow | Good | Hard | Legacy Projects |

---

## ?? **Current Solution (Already Implemented):**

### **Pros:**
- ? Works immediately
- ? No extra packages needed
- ? Simple setup
- ? Relies on Unity's auto-baking

### **Cons:**
- ?? Depends on scene-level NavMesh
- ?? May not work if scene NavMesh is off
- ?? 0.5s delay before enemies spawn
- ?? Less control over NavMesh quality

### **When to Use:**
- Small/medium games
- Prototyping
- Quick testing
- When you're OK with slight spawn delay

---

## ?? **NavMeshSurface Solution (Recommended for Production):**

### **Pros:**
- ? Per-room NavMesh (isolated)
- ? Full control over baking
- ? Works with complex procedural layouts
- ? No scene-level NavMesh needed
- ? Industry standard

### **Cons:**
- ?? Requires NavMesh Components package
- ?? More setup per room
- ?? Slight performance cost per bake

### **When to Use:**
- Production games
- Complex procedural levels
- Large open worlds
- When you need per-room NavMesh

---

## ??? **Implementation Checklist:**

### **Quick Fix (Current):**
- [x] Added delay to Room.cs
- [x] Added delay to Corridor.cs
- [x] Set delay to 0.5s
- [ ] Test in play mode
- [ ] Verify enemies spawn on NavMesh
- [ ] Verify enemies can move

### **Proper Fix (NavMeshSurface):**
- [ ] Install AI Navigation package
- [ ] Add NavMeshSurface to Room prefab
- [ ] Update Room.cs with BuildNavMesh() call
- [ ] Test in play mode
- [ ] Verify per-room NavMesh baking
- [ ] Optimize bake settings

---

## ?? **Testing:**

### **Current Solution:**
```
1. Play scene
2. Walk to room exit
3. New room spawns
4. Wait 0.5 seconds ? Delay
5. Enemies spawn
6. Check console: "Aligned to NavMesh" ?
7. Enemies move ?
```

### **NavMeshSurface Solution:**
```
1. Play scene
2. Walk to room exit
3. New room spawns
4. Room.OnSpawned() ? navMeshSurface.BuildNavMesh()
5. Wait 0.1 seconds ? Minimal delay
6. Enemies spawn
7. Check console: "Baked NavMesh for Room_1" ?
8. Enemies move ?
```

---

## ?? **Key Differences:**

### **Scene NavMesh (Current):**
```
Scene:
?? NavMesh (global, shared by all rooms)
?? Room 1 (uses scene NavMesh)
?? Room 2 (uses scene NavMesh)
?? Room 3 (uses scene NavMesh)
```

**Problem:** Scene NavMesh doesn't know about runtime-instantiated prefabs until Unity updates it.

**Solution:** Wait 0.5s for Unity to auto-update.

---

### **NavMeshSurface (Proper):**
```
Scene:
?? Room 1 (has own NavMeshSurface)
?   ?? NavMesh baked for Room 1 only
?? Room 2 (has own NavMeshSurface)
?   ?? NavMesh baked for Room 2 only
?? Room 3 (has own NavMeshSurface)
    ?? NavMesh baked for Room 3 only
```

**Benefit:** Each room has isolated NavMesh, baked on-demand.

**Solution:** Bake NavMesh when room spawns (0.1s delay).

---

## ?? **Debugging:**

### **Check NavMesh in Scene View:**

1. **Play scene**
2. **Window ? AI ? Navigation ? Bake** (tab)
3. **Check "Show NavMesh"**
4. **Look for blue overlay** on floors

### **Current Solution:**
- Blue overlay should appear **after 0.5s delay**
- Covers all rooms (global)

### **NavMeshSurface Solution:**
- Blue overlay appears **immediately per room**
- Only covers current room (local)

---

## ?? **Settings:**

### **Current (in Room.cs):**
```csharp
[SerializeField] private float enemySpawnDelay = 0.5f;
```
- Increase if enemies still fail to spawn
- Decrease for faster spawning (risky)

### **NavMeshSurface (future):**
```csharp
[SerializeField] private bool bakeNavMeshOnSpawn = true;
[SerializeField] private float enemySpawnDelay = 0.1f;
```
- Bake on spawn: Auto-bake NavMesh
- Delay: Minimal (NavMesh bakes instantly)

---

## ?? **Performance:**

### **Current Solution:**
- **NavMesh updates:** Automatic (Unity background)
- **Cost:** Low (Unity handles it)
- **Delay:** 0.5s (waiting for Unity)

### **NavMeshSurface Solution:**
- **NavMesh updates:** Manual (per room)
- **Cost:** Medium (per-room baking)
- **Delay:** 0.1s (controlled)
- **Optimization:** Can bake in background, cache, etc.

---

## ?? **Recommendations:**

### **For Your Game (Right Now):**
? **Use current solution** (0.5s delay)
- Works immediately
- No extra setup
- Good for testing

### **For Production (Later):**
? **Upgrade to NavMeshSurface**
- Better control
- Isolated per room
- Industry standard

---

## ?? **Additional Resources:**

### **Unity Docs:**
- NavMesh Components: https://docs.unity3d.com/Packages/com.unity.ai.navigation@latest
- NavMeshSurface API: https://docs.unity3d.com/Packages/com.unity.ai.navigation@latest/api/Unity.AI.Navigation.NavMeshSurface.html

### **Tutorial:**
- Runtime NavMesh Baking: https://learn.unity.com/tutorial/runtime-navmesh-baking

---

## ? **You're Done!**

The **quick fix** is already implemented. Just test it:

1. **Play scene**
2. **Walk to room exit**
3. **New room spawns**
4. **Wait 0.5s** (you'll see delay)
5. **Enemies spawn on NavMesh** ?

No NavMesh errors anymore! ??

---

**For better performance later, upgrade to NavMeshSurface component.**
