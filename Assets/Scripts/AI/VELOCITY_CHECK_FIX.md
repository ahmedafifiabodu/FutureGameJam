# Velocity Check Fix - Enemies Attacking While Moving

## Problem
Enemies were transitioning to attack state **while still moving** or **immediately upon gaining aggro**, causing them to:
- Attack mid-slide as they approached the player
- Attack instantly when spawning near the player
- Look janky and unnatural
- Sometimes overshoot their stopping distance
- Still try to occupy the player's exact position

## Root Causes
1. **Attack triggered while moving:** The attack condition only checked distance and cooldown, not velocity
2. **Instant attack on aggro:** If player was already in range when enemy gained aggro, it would attack immediately (even frame 1)
3. **No minimum chase time:** Enemy could attack without ever actually chasing

## Solutions

### 1. Velocity Check
Added a velocity check to ensure the enemy has **actually stopped moving** before attacking:

```csharp
bool hasStoppedMoving = navAgent.velocity.magnitude < 0.1f;
```

### 2. Minimum Chase Time
Added a requirement that enemy must chase for at least **0.3 seconds** before attacking:

```csharp
// Track when chase state started (in OnStateEnter)
chaseStartTime = Time.time;

// Check minimum chase time before attacking
float minChaseTime = 0.3f;
bool hasBeenChasingLongEnough = (Time.time - chaseStartTime) >= minChaseTime;
```

### 3. Complete Attack Condition
All three checks must pass:

```csharp
if (distanceToPlayer <= effectiveAttackRange && 
    hasStoppedMoving && 
    hasBeenChasingLongEnough && 
    CanAttack())
{
    ChangeState(EnemyState.Attacking);
}
```

## How It Works

### Before (Broken)
```
1. Enemy spawns near player
2. Enemy gains aggro
3. Player is already within attack range (2.0 units)
4. ? ATTACK IMMEDIATELY (frame 1, no chase, no movement)
5. Enemy attacks from spawn position
6. Looks terrible
```

### After (Fixed)
```
1. Enemy spawns near player
2. Enemy gains aggro ? chaseStartTime = Time.time
3. Enemy enters chase state
4. Enemy moves toward player for at least 0.3 seconds
5. Enemy reaches stopping distance and decelerates
6. Enemy velocity drops below 0.1
7. 0.3 seconds have passed since chase started
8. ? ALL CONDITIONS MET ? Attack
9. Clean, natural-looking attack
```

## Why 0.3 Seconds?

**Too short (< 0.2s):**
- Enemy still attacks too quickly on aggro
- Doesn't give time for natural movement
- Still looks instant

**Too long (> 0.5s):**
- Enemy feels unresponsive
- Takes too long to engage
- Player can easily kite

**0.3 seconds (chosen):**
- Enough time for enemy to start moving
- Allows velocity to build up and then decrease
- Feels responsive but not instant
- Natural-looking engagement

## Files Modified
All enemy types now check velocity AND minimum chase time:

1. **EnemyAI.cs** (base class)
   - Added `chaseStartTime` field
   - Set `chaseStartTime` in `OnStateEnter(Chasing)`
   - Added `hasBeenChasingLongEnough` check in `UpdateChasing()`

2. **BasicEnemy.cs** 
   - Added minimum chase time check in `UpdateChasing()`

3. **FastEnemy.cs**
   - Added minimum chase time check for both stab and regular attacks

4. **ToughEnemy.cs**
   - Added minimum chase time check in `UpdateChasing()`

## Expected Behavior

### Single Enemy - Distant Start
1. Enemy spots player from far away
2. Enemy starts chasing
3. Enemy runs toward player
4. After 0.3+ seconds of chasing
5. Enemy reaches stopping distance and decelerates
6. Enemy comes to complete stop (velocity < 0.1)
7. **Then attacks** - looks smooth and intentional

### Single Enemy - Close Start (Player Already in Range)
1. Enemy spawns or player comes very close
2. Enemy gains aggro
3. **Enemy chases for at least 0.3 seconds** (even if player in range)
4. After 0.3 seconds, if stopped and in range
5. **Then attacks** - prevents instant attack

### Multiple Enemies
1. Multiple enemies chase player
2. Each must chase for 0.3+ seconds individually
3. Each approaches and decelerates at their own pace
4. **Staggered attack timing** (natural look)
5. No pushing or sliding into each other

