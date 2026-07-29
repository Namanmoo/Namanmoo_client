# Extensible Weapon System Design

## Goal

Replace slot- and ID-specific weapon logic with a reusable weapon-definition
template. The system must support multiple variants of spear, sword, axe,
projectile, and gun weapons without requiring a new attack component for each
individual weapon.

## Scope

Primary implementation is limited to:

- `Assets/Scripts/Combat`
- `Assets/Scripts/Items`
- `Assets/Scripts/Enemies`

The following locations are allowed as explicit exceptions:

- `Assets/Scenes` for renaming Stage 1 to SampleStage
- `Assets/Tests` for automated coverage
- Existing scene builders, bootstrap code, and scene references that must change
  for the SampleStage rename and sample loadout
- Weapon-definition assets and generated placeholder visual assets

Enemy behavior, boss patterns, monster patterns, and a new damage-calculation
system are outside scope. Weapon hits continue to call the existing
`EnemyHealth.TakeDamage(int)` boundary.

## Weapon Taxonomy

```csharp
public enum WeaponCategory
{
    Melee,
    Ranged
}

public enum WeaponType
{
    Spear,
    Sword,
    Axe,
    Projectile,
    Gun
}
```

`WeaponCategory` is the broad attack family. `WeaponType` selects the attack
shape and behavior within that family.

The valid pairings are:

- `Melee`: `Spear`, `Sword`, `Axe`
- `Ranged`: `Projectile`, `Gun`

Weapon definitions reject or normalize invalid category/type combinations.

## Weapon Definition Template

`WeaponDefinition` is a `ScriptableObject`. Designers create additional weapons
by duplicating an asset and changing values rather than adding code.

Each definition contains:

- Stable weapon ID
- Display name
- `WeaponCategory`
- `WeaponType`
- Damage
- Attack interval in seconds
- Reach
- Collision radius
- Attack arc in degrees
- Projectile speed
- Projectile lifetime
- Optional hotbar icon
- Optional world/attack Sprite
- Optional display color used by placeholder visuals

All numeric combat fields are serialized, validated to non-negative or
strictly-positive ranges as appropriate, and independently editable per asset.
Fields unused by a weapon type remain harmless configuration data.

`ItemData` optionally references a `WeaponDefinition`. Existing non-weapon
items remain supported. Weapon consumers use the definition instead of
hard-coded item IDs or slot indexes.

## Input Contract

- Arrow keys remain the attack input.
- Exactly one cardinal direction must be held.
- Any diagonal combination is rejected for every weapon type.
- The first attack fires immediately when its cooldown is ready.
- Holding one direction repeats at the equipped definition's attack interval.
- Releasing, re-pressing, changing direction, or reselecting the weapon does not
  bypass the cooldown.
- Equipping a different weapon uses that weapon instance's controller timing
  without relying on a fixed hotbar slot.

## Attack Behavior

### Spear

- Melee, cardinal-only thrust.
- Uses the configured long reach.
- Hits targets within a narrow line/capsule extending from the player in the
  selected direction.
- Does not hit targets behind or substantially beside the player.

### Sword

- Melee, cardinal-only directional slash.
- Uses the configured medium reach.
- Hits targets within a 90-degree sector centered on the selected direction.
- The arc remains data-configurable, with the SampleStage example set to 90.

### Axe

- Melee, cardinal input triggers the attack.
- Uses the configured short reach.
- Hits every valid target within a 360-degree circle.
- Input direction affects only optional presentation, not hit eligibility.

### Projectile

- Ranged projectile with a relatively slow configured attack interval.
- Uses a relatively large configured circular collision radius.
- Speed, lifetime, damage, radius, and cooldown come from the definition.

### Gun

- Ranged projectile with a relatively fast configured attack interval.
- Uses a relatively small configured circular collision radius.
- Speed, lifetime, damage, radius, and cooldown come from the definition.

Each single attack damages an enemy at most once. Owner colliders and objects
without `EnemyHealth` are ignored.

## Runtime Components

`PlayerWeaponController` becomes the single input and cooldown coordinator. It
reads the equipped item's `WeaponDefinition` and delegates to reusable melee or
ranged attack execution.

Melee hit eligibility is isolated in testable geometry functions:

- Cardinal input validation
- Spear line/capsule membership
- Sword sector membership
- Axe radius membership

Ranged attacks use one configurable projectile component. Its collider radius,
speed, lifetime, damage, Sprite, and placeholder color come from the equipped
definition.

The old slot-specific `PlayerSwordShooter` and `PlayerAxeAttacker` behavior is
migrated to the generic controller. Compatibility wrappers may remain only if
required to migrate serialized scenes safely; no new logic should depend on
them.

## Inventory and Hotbar

The default `ItemHotbarController` creates an empty `PlayerInventory`. It does
not automatically add any starting weapon.

Sample loadout injection is separate from the general inventory controller.
Only SampleStage receives five example weapons:

1. Axe
2. Projectile
3. Spear
4. Sword
5. Gun

The existing first weapon's rotating thrown-sword behavior becomes the
`Projectile` example in slot two. The existing axe becomes the `Axe` example in
slot one.

The examples use configurable placeholder values that demonstrate relative
differences:

- Spear: longest melee reach and narrow cardinal hit shape
- Sword: medium melee reach and 90-degree sector
- Axe: shortest melee reach and full-circle hit shape
- Projectile: slower interval and larger collision radius
- Gun: faster interval and smaller collision radius

These are sample definition values, not hard-coded type constants.

## Visuals

The existing axe and thrown-sword assets are reused for the corresponding
SampleStage examples.

Missing spear, sword, and gun art uses generated solid-color placeholder
Sprites. Definitions expose icon and world Sprite references so replacing art
requires no combat code change.

## Scene Rename

`Assets/Scenes/Stage1.unity` becomes
`Assets/Scenes/SampleStage.unity`.

All build configuration, title-scene loading, scene-builder paths, runtime
bootstrap references, and tests are updated to use `SampleStage`. Existing
scene content and the user's player-relative camera configuration are
preserved.

The builder may retain internal Stage 1 class names where renaming would add
unrelated churn, but no runtime scene lookup may depend on the old asset name.

## Error Handling

- Null or invalid weapon definitions do not attack.
- Invalid or diagonal input does not attack.
- Invalid cooldown, reach, radius, speed, lifetime, arc, and damage values are
  clamped during validation.
- Category/type mismatches are surfaced by validation and cannot execute an
  incompatible attack path.
- Missing optional Sprites use placeholder visuals.

## Testing

Automated coverage includes:

- Enum values and valid category/type pairings
- Weapon-definition validation and independent per-asset values
- `ItemData` weapon-definition association
- Empty default inventory and hotbar
- SampleStage-only five-weapon loadout and slot order
- Cardinal-only input and diagonal rejection
- Spear long narrow hit geometry
- Sword directional 90-degree sector geometry
- Axe short 360-degree geometry
- One-hit-per-enemy melee behavior
- Projectile and gun cooldown differences
- Configurable ranged collision radii
- Owner and non-enemy collision rejection
- Scene rename references and SampleStage build
- Regression coverage for existing inventory selection and hotbar rendering
