# Enhanced Enemy AI System

## Overview
This is a complete revamp of the enemy AI system featuring:
- **ScriptableObject-based configuration** for easy enemy creation
- **Three distinct enemy types** with unique behaviors
- **Pain chance/stagger system** like Doom/Quake
- **Dual-radius aggro system** 
- **Integration with procedural level generation**
- **Custom editor window** for designers

## Enemy Types

### 1. Basic Enemy (Soldier)
- **Behavior**: Chases player with delayed memory (sees where player was 1 second ago)
- **Attack**: Bayonet thrust (melee)
- **Movement**: Cannot move while attacking
- **Rest Period**: Brief pause after attacking before resuming chase

### 2. Tough Enemy (Shotgunner)
- **Behavior**: Walks directly towards player
- **Attack**: Shotgun with cone-based damage
- **Aiming**: Can rotate to face player while aiming
- **Range**: Short-range cone attack (not projectiles)

### 3. Fast Enemy (Assassin)
- **Behavior**: 
  - **Close range**: Quick melee stab
  - **Medium range** (5-12m): Predicts player movement and jumps to intercept
  - **Far range**: Walks towards player
- **Attack**: Fast stab when in melee range
- **Special**: Jump attack with player prediction

## Aggro System

Each enemy has **two aggro radiuses**:

1. **Instant Aggro Radius** (smaller, red): Enemy immediately chases player
2. **Delayed Aggro Radius** (larger, yellow): Enemy chases after configurable delay (default 2s)

Aggro is **restricted to current room/corridor** - enemies won't chase through walls.

## Pain Chance / Stagger System

When enemies take damage:
- Random roll against `staggerChance` (0-1)
- If successful, enemy enters **stagger state**
- During stagger:
  - All animations interrupted
  - Cannot move or attack
  - Plays stagger animation
  - Brief stun duration

This creates Doom/Quake-like gameplay where well-placed shots interrupt enemy attacks.

## Spawning System

### Spawn Points
- Place `SpawnPoint` components in your rooms/corridors
- Types: Enemy, Prop, or Both
- Visualized with gizmos in editor

### Spawn Manager (`EnemySpawnManager`)
Handles:
- Enemy spawning based on iteration (difficulty scaling)
- Weighted random enemy selection
- Minimum iteration requirements per enemy type
- Prop spawning at dedicated points

### Difficulty Scaling
- Base enemies per room increases with iteration
- Enemy spawn chance increases slightly per iteration  
- More dangerous enemies unlock at higher iterations (configured per enemy)

## Integration with Procedural Generation

### LevelPiece Integration
Both `Room` and `Corridor` classes call spawn manager automatically:
```csharp
spawnManager.SpawnEnemiesAtPoints(spawnPoints, isRoom, currentIteration);
```

### ProceduralLevelGenerator
- Tracks `currentRoomIteration` 
- Passes iteration to spawn manager
- Enemies become progressively harder

## Animation Requirements

### Required Animator Parameters
All defined in `GameConstant.AnimationParameters`:
- `IsMoving` (bool): Movement active
- `IsChasing` (bool): Chasing player
- `Attack` (trigger): Basic attack
- `Shoot` (trigger): Ranged attack
- `Stagger` (trigger): Stagger/pain animation
- `Death` (trigger): Death animation
- `Jump` (trigger): Jump attack (Fast enemy)
- `Aiming` (bool): Aiming shotgun (Tough enemy)
- `MeleeAttack` (trigger): Melee stab (Fast enemy)

### Animation Events
For proper attack timing:
- `EnableAttackCollider()` - Enable hitbox
- `DisableAttackCollider()` - Disable hitbox
- `OnAttackComplete()` - Signal attack finished

## Using the Editor Window

### Opening the Window
`Tools > AI > Enemy Config Creator`

### Creating a New Enemy
1. Click "Create New"
2. Configure all settings:
   - **Identity**: Name, type, prefab
   - **Stats**: Health, damage, speed
 - **Aggro**: Instant/delayed radiuses
   - **Combat**: Attack range, cooldown
   - **Stagger**: Chance and duration
   - **Spawning**: Weight and min iteration
   - **Type-Specific**: Varies by enemy type
3. Click "Save"

### Quick Templates
`Tools > AI > Create Template Enemy Configs`

Creates three ready-to-use configs:
- BasicEnemy_Template
- ToughEnemy_Template  
- FastEnemy_Template

Located in: `Assets/ScriptableObjects/EnemyConfigs/`

## Setup Instructions

