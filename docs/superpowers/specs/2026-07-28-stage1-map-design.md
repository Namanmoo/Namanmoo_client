# Stage 1 Map Design

## Goal

Create `Assets/Scenes/Stage1.unity` from `mapEX1.png` as a playable 2D top-view stage.

## Scene Structure

- `Main Camera`: Orthographic camera centered on the complete stage.
- `Global Light 2D`: Global light for the URP 2D renderer.
- `Stage Map`: A simplified polygon matching the reference image:
  - a broad upper room,
  - a broad lower room,
  - a narrow vertical connector near the center.
- `Boundary`: A closed black outline around the stage with matching 2D collision.
- `Player`: A visible circular player placed safely inside the lower room.

## Map Geometry and Rendering

The stage uses a single concave polygon represented by clockwise points in local space. A generated mesh fills the polygon with medium gray. A closed `LineRenderer` draws a black outline above the fill. Small hand-drawn irregularities in the reference are intentionally smoothed while preserving its proportions and connected-room layout.

The map occupies approximately 18 world units horizontally and 16 vertically. The central connector remains wide enough for the circular player to pass comfortably.

## Collision

A closed `EdgeCollider2D` uses the same polygon points as the visible outline. The player uses a `CircleCollider2D` and a dynamic `Rigidbody2D`, so the outline prevents it from leaving the gray area. The player's gravity scale is `0`, interpolation is enabled, and Z rotation is frozen.

## Player

The player is a simple colored circle rendered above the map. It has `PlayerMovement` attached with the default speed of `5` and starts near the center of the lower room, away from the boundary.

## Implementation

An Editor-only scene builder creates the polygon mesh, outline, collider, circular player sprite, camera, and light, then saves the result as `Assets/Scenes/Stage1.unity`. Generated reusable assets live under `Assets/Stage1`.

## Verification

- Run the builder in Unity batch mode.
- Confirm `Stage1.unity` and its generated assets exist.
- Open/compile the scene in Unity without C# errors.
- Inspect the saved scene for the camera, map, closed boundary collider, and configured player.
- Run a Play Mode test that moves the player toward an outer wall and verifies its center remains inside the stage boundary.

## Scope

Decorations, enemies, animation, camera following, scene transitions, and additional gameplay systems are not included.
