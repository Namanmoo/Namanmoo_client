# Player Dash Steering and Charge UI Design

## Goal

Allow the player to steer instantly during a dash and show the remaining dash charges directly below the top-left player health bar.

## Dash Steering

- A dash still starts from the current movement direction, falling back to the last non-zero movement direction.
- While a dash is active, each non-zero WASD input immediately replaces the dash direction.
- Diagonal input is normalized so steering does not change dash speed.
- Releasing all movement keys preserves the most recent dash direction.
- Ordinary `PlayerMovement` physics remain suppressed until the dash ends, but `PlayerMovement` continues recording input so `PlayerDash` can steer from it.
- Dash duration, charge spending, recharge, invulnerability, and afterimages keep their existing behavior.

## Charge State Notifications

`PlayerDash` exposes a `ChargesChanged` event carrying current and maximum charges. The event fires when:

- a dash spends a charge;
- recharge restores a charge;
- `MaxCharges` changes and alters the number of slots or clamps current charges;
- a view connects and requests an initial render.

The existing read-only `CurrentCharges` and runtime-adjustable `MaxCharges` remain available.

## Charge UI

A focused `PlayerDashChargeView` owns only charge indicator rendering. It is created in the same overlay canvas as the player health bar and positioned directly below it.

- Indicator count always equals `MaxCharges`.
- Indicators are arranged horizontally from left to right.
- Available charges use a yellow circular fill with a gray circular outline.
- Spent charges use a gray circular fill with the same gray outline.
- The view rebuilds its indicator list when the maximum changes and only recolors existing indicators when the current count changes.
- Indicators are non-interactive and do not block raycasts.

Circles use Unity UI `Image` components backed by a small runtime-created circular sprite shared by the view. The outline is a slightly larger gray circle behind the fill, avoiding dependency on an external art asset.

## Construction and Scene Integration

`Stage1PlayerHealthSetup.Create` ensures `PlayerDash` exists, creates the health bar, then creates and binds `PlayerDashChargeView` beneath it. `PlayerFactory` continues configuring the player renderer for dash afterimages.

Both `SampleStage` and `Dungeon` are regenerated through their existing scene builders so their serialized player canvases contain the charge UI.

## Validation and Edge Cases

- A missing or disabled view does not affect dash behavior.
- Invalid maximum charge values remain clamped to at least one.
- Increasing maximum charges adds gray spent indicators because new capacity starts empty and recharges normally.
- Decreasing maximum charges removes excess indicators and clamps current charges.
- UI event subscriptions disconnect on disable to avoid duplicate updates.

## Testing

Edit Mode tests cover immediate direction replacement, normalized diagonal steering, preserving direction on zero input, charge event emission, indicator count, and available/spent colors. Scene integration tests verify both saved player scenes contain a bound charge view. Existing dash, movement, health, SampleStage, and Dungeon focused suites run after implementation, followed by compilation verification.

## Out of Scope

- Partial recharge progress inside a circle;
- animations when spending or restoring a charge;
- controller input or input rebinding;
- sound effects;
- changing the existing health bar design.
