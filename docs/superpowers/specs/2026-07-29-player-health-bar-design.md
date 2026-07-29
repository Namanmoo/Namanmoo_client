# Player Health Bar Design

## Goal

Add a compact, cartoon-style player health bar to the top-left of Stage 1. The UI uses `HP_heart.png` as its heart icon and follows the visual character of `HP_bar.png` without embedding the reference image's fixed heart, number, or fill.

## Player Health

- Add a dedicated `PlayerHealth` component with a default maximum and current health of 20.
- `TakeDamage(int amount)` ignores non-positive values and clamps current health to zero.
- Health reaching zero does not destroy or disable the player in this iteration.
- The component publishes health changes so the UI does not poll every frame.
- Pressing `H` during play applies one point of test damage.
- Future enemy attacks can call the same `TakeDamage` API.

## Health Bar UI

- Create a screen-space overlay canvas anchored to the top-left.
- Use a compact footprint of 260 by 54 reference pixels, inset 24 pixels from the top and left edges.
- Place `HP_heart.png` at the left.
- Display current and maximum health as dynamic text, initially `20/20`.
- Draw a cream-colored track with a dark outline and a red fill to evoke the hand-drawn reference.
- The red fill is left-anchored and scales horizontally according to `CurrentHealth / MaxHealth`.
- The heart, text, and bar remain separate UI elements so no fixed content from `HP_bar.png` overlaps dynamic content.
- The canvas uses `Scale With Screen Size` so the UI keeps a stable perceived size across common resolutions.

## Integration

- Import the heart image as a Unity Sprite under `Assets/UI`.
- Add the health component and UI through both supported Stage 1 construction paths:
  - `Stage1SceneBuilder`
  - `Stage1RuntimeBootstrap`
- Keep health UI construction in focused setup/factory/view classes, following the existing item-hotbar pattern.
- Rebuild `Assets/Scenes/Stage1.unity` through the existing scene builder after the code and assets are ready.

## Validation

- Editor tests cover initial health, damage, clamping, ignored invalid damage, and health-change notification.
- UI tests cover top-left anchoring, compact dimensions, heart sprite assignment, initial text, and fill ratio updates.
- Scene-builder tests confirm the player owns `PlayerHealth` and the generated scene contains a correctly configured health canvas.
- A Play Mode test verifies that applying damage updates the visible text and fill.
- Run the relevant Edit Mode and Play Mode suites, rebuild Stage 1, and inspect build/test logs for errors.

## Scope

This iteration does not add enemy attacks, healing, regeneration, death behavior, animation, sound, or saved health. The `H` key is intentionally a temporary test input.
