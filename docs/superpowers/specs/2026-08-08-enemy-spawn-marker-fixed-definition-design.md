# EnemySpawnMarker Fixed Definition Design

## Goal

Let a designer optionally pin a specific `EnemyDefinition` to an individual
`EnemySpawnMarker` inside a `RoomContentTemplate`, so a template can mix
designer-chosen enemies with the existing random selection at the same time.

## Scope

- Add an optional `EnemyDefinition` reference to `EnemySpawnMarker`.
- When a marker specifies one, that marker always spawns that definition.
- When a marker leaves it unset, behavior is unchanged: the marker's slot is
  filled by the existing `DungeonEncounter.SelectDefinitions` random pool
  logic.
- A template may freely mix fixed and unfixed markers.

Out of scope: any change to obstacle placement, to `RoomContentTemplate`'s
gizmos, to `DungeonLayout`/`RoomShape`/`RoomBuilder`, or to Boss/Treasure/
Shop/Start room behavior.

## Components

### EnemySpawnMarker (modified)

Adds `[SerializeField] private EnemyDefinition fixedEnemyDefinition;` and a
public `FixedEnemyDefinition` getter. `null` means "no fixed type — use the
random pool," matching the marker's existing default (empty) authoring
state, so every template built before this change keeps working unchanged.

### RoomContentTemplate (modified)

`SpawnMarkerPositions() : List<Vector2>` is renamed to
`SpawnMarkers() : List<EnemySpawnMarker>`, returning the marker components
themselves instead of just their positions. Callers read both
`.transform.position` and `.FixedEnemyDefinition` off each returned marker.
No new type is introduced — `EnemySpawnMarker` already carries everything a
caller needs.

### DungeonEncounter (modified)

`SpawnEnemies` resolves an `EnemyDefinition` per marker instead of per
position:

1. Instantiate the selected template and call `SpawnMarkers()`.
2. Count markers whose `FixedEnemyDefinition` is `null` — call this
   `randomCount`.
3. Call the existing `SelectDefinitions(normalEnemyDefinitions, randomCount,
   roomSeed)` to fill exactly the unfixed slots.
4. Walk the markers in order. A marker with a fixed definition uses it
   directly. A marker without one consumes the next entry from the random
   selection, in order.
5. If the random selection came up short (empty or under-sized
   `normalEnemyDefinitions` pool), the unfixed markers that ran out are
   skipped — they spawn nothing, but fixed markers are unaffected and still
   spawn.
6. If no marker produced an enemy at all, `SpawnEnemies` returns `null`,
   matching today's "nothing to fight" behavior.

This replaces the current `if (definitions.Length == 0) { return null; }`
gate, which no longer describes the right condition once fixed markers can
guarantee enemies independent of the random pool's size.

## Data Flow and Lifecycle

Unchanged outside the resolution logic above: template selection is still
seeded from `roomSeed`, so re-entering a room reproduces the same template,
the same fixed/random split, and the same random assignment for the unfixed
slots.

## Testing

- Update `RoomContentTemplateTests.cs`'s two existing tests for the
  `SpawnMarkers()` rename (assert on `.transform.position` of each returned
  marker instead of a raw `Vector2` list).
- Add a test proving a marker with `FixedEnemyDefinition` set always spawns
  that definition, regardless of the random pool or seed.
- Add a test proving mixed fixed/unfixed markers on the same template each
  resolve correctly (fixed marker gets its assigned definition, unfixed
  marker gets one from the random pool).
- Add a test proving an unfixed marker with an empty/insufficient random pool
  is skipped while a sibling fixed marker still spawns.
- Preserve the existing `DungeonEncounterTests` and `RoomContentTemplateTests`
  behavior for templates with no fixed markers (today's tests should keep
  passing with only the mechanical `SpawnMarkers()` rename applied).
