# Configurable Melee Weapon Motion Design

## Goal

Add visible, weapon-specific attack motion for axe, sword, and spear attacks while keeping damage, attack interval, range, and motion presentation independently configurable for every weapon through Unity Inspector weapon assets.

## Architecture

`WeaponDefinition` remains the single source of truth for weapon identity, combat balance, sprites, and presentation settings. Each production weapon is represented by a separate `WeaponDefinition` ScriptableObject asset. `PlayerWeaponController` reads the equipped definition, preserves the existing hit and cooldown behavior, and creates a short-lived melee visual for melee weapons.

The melee visual and its time-based animation live in a focused `MeleeWeaponVisual` component. It does not perform damage or search for enemies. This keeps presentation independent from combat behavior and prevents animation timing from changing whether an enemy is hit.

## Inspector Configuration

Every `WeaponDefinition` exposes the existing combat fields:

- Damage
- Attack interval
- Reach
- Collision radius
- Attack arc

It also exposes melee presentation fields with safe defaults:

- Motion duration: `0.25` seconds
- World sprite
- Display color
- Visual scale: `1`
- Visual distance from the player: `0.75`
- Start angle
- End angle

The meaning of the motion fields depends on weapon type:

- Axe rotates through a full circular swing around the player.
- Sword sweeps through a directional arc.
- Spear moves outward in the attack direction and returns.

All numeric values are clamped in `OnValidate()` and `Configure()` so invalid Inspector or runtime values cannot create negative duration, range, scale, or distance.

## Default Weapon Assets

Create separate Unity assets for the sample axe, projectile weapon, spear, sword, and gun. Their initial combat values match the current `SampleWeaponFactory` defaults:

| Weapon | Damage | Interval | Reach | Radius | Arc |
|---|---:|---:|---:|---:|---:|
| Axe | 10 | 1.0 | 1.3 | 0.2 | 360 |
| Projectile | 5 | 0.8 | 0 | 0.6 | 0 |
| Spear | 8 | 0.8 | 3.0 | 0.2 | 20 |
| Sword | 7 | 0.6 | 2.0 | 0.2 | 90 |
| Gun | 3 | 0.2 | 0 | 0.15 | 0 |

The sample loadout accepts Inspector-assigned assets and uses them before any fallback definitions. `SampleWeaponFactory` remains available as a safe runtime fallback and supplies the same presentation defaults when no assets are assigned.

## Attack Flow

1. `PlayerWeaponController` reads the equipped `WeaponDefinition`.
2. Direction input and the configured attack interval are checked as they are today.
3. For a melee weapon, the controller immediately performs the existing geometry-based damage calculation.
4. The controller creates a child visual using the definition's world sprite and presentation values.
5. `MeleeWeaponVisual` animates according to `WeaponType` and destroys itself when the configured motion duration elapses.
6. For a ranged weapon, the existing projectile behavior is unchanged.

Each attack creates its own visual, so rapid weapons can begin a new motion according to their configured attack interval without corrupting an earlier visual.

## Motion Behavior

### Axe

The visual starts in the input direction at the configured distance and completes a 360-degree rotation during the motion duration.

### Sword

The visual is anchored in the input direction and interpolates from the configured start angle to the configured end angle. The default sweep is centered on the attack direction.

### Spear

The visual points in the input direction. It interpolates from the player to the configured visual distance, then returns using a triangular time curve.

## Error Handling

- A missing world sprite skips visual creation but does not skip damage.
- A missing or invalid equipped definition continues to block attacks as it does now.
- Non-melee weapon types cannot create a melee visual.
- Zero or negative presentation values are clamped to safe minimums.
- Destroyed players automatically destroy child melee visuals.

## Testing

Editor tests will verify:

- Presentation values use the documented defaults and clamp invalid input.
- Axe, sword, and spear create a melee visual when attacking.
- The created visual receives the equipped weapon's sprite, color, scale, and duration.
- Each weapon type follows its intended motion at representative normalized times.
- Missing sprites do not prevent melee damage.
- Ranged weapons continue to create projectiles and do not create melee visuals.

Existing geometry, damage, inventory, scene integration, and projectile tests must continue to pass.

## Scope

This change adds code-driven sprite motion only. It does not add Animator Controller clips, character-body animations, sound effects, particles, camera shake, or new weapon types.
