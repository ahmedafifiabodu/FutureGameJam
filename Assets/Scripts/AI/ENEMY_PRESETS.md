# Enemy Profile Presets

Copy these presets directly into Unity's ScriptableObject inspector.

---

## Basic Enemy - Standard

```yaml
Enemy Name: Basic Soldier
Enemy Type: Basic
Max Health: 80
Pain Chance: 0.3
Stagger Duration: 0.5

Patrol Speed: 2.0
Chase Speed: 3.5
Rotation Speed: 120
Waypoint Reach Distance: 0.5

Instant Aggro Range: 8.0
Delayed Aggro Range: 15.0
Aggro Delay: 1.5
Vision Angle: 120
Lose Aggro Time: 3.0

Attack Damage: 20
Attack Range: 2.0
Attack Cooldown: 1.5
Attack Duration: 0.8
Attack Recovery Time: 0.5

Spawn Weight: 60
Min Room Iteration: 0
Max Per Room: 3
```

---

## Basic Enemy - Elite (Late Game)

```yaml
Enemy Name: Basic Soldier Elite
Enemy Type: Basic
Max Health: 120
Pain Chance: 0.2
Stagger Duration: 0.4

Patrol Speed: 2.5
Chase Speed: 4.0
Rotation Speed: 150
Waypoint Reach Distance: 0.5

Instant Aggro Range: 10.0
Delayed Aggro Range: 18.0
Aggro Delay: 1.0
Vision Angle: 140
Lose Aggro Time: 4.0

Attack Damage: 30
Attack Range: 2.5
Attack Cooldown: 1.2
Attack Duration: 0.8
Attack Recovery Time: 0.3

Spawn Weight: 40
Min Room Iteration: 4
Max Per Room: 2
```

---

## Tough Enemy - Standard

```yaml
Enemy Name: Shotgunner
Enemy Type: Tough
Max Health: 150
Pain Chance: 0.15
Stagger Duration: 0.6

Patrol Speed: 1.5
Chase Speed: 2.5
Rotation Speed: 90
Waypoint Reach Distance: 0.5

Instant Aggro Range: 8.0
Delayed Aggro Range: 15.0
Aggro Delay: 2.0
Vision Angle: 100
Lose Aggro Time: 3.5

Attack Damage: 30
Attack Range: 8.0
Attack Cooldown: 2.5
Attack Duration: 1.2
Attack Recovery Time: 0.8

Spawn Weight: 30
Min Room Iteration: 2
Max Per Room: 2
```

---

## Tough Enemy - Heavy (Late Game)

```yaml
Enemy Name: Heavy Shotgunner
Enemy Type: Tough
Max Health: 200
Pain Chance: 0.1
Stagger Duration: 0.4

Patrol Speed: 1.2
Chase Speed: 2.0
Rotation Speed: 80
Waypoint Reach Distance: 0.5

Instant Aggro Range: 10.0
Delayed Aggro Range: 18.0
Aggro Delay: 1.5
Vision Angle: 120
Lose Aggro Time: 4.0

Attack Damage: 40
Attack Range: 10.0
Attack Cooldown: 2.0
Attack Duration: 1.0
Attack Recovery Time: 0.6

Spawn Weight: 25
Min Room Iteration: 5
Max Per Room: 1
```

---

## Fast Enemy - Standard

```yaml
Enemy Name: Runner
Enemy Type: Fast
Max Health: 60
Pain Chance: 0.4
Stagger Duration: 0.5

Patrol Speed: 2.5
Chase Speed: 5.0
Rotation Speed: 180
Waypoint Reach Distance: 0.5

Instant Aggro Range: 10.0
Delayed Aggro Range: 20.0
Aggro Delay: 1.0
Vision Angle: 140
Lose Aggro Time: 2.5

Attack Damage: 15
Attack Range: 1.5
Attack Cooldown: 1.0
Attack Duration: 0.6
Attack Recovery Time: 0.3

Spawn Weight: 10
Min Room Iteration: 3
Max Per Room: 2
```

---

## Fast Enemy - Assassin (Late Game)

```yaml
Enemy Name: Assassin
Enemy Type: Fast
Max Health: 80
Pain Chance: 0.35
Stagger Duration: 0.4

Patrol Speed: 3.0
Chase Speed: 6.0
Rotation Speed: 200
Waypoint Reach Distance: 0.5

Instant Aggro Range: 12.0
Delayed Aggro Range: 22.0
Aggro Delay: 0.8
Vision Angle: 160
Lose Aggro Time: 2.0

Attack Damage: 25
Attack Range: 2.0
Attack Cooldown: 0.8
Attack Duration: 0.5
Attack Recovery Time: 0.2

Spawn Weight: 20
Min Room Iteration: 6
Max Per Room: 2
```

