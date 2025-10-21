# Player Entry Tracking Fix

## Problem
Enemies were not detecting the player even when in the same room because the `EnemyRoomTracker` was checking if the player was within the room's collider bounds, but the check was failing before the player officially "entered" through the entrance trigger.

The log showed:
```
[EnemyAI] Basic Enemy(Clone) - Player not in same room/corridor
```

## Root Cause
The `EnemyRoomTracker.IsPlayerInSameRoomOrCorridor()` method only checked if the player was within the room's collider bounds, but didn't account for whether the player had entered through the proper entrance trigger. This caused false negatives when the player was technically in the room space but hadn't triggered the entrance.

## Solution
Added a boolean flag `playerHasEntered` to track when the player officially enters a level piece (room or corridor) through the entrance trigger.

## Changes Made

### 1. LevelPiece.cs
**Added:**
- `playerHasEntered` boolean field (serialized for debugging in Inspector)
- `PlayerHasEntered` public property
- `OnPlayerEntered()` method that sets the flag to true when player enters

```csharp
[Header("Player Tracking")]
[SerializeField] protected bool playerHasEntered = false;

public bool PlayerHasEntered => playerHasEntered;

public virtual void OnPlayerEntered()
{
    playerHasEntered = true;
    Debug.Log($"[LevelPiece] Player entered {gameObject.name}");
}
```

### 2. RoomEntranceTrigger.cs
**Modified:**
- Changed `parentRoom` field to `parentLevelPiece` (more generic)
- Updated `Awake()` to find parent `LevelPiece` instead of just `Room`
- Updated `OnTriggerEnter()` to:
  - Call `parentLevelPiece.OnPlayerEntered()` when player enters
  - Support both Room and Corridor types
  - Better logging with level piece name

```csharp
// Notify parent level piece that player has entered
if (parentLevelPiece != null)
{
    parentLevelPiece.OnPlayerEntered();
}
```

### 3. EnemyRoomTracker.cs
**Modified `IsPlayerInSameRoomOrCorridor()`:**
Added a check for `PlayerHasEntered` before doing bounds checking:

```csharp
// Check if player has entered this level piece first
if (!currentLevelPiece.PlayerHasEntered)
{
    if (enableDebugLogs && Time.frameCount % 120 == 0)
        Debug.Log($"[EnemyRoomTracker] {gameObject.name} - Player has NOT entered {currentLevelPiece.name} yet");
    return false;
}
```

## How It Works

### Flow:
1. **Player spawns** ? Room/corridor `playerHasEntered` is `false`
2. **Player crosses entrance trigger** ? `RoomEntranceTrigger.OnTriggerEnter()` fires
3. **Trigger calls** ? `parentLevelPiece.OnPlayerEntered()`
4. **Flag set** ? `playerHasEntered` becomes `true`
5. **Enemies detect** ? `EnemyRoomTracker.IsPlayerInSameRoomOrCorridor()` returns `true`
6. **Enemy AI runs** ? `CheckPlayerDetection()` proceeds, enemy gains aggro

### Starting Room Exception
For the starting room (where player spawns), you'll need to manually set `playerHasEntered = true` in the Inspector or call `OnPlayerEntered()` when the game starts. Otherwise, add this to the starting room's initialization:

```csharp
// In Room.cs for starting room
if (startingRoom)
{
    playerHasEntered = true; // Player starts here
}
```

## Benefits

? **Clear player tracking** - Know exactly when player has entered a room/corridor
? **Prevents premature detection** - Enemies won't aggro until player officially enters
? **Works with both rooms and corridors** - Generic `LevelPiece` base class
? **Debug-friendly** - Boolean visible in Inspector, debug logs show entry events
? **No bounds checking issues** - Explicit entry trigger instead of position checks

## Expected Logs (With Debug Enabled)

### When Player Enters Room:
```
[RoomEntranceTrigger] Player entered: Arena Round
[LevelPiece] Player entered Room_Arena_Round(Clone)
[EnemyRoomTracker] Basic Enemy(Clone) - Player IS in same room (Room_Arena_Round(Clone))
[EnemyAI] Basic Enemy(Clone) - Distance to player: 5.23, Instant aggro range: 8, Can see: True
[EnemyAI] Basic Enemy(Clone) gained aggro on player at position (1.33, 0.21, 7.98)!
[EnemyAI] Basic Enemy(Clone) entering chase state - NavAgent enabled: True, isStopped: False, speed: 4
```

## Testing

1. ? **Enable debug logs** on Enemy AI and EnemyRoomTracker
2. ? **Play the game** and enter a room
3. ? **Check console** - should see "Player entered" message
4. ? **Verify enemy detects** - should see aggro message shortly after
5. ? **Watch enemy move** - should chase player

## Troubleshooting

### Issue: Starting room enemies don't detect player
**Solution:** Set `playerHasEntered = true` in Inspector for starting room, or modify `Room.cs`:
```csharp
protected override void Awake()
{
    base.Awake();
    
    if (startingRoom)
    {
        playerHasEntered = true; // Player spawns here
    }
}
```

### Issue: Corridor enemies don't detect player
**Solution:** Make sure corridors have `RoomEntranceTrigger` components at their entrances, just like rooms.

### Issue: Player entered but enemies still don't detect
**Solution:** Check other detection requirements:
- Vision angle (increase to 180° for testing)
- Detection ranges (increase instant aggro range)
- NavMesh placement (ensure enemy is on NavMesh)
- Line of sight (check for obstacles blocking vision)

## Related Files
- `Assets/Scripts/ProceduralGeneration/LevelPiece.cs`
- `Assets/Scripts/ProceduralGeneration/RoomEntranceTrigger.cs`
- `Assets/Scripts/AI/Core/EnemyRoomTracker.cs`
- `Assets/Scripts/ProceduralGeneration/Room.cs`
- `Assets/Scripts/ProceduralGeneration/Corridor.cs`
