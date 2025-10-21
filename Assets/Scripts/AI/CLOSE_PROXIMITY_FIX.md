# Close Proximity Awareness Fix - Quick Reference

## Problem
Enemy runs toward delayed position (1s old), **runs past stationary player**, player ends up behind enemy (175°), enemy loses aggro despite player being 1-2m away.

## Solution Summary

### 1. Close Proximity Detection (2.5m) - `EnemyAI.cs`
```csharp
// Enemy detects player within 2.5m even without line of sight
float closeProximityRange = 2.5f;
if (distanceToPlayer <= closeProximityRange)
{
  GainAggro(); // Maintain/regain aggro regardless of vision
}
```
**Why**: Simulates hearing/awareness - enemy should know when player is right next to them.

### 2. Immediate Position Tracking (3m) - `BasicEnemy.cs`
```csharp
// When player is very close, track actual position instead of delayed
float closeProximityRange = 3f;
bool playerIsVeryClose = distanceToPlayer <= closeProximityRange;

if (playerIsVeryClose)
{
 delayedPlayerPosition = player.position; // Immediate tracking!
}
```
**Why**: Prevents overshooting stationary player. Enemy is more accurate at close range.

### 3. Attack with Proximity Override - `BasicEnemy.cs`
```csharp
// Can attack if see player OR player is very close
if (distanceToPlayer <= effectiveAttackRange && 
    hasStoppedMoving && 
    hasBeenChasingLongEnough && 
    (canSeePlayerNow || playerIsVeryClose) &&  // Vision OR proximity
    CanAttack())
{
    ChangeState(EnemyState.Attacking);
}
```
**Why**: Allows attacking player who moved behind enemy at close range.

## Key Ranges

| Range | Purpose | Location |
|-------|---------|----------|
| **2.5m** | Close proximity detection (no vision needed) | `EnemyAI.CheckPlayerDetection()` |
| **3.0m** | Immediate position tracking range | `BasicEnemy.UpdateChasing()` |
| **Attack Range** | Actual attack distance | `EnemyProfile.attackRange` (typically 2m) |

## Visual Guide

```
Player at different positions:

Far Away (>3m):        Close Range (<3m):        Very Close (<2.5m):
Enemy ? [1s delay] ? Player    Enemy ? [instant] ? Player   Enemy ? ? Player
           (No vision needed!)

Vision: Required      Vision: Preferred            Vision: NOT required
Tracking: Delayed (1s) Tracking: ImmediateDetection: Automatic
```

## Behavior Changes

### Before Fix:
1. Enemy spots player
2. Chases delayed position (1s old)
3. **Runs past stationary player**
4. Player is now behind enemy (175° angle)
5. Enemy can't see player (outside 60° cone)
6. **Enemy loses aggro** ?

### After Fix:
1. Enemy spots player
2. Chases delayed position
3. **At 3m range: switches to immediate tracking**
4. **Stops near player** (doesn't overshoot)
5. If player gets behind: **2.5m proximity maintains aggro**
6. **Enemy turns and attacks** ?

## Testing

Enable debug logs and watch for these messages:

```
// Far range - normal behavior
"[BasicEnemy] Updated delayed position to (x, y, z)"

// Close range - immediate tracking activated
"[BasicEnemy] Player VERY CLOSE - tracking actual position: (x, y, z)"

// Very close - proximity awareness
"[EnemyAI] Basic Enemy(Clone) - Player in CLOSE PROXIMITY! Gaining aggro regardless of vision"

// Player behind but close - attack still possible
"[BasicEnemy] Can't see player, moving to last delayed position"
// No aggro loss because player is within 2.5m!
```

## Tuning Tips

**Enemy too aggressive?**
- Decrease close proximity range from 2.5m to 2m
- Increase attack cooldown

**Enemy still runs past player?**
- Increase immediate tracking range from 3m to 4m
- Decrease delayed position update time (faster updates)

**Enemy loses aggro too easily?**
- Increase close proximity range from 2.5m to 3m
- Increase `loseAggroTime` in profile

**Enemy not accurate enough?**
- Increase immediate tracking range from 3m to 5m
- Decrease delayed position time (more frequent updates)
