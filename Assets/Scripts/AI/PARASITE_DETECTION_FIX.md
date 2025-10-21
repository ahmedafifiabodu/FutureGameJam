# Parasite Player Detection and Attack Loop Fix

## Problem Summary

The enemy was stuck in an attack-reposition-attack loop with the stationary parasite player:

1. **Vision Issues**: Raycast was too high for small parasite player
2. **Attack Behind Player**: Enemy could attack player even when behind them (outside vision cone)
3. **Immediate Re-attack**: Enemy would reposition and instantly attack again without proper tracking
4. **No Vision Check on Attack**: Enemy could attack without seeing the player
5. **Running Past Player**: Enemy chases delayed position, runs past stationary player, player ends up behind enemy, enemy loses aggro

## Root Causes

### 1. High Raycast Origin
```csharp
// OLD - Too high for parasite
Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;
```

### 2. Missing Vision Check in Attack Condition
The enemy could attack when within range even if the player was behind them:
```csharp
// OLD - No vision requirement
if (distanceToPlayer <= effectiveAttackRange && hasStoppedMoving && hasBeenChasingLongEnough && CanAttack())
```

### 3. Immediate Chase-to-Attack Transition
After completing an attack, the enemy would:
- Return to chase state
- Move to delayed position (1 second old)
- Get within range immediately
- Attack again (player hasn't moved)

### 4. Running Past Stationary Player **NEW ISSUE**
Enemy behavior with delayed targeting:
- Enemy spots player and starts chasing
- Enemy targets **delayed position** (where player was 1 second ago)
- Player is stationary, so enemy runs **past** the player to reach the old position
- Player ends up **behind enemy** (175° angle - nearly directly behind)
- Enemy reaches delayed position, can't see player (outside 60° vision cone)
- Enemy **loses aggro** despite player being 1-2 meters away

## Solutions Implemented

### 1. Fixed Vision Raycast for Small Player (`EnemyAI.cs`)

**Change**: Adjusted raycast to target lower position for parasite player

```csharp
protected virtual bool CanSeePlayer()
{
 // ... existing code ...
    
    // FIXED: Use lower target position for small parasite
    Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;
    Vector3 targetPosition = player.position + Vector3.up * 0.25f; // Lower target for parasite
    Vector3 rayDirection = (targetPosition - rayOrigin).normalized;
  float rayDistance = Vector3.Distance(rayOrigin, targetPosition);
    
    // ... rest of raycast logic ...
}
```

**Why**: Parasite is very small, so we need to aim the raycast lower to actually hit the player collider.

### 2. Added Vision Requirement for Attacks (`BasicEnemy.cs`)

**Change**: Enemy must see player OR player must be very close to attack

```csharp
protected override void UpdateChasing()
{
    // ... existing code ...
    
    // Store vision state
    bool canSeePlayerNow = CanSeePlayer();
  bool playerIsVeryClose = distanceToPlayer <= 3f;
    
 // ... update delayed position only when can see player OR player is very close ...
    
    // FIXED: Require vision OR close proximity to attack
  if (distanceToPlayer <= effectiveAttackRange && 
        hasStoppedMoving && 
        hasBeenChasingLongEnough && 
      (canSeePlayerNow || playerIsVeryClose) &&  // NEW REQUIREMENT
 CanAttack())
    {
        ChangeState(EnemyState.Attacking);
    }
}
```

**Why**: Prevents attacking player from far away when vision is blocked, but allows attacks when player is very close (simulates awareness/hearing).

### 3. Added Close Proximity Awareness (`EnemyAI.cs`) **NEW FIX**

**Change**: Enemy detects player within 2.5 meters even without line of sight

```csharp
protected virtual void CheckPlayerDetection()
{
    // ... existing code ...
    
    float distanceToPlayer = Vector3.Distance(transform.position, player.position);
    
    // CLOSE PROXIMITY DETECTION - Player very close regardless of vision
    float closeProximityRange = 2.5f;
    if (distanceToPlayer <= closeProximityRange)
    {
        if (enableDebugLogs && Time.frameCount % 120 == 0)
Debug.Log($"[EnemyAI] {gameObject.name} - Player in CLOSE PROXIMITY! Gaining aggro");
        GainAggro();
        return;
    }
    
    // ... rest of detection logic ...
}
```

**Why**: 
- Simulates **hearing/awareness** - enemy should know when player is right next to them
- Prevents enemy from **losing aggro** when player is literally behind them at 1m distance
- Fixes the "running past player" issue

### 4. Smart Position Tracking at Close Range (`BasicEnemy.cs`) **NEW FIX**

**Change**: When player is very close, track their actual position instead of delayed position

```csharp
protected override void UpdateChasing()
{
    float distanceToPlayer = Vector3.Distance(transform.position, player.position);
    float closeProximityRange = 3f;
    bool playerIsVeryClose = distanceToPlayer <= closeProximityRange;
    
    bool canSeePlayerNow = CanSeePlayer();
    if (canSeePlayerNow || playerIsVeryClose)
    {
      lastSeenPlayerTime = Time.time;
 
        // When player is very close, track them more accurately
        if (playerIsVeryClose)
  {
            delayedPlayerPosition = player.position;  // Immediate tracking!
            positionUpdateTimer = 0f;
        }
        else
        {
   // Normal delayed tracking when player is far
 positionUpdateTimer += Time.deltaTime;
            if (positionUpdateTimer >= delayedPositionTime)
         {
                delayedPlayerPosition = player.position;
      positionUpdateTimer = 0f;
     }
        }
    }
}
```

**Why**:
- Prevents enemy from **overshooting** stationary player
- Enemy tracks **actual position** when close, not 1-second-old position
- More realistic behavior - enemy should be more accurate at close range
- Fixes the "player ends up behind enemy" issue

### 5. Improved Chase Logic (`BasicEnemy.cs`)

**Changes**:
1. Track vision state AND proximity during chase
2. Don't update delayed position when can't see player (unless very close)
3. Only lose aggro if reach delayed position AND player is not visible AND not close
4. Increased minimum chase time from 0.3s to 0.5s

**Why**: 
- Prevents endless chase to old positions when player is lost
- Gives player more time to move between enemy attack opportunities
- Makes enemy behavior more realistic (can't track invisible player, but aware of close player)
- Stops the aggro loss loop when player is right behind enemy

## Testing Checklist

Test these scenarios to verify the fix:

- [ ] Enemy can detect and chase stationary parasite player
- [ ] Enemy doesn't run past stationary player (should stop near them)
- [ ] Enemy turns around to attack if player is behind but very close (< 3m)
- [ ] Enemy doesn't lose aggro when player is right behind them (< 2.5m)
- [ ] Enemy loses aggro if player moves far away and hides
- [ ] Enemy properly tracks moving parasite player
- [ ] Enemy doesn't spam attacks on stationary player
- [ ] Raycast visualization in Scene view shows correct targeting (look for green line to player)

## Expected Behavior Now

### Scenario 1: Stationary Player
1. **Initial Detection**: Enemy spots parasite player within vision cone
2. **Chase**: Enemy moves toward delayed position (1s old player position)
3. **Close Range Tracking**: When within 3m, enemy switches to **immediate tracking** of actual position
4. **No Overshoot**: Enemy stops near player instead of running past them
5. **Close Proximity Awareness**: If player ends up behind enemy but within 2.5m, enemy **maintains aggro**
6. **Attack**: Enemy turns to face player and attacks

### Scenario 2: Moving Player
1. **Chase**: Enemy follows delayed position (1s old)
2. **Vision Updates**: Enemy updates delayed position every 1 second while player is visible
3. **Lost Sight**: If player breaks line of sight, enemy moves to last known position
4. **Aggro Loss**: After 3 seconds without seeing player, enemy loses aggro (unless player gets close)

### Scenario 3: Player Sneaks Behind
1. **Enemy Chasing**: Enemy is chasing player
2. **Player Circles Behind**: Player moves behind enemy (outside vision cone)
3. **Close Proximity Check**: If player is within 2.5m, enemy **maintains aggro** and turns around
4. **Far Away**: If player is >2.5m away and behind, enemy may lose aggro after timeout

## Configuration Notes

You can tune these values:

### In EnemyProfile ScriptableObject:
- `attackRecoveryTime`: Time after attack before enemy can move (default: 0.5s)
- `attackCooldown`: Time between attacks (default: 1.5s)
- `visionAngle`: Width of vision cone (default: 120°)
- `loseAggroTime`: Time to lose aggro after losing sight (default: 3s)

### In `EnemyAI.cs`:
- **Close Proximity Detection**: `2.5f` meters - range to detect player regardless of vision
  - Simulates hearing/awareness
  - Prevents aggro loss when player is very close

### In `BasicEnemy.cs`:
- `delayedPositionTime`: How old the target position is (default: 1s)
- `minChaseTime`: Minimum time chasing before first attack (now: 0.5s)
- **Close Proximity Tracking**: `3f` meters - range to use immediate position tracking instead of delayed
  - Prevents overshooting stationary player
  - Makes close-range combat more responsive

## Key Mechanics

### Vision vs Proximity
- **Vision (60° cone)**: Required for initial detection and ranged aggro
- **Close Proximity (2.5m radius)**: Overrides vision requirement for awareness
  - Enemy "hears" or "senses" player nearby
  - Prevents unrealistic aggro loss at point-blank range

### Delayed Tracking vs Immediate Tracking
- **Far Range (>3m)**: Delayed position (1 second old) - "slow reaction"
- **Close Range (<3m)**: Immediate position - "focused combat"
  - Prevents running past stationary targets
  - More accurate close combat
  - Still allows flanking/dodging for player

## Files Modified

1. `Assets/Scripts/AI/Core/EnemyAI.cs` 
   - Fixed vision raycast height for parasite
   - Added close proximity awareness (2.5m detection regardless of vision)
   
2. `Assets/Scripts/AI/EnemyTypes/BasicEnemy.cs` 
   - Added vision/proximity requirement for attacks
   - Improved chase logic with close-range immediate tracking
   - Better aggro loss conditions

## Related Issues

- If enemy still has issues detecting parasite:
  - Check that parasite has collider with "Player" tag
  - Verify parasite transform position is at correct height
  - Enable debug logs to see raycast results in console
  
- If enemy loses aggro too easily:
  - Increase `profile.loseAggroTime` in EnemyProfile
  - Decrease `minChaseTime` in BasicEnemy
  - Increase `visionAngle` in EnemyProfile
  - Increase close proximity ranges in code (currently 2.5m and 3m)

- If enemy still attacks too frequently:
  - Increase `profile.attackCooldown`
  - Increase `profile.attackRecoveryTime`
  - Increase `minChaseTime` in BasicEnemy

- If enemy runs past player too often:
  - Decrease close proximity tracking range (currently 3m)
  - Adjust `delayedPositionTime` (make smaller for faster updates)
  - Adjust `navAgent.stoppingDistance` in enemy profile

- If enemy is too aggressive at close range:
  - Decrease close proximity detection range (currently 2.5m)
  - Increase attack cooldown
  - Add minimum distance requirement for attacks
