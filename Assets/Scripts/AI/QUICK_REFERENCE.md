# Enemy AI Quick Reference Card

## ?? Quick Start (5 Minutes)

### 1. Setup Wizard
`Tools ? AI ? Enemy Setup Wizard`

Follow steps:
1. Create profile
2. Setup prefab  
3. Add spawn points
4. Configure manager
5. Done!

---

## ?? Enemy Types at a Glance

| Type | Health | Speed | Behavior | Best For |
|------|--------|-------|----------|----------|
| **Basic** | 80 | Medium | Chases old position, bayonet thrust | Early game |
| **Tough** | 150 | Slow | Direct chase, shotgun blast | Mid-late game |
| **Fast** | 60 | Fast | Jump predict attack, quick stab | Late game |

---

## ?? Key Stats

### Aggro Ranges
```
Instant Aggro: 5-10 units  (immediate chase)
Delayed Aggro: 10-20 units (chase after delay)
Aggro Delay: 1-2 seconds
```

### Pain Chance (Stagger)
```
Low:    0.1-0.2  (tanky enemies)
Medium: 0.3-0.4  (standard)
High:   0.5-0.7  (weak enemies)
```

### Attack Cooldown
```
Fast:   0.8-1.2s (relentless)
Normal: 1.5-2.0s (balanced)
Slow:   2.5-3.0s (heavy hitters)
```

---

## ?? Difficulty Scaling

### Spawn Settings
```
Base Enemies Per Room: 2
Enemies Per Iteration: +0.5
Max Enemies Per Room: 8
Spawn Chance Increase: +5% per iteration
```

### Iteration Tiers
```
Iteration 0-2:  Easy   (2-3 enemies, Basic heavy)
Iteration 3-5:  Medium (3-4 enemies, mixed)
Iteration 6+:   Hard   (5-6 enemies, Fast/Tough)
```

### Weight Examples
```
Early (0-2):  Basic 70, Tough 30, Fast 0
Mid (3-5):    Basic 50, Tough 40, Fast 10  
Late (6+):    Basic 30, Tough 40, Fast 30
```

---

## ??? Required Components

### On Enemy Prefab
- ? NavMeshAgent
- ? Animator
- ? AudioSource
- ? EnemyHealth
- ? EnemyRoomTracker
- ? BasicEnemy / ToughEnemy / FastEnemy

### In Scene
- ? EnemySpawnManager (auto-created)
- ? NavMesh baked on floors

---

## ?? Animation Parameters

```csharp
IsMoving    (Bool)     // Idle ? Walk
IsChasing   (Bool)     // Walk ? Run
Attack      (Trigger)  // Attack animation
Stagger     (Trigger)  // Pain/hit reaction
Death       (Trigger)  // Death animation
Jump        (Trigger)  // Jump (FastEnemy only)
```

### Animation Events
Add to attack animation:
- `DealDamage` at hit frame
- `EndAttack` at final frame

---

## ?? Spawn Points

### Setup
1. Create empty GameObject in room
2. Add `SpawnPoint` component
3. Set type (Enemy/Prop)
4. Position and rotate
5. Done! (Auto-detected by room)

### Gizmos
- **Green sphere**: Enemy spawn
- **Blue arrow**: Forward direction

---

## ?? Common Tweaks

### Make Easier
- ?? Reduce `Enemies Per Iteration` (0.3)
- ?? Reduce `Spawn Chance Increase` (0.03)
- ?? Lower enemy health in profiles

### Make Harder
- ?? Increase `Base Enemies Per Room` (3)
- ?? Increase `Spawn Chance Increase` (0.1)
- ?? Add more Tough/Fast enemies early

### Reduce Stagger Spam
- ?? Lower `Pain Chance` (0.2)
- ?? Increase stagger cooldown (harder to implement)

---

## ?? Debugging

### Enable Debug Logs
Check boxes in Inspector:
- `EnemyAI ? Enable Debug Logs`
- `EnemySpawnManager ? Enable Debug Logs`

### Gizmos (Select Enemy in Scene)
- Yellow circle: Instant aggro
- Orange circle: Delayed aggro  
- Red circle: Attack range
- Green line: Can see player
- Cyan dot: Last known position

### Console Commands
Watch for:
- `[EnemyAI] State changed`
- `[EnemyHealth] Took damage`
- `[EnemySpawnManager] Spawned X enemies`

---

## ?? Pro Tips

1. **NavMesh**: Bake before testing!
2. **Profiles**: Create variants for iterations (e.g., BasicEnemy_Tough)
3. **Weights**: Sum to 100 for easy percentages
4. **Spawn Points**: 4-6 per room recommended
5. **Testing**: Use `currentRoomIteration` field to skip ahead

---

## ?? Files to Edit

### For New Enemy Variant
1. Duplicate `EnemyProfile` ? Modify stats
2. Assign to existing prefab OR create new
3. Add to `EnemySpawnManager` array

### For New Room
1. Add `SpawnPoint` components
2. Bake NavMesh
3. Test spawn in play mode

---

## ?? Troubleshooting

| Problem | Solution |
|---------|----------|
| Enemies not spawning | Check NavMesh, spawn manager, spawn points |
| Enemies stuck | Verify NavMesh coverage, obstacles |
| Too many enemies | Adjust spawn chance, max per room |
| Enemies don't attack | Check attack range, animation events |
| Stagger not working | Verify pain chance > 0, animation parameter |

---

## ?? Full Documentation
See `Assets/Scripts/AI/README.md` for complete guide

---

**Hotkeys:**
- `Tools ? AI ? Enemy Setup Wizard` (full setup)
- `Create ? AI ? Enemy Profile` (quick profile)

**Questions?** Check console with debug logs enabled!