### 1. Create Enemy Configurations
```
Tools > AI > Create Template Enemy Configs
```

### 2. Assign Enemy Prefabs
For each config:
- Open in Inspector
- Assign `enemyPrefab` field
- Configure stats as needed

### 3. Setup Spawn Manager
1. Create empty GameObject: "SpawnManager"
2. Add `EnemySpawnManager` component
3. Assign all enemy configs to list
4. Configure spawn settings
5. Optionally add prop prefabs

### 4. Add Spawn Points to Rooms
1. Create empty GameObjects as children of Room/Corridor prefabs
2. Add `SpawnPoint` component
3. Set spawn type (Enemy/Prop/Both)
4. Position where you want enemies to appear
5. Face forward direction (blue line gizmo)

### 5. Setup Enemy Prefabs
Each enemy prefab needs:
- `EnemyController` component
- `NavMeshAgent` component
- `Animator` component  
- `Collider` for body
- `Collider` for attacks (assign to `attackCollider` field)
- Assigned `EnemyConfigSO` reference

### 6. Configure Animations
Setup animator with:
- All required parameters (see Animation Requirements)
- Animation events for attack timing
- Proper transitions between states

## Optimization Features

### Object Pooling
- Enemies are pooled per configuration
- Pre-instantiates 3 of each type at start
- Creates more as needed
- Reuses inactive enemies

### NavMesh Integration
- Uses Unity's NavMeshAgent for pathfinding
- Automatic obstacle avoidance
- Dynamic NavMesh carving support

### State Machine Pattern
- Clean separation of concerns
- Easy to add new states
- Optimized state transitions
- No Update() overhead in state classes

## Debugging

### Gizmos
- **Red sphere**: Instant aggro radius
- **Yellow sphere**: Delayed aggro radius  
- **Blue sphere**: Attack range
- **Red/Green spheres**: Spawn points

### Debug Logs
Enable in `ProceduralLevelGenerator`:
```csharp
enableDebugLogs = true
```

Shows:
- Room generation
- Enemy spawning
- State transitions

## Migration from Old System

### Old Files (Can be deleted after migration)
- `Enemy.cs` (replaced by `EnemyController.cs`)
- `EnemyStateManager.cs` (functionality moved to `EnemyController.cs`)
- `Enemies.cs` (replaced by `EnemyConfigSO.cs`)
- `EnemyManager.cs` (replaced by `EnemySpawnManager.cs`)
- `EnemyPooling.cs` (pooling now in `EnemySpawnManager.cs`)
- Old state files in `Enemy State/` folder

### What to Keep
- `Projectile.cs` (still used for ranged enemies)
- `ProjectilePooling.cs` (still used for ranged attacks)
- Animation scripts if any

## Extending the System

### Adding a New Enemy Type
1. Add enum value to `EnemyType` in `EnemyConfigSO.cs`
2. Create new state class inheriting `IEnemyState`
3. Add logic in `EnemyChaseStateNew.UpdateState()` for type
4. Add type-specific config properties
5. Update editor window to show new properties

### Adding New Behaviors
Create new state class:
```csharp
public class MyNewState : IEnemyState
{
    public void EnterState(EnemyController enemy) { }
    public void UpdateState(EnemyController enemy) { }
    public void ExitState(EnemyController enemy) { }
}
```

### Custom Attack Patterns
Override in specific enemy type's attack state:
```csharp
private void ExecuteCustomAttack(EnemyController enemy)
{
    // Your attack logic here
}
```

## Performance Considerations

- Enemies only update when active
- Aggro checks optimized with radius-based culling
- State machine prevents unnecessary calculations
- Pooling reduces instantiation overhead
- NavMesh handles pathfinding efficiently

## Troubleshooting

### Enemies not spawning
- Check spawn points are active
- Verify enemy configs are assigned in spawn manager
- Check min iteration requirements
- Enable debug logs

### Enemies not chasing player
- Ensure player has "Player" tag
- Check aggro radius settings
- Verify NavMesh is baked
- Check if enemy is in stagger/dead state

### Animations not playing
- Verify animator parameters exist
- Check state transitions
- Ensure animator controller is assigned
- Check animation event callbacks

### Stagger not working
- Ensure `staggerChance` > 0
- Verify `TakeDamage()` is being called
- Check stagger animation exists
- Enable debug logs to see damage events

## Credits
Built with Unity NavMesh, ScriptableObjects, and State Machine pattern.
Inspired by classic FPS games like Doom, Quake, and modern roguelikes.