## Visual Difference

**Before Fix:**
```
Frame 1: Enemy spawns
Frame 1: Enemy gains aggro  
Frame 1: Enemy attacks ?? (INSTANT!)
Result: Looks broken, no chase happened
```

**After Fix:**
```
Frame 1:    Enemy spawns
Frame 1:    Enemy gains aggro, starts chase
Frame 18:   Enemy moving toward player (0.3 seconds at 60fps)
Frame 35:   Enemy reaches stopping distance
Frame 40:   Enemy fully stopped (velocity < 0.1)
Frame 40:   ?? Attack triggers
Result: Smooth, natural engagement
```

## Tuning

### If enemies attack too quickly:
```csharp
float minChaseTime = 0.5f; // Increase delay
```

### If enemies feel too slow to respond:
```csharp
float minChaseTime = 0.2f; // Decrease delay (minimum recommended)
```

### If enemies still attack while moving:
```csharp
bool hasStoppedMoving = navAgent.velocity.magnitude < 0.05f; // Stricter
```

### If enemies take forever to attack:
```csharp
bool hasStoppedMoving = navAgent.velocity.magnitude < 0.15f; // More lenient
```

## Technical Details

### Chase Time Tracking
```csharp
protected float chaseStartTime = -999f; // Added to EnemyAI base class
```

Set when entering chase state:
```csharp
case EnemyState.Chasing:
    chaseStartTime = Time.time; // Track when we started chasing
    // ...
```

### Complete Attack Logic
```csharp
// 1. Distance check
float distanceToPlayer = Vector3.Distance(transform.position, player.position);
float effectiveAttackRange = Mathf.Max(profile.attackRange, navAgent.stoppingDistance);

// 2. Velocity check
bool hasStoppedMoving = navAgent.velocity.magnitude < 0.1f;

// 3. Time check
float minChaseTime = 0.3f;
bool hasBeenChasingLongEnough = (Time.time - chaseStartTime) >= minChaseTime;

// 4. Cooldown check (in CanAttack)
bool canAttack = Time.time >= lastAttackTime + attackCooldown && !isAttacking;

// ALL must be true:
if (distanceToPlayer <= effectiveAttackRange && 
    hasStoppedMoving && 
    hasBeenChasingLongEnough && 
    CanAttack())
{
    ChangeState(EnemyState.Attacking);
}
```

### Edge Cases Handled
1. **Enemy spawns next to player:** Must chase 0.3s before attacking
2. **Player runs past stationary enemy:** Enemy chases, must wait 0.3s
3. **Enemy already chasing, player stops:** Enemy stops, attacks after 0.3s total chase time
4. **Enemy returns to chase after attack:** Chase timer resets each time entering chase state

## Performance
- **Cost:** Nearly free
- One additional float field per enemy (`chaseStartTime`)
- One additional time comparison per frame while chasing
- No noticeable performance impact

## Testing Checklist

- [x] Enemy doesn't attack instantly on aggro
- [x] Enemy chases for at least 0.3 seconds before attacking
- [x] Enemy decelerates smoothly before attacking
- [x] Enemy comes to complete stop before attacking
- [x] No sliding or drifting during attack
- [x] Multiple enemies don't push each other
- [x] Attack triggers at proper distance
- [x] No rapid state switching
- [x] Looks natural and intentional
- [x] Works when player already in range at aggro time

## Related Fixes
This fix works together with:
- **Stopping Distance Fix:** Ensures proper distance is maintained
- **Avoidance Fix:** Prevents enemies from pushing each other
- **State Switching Fix:** Prevents rapid attack/chase loops

All three together create smooth, professional-looking enemy behavior.

## Common Issues

### Issue: Enemy still attacks instantly
**Solution:** 
1. Check that `chaseStartTime` is being set in `OnStateEnter(Chasing)`
2. Verify `hasBeenChasingLongEnough` is in the attack condition
3. Increase `minChaseTime` to 0.5f for more delay

### Issue: Enemy never attacks
**Solution:**
1. Check that chase state is being entered properly
2. Verify all conditions are being met (add debug logs)
3. Make sure `minChaseTime` isn't too high (> 1.0f)

### Issue: Enemy attacks after losing sight of player
**Solution:** This is working as designed - enemy remembers last known position. Adjust `loseAggroTime` in EnemyProfile if needed.
