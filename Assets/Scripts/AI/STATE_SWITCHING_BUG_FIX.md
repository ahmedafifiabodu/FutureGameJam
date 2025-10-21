# Enemy State Switching Bug Fix

## Problem
Enemies were rapidly switching between `Chasing` and `Attacking` states even when the player was not moving. This created a loop:

1. Enemy enters `Chasing` state
2. Player is within attack range ? transition to `Attacking`
3. Attack completes ? `ReturnToChase()` is called
4. Enemy enters `Chasing` state again
5. Player is STILL within attack range (hasn't moved) ? immediately transition to `Attacking` again
6. **REPEAT INFINITELY**

### Root Causes
1. **No guard against state transitions during attacks** - `UpdateChasing()` was being called even while `isAttacking` was true
2. **Attack cooldown not properly enforced** - State transition checks didn't verify `CanAttack()` properly
3. **Missing state exit handling** - `isAttacking` flag wasn't being cleared when leaving attack state

## Solution

### 1. Added Attack Guard in `UpdateChasing()`
**All enemy types now check if currently attacking before processing chase logic:**

```csharp
protected override void UpdateChasing()
{
    if (!player) return;

    // Don't allow state transitions if currently attacking
    if (isAttacking)
    {
        if (enableDebugLogs)
            Debug.Log($"[Enemy] Still attacking, skipping chase update");
        return;
    }
    
    // ... rest of chase logic
}
```

This prevents the state machine from even checking attack conditions while an attack is in progress.

### 2. Properly Cleared `isAttacking` Flag
**Added state exit handler in `EnemyAI.cs`:**

```csharp
protected virtual void OnStateExit(EnemyState state)
{
    switch (state)
    {
        case EnemyState.Attacking:
            // Ensure attacking flag is cleared when leaving attack state
            isAttacking = false;
            break;
    }
}
```

**Updated `ReturnToChase()` to explicitly clear the flag:**

```csharp
protected virtual void ReturnToChase()
{
    if (currentState == EnemyState.Attacking && hasAggro)
    {
        isAttacking = false; // Clear attacking flag before state change
        ChangeState(EnemyState.Chasing);
    }
    else
    {
        // Just clear the flag if state already changed
        isAttacking = false;
    }
}
```

### 3. Attack Cooldown Enforcement
The `CanAttack()` method already checks:
```csharp
protected virtual bool CanAttack()
{
    return Time.time >= lastAttackTime + profile.attackCooldown && !isAttacking;
}
```

This ensures that even if the player is in range, the enemy must wait for the cooldown before attacking again.

## Files Modified

### Core Files
- `Assets\Scripts\AI\Core\EnemyAI.cs`
  - Added `OnStateExit()` implementation
  - Added attack guard in `UpdateChasing()`
  - Fixed `ReturnToChase()` to clear `isAttacking` flag

### Enemy Type Files
- `Assets\Scripts\AI\EnemyTypes\BasicEnemy.cs`
  - Added attack guard in `UpdateChasing()`
  
- `Assets\Scripts\AI\EnemyTypes\FastEnemy.cs`
  - Added attack guard in `UpdateChasing()`
  
- `Assets\Scripts\AI\EnemyTypes\ToughEnemy.cs`
  - Added attack guard in `UpdateChasing()`

## Expected Behavior Now

1. Enemy detects player and enters `Chasing` state
2. Enemy moves towards player
3. When player is in attack range AND cooldown has passed:
   - `CanAttack()` returns true
   - Enemy transitions to `Attacking` state
   - `isAttacking = true`
4. During attack:
   - `UpdateChasing()` returns early (due to attack guard)
   - No state transitions can occur
5. After attack completes:
   - `EndAttack()` is called
   - `ReturnToChase()` is invoked after recovery time
   - `isAttacking` is cleared
   - Enemy returns to `Chasing` state
6. Enemy continues chasing but CANNOT attack again until:
   - Player is still in range
   - Attack cooldown has passed (`attackCooldown` from profile)
   
## Testing
To verify the fix:
1. Place an enemy in a scene with a player
2. Enable debug logs on the enemy
3. Let the enemy chase and attack
4. Observe that:
   - Enemy attacks once
   - Returns to chasing
   - Waits for cooldown before attacking again
   - No rapid state switching occurs

## Configuration
You can tune the attack behavior via the `EnemyProfile`:
- `attackCooldown` - Time between attacks (increase to make enemies attack less frequently)
- `attackDuration` - How long the attack animation/state lasts
- `attackRecoveryTime` - Delay before returning to chase after attack ends

## Related Files
- `Assets\Scripts\AI\CHASE_BUG_FIX.md` - Related chase state fixes
- `Assets\Scripts\AI\TROUBLESHOOTING.md` - General debugging guide
- `Assets\Scripts\AI\README.md` - Main AI system documentation
