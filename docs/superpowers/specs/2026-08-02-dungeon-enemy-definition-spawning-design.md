# Dungeon EnemyDefinition Spawning Design

## Goal

Change only the Dungeon normal-enemy spawning path so that
`DungeonEncounter` passes an `EnemyDefinition` directly to `EnemyFactory`.
Keep the existing Stage 1 encounter and boss spawning paths unchanged.

## Scope

- Replace `DungeonEncounter`'s Krab sprite reference with one serialized
  `EnemyDefinition` reference for normal rooms.
- Add a Dungeon Krab `EnemyDefinition` asset containing the same appearance,
  statistics, and `ChaseContact` behavior currently supplied by `KrabFactory`.
- Spawn normal-room enemies with `EnemyFactory.Create`.
- Preserve the existing room-dependent enemy count, deterministic spawn
  positions, instance names, room-clear gate, and boss behavior.
- Keep `Stage1KrabEncounterSetup -> KrabFactory -> EnemyFactory` intact.

This change does not add multiple normal-enemy definitions, weighted selection,
floor-specific encounter tables, or a new behavior pattern.

## Architecture

The normal Dungeon spawn flow becomes:

```text
DungeonRunner
  -> DungeonEncounter.Spawn
  -> DungeonEncounter.SpawnEnemies
  -> EnemyFactory.Create(EnemyDefinition, EnemySpawnRequest)
```

`DungeonEncounter` owns encounter composition: how many enemies appear, where
they appear, and which definition is used. `EnemyDefinition` owns identity,
appearance, statistics, and behavior selection. `EnemyFactory` owns runtime
GameObject assembly and attaches the controller selected by
`EnemyDefinition.BehaviorType`.

Boss rooms continue to use `BossFactory` because boss construction and UI are
outside the general-enemy path.

## Components

### DungeonEncounter

- Serialize one `EnemyDefinition normalEnemyDefinition`.
- Accept the definition through its test/runtime configuration API.
- Return no enemies when the definition is absent, matching the current
  missing-sprite behavior.
- For every selected spawn point, construct an `EnemySpawnRequest` using the
  room root, player target, position, and existing `"Krab N"` instance-name
  convention.
- Call `EnemyFactory.Create(normalEnemyDefinition, request)` directly.

### Dungeon Krab Definition

Create a project asset configured with the same values currently created by
`KrabFactory`:

- Stable Krab ID and display name.
- Existing Dungeon Krab sprite.
- `EnemyBehaviorType.ChaseContact`.
- Existing health, movement speed, and contact damage values.
- Existing normalized unused ranged values so validation remains valid.

The Dungeon scene references this asset from its `DungeonEncounter` component.

### KrabFactory

No behavior change. It remains available to `Stage1KrabEncounterSetup` and any
existing tests or callers.

## Data Flow and Lifecycle

When `DungeonRunner` enters an uncleared normal room, it calls
`DungeonEncounter.Spawn`. The encounter calculates the deterministic spawn
points from the room seed. Each point is converted into an
`EnemySpawnRequest`, and `EnemyFactory` creates the enemy from the serialized
definition. The returned `EnemyHealth` objects are passed to the existing
`Stage1EncounterGate`.

Entering a boss room continues through `BossFactory`. Entering a room whose
enemy count is zero, or whose normal definition is missing, produces no gate
and is treated as cleared by `DungeonRunner`, consistent with current behavior.

## Validation and Errors

`EnemyFactory` remains the authority for validating definitions and spawn
requests. Invalid assigned definitions fail with its existing argument
exceptions. A missing normal definition is treated as “no normal encounter”
rather than calling the factory with null.

## Testing

- Add or update an Editor test proving `DungeonEncounter` creates a normal
  enemy whose runtime components and values come from the supplied
  `EnemyDefinition`.
- Ensure the Dungeon scene serializes a non-null normal-enemy definition and
  no longer depends on the raw Krab sprite field.
- Preserve existing EnemyFactory, Dungeon room, Dungeon boss, and Stage 1 Krab
  tests.
- Run focused Editor/PlayMode tests followed by the available project build or
  full relevant test suite.

