# Enemy Chase Bug Fix

## Problem
Enemies were detecting the player (red gizmo arrows showing), but not moving to chase them. The NavMeshAgent appeared to be configured but enemies remained stationary.

## Root Causes Identified

### 1. **BasicEnemy - Uninitialized Delayed Position**
In `BasicEnemy.cs`, the `delayedPlayerPosition` was initialized to `transform.position` (the enemy's spawn location) and only updated via `Invoke()` with a delay. This meant:
- When aggro was gained, the enemy tried to move to its own position
- The first destination was never the player's actual position
- The delayed update system never kicked in properly

### 2. **NavMeshAgent State Management**
The NavMeshAgent's `isStopped` state wasn't properly validated when transitioning between states. Missing checks for:
- `navAgent.isActiveAndEnabled`
- `navAgent.isOnNavMesh`

### 3. **Missing Position Initialization on Aggro**
When `GainAggro()` was called, the player's position wasn't being captured immediately, leading to undefined behavior.

## Fixes Applied

### BasicEnemy.cs Changes

1. **Immediate Position Update on Chase Start**
   ```csharp
   protected override void OnStateEnter(EnemyState state)
   {
       if (state == EnemyState.Chasing)
       {
           // Immediately set delayed position when starting chase
           if (player)
           {
               delayedPlayerPosition = player.position;
               positionUpdateTimer = 0f;
           }
       }
   }
   ```

2. **Timer-Based Position Updates**
   Replaced `Invoke()` with a timer-based system in `UpdateChasing()`:
   ```csharp
   positionUpdateTimer += Time.deltaTime;
   if (positionUpdateTimer >= delayedPositionTime)
   {
       delayedPlayerPosition = player.position;
       positionUpdateTimer = 0f;
   }
   ```

3. **NavMeshAgent Validation**
   Added checks before setting destinations:
   ```csharp
   if (navAgent && navAgent.isActiveAndEnabled && navAgent.isOnNavMesh)
   {
       navAgent.SetDestination(delayedPlayerPosition);
   }
   ```

### EnemyAI.cs Changes

1. **Enhanced OnStateEnter()**
   Added comprehensive NavMeshAgent validation:
   ```csharp
   case EnemyState.Chasing:
       if (navAgent && navAgent.isActiveAndEnabled && navAgent.isOnNavMesh)
       {
           navAgent.isStopped = false;
           navAgent.speed = profile.chaseSpeed;
       }
       else
       {
           Debug.LogWarning("NavAgent issues...");
       }
   ```

2. **Improved UpdateChasing()**
   - Added NavMeshAgent validation before SetDestination
   - Added periodic debug logging (every 60 frames)
   - Better error messages when NavMeshAgent has issues

3. **Enhanced GainAggro()**
   ```csharp
   if (player)
   {
       lastKnownPlayerPosition = player.position;
   }
   ```
   Ensures the player's position is captured when aggro is gained.

4. **Better Start() Initialization**
   - Added NavMesh validation check
   - Attempts to warp enemy to NavMesh if not already on it
   - Explicitly enables NavMeshAgent
   - Better error logging for diagnostics
   - Improved player reference finding with null checks

## Testing Checklist

When testing, verify:

? Enemies spawn on NavMesh (check console logs)
? Enemies detect player (green gizmo line = visible)
? Enemies move toward player when aggro gained
? NavMeshAgent velocity > 0 when chasing
? Blue gizmo sphere shows delayed target position (BasicEnemy only)
? Enemies stop moving when attacking
? Enemies resume chase after attack recovery

## Debug Features Added

### Console Logs
- NavMeshAgent configuration at spawn
- State transitions with NavMeshAgent status
- Chase destination updates (once per second)
- NavMesh alignment warnings

### Visual Gizmos
- Red arrow: Direction to player (detection ray)
- Green line: Player is visible
- Blue sphere/line: Delayed target position (BasicEnemy)
- Cyan sphere: Last known player position (all enemies)

## Common Issues & Solutions

### Issue: "NavAgent issues: enabled=True, onNavMesh=False"
**Solution:** 
- Check if NavMesh is baked for the room
- Verify spawn points are close to NavMesh surface
- Enable "Auto Align to NavMesh" on SpawnPoint component

### Issue: Enemy moves but very slowly
**Solution:**
- Check EnemyProfile `chaseSpeed` value (should be 4+)
- Verify NavMeshAgent speed is being set correctly
- Check for NavMesh obstacles blocking path

### Issue: Enemy gains aggro but immediately loses it
**Solution:**
- Check `loseAggroTime` in EnemyProfile (should be 3+ seconds)
- Verify room tracking is working (EnemyRoomTracker component)
- Check player is in same room/corridor as enemy

## Performance Considerations

- Debug logs only print once per second (every 60 frames) to reduce spam
- NavMesh validation checks use short-circuit evaluation
- Position updates use timer instead of repeated Invoke() calls

## Related Files
- `Assets/Scripts/AI/Core/EnemyAI.cs`
- `Assets/Scripts/AI/EnemyTypes/BasicEnemy.cs`
- `Assets/Scripts/AI/Core/EnemyProfile.cs`
- `Assets/Scripts/AI/Spawning/SpawnPoint.cs`
