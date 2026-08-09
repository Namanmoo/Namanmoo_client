# Dungeon Normal Room Content Template Design

## Goal

Replace the randomly-computed enemy spawn positions in Normal dungeon rooms
with hand-authored "room content" prefabs that the designer builds in the
Unity Editor, so obstacle placement and monster placement can be art-directed
instead of procedurally scattered.

## Scope

- Add a pool of hand-authored `RoomContentTemplate` prefabs for Normal rooms
  only. Boss, Treasure, Shop, and Start rooms are unaffected.
- Each template contains freely placed obstacle GameObjects (sprite +
  `Collider2D`, using art the designer supplies) and a fixed number of
  `EnemySpawnMarker` points.
- `DungeonEncounter` picks one template per Normal room (deterministically,
  from the existing room seed) and instantiates it under the room root.
- Enemy count per Normal room becomes whatever number of markers the chosen
  template has. The current distance-from-start difficulty scaling
  (`RoomSpawnPoints.EnemyCount`) is removed for Normal rooms.
- Enemy *type* selection still uses the existing `DungeonEncounter.
  SelectDefinitions` logic, now assigning types to marker positions instead of
  `RoomSpawnPoints`-generated positions.

Out of scope: obstacles or templates for Boss/Treasure/Shop/Start rooms,
changes to `DungeonLayout` (room graph generation), changes to `RoomShape` or
`RoomBuilder` (walls, floor, doors stay fully procedural), and any change to
existing balance values (movement speed, health, damage, etc.).

## Architecture

Current flow:

```text
DungeonRunner.Enter
  -> RoomBuilder.Build        (ground, walls, doors — unchanged)
  -> DungeonEncounter.Spawn
       -> SpawnEnemies (Normal) -> RoomSpawnPoints.Inside (random positions)
       -> SpawnBoss (Boss)      (unchanged)
```

New flow for Normal rooms only:

```text
DungeonEncounter.Spawn
  -> SpawnEnemies (Normal)
       -> pick RoomContentTemplate from pool (seeded by roomSeed)
       -> Instantiate(template, roomRoot)
       -> read EnemySpawnMarker positions from the instance
       -> SelectDefinitions assigns enemy types to those positions (unchanged)
```

The template system lives entirely inside `DungeonEncounter`. `IRoomEncounter`,
`RoomBuilder`, and `DungeonRunner` are not touched — this keeps the change
isolated the same way the existing `normalEnemyDefinitions` array is scoped to
`DungeonEncounter`.

## Components

### RoomContentTemplate (new)

- A prefab root component. Holds no required fields; its job is to be a
  recognizable prefab type for the designer and (optionally) draw Scene-view
  gizmos while authoring (see Authoring Conventions).
- Children are whatever the designer places: obstacle GameObjects (sprite +
  `Collider2D`) anywhere, and `EnemySpawnMarker` children marking enemy spots.

### EnemySpawnMarker (new)

- Empty marker `MonoBehaviour`, no fields. Its Transform position is the only
  data read at runtime.

### DungeonEncounter (modified)

- Add `[SerializeField] private RoomContentTemplate[] normalRoomTemplates;`
- In `SpawnEnemies`, replace the `RoomSpawnPoints.Inside` call with:
  1. Pick a template from `normalRoomTemplates` using a `DeterministicRandom`
     seeded from `roomSeed` (same reproducibility guarantee as today — leaving
     and re-entering a room gives the same template and layout).
  2. Instantiate it under `roomRoot` at the local origin (matching
     `RoomShape.Bounds`, which is always centered at `(0, 0)`).
  3. Collect world positions from the instance's `EnemySpawnMarker` children.
  4. Feed those positions into the existing `SelectDefinitions` +
     `EnemyFactory.Create` path, unchanged.
- A room with zero configured templates behaves like a missing definition
  today: no gate, room treated as cleared on entry.

### RoomSpawnPoints

- No longer called from the Normal-room path. Left in place unchanged (it has
  its own tests and is simple, self-contained geometry math); nothing in this
  design requires deleting it.

## Authoring Conventions

Templates must work correctly no matter which of the four door sides a given
room instance actually has open, because the same template can be reused
across rooms with different door layouts. Designers should keep obstacles and
markers clear of:

- All four wall edges (`RoomShape.Bounds` is a fixed 44×30 rect centered at
  the origin) — inset at least `RoomSpawnPoints.WallInset` (5 units).
- All four potential door zones, not just the ones present in any one room —
  `RoomSpawnPoints.DoorClearance` (9 units) from each side's midpoint.
- Room center — `RoomSpawnPoints.CentreClearance` (5 units) — since that's
  roughly where the player lands entering some rooms.

`RoomContentTemplate` will draw these zones as Scene-view gizmos
(`OnDrawGizmos`, editor-only, no runtime effect) so designers can place
obstacles and markers against a visual guide instead of computing coordinates
by hand.

Registration: finished template prefabs are dragged into `DungeonEncounter`'s
`normalRoomTemplates` array in the Inspector, the same way enemy definitions
are assigned today.

## Data Flow and Lifecycle

Unchanged outside `SpawnEnemies`: `DungeonRunner` still destroys and rebuilds
the room root on every door transition, and rebuilding a previously-visited
room reproduces the same template pick and marker layout because template
selection is seeded from `roomSeed`, which is itself derived deterministically
from the dungeon seed, floor, and cell.

## Testing

- Add or update an Editor test proving `DungeonEncounter.SpawnEnemies` selects
  a template deterministically from `roomSeed` and spawns exactly as many
  enemies as the template has `EnemySpawnMarker` children.
- Preserve existing `DungeonEncounter`, `RoomSpawnPoints`, and Dungeon room
  transition tests — this change does not alter their inputs or outputs.
