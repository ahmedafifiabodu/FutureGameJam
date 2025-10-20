# Procedural Level Generation System

## Overview
This system generates procedural levels by dynamically spawning and connecting rooms through corridors. As the player progresses, old sections are destroyed to optimize memory.

## Components

### 1. ConnectionPoint
- Represents entrance (Point A) and exit (Point B) points
- Place these as child objects in your room/corridor prefabs
- **Point A** = Entrance (green gizmo)
- **Point B** = Exit (red gizmo)

### 2. Room
- Represents a room in the level
- Must have at least one **Point B** (exit)
- Regular rooms should also have **Point A** (entrance)
- Starting room only needs **Point B**

### 3. Corridor
- Connects rooms together
- Must have both **Point A** (entrance) and **Point B** (exit)
- **Point B** connects to room exit
- **Point A** connects to next room entrance

### 4. ProceduralLevelGenerator
- Main manager that handles spawning and cleanup
- Place one in your scene
- Manages room/corridor generation and deletion

### 5. RoomEntranceTrigger
- Detects when player enters a room
- Triggers cleanup of previous room/corridor
- Attach to a trigger collider at room entrance

## Setup Instructions

### Step 1: Create Room Prefabs

1. Create a new GameObject for your room
2. Add the `Room` component
3. Create child GameObjects for connection points:
   - **EntrancePoint**: Add `ConnectionPoint` component, set Type to **A**
   - **ExitPoint**: Add `ConnectionPoint` component, set Type to **B**
4. Add entrance trigger:
   - Create child GameObject at entrance
   - Add `BoxCollider` (set as trigger)
   - Add `RoomEntranceTrigger` component
5. Optional: Assign door GameObjects in Room component
6. Save as prefab in `Assets/Prefabs/Rooms/` folder

**Example Hierarchy:**
```
Room_01 (Room component)
??? EntrancePoint (ConnectionPoint Type A)
??? ExitPoint (ConnectionPoint Type B)
??? EntranceTrigger (BoxCollider + RoomEntranceTrigger)
??? Geometry
?   ??? Floor
?   ??? Walls
?   ??? Door (optional)
```

### Step 2: Create Corridor Prefabs

1. Create a new GameObject for your corridor
2. Add the `Corridor` component
3. Create child GameObjects for connection points:
   - **EntrancePoint**: Add `ConnectionPoint` component, set Type to **A**
   - **ExitPoint**: Add `ConnectionPoint` component, set Type to **B**
4. Save as prefab in `Assets/Prefabs/Corridors/` folder

**Example Hierarchy:**
```
Corridor_01 (Corridor component)
??? EntrancePoint (ConnectionPoint Type A)
??? ExitPoint (ConnectionPoint Type B)
??? Geometry
    ??? Floor
    ??? Walls
```

### Step 3: Create Starting Room

1. Create a special room prefab
2. Add `Room` component
3. Mark as **Starting Room** in inspector
4. Only needs **Point B** (exit) - no entrance
5. Save as separate prefab

### Step 4: Setup ProceduralLevelGenerator

1. Create empty GameObject in scene: `LevelGenerator`
2. Add `ProceduralLevelGenerator` component
3. Configure settings:

**Prefab Pools:**
- Click **+** to add room prefabs
  - Assign room prefab
  - Set weight (1-100, higher = more likely to spawn)
  - Add description (optional)
- Click **+** to add corridor prefabs
  - Assign corridor prefab
  - Set weight (1-100)
  - Add description (optional)

**Starting Room:**
- Assign your starting room prefab

**Player Reference:**
- Assign player Transform (or leave empty - auto-finds by tag)
- Set `Proximity Check Distance` (how close player needs to be to trigger generation)

**Debug:**
- Enable `Enable Debug Logs` for testing

### Step 5: Player Setup

Make sure your player GameObject:
1. Has tag **"Player"** OR
2. Has `FirstPersonZoneController` component (system auto-detects)

