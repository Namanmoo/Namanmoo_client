# Player Sprite Replacement Design

## Goal

Replace the Stage1 player's generated blue circle visual with the supplied
`player.png` artwork while preserving all existing movement, collision, inventory,
and hotbar behavior.

## Asset

- Preserve the root `player.png` source file.
- Copy it to `Assets/Player/player.png`.
- Import it as a single transparent Sprite.
- Preserve the original 221:354 aspect ratio and white artwork.

## Player Rendering

- Both the saved Stage1 scene builder and runtime bootstrap must reference the
  imported player Sprite.
- Set `SpriteRenderer.color` to opaque white so the artwork is not tinted.
- Scale the renderer so the visual height is approximately 2 world units while
  preserving the source aspect ratio.
- Keep sorting order 4 and the existing player position.

## Gameplay Compatibility

- Keep `Rigidbody2D` settings unchanged.
- Keep the existing `CircleCollider2D` with radius 0.5.
- Keep `PlayerMovement`, inventory, and item-hotbar setup unchanged.
- Missing Sprite references must fail early with an actionable message rather
  than silently restoring the generated circle.

## Scene and Runtime Paths

- `Stage1SceneBuilder` loads `Assets/Player/player.png`, applies the Sprite and
  scale, then saves the updated Stage1 scene.
- `Stage1RuntimeBootstrap` receives the same Sprite through a serialized field,
  fills it automatically in the editor, and validates it before creating the
  player.

## Verification

- Editor tests verify Sprite import settings, scene Sprite reference, white
  renderer color, preserved aspect ratio, approximate 2-unit height, and
  unchanged collider radius.
- Existing movement, item-hotbar EditMode, and PlayMode tests remain green.
- Rebuild `Assets/Scenes/Stage1.unity` and verify the serialized player setup.

