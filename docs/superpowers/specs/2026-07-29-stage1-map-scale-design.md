# Stage1 Map 2.5x Scale Design

## Goal

Increase the playable Stage1 map footprint to 2.5 times its current size while
keeping the camera's visible world area unchanged.

## Map Geometry

- Keep the existing `OutlinePoints` as the unscaled source shape.
- Add a single map scale value of `2.5f`.
- Build one cached scaled outline from the source points.
- Expose the scaled outline through `Stage1MapDefinition.Outline`.
- Triangulate the scaled outline so the floor mesh, boundary renderer, and edge
  collider all consume the same coordinates.
- Make `Stage1MapDefinition.Contains` test against the scaled outline.

## Unchanged Behavior

- Keep the Stage1 camera orthographic size at `10f`.
- Keep the player position, visual size, collision size, and movement speed
  unchanged.
- Keep boundary line width and collider edge radius unchanged in world units.
- Keep the item hotbar and other UI unchanged.

## Scene Integration

- `Stage1SceneBuilder` and `Stage1RuntimeBootstrap` continue consuming
  `Stage1MapDefinition.Outline`; no independent scaling is applied in either
  builder.
- Rebuild `Assets/Scenes/Stage1.unity` after the definition changes.

## Verification

- Map-definition tests verify representative source coordinates become exactly
  2.5 times larger.
- Tests verify points inside the expanded region are accepted and points beyond
  the scaled boundary are rejected.
- Scene-builder tests verify the camera remains at orthographic size `10f` and
  the generated boundary uses scaled coordinates.
- Existing EditMode and PlayMode test suites must remain green.

