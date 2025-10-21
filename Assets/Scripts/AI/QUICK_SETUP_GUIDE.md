# Quick Setup Guide - Enhanced Enemy AI System

## ? Quick Start (5 Minutes)

### Step 1: Create Template Configs
1. In Unity menu: `Tools > AI > Create Template Enemy Configs`
2. This creates 3 ready-to-use configs in `Assets/ScriptableObjects/EnemyConfigs/`

### Step 2: Setup Spawn Manager
1. Find or create "GameManager" GameObject in scene
2. Add Component: `EnemySpawnManager`
3. In Inspector:
   - Drag all 3 template configs into "Enemy Configurations" list
   - Set "Base Enemies Per Room": 3
   - Set "Enemies Per Iteration": 0.5
   - (Optional) Add prop prefabs

### Step 3: Open Enemy Config Creator
1. Menu: `Tools > AI > Enemy Config Creator`
2. Select each template config from Project window
3. Assign enemy prefabs in the editor window
4. Adjust values as needed
5. Click "Save"

### Step 4: Add Spawn Points to Your Rooms
1. Open a Room prefab
2. Create empty GameObject as child: "SpawnPoints"
3. Create children under SpawnPoints:
   - "Spawn_Enemy_01", "Spawn_Enemy_02", etc.
4. Add `SpawnPoint` component to each
5. Set Type to "Enemy"
6. Position them where enemies should appear

### Step 5: Setup Enemy Prefabs
For each enemy prefab:
1. Add `EnemyController` component
2. Assign:
   - Config: The corresponding template config
   - Agent: NavMeshAgent component
   - Animator: Animator component
   - Attack Collider: Melee hitbox collider
   - (Optional) Projectile Spawn Point for ranged enemies
3. Setup Animator Controller with required parameters
4. Add animation events for attacks

## ?? Required Components Checklist

### Enemy Prefab Must Have:
- ? EnemyController
- ? NavMeshAgent
- ? Animator
- ? Collider (body)
- ? Collider (attack hitbox)
- ? Rigidbody (if using physics)

### Room/Corridor Prefabs Must Have:
- ? Multiple SpawnPoint components
- ? ConnectionPoint A (entrance)
- ? ConnectionPoint B (exit)
- ? NavMesh baked or NavMesh Surface

### Scene Setup:
- ? Player GameObject with "Player" tag
- ? EnemySpawnManager in scene
- ? ProceduralLevelGenerator configured
- ? NavMesh baked

## ?? Testing Your Setup

### Test 1: Spawn in Editor
1. Open a room prefab in prefab mode
2. Place an enemy prefab in scene
3. Enter Play mode
4. Enemy should:
   - Stand idle initially
   - Chase when you get close
   - Attack when in range

### Test 2: Procedural Spawning
1. Start game from starting room
2. Progress through rooms
3. Enemies should spawn automatically
4. Check console for spawn logs

### Test 3: Combat
1. Shoot enemies
2. Check for stagger animation (30% chance)
3. Verify death animation plays
4. Confirm enemy is destroyed after death

## ?? Quick Troubleshooting

| Problem | Solution |
|---------|----------|
| Enemies not spawning | Check spawn manager has configs assigned |
| Enemies not moving | Verify NavMesh is baked, check agent settings |
| Enemies not attacking | Check attack range in config, verify animator |
| No stagger | Set staggerChance > 0 in config |
| Compilation errors | Ensure all new files are imported |

## ?? File Structure

```
Assets/
??? Scripts/
?   ??? AI/
?       ??? Enemy/
?       ?   ??? Configuration/
??   ?   ??? EnemyConfigSO.cs ?NEW
?       ?   ??? States/
?       ?   ?   ??? IEnemyState.cs ?NEW
?   ?   ?   ??? EnemyStates.cs ?NEW
?       ?   ??? Editor/
?       ?   ?   ??? EnemyConfigEditorWindow.cs ?NEW
?       ?   ?   ??? EnemyConfigTemplateCreator.cs ?NEW
?       ?   ??? EnemyController.cs ?NEW
?    ??? Spawning/
?       ? ??? SpawnPoint.cs ?NEW
?       ?   ??? EnemySpawnManager.cs ?NEW
?   ??? README_ENEMY_AI.md ?NEW
?
??? ScriptableObjects/
    ??? EnemyConfigs/
        ??? BasicEnemy_Template.asset ?NEW
        ??? ToughEnemy_Template.asset ?NEW
     ??? FastEnemy_Template.asset ?NEW
```

## ?? Next Steps

### Customize Enemies
1. Open Enemy Config Creator
2. Select a config
3. Adjust values:
   - Increase health for harder enemies
   - Adjust aggro radiuses for sneaking gameplay
   - Tweak stagger chance for difficulty
   - Set spawn weights for rarity

### Create New Enemy Types
1. Duplicate an existing config
2. Rename it
3. Adjust all values
4. Create new prefab
5. Assign to spawn manager

### Balance Difficulty
1. Play through several rooms
2. Note enemy counts and types
3. Adjust spawn manager settings:
- `baseEnemiesPerRoom`
   - `enemiesPerIteration`
   - `maxEnemiesPerRoom`
4. Adjust individual enemy configs:
   - `spawnWeight`
   - `minRoomIteration`

## ?? Pro Tips

1. **Use Templates**: Always start from template configs
2. **Test in Isolation**: Test single enemies before mass spawning
3. **Watch Gizmos**: Select enemies in editor to see aggro ranges
4. **Enable Logs**: Turn on debug logs during development
5. **Iterate Quickly**: Use hot reload - change configs while game runs
6. **Balance Last**: Get functionality working first, balance after

## ?? Additional Resources

- Full documentation: `Assets/Scripts/AI/README_ENEMY_AI.md`
- Enemy Types reference: See "Enemy Types" section in README
- Animation setup: See "Animation Requirements" in README
- Debugging guide: See "Debugging" section in README

## ? Need Help?

Common questions:

**Q: Where do I start?**
A: Follow the Quick Start above. Create templates first!

**Q: My animations aren't working**
A: Check animator has all required parameters from GameConstant.AnimationParameters

**Q: Enemies spawn but don't move**
A: Bake NavMesh or add NavMesh Surface component

**Q: How do I make enemies harder?**
A: Increase health, damage, and aggro radius. Decrease stagger chance.

**Q: Can I have custom enemy types?**
A: Yes! See "Extending the System" in main README

---

**You're ready to go!** ??

Start with the Quick Start steps above, then refer to the full README for advanced features.