---

## Spawn Manager Preset

### Early Game (Iteration 0-2)
```yaml
Weighted Enemy Prefabs:
- Basic Soldier (Weight: 70, Min: 0, Max: 3)
- Shotgunner (Weight: 30, Min: 2, Max: 1)

Base Enemies Per Room: 2
Enemies Per Iteration: 0.4
Max Enemies Per Room: 5
Enemy Spawn Chance: 0.7
Spawn Chance Increase: 0.05

Corridor Spawn Chance: 0.3
Max Enemies Per Corridor: 1
```

### Mid Game (Iteration 3-5)
```yaml
Weighted Enemy Prefabs:
- Basic Soldier (Weight: 50, Min: 0, Max: 2)
- Basic Soldier Elite (Weight: 30, Min: 4, Max: 2)
- Shotgunner (Weight: 30, Min: 2, Max: 2)
- Runner (Weight: 20, Min: 3, Max: 2)

Base Enemies Per Room: 3
Enemies Per Iteration: 0.5
Max Enemies Per Room: 6
Enemy Spawn Chance: 0.75
Spawn Chance Increase: 0.05

Corridor Spawn Chance: 0.4
Max Enemies Per Corridor: 2
```

### Late Game (Iteration 6+)
```yaml
Weighted Enemy Prefabs:
- Basic Soldier (Weight: 30, Min: 0, Max: 2)
- Basic Soldier Elite (Weight: 40, Min: 4, Max: 2)
- Shotgunner (Weight: 30, Min: 2, Max: 1)
- Heavy Shotgunner (Weight: 25, Min: 5, Max: 1)
- Runner (Weight: 20, Min: 3, Max: 2)
- Assassin (Weight: 15, Min: 6, Max: 1)

Base Enemies Per Room: 3
Enemies Per Iteration: 0.6
Max Enemies Per Room: 8
Enemy Spawn Chance: 0.8
Spawn Chance Increase: 0.05

Corridor Spawn Chance: 0.5
Max Enemies Per Corridor: 2
```

---

## FastEnemy Specific Settings

Add these to FastEnemy component in Inspector:

```yaml
Medium Range Min: 5.0
Medium Range Max: 12.0
Jump Height: 3.0
Jump Duration: 0.8
Jump Prediction Multiplier: 1.2
Jump Cooldown: 3.0
Stab Windup Time: 0.2
Stab Range: 1.5
```

---

## ToughEnemy Specific Settings

Add these to ToughEnemy component in Inspector:

```yaml
Shotgun Aim Time: 0.8
Shotgun Cone Angle: 30
Shotgun Range: 8.0
Shotgun Pellet Count: 8
Shotgun Damage Per Pellet: 5.0
```

---

## BasicEnemy Specific Settings

Add these to BasicEnemy component in Inspector:

```yaml
Delayed Position Time: 1.0
Attack Windup Time: 0.3
Thrust Distance: 2.0
```

---

## Testing Preset (Easy Mode)

For quick testing and iteration:

```yaml
Enemy Name: Test Dummy
Enemy Type: Basic
Max Health: 30
Pain Chance: 0.8
Stagger Duration: 1.0

Chase Speed: 2.0
Attack Damage: 5
Attack Cooldown: 3.0

Spawn Weight: 100
Min Room Iteration: 0
Max Per Room: 1
```

---

## Boss Preset (Future)

Template for boss enemy:

```yaml
Enemy Name: Boss
Enemy Type: Tough
Max Health: 500
Pain Chance: 0.05
Stagger Duration: 0.3

Chase Speed: 3.0
Attack Damage: 50
Attack Range: 10.0
Attack Cooldown: 3.0

Spawn Weight: 1
Min Room Iteration: 10
Max Per Room: 1
```

---

## Notes

### Balancing Tips:
- **Health**: 1 hit = ~20 damage (weapon dependent)
- **Pain Chance**: High for weak, low for tanks
- **Chase Speed**: 2-3 = slow, 4-5 = medium, 6+ = fast
- **Attack Cooldown**: Match to player weapon fire rate
- **Spawn Weight**: Keep total ~100 for easy percentages

### Difficulty Curve:
- Start with 2-3 Basic enemies
- Add Tough at iteration 2
- Add Fast at iteration 3
- Introduce elites at iteration 4-5
- Mix all types at iteration 6+

### Room Size Considerations:
- Small rooms: 2-4 enemies max
- Medium rooms: 4-6 enemies
- Large rooms: 6-8 enemies
- Corridors: 1-2 enemies only

---

**Use these presets as starting points and adjust based on your game's feel!**
