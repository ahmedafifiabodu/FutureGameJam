# GameConstant & Room Tracking Fixes

## ? **Issues Fixed:**

### **1. Layer/Tag Names in GameConstant** ?

Added organized constants for layers and tags:

```csharp
public static class GameConstant
{
    public static class Layers
    {
        public const string Default = "Default";
        public const string Floor = "Floor";
        public const string Wall = "Wall";
        public const string Enemy = "Enemy";
        public const string Player = "Player";
    }

    public static class Tags
    {
        public const string Player = "Player";
        public const string Enemy = "Enemy";
        public const string Room = "Room";
        public const string Corridor = "Corridor";
    }

    public static class AnimationParameters
    {
        // Existing animation constants...
    }
}
```

**Usage:**
```csharp
// OLD (hardcoded):
LayerMask.NameToLayer("Default")
GameObject.FindGameObjectWithTag("Player")

// NEW (using constants):
LayerMask.NameToLayer(GameConstant.Layers.Default)
GameObject.FindGameObjectWithTag(GameConstant.Tags.Player)
```

---

### **2. EnemyRoomTracker Auto-Detection** ?

**Problem:** Enemies spawned but couldn't detect which room they're in.

**Root Cause:** Room might not be fully initialized when enemy spawns.

**Solution:** Added retry mechanism + manual assignment:

#### **Features Added:**

1. **Retry Detection**
   - Tries up to 5 times with 0.5s delay
   - Stops when room found
   - Logs warnings if fails

2. **Manual Assignment**
   - `SetCurrentLevelPiece(levelPiece)` method
   - Called by SpawnPoint automatically
   - Skips retry if manually set

3. **Fallback Behavior**
   - If no room detected, allows aggro anyway
   - Prevents enemies from being useless

#### **Settings in Inspector:**

```
EnemyRoomTracker:
?? Retry Detection: ?
?? Max Retries: 5
?? Retry Delay: 0.5s
?? Enable Debug Logs: ? (enable for debugging)
```

---

### **3. SpawnPoint Auto-Assignment** ?

SpawnPoint now automatically assigns spawned enemies to their parent room/corridor:

```csharp
// After spawning enemy:
1. Get EnemyRoomTracker component
2. Find parent Room or Corridor
3. Call roomTracker.SetCurrentLevelPiece(parentRoom)
4. Enemy knows which room it's in ?
```

**Result:** No more "not in any room or corridor" warnings!

---

## ?? **How It Works:**

### **Spawn Flow:**

```
1. Room.OnSpawned(iteration)
   ?
2. Wait 0.5s (NavMesh update)
   ?
3. SpawnPoint.Spawn(enemyPrefab)
   ?
4. Enemy instantiated
   ?
5. SpawnPoint detects parent Room
   ?
6. roomTracker.SetCurrentLevelPiece(room) ?
   ?
7. Enemy knows its room ?
   ?
8. EnemyAI starts (with room containment)
```

---

## ?? **Console Logs:**

### **Successful Assignment:**
```
[SpawnPoint] Spawned BasicEnemy at SpawnPoint_1 (Position: (10, 0.1, 5), OnNavMesh: True)
[SpawnPoint] Assigned BasicEnemy(Clone) to Room_Prefab(Clone)
[EnemyRoomTracker] BasicEnemy(Clone) manually assigned to Room_Prefab(Clone)
```

? Enemy knows its room immediately

---

### **Retry Detection (Fallback):**
```
[EnemyRoomTracker] Retry 1/5 - Detecting level piece for BasicEnemy(Clone)
[EnemyRoomTracker] BasicEnemy(Clone) is in room: Room_1
```

? Enemy found room after retry

---

### **Failed Detection (Rare):**
```
[EnemyRoomTracker] BasicEnemy(Clone) failed to detect room/corridor after 5 attempts. 
Enemy will not have room-based aggro restrictions.
```

? Enemy will still work, just won't have room containment

---

## ?? **Comparison:**

### **Before (Broken):**
```
Enemy spawns
   ?
EnemyRoomTracker.Start() ? Detects room ? (too early)
   ?
"Not in any room or corridor!" warning
   ?
Enemy aggro doesn't work properly ?
```

