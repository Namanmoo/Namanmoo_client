# Player Dash Design

## Goal

Add a keyboard dash to the player. Pressing either Shift key spends one charge and moves the player in a fixed direction for 0.6 seconds at three times normal movement speed. The player is invulnerable and leaves fading afterimages during the dash.

Dash timing, speed, charge count, recharge timing, and afterimage timing must be adjustable at runtime so items can modify them.

## Player Experience

- The player starts with all two dash charges available.
- Pressing left or right Shift starts a dash when at least one charge and a valid direction are available.
- Current movement input determines the dash direction. With no current input, the most recent non-zero movement direction is used.
- Before the player has moved for the first time, Shift does not start a dash.
- Dash direction is locked for the entire dash. Normal movement input does not steer the player until the dash ends.
- The default dash lasts 0.6 seconds and moves at three times the current normal movement speed.
- The player is invulnerable for the dash duration.
- Fading, semi-transparent copies of the player's current sprite appear behind the player while dashing.

## Charges and Recharge

- The default maximum charge count is two.
- Starting a valid dash immediately consumes one charge.
- Recharge is sequential, not parallel.
- When the player has fewer than the maximum charges, one charge is restored after five seconds.
- If both default charges are empty, the first returns after five seconds and the second after another five seconds.
- The recharge timer continues while the player is dashing.
- Increasing the maximum charge count at runtime does not immediately grant the newly added charges; they recharge normally.
- Decreasing the maximum charge count clamps the current charge count to the new maximum.

## Architecture

### `PlayerMovement`

`PlayerMovement` remains responsible for ordinary WASD movement. It records the latest non-zero normalized movement direction and exposes:

- current normalized movement direction;
- last non-zero movement direction;
- current movement speed;
- a way for `PlayerDash` to temporarily suppress ordinary movement.

Normal movement resumes when the dash ends.

### `PlayerDash`

A separate `PlayerDash` component owns:

- Shift input handling;
- dash activation and fixed dash direction;
- dash duration and movement;
- current and maximum charges;
- sequential recharge state;
- dash invulnerability requests;
- afterimage emission.

Keeping dash state outside `PlayerMovement` prevents input, charge, visual, and damage rules from accumulating in the basic movement component.

The component exposes runtime-adjustable properties:

- `Duration` (default `0.6f`);
- `SpeedMultiplier` (default `3f`);
- `MaxCharges` (default `2`);
- `RechargeDuration` (default `5f`);
- `AfterimageInterval`;
- `AfterimageLifetime`.

Setters clamp invalid values to safe minimums. Items can retain a reference to the player's `PlayerDash` and modify these properties without using static process-wide state. This provides globally accessible player ability values while avoiding settings leaking between scenes, players, or tests.

Read-only state exposes the current charge count and whether a dash is active so future UI or items can observe the ability.

### `PlayerHealth`

`PlayerHealth` gains an API that grants invulnerability until a supplied time or for a supplied duration. It extends the existing invulnerability deadline using the later end time and never shortens protection already granted by damage or another effect.

`PlayerDash` grants protection covering the dash duration when a dash successfully begins. Failed dash attempts do not grant invulnerability.

### `PlayerDashAfterimage`

Each emitted afterimage is a lightweight GameObject with a `SpriteRenderer` matching the player's current sprite, transform, flip state, color, and sorting placement. It has no collider or gameplay behavior. It fades its alpha to zero over `AfterimageLifetime` and then destroys itself.

Afterimages are emitted only while a dash is active. The initial implementation creates and destroys the small number of renderers directly; pooling is outside scope unless profiling later shows a need.

### Player Construction

`PlayerFactory` attaches and wires `PlayerDash` alongside `PlayerMovement`. It supplies the player visual renderer and `PlayerHealth` references needed for afterimages and invulnerability. Scene builders that rely on `PlayerFactory` receive the feature automatically.

Any existing scene player not created by the factory must be updated by its setup path so the component and references are present.

## Data Flow

1. `PlayerMovement` reads WASD input and records current and last movement directions.
2. `PlayerDash` detects a Shift press.
3. It rejects the request if already dashing, out of charges, or no current/previous direction exists.
4. A valid request consumes one charge, locks the chosen direction, suppresses ordinary movement, and grants dash-duration invulnerability.
5. During physics updates, `PlayerDash` moves the `Rigidbody2D` at `PlayerMovement.MoveSpeed * SpeedMultiplier`.
6. During frame updates, it emits afterimages at the configured interval.
7. When the duration ends, it restores ordinary movement.
8. Independently, the sequential recharge timer restores one missing charge per `RechargeDuration`.

## Edge Cases and Validation

- Both Shift keys trigger the same ability, but holding Shift does not repeatedly dash; each dash requires a new press.
- A Shift press during an active dash is ignored and does not queue another dash.
- Zero or negative duration cannot create a permanent dash.
- Speed multiplier cannot be negative.
- Maximum charges cannot be less than one.
- Recharge duration cannot be zero or negative.
- Missing optional visual references prevent afterimages but do not prevent movement or charge behavior.
- Missing required movement, body, or health dependencies are detected during component initialization with a clear error.
- Changing recharge duration affects the active recharge cycle using the new configured duration.

## Testing

Edit Mode tests cover:

- choosing current input before the last direction;
- rejecting a dash without any valid direction;
- starting fully charged;
- consuming exactly one charge for a valid dash;
- ignoring activation while already dashing;
- locking dash direction;
- restoring one charge per five-second sequential interval;
- runtime changes to maximum charges and recharge duration;
- extending, rather than shortening, `PlayerHealth` invulnerability;
- default configurable values;
- factory attachment and required references;
- afterimage lifetime and fade calculation where practical without frame timing.

Focused Unity tests are run first, followed by the complete Edit Mode suite and relevant Play Mode tests. A project build or script compilation check verifies scene and runtime integration.

## Out of Scope

- Dash charge UI;
- sound, camera shake, or controller vibration;
- gamepad input and input rebinding;
- enemy dash behavior;
- afterimage pooling;
- collision phasing through walls or enemies;
- defining a specific item that modifies dash values.

The public runtime properties are the extension point for a later item implementation.
