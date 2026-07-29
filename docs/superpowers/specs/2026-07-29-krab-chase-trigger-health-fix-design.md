# Krab Chase, Trigger Contact, and Health UI Fix

## Goal

Fix the Stage1 krab encounter so that saved-scene krabs chase the player,
move at half the player's speed, overlap the player without physically
blocking movement, deal contact damage, and visibly update both the player's
health number and gauge.

## Root Cause

`KrabEnemy.Initialize` assigns its player target to a non-serialized field.
The editor scene builder supplies the target before saving Stage1, but the
reference is lost when the scene is reloaded. The krabs therefore neither
chase nor reach the player to apply damage. Existing health model and view
unit tests show that a successful `PlayerHealth` damage event updates the
number and fill amount.

## Behavior

- Each Stage1 krab stores a persistent reference to the Stage1 player.
- Krabs move toward that player at 2.5 world units per second.
- Player speed remains 5 world units per second.
- A krab retains a solid main collider for map-boundary collision.
- Each krab ignores collision response only against the player's collider,
  allowing their transforms to overlap without pushing.
- A separate trigger contact sensor detects the overlapping player.
- Trigger enter and trigger stay both use the existing protected damage path:
  2 damage followed by one second of player invulnerability.
- A successful contact hit changes the health text and fill amount together.
- Krabs remain dynamic Rigidbody2D objects and retain physical map-boundary
  collision. Only the krab-player collider pair ignores collision response.

## Implementation

1. Serialize the `KrabEnemy` target reference assigned by the shared Stage1
   encounter setup.
2. Change the default krab speed from 3 to 2.5.
3. Replace player damage collision callbacks with trigger callbacks.
4. Keep the main krab collider solid, add a trigger contact sensor, and ignore
   the main krab-player collision pair.
5. Rebuild Stage1 so the player references and trigger settings are saved.

## Testing

- A focused unit test asserts a calculated chase velocity of 2.5.
- A scene integration test asserts all five saved krabs reference the player,
  use speed 2.5, retain solid boundary colliders, and have trigger sensors.
- A PlayMode test overlaps a krab and player and verifies:
  - health changes from 20 to 18;
  - the UI text changes to `18/20`;
  - the fill amount changes to `0.9`;
  - neither object is physically pushed apart.
- Existing invulnerability, encounter gate, EditMode, and PlayMode suites
  remain green.

## Scope

This change does not alter player speed, enemy health, enemy count, weapon
damage, gate rules, map layout, or the existing one-second invulnerability
duration.
