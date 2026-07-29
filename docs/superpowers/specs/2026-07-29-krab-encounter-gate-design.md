# Krab Encounter Gate Design

## Goal

Add five `enemy_krab.png` enemies to the lower Stage 1 combat area. Each krab chases the player, is constrained by the same physical map boundary, has 5 health, deals 2 contact damage, and grants the player one second of global damage invulnerability. A physical gate blocks the middle passage until all five krabs are dead.

## Enemy Asset and Presentation

- Copy the exact source image to `Assets/Enemies/enemy_krab.png`.
- Import it as a transparent, clamped, non-mipmapped Single Sprite.
- Preserve the image aspect ratio and render each enemy at 2 world units tall.
- Use sorting order 4 so enemies share the gameplay character layer.

## Krab Configuration

- Create exactly five krabs in the lower map area at fixed, non-overlapping positions.
- Give each krab an `EnemyHealth` maximum and current health of 5.
- Give each krab a dynamic `Rigidbody2D` with zero gravity, interpolated movement, continuous collision detection, and frozen rotation.
- Use a `CircleCollider2D` sized to the central body so claws and legs do not create oversized collision.
- Move toward the live player at 3 world units per second using `Rigidbody2D.MovePosition` in `FixedUpdate`.
- Stop moving when the player reference is absent.
- Because krabs use normal dynamic bodies and colliders, the existing Stage 1 `EdgeCollider2D` boundary constrains them in the same way as the player.

## Player Contact Damage and Invulnerability

- On collision with the player, request 2 damage through `PlayerHealth`.
- `PlayerHealth` owns the global invulnerability deadline so simultaneous contact from multiple enemies cannot bypass it.
- A successful contact hit starts one second of invulnerability.
- Damage requests during invulnerability are rejected without changing health or notifying the health UI.
- Non-positive damage remains ignored.
- The temporary `H` debug damage uses the same protected damage path.
- Player death behavior remains unchanged: health clamps at zero and the player remains present.

## Encounter Gate

- Place a visible horizontal gate across the central passage at world position `(-4.5, 0.5)` with size `(13, 0.6)`.
- Give the gate a solid `BoxCollider2D`, so both player and enemies are physically blocked.
- Render it above the floor with a dark outline and red interior matching the hand-drawn UI palette.
- Register exactly the five lower-area krabs with `Stage1EncounterGate`.
- `EnemyHealth` emits a death event once when health reaches zero.
- The gate tracks registered living enemies through those events.
- When the living count reaches zero, disable the collider and hide the visuals immediately.
- Destroyed or missing registered enemies also count as defeated without blocking the gate permanently.

## Construction Paths

- `Stage1SceneBuilder` loads and validates the krab Sprite, creates the five enemies and gate, and saves them into `Assets/Scenes/Stage1.unity`.
- `Stage1RuntimeBootstrap` assigns the same Sprite in editor validation and creates the same encounter under `Generated Stage`.
- Both paths share focused enemy and gate setup code so positions, health, movement, damage, and gate behavior cannot drift.

## Validation

- Editor tests cover configured health 5, chase direction and speed, missing-player behavior, global one-second invulnerability, contact damage 2, one-shot death notification, five-enemy registration, and gate opening only after all five deaths.
- Play Mode physics tests verify boundary collision, real player contact damage, simultaneous-contact invulnerability, weapon damage, and gate collider removal.
- Scene integration tests verify the exact imported Sprite, five lower-area enemies, their physics settings and positions, gate coordinates, and both construction paths.
- Rebuild Stage 1 and run the complete Edit Mode and Play Mode suites.

## Scope

This iteration does not add enemy attack animations, knockback, pathfinding around obstacles, spawning waves, loot drops, enemy health bars, respawning, or a next-stage transition beyond opening the middle passage.