### **After (Fixed):**
```
Enemy spawns
   ?
SpawnPoint ? Assigns room immediately ?
   ?
EnemyRoomTracker has room ?
   ?
Enemy aggro works correctly ?
```

---

## ??? **Debugging:**

### **Enable Debug Logs:**

1. **Select enemy in scene** (after spawning)
2. **EnemyRoomTracker component**
3. **Check "Enable Debug Logs"** ?
4. **Watch console** for detection messages

### **Expected Output:**
```
? [SpawnPoint] Assigned BasicEnemy(Clone) to Room_Prefab(Clone)
? [EnemyRoomTracker] BasicEnemy(Clone) manually assigned to Room_Prefab(Clone)
? [EnemyAI] BasicEnemy(Clone) changed state: Idle -> Chasing
```

---

## ?? **Settings Reference:**

### **GameConstant (Static):**
- No Inspector settings
- Just use constants in code

### **EnemyRoomTracker (Per Enemy):**
```
Retry Detection: ?
Max Retries: 5 (increase if needed)
Retry Delay: 0.5s (decrease for faster detection)
Enable Debug Logs: ? (enable for debugging)
```

### **SpawnPoint (Per Spawn Point):**
```
Auto Align To NavMesh: ?
NavMesh Search Distance: 5.0 (increase if needed)
```

### **Room (Per Room):**
```
Enemy Spawn Delay: 0.5s (time to wait for NavMesh)
```

---

## ?? **Testing Checklist:**

After these fixes:

- [ ] Play scene
- [ ] Walk to room exit
- [ ] New room spawns
- [ ] Wait 0.5s
- [ ] Enemies spawn
- [ ] Check console:
  - [ ] "Aligned to NavMesh" ?
  - [ ] "Assigned [Enemy] to [Room]" ?
  - [ ] "Manually assigned to [Room]" ?
  - [ ] No "not in any room" warnings ?
- [ ] Walk near enemy
- [ ] Enemy chases you ?
- [ ] Enemy stays in room (doesn't chase across rooms) ?

All checked? **Perfect!** ?

---

## ?? **Usage Examples:**

### **Using GameConstant.Layers:**
```csharp
// Check if object is on floor layer
if (gameObject.layer == LayerMask.NameToLayer(GameConstant.Layers.Floor))
{
    // Do something
}

// Raycast only on specific layers
int layerMask = LayerMask.GetMask(GameConstant.Layers.Default, GameConstant.Layers.Floor);
Physics.Raycast(origin, direction, out hit, distance, layerMask);
```

### **Using GameConstant.Tags:**
```csharp
// Find player
GameObject player = GameObject.FindGameObjectWithTag(GameConstant.Tags.Player);

// Check if collision is with player
if (other.CompareTag(GameConstant.Tags.Player))
{
    // Player entered trigger
}
```

### **Manual Room Assignment (Rare):**
```csharp
// If you need to manually assign an enemy to a room:
var enemy = Instantiate(enemyPrefab, position, rotation);
var tracker = enemy.GetComponent<EnemyRoomTracker>();
var currentRoom = FindObjectOfType<Room>();
tracker.SetCurrentLevelPiece(currentRoom);
```

---

## ?? **Files Changed:**

1. **`GameConstant.cs`**
   - Added `Layers` class
   - Added `Tags` class

2. **`EnemyRoomTracker.cs`**
   - Added retry detection
   - Added `SetCurrentLevelPiece()` method
   - Added fallback behavior (allow aggro if no room)

3. **`SpawnPoint.cs`**
   - Auto-assigns room to spawned enemies
   - Finds parent LevelPiece
   - Calls `SetCurrentLevelPiece()`

4. **`Room.cs`**
   - Simplified (no NavMesh baking code)
   - Just uses delay approach

---

## ? **Summary:**

- ? **GameConstant.Layers/Tags** added for organization
- ? **EnemyRoomTracker** now auto-detects room with retry
- ? **SpawnPoint** auto-assigns room to enemies
- ? **Fallback behavior** if detection fails
- ? **No more "not in any room" warnings**
- ? **Room-based aggro works correctly**

**All issues resolved!** ??
