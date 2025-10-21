# Stopping Distance and Enemy Avoidance Fix

## Problems

### 1. Enemies Trying to Occupy Player's Exact Position
**Symptom:** Enemies would walk right into the player, trying to occupy the exact same position, ignoring stopping distance.

**Root Causes:**
- Attack range check was `distanceToPlayer <= profile.attackRange`
- Stopping distance was set to `attackRange * 0.8f` but never actually checked
- Enemies would keep moving until within attack range, not stopping distance
- NavAgent would pathfind directly to player's position, not stopping early
- **Enemy would attack while still moving** - no check for whether NavAgent had actually stopped

### 2. Enemies Pushing Each Other
**Symptom:** Multiple enemies would collide and physically push each other around while trying to reach the player.

**Root Causes:**
- All enemies pathfinding to the exact same point (player's position)
- No obstacle avoidance configured on NavMeshAgent
- No avoidance priority randomization
- Constantly recalculating paths every frame, causing jitter and pushing

## Solutions

### 1. Respect Stopping Distance in Attack Range Check

**Changed attack condition from:**
```csharp
if (distanceToPlayer <= profile.attackRange && CanAttack())
{
    ChangeState(EnemyState.Attacking);
}
```

**To:**
```csharp
// Use stopping distance OR attack range, whichever is larger
float effectiveAttackRange = Mathf.Max(profile.attackRange, navAgent.stoppingDistance);

// Only attack if within range AND we've stopped moving
bool hasStoppedMoving = navAgent.velocity.magnitude < 0.1f;

if (distanceToPlayer <= effectiveAttackRange && hasStoppedMoving && CanAttack())
{
    ChangeState(EnemyState.Attacking);
}
```

**Key Changes:**
- Uses `effectiveAttackRange` to respect stopping distance
- **Checks `navAgent.velocity`** to ensure enemy has actually stopped
- Prevents attacking while sliding into position
- Ensures proper spacing before attack

### 2. Enable NavMeshAgent Avoidance

**Added to `EnemyAI.Start()`:**
```csharp
// Enable avoidance to prevent enemies from pushing each other
navAgent.obstacleAvoidanceType = UnityEngine.AI.ObstacleAvoidanceType.HighQualityObstacleAvoidance;
navAgent.avoidancePriority = Random.Range(30, 70); // Randomize priority
navAgent.radius = 0.5f; // Ensure proper spacing
```

**Key Points:**
- **High Quality Avoidance:** Best avoidance algorithm for enemy AI
- **Randomized Priority:** Prevents all enemies from deferring to each other (which causes deadlocks)
- **Proper Radius:** Ensures enemies maintain personal space

### 3. Reduce Path Recalculation

**Changed from constantly setting destination:**
```csharp
navAgent.SetDestination(player.position); // Called every frame
```

**To conditional updates:**
```csharp
// Only update destination if not already close to it
float distanceToDestination = Vector3.Distance(transform.position, lastKnownPlayerPosition);

if (distanceToDestination > navAgent.stoppingDistance + 0.5f)
{
    navAgent.SetDestination(lastKnownPlayerPosition);
}
```

**Benefits:**
- Reduces CPU overhead from constant pathfinding
- Prevents jittering when near destination
- Allows NavMeshAgent's built-in avoidance to work properly
- Enemies stop pushing when they reach stopping distance

## Files Modified

### Core AI System
**`Assets\Scripts\AI\Core\EnemyAI.cs`**
1. Added avoidance configuration in `Start()`
2. Updated `UpdateChasing()` to:
   - Check effective attack range (max of attack range and stopping distance)
   - **Check NavAgent velocity before allowing attack**
   - Only update destination when necessary
   - Prevent path recalculation spam

### Enemy Types
**All enemy types updated with the same stopping distance and velocity logic:**

**`Assets\Scripts\AI\EnemyTypes\BasicEnemy.cs`**
- Uses `effectiveAttackRange` for attack checks
- **Checks `hasStoppedMoving` before attacking**
- Only updates destination to `delayedPlayerPosition` when far enough away

**`Assets\Scripts\AI\EnemyTypes\FastEnemy.cs`**
- Uses `effectiveStabRange` for stab attack checks
- Uses `effectiveAttackRange` for regular attack checks
- **Checks `hasStoppedMoving` before attacking**
- Only updates destination when far from player

**`Assets\Scripts\AI\EnemyTypes\ToughEnemy.cs`**
- Uses `effectiveAttackRange` for shotgun attack checks
- **Checks `hasStoppedMoving` before attacking**
- Only updates destination when far from player

## How It Works Now

### Enemy Approach Behavior
1. **Far from Player (> stopping distance + 0.5f):**
   - NavAgent destination is updated to player position
   - Enemy moves toward player
   - Avoidance system steers around other enemies
   - `hasStoppedMoving = false` (velocity > 0.1)

2. **Near Stopping Distance (? stopping distance + 0.5f):**
   - NavAgent destination is **not** updated (prevents jitter)
   - Enemy relies on NavAgent's built-in stopping distance
   - Enemy naturally slows down and stops at appropriate distance
   - Avoidance prevents pushing other enemies
   - **Velocity decreases** as enemy comes to stop

3. **Stopped Within Attack Range:**
   - `effectiveAttackRange = Max(attackRange, stoppingDistance)`
   - `hasStoppedMoving = true` (velocity < 0.1)
   - **All conditions met:** within range + stopped + cooldown ready
   - Enemy transitions to Attacking state
   - NavAgent is stopped (`isStopped = true`)
   - Enemy faces player and performs attack

### Why Velocity Check Is Critical

**Without velocity check:**
```
Enemy enters stopping range ? Tries to attack while still sliding ? Looks janky
```

**With velocity check:**
```
Enemy enters stopping range ? Continues moving ? Slows down ? Stops ? THEN attacks
```

This creates smooth, natural-looking behavior where the enemy:
1. Chases player
2. **Slows down as it approaches**
3. **Comes to a complete stop**
4. **Then attacks**

Instead of attacking mid-slide or while still moving.

### Multiple Enemy Behavior
When multiple enemies are chasing the same player:

1. **Spread Out:** Randomized avoidance priority causes natural spacing
2. **No Pushing:** High quality avoidance steers enemies around each other
3. **Form Semi-Circle:** Enemies naturally position themselves around player
4. **Maintain Distance:** Each respects stopping distance independently
5. **Stop Before Attacking:** Each waits until fully stopped before attacking

## Configuration

### NavMeshAgent Settings (Auto-configured in code)
- **Stopping Distance:** `attackRange * 0.8f` (set in code)
- **Obstacle Avoidance:** `HighQualityObstacleAvoidance`
- **Avoidance Priority:** Random between 30-70
- **Radius:** 0.5f (adjust if enemies are larger/smaller)
- **Velocity Threshold:** 0.1 units/sec (for stopped check)

### EnemyProfile Settings (Designer-configurable)
- **Attack Range:** How close enemy needs to be to attack
  - Stopping distance is automatically 80% of this
  - Example: attackRange = 2.5f ? stoppingDistance = 2.0f
### Fine-Tuning

**If enemies attack too early (before fully stopped):**
- Decrease velocity threshold in code: `navAgent.velocity.magnitude < 0.05f` (stricter)
- Increase stopping distance: `navAgent.stoppingDistance = profile.attackRange * 0.9f`

**If enemies take too long to attack:**
- Increase velocity threshold: `navAgent.velocity.magnitude < 0.2f` (more lenient)
- Ensure stopping distance isn't too large

**If enemies are too far away:**
- Increase `attackRange` in EnemyProfile
- Or change multiplier in `EnemyAI.Start()`: `navAgent.stoppingDistance = profile.attackRange * 0.9f;`

**If enemies still push too much:**
- Increase `navAgent.radius` (gives more personal space)
- Use `MedQualityObstacleAvoidance` if performance is an issue

**If enemies get stuck:**
- Lower avoidance priority range: `Random.Range(40, 60)`
- Reduce `navAgent.radius`

## Testing

### Single Enemy Test
1. Place one enemy in scene with player
2. Enable debug logs
3. Observe:
   - Enemy approaches player
   - **Enemy slows down naturally as it gets close**
   - **Enemy comes to complete stop**
   - **Only then does attack trigger**
   - No jittering when standing still near player

### Multiple Enemy Test
1. Place 3-5 enemies around player
2. Let them all chase player
3. Observe:
   - Enemies spread out naturally
   - No pushing or shoving
   - Enemies maintain spacing from each other
   - **Each enemy stops individually before attacking**
   - Form semi-circle around player at appropriate distance

### Debug Visualization
In Scene view, you can see:
- **Cyan circles:** Attack range zones
- **Yellow circles:** Aggro detection ranges
- **Green lines:** Line of sight to player
- NavMeshAgent stopping distance (visible when enemy is selected)
- **Velocity vectors** (can be enabled in NavMeshAgent debug)

## Performance Notes

### Optimizations Applied
1. **Reduced SetDestination calls:** Only when needed (not every frame)
2. **Frame-based debug logging:** Only logs every 60 frames in chase mode
3. **Distance checks before pathfinding:** Avoids unnecessary calculations
4. **Velocity check:** Very cheap operation (magnitude calculation)

### Performance Impact
- **High Quality Avoidance:** Slight CPU increase per enemy
  - 5-10 enemies: Negligible impact
  - 20+ enemies: Consider using `MedQualityObstacleAvoidance`
- **Velocity Check:** Nearly free (already calculated by NavMeshAgent)
  
## Related Documentation
- `STATE_SWITCHING_BUG_FIX.md` - Attack state management fixes
- `CHASE_BUG_FIX.md` - Chase state behavior fixes
- `TROUBLESHOOTING.md` - General AI debugging guide
- `README.md` - Complete AI system overview

## Common Issues

### Issue: Enemies still walk into player
**Solution:** 
1. Check that `navAgent.stoppingDistance` is properly set in Inspector or code
2. Verify velocity check is working: add debug log for `navAgent.velocity.magnitude`
3. Ensure NavAgent is on NavMesh and enabled

### Issue: Enemies won't attack at all
**Solution:** 
1. Enemy might not be stopping (increase stopping distance)
2. Velocity threshold too strict (increase from 0.1 to 0.2)
3. Check that `CanAttack()` returns true (cooldown, isAttacking flag)

### Issue: Enemies attack while still moving
**Solution:** 
1. Verify velocity check is in place: `hasStoppedMoving = navAgent.velocity.magnitude < 0.1f`
2. Make threshold stricter: change 0.1 to 0.05
3. Increase stopping distance so enemy has more room to decelerate

### Issue: Enemies stuck in doorways
**Solution:** This is a NavMesh issue, not stopping distance. Ensure NavMesh properly covers doorways and corridors.

### Issue: Performance problems with many enemies
**Solution:** 
1. Change to `MedQualityObstacleAvoidance`
2. Reduce avoidance calculations in NavMesh settings
3. Consider pooling/despawning distant enemies

### Issue: Enemies slide or drift when attacking
**Solution:**
1. Ensure `navAgent.isStopped = true` in `UpdateAttacking()`
2. Check that velocity check is working
3. May need to manually set velocity to zero: `navAgent.velocity = Vector3.zero`
