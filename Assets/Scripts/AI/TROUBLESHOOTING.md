# Enemy Spawn Troubleshooting Guide

## Common Issue: "No valid enemy prefabs for iteration X"

This error means no enemies can spawn at the current room iteration. Here's how to fix it:

### ? Checklist:

#### 1. **Check Enemy Profile Assignment**
In `EnemySpawnManager` Inspector:
- Expand `Enemy Prefabs` array (Element 0)
- **Enemy Prefab**: Must be assigned (drag your enemy prefab)
- **Profile**: Must be assigned (drag the EnemyProfile ScriptableObject)
- **Weight**: Can be 0 (will use profile's weight) or custom value

#### 2. **Check Profile Settings**
Select your EnemyProfile asset:
- **Spawn Weight**: Must be > 0 (e.g., 50)
- **Min Room Iteration**: Must be ? current iteration (start with 0)
- **Max Per Room**: Must be > 0 (e.g., 3)

#### 3. **Check Spawn Points**
Select your Room prefab:
- Must have `SpawnPoint` components as children
- Each spawn point needs `Type` set to `Enemy` (not Prop)
- Spawn points should be inside the room bounds

#### 4. **Enable Debug Logs**
In `EnemySpawnManager` Inspector:
- Check `Enable Debug Logs`
- Play scene and read console for detailed info

---

## Error Messages & Solutions

### "[EnemySpawnManager] Validated 0/1 enemy prefabs"

**Problem**: Profile is not assigned

**Solution**:
1. Select EnemySpawnManager in scene
2. Expand Enemy Prefabs array
3. Assign both:
   - Enemy Prefab (the GameObject)
   - Profile (the ScriptableObject)

---

### "[EnemySpawnManager] No valid enemy prefabs for iteration X"

**Problem**: Min Room Iteration is too high

**Solution**:
1. Select your EnemyProfile asset
2. Set `Min Room Iteration` to 0 (or lower than X)
3. Save

---

### "[EnemySpawnManager] Total weight is 0!"

**Problem**: All weights are zero

**Solution**:
1. Check EnemyProfile: `Spawn Weight` must be > 0
2. OR set custom weight in EnemySpawnManager (Weight field)

---

### "Spawned 0 enemies at X spawn points"

**Possible Causes**:

#### A. Spawn chance failed
- Increase `Enemy Spawn Chance` in spawn manager (try 1.0 for testing)

#### B. No valid prefabs at iteration
- Lower `Min Room Iteration` in profile to 0

#### C. Reached max per room
- Increase `Max Per Room` in profile

#### D. Weight is 0
- Set profile's `Spawn Weight` to 50+

---

## Quick Fix for Testing

Use these settings for guaranteed spawning:

### EnemyProfile:
```
Spawn Weight: 100
Min Room Iteration: 0
Max Per Room: 10
```

### EnemySpawnManager:
```
Base Enemies Per Room: 2
Enemy Spawn Chance: 1.0 (100%)
Weight: 0 (use profile)
```

### WeightedEnemyPrefab:
```
Enemy Prefab: [Your enemy GameObject]
Profile: [Your EnemyProfile]
Weight: 0 (will use profile's 100)
```

Then test in play mode. You should see spawns immediately.

---

## Debug Workflow

### Step 1: Enable Logs
- EnemySpawnManager ? Enable Debug Logs ?

### Step 2: Play Scene
- Watch console output

### Step 3: Read Messages
Look for these lines in order:

```
? "[EnemySpawnManager] Validated 1/1 enemy prefabs"
   ? Good! Profile is assigned

? "[EnemySpawnManager] Valid: BasicEnemy (Profile: Basic, Weight: 50, ...)"
   ? Good! Enemy is valid

? "[EnemySpawnManager] Attempting to spawn 2 enemies (iteration 1, ...)"
   ? Good! Spawn attempt started

? "[EnemySpawnManager] No valid enemy prefabs for iteration 1"
   ? BAD! Min Room Iteration is too high

? "[EnemySpawnManager] Spawned 0/2 enemies"
   ? BAD! Check spawn chance or weights
```

---

## Animation Parameters (Bonus)

If enemies spawn but don't animate:

### Required Animator Parameters:
```
IsMoving (Bool)
IsChasing (Bool)
Attack (Trigger)
Stagger (Trigger)
Death (Trigger)
Jump (Trigger) - FastEnemy only
```

Add these in Animator window ? Parameters tab

---

## Still Not Working?

### Check This:
1. ? NavMesh is baked on room floor
2. ? Enemy prefab has all required components:
   - NavMeshAgent
   - EnemyHealth
   - EnemyRoomTracker
   - BasicEnemy/ToughEnemy/FastEnemy
   - Animator
3. ? Spawn points are children of Room prefab
4. ? Room prefab has Room component
5. ? ProceduralLevelGenerator is in scene

### Test Pattern:
```
1. Create EnemyProfile (Right-click ? Create ? AI ? Enemy Profile)
2. Set: Weight=100, MinIteration=0, MaxPerRoom=5
3. Create enemy prefab with all components
4. Assign profile to enemy prefab's AI component
5. Add to EnemySpawnManager array
6. Add SpawnPoints to room prefab
7. Bake NavMesh
8. Play!
```

---

## Example Setup (Copy-Paste)

### In Inspector - EnemySpawnManager:

```
Enemy Prefabs: 1
  Element 0:
    Enemy Prefab: BasicEnemyPrefab
    Profile: BasicEnemyProfile
    Weight: 0

Base Enemies Per Room: 2
Enemies Per Iteration: 0.5
Max Enemies Per Room: 8
Enemy Spawn Chance: 1.0
Spawn Chance Increase Per Iteration: 0.0

Enable Debug Logs: ?
```

### In EnemyProfile Asset:

```
Enemy Name: Basic
Enemy Type: Basic
Spawn Weight: 100
Min Room Iteration: 0
Max Per Room: 5
```

This setup guarantees 2 enemies every room!

---

## Contact & Help

- Full docs: `Assets/Scripts/AI/README.md`
- Quick ref: `Assets/Scripts/AI/QUICK_REFERENCE.md`
- Presets: `Assets/Scripts/AI/ENEMY_PRESETS.md`

**Enable debug logs and read the console!** It tells you exactly what's wrong.
