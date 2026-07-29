# Sword Fire Cooldown Design

## Problem

The slot-one sword currently has a held-input rate limit, but releasing and
pressing an arrow key again bypasses it. Rapid tapping can therefore create far
more projectiles than the configured firing rate.

## Desired Behavior

- Pressing a firing direction when the sword is ready fires immediately.
- Holding a firing direction continuously fires one sword every 0.5 seconds.
- Releasing and pressing again does not bypass the remaining cooldown.
- Switching away from and back to slot one does not bypass the remaining
  cooldown.
- Direction changes affect the next projectile without resetting the cooldown.

## Design

Keep the existing timestamp-based firing system in `PlayerSwordShooter`.
Change the default rate from three shots per second to two shots per second.
Remove input activation as an alternative condition for firing, so every shot
after the initial ready state requires `currentTime >= nextShotTime`.

`nextShotTime` starts at zero, so the first input at normal non-negative Unity
time still fires immediately. After firing, it advances by `1 / shotsPerSecond`,
which is 0.5 seconds with the new default.

## Alternatives Considered

1. Timestamp cooldown on every shot.
   This is preferred because it fits the current code and cannot be bypassed by
   release, re-press, or equipment selection changes.
2. Coroutine-based repeating fire.
   This requires additional coroutine cancellation and restart handling.
3. Input System repeat interactions.
   This expands the change into input-action configuration without improving
   the simple timing rule.

## Testing

Update the shooter tests to verify:

- The default `shotsPerSecond` value is `2`.
- A held direction fires at time `0`, does not fire at `0.49`, and fires at
  `0.5`.
- Releasing at `0.1` and pressing again at `0.2` does not create another
  projectile.
- The next shot fires once the original cooldown reaches `0.5`.
- Switching away from and back to slot one does not reset the cooldown.

Run the focused `PlayerSwordShooterTests`, the item-hotbar Play Mode tests, and
the Stage 1 builder tests after the change.
