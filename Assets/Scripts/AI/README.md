# Enemy AI System - Complete Guide

## Overview
This is a complete, designer-friendly enemy AI system integrated with the procedural level generator. It features three enemy types, dynamic spawning with difficulty scaling, NavMesh pathfinding, and room-based aggro containment.

---

## Table of Contents
1. [Core Components](#core-components)
2. [Enemy Types](#enemy-types)
3. [Spawning System](#spawning-system)
4. [Integration with Procedural Generation](#integration-with-procedural-generation)
5. [Setup Guide](#setup-guide)
6. [Designer Workflow](#designer-workflow)
7. [Animation Setup](#animation-setup)
8. [Balancing Guide](#balancing-guide)

---

## Core Components

### 1. **EnemyHealth**
- Handles health, damage, death, and stagger system
- Implements `IDamageable` interface (integrates with weapon system)
- **Pain Chance System**: Enemies have a chance to get staggered when hit (like Doom/Quake)
- **Ragdoll Support**: Enemies can ragdoll on death
- **Events**: `OnDamaged`, `OnStaggered`, `OnDeath` for easy hooking

**Key Settings:**
- `Max Health`: Total HP
- `Pain Chance`: Probability of stagger (0-1)
- `Stagger Duration`: How long stagger lasts
- `Use Ragdoll`: Enable physics-based death

---

### 2. **EnemyProfile** (ScriptableObject)
Designer-friendly configuration for all enemy stats.

**Categories:**
- **Health & Defense**: HP, pain chance, stagger duration
- **Movement**: Patrol speed, chase speed, rotation speed
- **Detection & Aggro**: 
  - `Instant Aggro Range`: Immediate chase radius
  - `Delayed Aggro Range`: Delayed chase radius
  - `Aggro Delay`: Time before chasing in delayed range
  - `Vision Angle`: Field of view cone
  - `Lose Aggro Time`: Time to forget player
- **Attack**: Damage, range, cooldown, duration
- **Spawning**: Weight, min iteration, max per room
- **Audio/VFX**: Sound effects and particle prefabs

**How to Create:**
1. Right-click in Project ? `Create > AI > Enemy Profile`
2. Configure all stats
3. Assign to enemy prefab

---

### 3. **EnemyAI** (Base Class)
Core AI state machine. All enemy types inherit from this.

**States:**
- `Idle`: Standing still
- `Patrol`: Walking between waypoints (override in derived classes)
- `Chasing`: Pursuing player
- `Attacking`: Performing attack
- `Staggered`: Stunned from pain chance
- `Dead`: Defeated

**Key Features:**
- NavMesh-based pathfinding
- Room/corridor containment (enemies only aggro in same room)
- Two-tier aggro system (instant + delayed)
- Vision cone detection with raycasting
- Automatic animation parameter updates

---

### 4. **EnemyRoomTracker**
Tracks which room/corridor an enemy is in. Restricts aggro to current level piece only.

**Features:**
- Auto-detects current room/corridor on spawn
- Checks if player is in same level piece
- Integrates with `ProceduralGeneration` system

---

## Enemy Types

### 1. **BasicEnemy**
**Behavior:**
- Sees where player *was* 1 second ago and goes there
- When close, performs bayonet thrust
- Cannot move during attack
- Takes time to recover after attack

**Unique Settings:**
- `Delayed Position Time`: How old the position to chase (default 1s)
- `Attack Windup Time`: Time before thrust
- `Thrust Distance`: Bayonet reach

**Best For:** Early game, slower players

---

### 2. **ToughEnemy**
**Behavior:**
- Walks **directly** towards player (no delay)
- When in range, aims shotgun (can rotate while aiming)
- Fires shotgun in cone (multiple raycasts, not projectiles)
- Tankier, more dangerous

**Unique Settings:**
- `Shotgun Aim Time`: Time to aim before firing
- `Shotgun Cone Angle`: Spread angle
- `Shotgun Range`: Effective range
- `Shotgun Pellet Count`: Number of raycasts
- `Shotgun Damage Per Pellet`: Damage per hit

**Best For:** Mid-game, high threat rooms

---

### 3. **FastEnemy**
**Behavior:**
- Walks normally when close or far
- When in **medium range**, predicts player position and **jumps** there
- When in melee range, performs quick stab

**Unique Settings:**
- `Medium Range Min/Max`: Distance range for jump attack
- `Jump Height`: Arc height
- `Jump Duration`: Jump animation time
- `Jump Prediction Multiplier`: How far ahead to predict
- `Jump Cooldown`: Time between jumps
- `Stab Windup Time`: Quick stab delay
- `Stab Range`: Melee range

**Best For:** Late game, agile threat

---

## Spawning System

### 1. **SpawnPoint**
Place in rooms/corridors to define spawn locations.

**Types:**
- `Enemy`: Spawns enemies
- `Prop`: Spawns props/decorations

**Settings:**
- `Spawn On Start`: Auto-spawn immediately
- `Spawn Only Once`: Prevent respawning
- `Specific Enemy Prefabs`: Override spawn pool

**Gizmos:**
- Green sphere: Enemy spawn
- Blue arrow: Forward direction

---

### 2. **WeightedEnemyPrefab**
Configuration for enemy spawn chances.

**Settings:**
- `Enemy Prefab`: Prefab to spawn
- `Profile`: Enemy profile reference
- `Weight`: Spawn probability (higher = more common)
- `Min Room Iteration`: When this enemy starts appearing
- `Max Per Room`: Cap per room

**Example:**
```
Basic Enemy: Weight 60, Min Iteration 0, Max 3
Tough Enemy: Weight 30, Min Iteration 2, Max 2
Fast Enemy: Weight 10, Min Iteration 4, Max 1
```

---

### 3. **EnemySpawnManager**
Manages all enemy spawning with difficulty scaling.

**Difficulty Scaling:**
- `Base Enemies Per Room`: Starting count
- `Enemies Per Iteration`: Increase per room (+0.5 recommended)
- `Max Enemies Per Room`: Cap
- `Enemy Spawn Chance`: Base probability
- `Spawn Chance Increase Per Iteration`: Gradual increase (+0.05 recommended)

**Corridor Settings:**
- `Corridor Spawn Chance`: Lower than rooms
- `Max Enemies Per Corridor`: Usually 1-2

**How it Works:**
1. ProceduralLevelGenerator spawns room/corridor
2. Calls `Room.OnSpawned(iteration)`
3. Room finds spawn points
4. Spawn manager calculates enemy count based on iteration
5. Randomly selects enemies from weighted pool
6. Spawns enemies at spawn points

---

## Integration with Procedural Generation

### Updated Classes:

#### **LevelPiece**
- Added `SpawnPoint[] spawnPoints` field
- Added `OnSpawned(int roomIteration)` virtual method
- Auto-finds spawn points in children

#### **Room**
- Implements `OnSpawned()` to trigger enemy/prop spawning
- Calls `EnemySpawnManager.SpawnEnemiesAtPoints()`

#### **Corridor**
- Same as Room, but with corridor-specific spawn logic

#### **ProceduralLevelGenerator**
- Tracks `currentRoomIteration` for difficulty scaling
- Updates spawn manager on each new section
- Calls `OnSpawned()` when spawning rooms/corridors

---

## Setup Guide

### Step 1: Create Enemy Profiles
1. `Create > AI > Enemy Profile`
2. Name it (e.g., "BasicEnemy_Profile")
3. Configure all stats (health, speed, aggro ranges, etc.)
4. Save

### Step 2: Create Enemy Prefabs
1. Create GameObject with model
2. Add components:
   - `NavMeshAgent`
   - `Animator`
   - `AudioSource`
   - `EnemyHealth`
   - `EnemyRoomTracker`
   - One of: `BasicEnemy`, `ToughEnemy`, `FastEnemy`
3. Assign `EnemyProfile` to AI component
4. Configure ragdoll (optional):
   - Add Rigidbodies to limbs
   - Add Colliders to limbs
   - Mark as kinematic initially
5. Save as prefab

### Step 3: Setup NavMesh
1. Select all walkable surfaces in scene
2. Mark as `Navigation Static`
3. Window ? AI ? Navigation
4. Bake NavMesh
5. Repeat for each room/corridor prefab

### Step 4: Add Spawn Points to Rooms
1. Open room prefab
2. Create empty GameObjects as children
3. Add `SpawnPoint` component
4. Set type (Enemy/Prop)
5. Position and rotate (blue arrow = forward)
6. Spawn points auto-detected by `LevelPiece`

### Step 5: Configure Spawn Manager
1. Create GameObject in scene: "EnemySpawnManager"
2. Add `EnemySpawnManager` component
3. Configure `WeightedEnemyPrefab[]`:
   - Assign enemy prefabs
   - Set weights
   - Set min iterations
   - Set max per room
4. Configure difficulty scaling settings
5. Save as prefab (optional for DontDestroyOnLoad)

### Step 6: Test
1. Play scene
2. Walk to room exits to generate new sections
3. Observe enemy spawning
4. Check iteration count increases
5. Verify aggro ranges and behavior

---

## Designer Workflow

### Creating a New Enemy Variant:
1. Duplicate existing enemy profile
2. Modify stats (increase health, reduce pain chance, etc.)
3. Create new prefab variant (optional different model)
4. Assign new profile
5. Add to `EnemySpawnManager` weighted list
6. Set appropriate min iteration (e.g., 5 for late-game variant)

### Balancing Enemy Spawns:
**Easy Room (Iteration 0-2):**
- 2-3 Basic Enemies
- Weight: Basic 70, Tough 30, Fast 0

**Medium Room (Iteration 3-5):**
- 3-4 Enemies (mix)
- Weight: Basic 50, Tough 40, Fast 10

**Hard Room (Iteration 6+):**
- 5-6 Enemies
- Weight: Basic 30, Tough 40, Fast 30

### Adjusting Difficulty Curve:
- **Easier:** Reduce `Enemies Per Iteration` (0.3 instead of 0.5)
- **Harder:** Increase `Spawn Chance Increase Per Iteration` (0.1 instead of 0.05)
- **Spiky:** Reduce `Base Enemies Per Room` but increase `Enemies Per Iteration`

---

## Animation Setup

### Required Animation Parameters:
- `IsMoving` (Bool): Idle ? Walk
- `IsChasing` (Bool): Walk ? Run
- `Attack` (Trigger): Attack animation
- `Stagger` (Trigger): Pain/stagger animation
- `Death` (Trigger): Death animation
- `Jump` (Trigger): Jump animation (FastEnemy only)

### Animation Events:
Add events to attack animations:
- `DealDamage` at hit frame
- `EndAttack` at end frame

Example:
```csharp
// In attack animation, add event at frame 15:
// Function: DealDamage
// Function: EndAttack (at final frame)
```

### Animator Controller Setup:
1. Create states: Idle, Walk, Run, Attack, Stagger, Death, Jump
2. Add parameters listed above
3. Create transitions:
   - Idle ? Walk (IsMoving = true)
   - Walk ? Idle (IsMoving = false)
   - Walk ? Run (IsChasing = true)
   - Any State ? Attack (Attack trigger)
   - Any State ? Stagger (Stagger trigger)
   - Any State ? Death (Death trigger)

---

## Balancing Guide

### Pain Chance:
- **Low (0.1-0.2)**: Tanky enemies, less interruption
- **Medium (0.3-0.4)**: Standard enemies
- **High (0.5-0.7)**: Weak enemies, easily staggered

### Aggro Ranges:
- **Instant**: 5-10 units (close encounters)
- **Delayed**: 10-20 units (spotted from distance)
- **Aggro Delay**: 1-2 seconds (reaction time)

### Attack Cooldown:
- **Fast**: 0.8-1.2s (relentless)
- **Normal**: 1.5-2s (balanced)
- **Slow**: 2.5-3s (heavy hitters)

### Health Scaling:
- **Iteration 0-2**: 50-100 HP
- **Iteration 3-5**: 100-150 HP
- **Iteration 6+**: 150-250 HP

*Use EnemyProfile variants for each iteration tier*

---

## Debug Features

### Gizmos:
- **Yellow Sphere**: Instant aggro range
- **Orange Sphere**: Delayed aggro range
- **Red Sphere**: Attack range
- **Green/Red Line**: Vision ray (green = can see player)
- **Cyan Sphere**: Last known player position
- **Blue Sphere**: Delayed chase position (BasicEnemy)
- **Yellow Cone**: Shotgun spread (ToughEnemy)
- **Green Arc**: Jump trajectory (FastEnemy)

### Console Logs:
Enable `enableDebugLogs` in components for detailed logging:
- State changes
- Aggro gain/loss
- Attack triggers
- Damage dealt
- Spawn events

---

## Performance Optimization

### Recommended Settings:
- **Max Enemies Per Room**: 6-8 (depending on target hardware)
- **NavMesh Baking**: Moderate quality (balance accuracy vs. performance)
- **Vision Raycast**: Only check when in aggro range (already implemented)
- **Animation**: Use root motion for movement (optional)

### Object Pooling (Future):
Consider implementing enemy pooling if spawning/despawning many enemies rapidly.

---

## Common Issues & Solutions

### Issue: Enemies not spawning
**Solution:** 
- Check NavMesh is baked on room floors
- Verify spawn points are inside room bounds
- Check `EnemySpawnManager` has enemy prefabs assigned

### Issue: Enemies aggro through walls
**Solution:**
- Ensure line-of-sight raycasting is enabled
- Check room colliders are not marked as triggers

### Issue: Enemies stuck in place
**Solution:**
- Verify NavMeshAgent is enabled
- Check NavMesh obstacles aren't blocking
- Ensure room has walkable NavMesh surface

### Issue: Too many/few enemies
**Solution:**
- Adjust `Enemy Spawn Chance` in spawn manager
- Modify `Enemies Per Iteration` scaling
- Check spawn point count in rooms

---

## Future Enhancements

Possible additions:
- **Patrol Waypoints**: Add waypoint system for idle patrol
- **Squad Behavior**: Enemies coordinate attacks
- **Special Attacks**: Boss-specific abilities
- **Death Drops**: Loot system
- **Audio Director**: Dynamic music based on combat
- **AI Director**: L4D-style pacing system

---

## File Structure

```
Assets/Scripts/AI/
??? Core/
?   ??? EnemyAI.cs              (Base AI state machine)
?   ??? EnemyHealth.cs          (Health & damage system)
?   ??? EnemyProfile.cs         (ScriptableObject stats)
?   ??? EnemyRoomTracker.cs     (Room containment)
??? EnemyTypes/
?   ??? BasicEnemy.cs           (Bayonet enemy)
?   ??? ToughEnemy.cs           (Shotgun enemy)
?   ??? FastEnemy.cs            (Jump enemy)
??? Spawning/
    ??? SpawnPoint.cs           (Spawn marker)
    ??? EnemySpawnManager.cs    (Spawn orchestrator)
    ??? WeightedEnemyPrefab.cs  (Spawn configuration)
```

---

## Credits
- **Architecture**: Modular OOP design with ScriptableObjects
- **Integration**: Seamless with existing `ProceduralGeneration` system
- **Designer-Friendly**: Inspector-based workflow, no code required for variants

---

**Questions?** Check console logs with `enableDebugLogs = true`