## How It Works

### Generation Flow

1. **Start**: Starting room spawns at origin
2. **Player Approaches Exit**: When player gets within proximity distance of Point B
3. **Corridor Spawns**: Random corridor spawns, Point B aligned with room's Point B
4. **Room Spawns**: Random room spawns, Point A aligned with corridor's Point A
5. **Player Enters New Room**: RoomEntranceTrigger detects entry
6. **Cleanup**: Previous room and corridor are destroyed
7. **Repeat**: Process continues as player progresses

### Weighted Randomness

Each prefab has a weight value (1-100):
- Higher weight = more likely to spawn
- Weight 50 = normal probability
- Weight 100 = twice as likely as weight 50
- Weight 10 = rare spawns

**Example:**
```
Room_Common: Weight 70 (70% of total)
Room_Rare: Weight 20 (20% of total)
Room_Boss: Weight 10 (10% of total)
```

## Testing & Debugging

### Visual Gizmos

When selected in editor:
- **Green sphere** = Point A (entrance)
- **Red sphere** = Point B (exit)
- **Yellow sphere** = Player proximity range
- **Cyan/Yellow arrows** = Point direction

### Debug Logs

Enable `Enable Debug Logs` to see:
- Prefab validation
- Generation events
- Room entry detection
- Cleanup operations

### Common Issues

**Problem**: Rooms/corridors not aligning
- **Solution**: Ensure ConnectionPoints are positioned correctly
- Points should face the direction of connection
- Use gizmos to verify alignment

**Problem**: Player falls through floor
- **Solution**: Ensure all geometry has colliders
- Check that floors are continuous between pieces

**Problem**: Generation not triggering
- **Solution**: Check proximity distance setting
- Verify player has correct tag
- Enable debug logs to see detection

**Problem**: Previous rooms not deleting
- **Solution**: Ensure RoomEntranceTrigger is on entrance collider
- Verify collider is set as trigger
- Check player collider/rigidbody setup

## Best Practices

### Connection Point Placement

- Place Points at the center of doorways
- Point forward direction should face INTO the room/corridor
- Keep Point A and Point B at same Y height for smooth alignment

### Room Design

- Keep entrance and exit on opposite sides
- Add navmesh/walkable paths between entrance and exit
- Test each room prefab independently first

### Performance

- Keep room geometry optimized
- Use occlusion culling
- Limit active room/corridor count (system auto-manages)
- Consider object pooling for frequently spawned prefabs

### Level Design

- Create variety: small, medium, large rooms
- Mix corridor lengths for pacing
- Use weights to control rarity
- Test weight distribution for desired difficulty curve

## Example Weight Configurations

### Balanced Exploration
```
Small Room: 40
Medium Room: 40
Large Room: 20

Short Corridor: 50
Long Corridor: 50
```

### Dungeon Crawler
```
Combat Room: 50
Treasure Room: 20
Trap Room: 20
Boss Room: 10

Narrow Corridor: 60
Wide Corridor: 40
```

### Procedural Story
```
Story Room: 30
Empty Room: 50
Puzzle Room: 15
Key Room: 5

Straight Corridor: 70
Curved Corridor: 30
```

## Advanced Customization

### Adding Room Events

Extend the `Room` class:

```csharp
public class CustomRoom : Room
{
    public override void OnPlayerEnter()
    {
        // Spawn enemies
        // Play music
        // Trigger events
    }
}
```

### Custom Generation Logic

Extend `ProceduralLevelGenerator`:

```csharp
public class CustomGenerator : ProceduralLevelGenerator
{
    protected override Room SpawnRoom(ConnectionPoint exitPoint)
    {
        // Custom logic (e.g., difficulty scaling)
        return base.SpawnRoom(exitPoint);
    }
}
```

## Support

For issues or questions:
1. Check debug logs
2. Verify setup steps
3. Test individual prefabs
4. Check collision/trigger setup

Happy level generating! ??
