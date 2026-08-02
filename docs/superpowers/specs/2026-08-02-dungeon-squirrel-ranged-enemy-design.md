# Dungeon Squirrel Ranged Enemy Design

## Goal

Add a ranged Squirrel enemy to Dungeon normal rooms. Krabs and Squirrels must
appear together with deterministic 50:50 selection for remaining enemy slots,
while every populated normal room containing at least two enemies includes at
least one of each type.

## Scope

- Use `Assets/Enemies/enemy_squirrel.png` as the Squirrel body sprite.
- Use `EnemyBehaviorType.ApproachAndShoot` and the existing
  `ApproachAndShootEnemyController`.
- Create a persistent temporary blue-square projectile sprite.
- Create a persistent `DungeonSquirrel.asset` `EnemyDefinition`.
- Generalize `DungeonEncounter` from one normal definition to an array of
  normal definitions.
- Configure Dungeon with the existing Krab definition and the new Squirrel
  definition.
- Keep Dungeon boss spawning, Stage 1 Krab spawning, Hotbar, and HotKey
  behavior unchanged.

## Squirrel Definition

- ID: `squirrel`
- Display name: `Squirrel`
- Body sprite: `Assets/Enemies/enemy_squirrel.png`
- Projectile sprite: a generated blue-square PNG asset
- Behavior: `ApproachAndShoot`
- Maximum health: 5
- Movement speed: 2
- Attack damage: 1
- Attack range: 7
- Attack interval: 1.5 seconds
- Projectile speed: 6
- Projectile lifetime: 3 seconds
- Projectile radius: 0.2

The temporary projectile is a persistent project asset so the serialized
definition remains valid across scene loads. Replacing it with an acorn later
requires only changing `DungeonSquirrel.asset.ProjectileSprite`.

## Architecture

The Dungeon spawn flow becomes:

```text
DungeonEncounter
  -> normalEnemyDefinitions[]
     -> DungeonKrab.asset
     -> DungeonSquirrel.asset
  -> deterministic definition selection
  -> EnemyFactory.Create(selectedDefinition, request)
```

`DungeonEncounter` owns encounter composition and definition selection.
`EnemyDefinition` owns identity, sprites, statistics, and behavior type.
`EnemyFactory` continues to own runtime GameObject assembly and attaches the
existing controller selected by `BehaviorType`.

## Selection Rules

For a normal room with `N` spawn positions:

1. If `N` is zero, spawn nothing.
2. Filter out null definition references.
3. When both configured definitions are available and `N >= 2`, reserve one
   slot for Krab and one for Squirrel.
4. Fill the remaining `N - 2` slots by deterministic 50:50 selection using
   `roomSeed`.
5. Deterministically shuffle the resulting definition order so the guaranteed
   types do not always occupy the first two spawn positions.
6. Pass each selected definition and its existing spawn position to
   `EnemyFactory.Create`.

The same room seed, definitions, and room data must always produce the same
enemy-type sequence. With only one valid definition, every slot uses it. This
keeps missing optional data from emptying an otherwise valid room.

## Naming

Runtime instance names derive from the selected definition:

```text
Krab 1
Squirrel 1
Krab 2
```

Counters are per definition within the room.

## Asset and Scene Wiring

`DungeonSceneBuilder` creates or updates:

- `Assets/Enemies/DungeonKrab.asset`
- `Assets/Enemies/DungeonSquirrel.asset`
- `Assets/Enemies/TemporaryBlueProjectile.png`

It assigns the two definitions to `DungeonEncounter.normalEnemyDefinitions`.
The checked-in Dungeon scene is updated only at that serialized field. Tests
must not call `DungeonSceneBuilder.Build()`, because doing so regenerates
unrelated scene UI such as Hotbar and HotKey objects.

## Validation

- `EnemyFactory` remains responsible for validating each selected definition.
- The Squirrel definition satisfies all ranged projectile validation.
- Missing/null definitions are ignored before selection.
- If no valid normal definition remains, the room produces no normal
  encounter, matching the existing missing-definition behavior.

## Testing

- A normal room with at least two enemies contains both a
  `ChaseContactEnemyController` and an `ApproachAndShootEnemyController`.
- Squirrel health is 5 and its body sprite comes from
  `enemy_squirrel.png`.
- Squirrel projectiles use the temporary blue-square sprite.
- Repeated selection with the same seed produces the same definition order.
- Remaining slots exercise both outcomes across fixed hand-picked seeds.
- The stored Dungeon scene references both definitions without rebuilding the
  scene.
- Existing Dungeon scene and boss PlayMode tests remain green.
- The `Dungeon.unity` diff contains no Hotbar or HotKey changes.

