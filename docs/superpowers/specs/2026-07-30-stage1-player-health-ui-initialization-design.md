# Stage 1 Player Health UI Initialization Design

## Problem

When Stage 1 starts, `PlayerHealth` correctly initializes the player's health to
20, but the health UI initially displays `0/20` with an empty gauge. The UI only
updates to `18/20` after the player takes the first hit.

The serialized `PlayerHealthBarView` can run `OnEnable` before
`PlayerHealth.Awake`. It therefore renders the default `CurrentHealth` value of
zero. `PlayerHealth.Awake` later assigns the maximum health but does not notify
the already connected view.

## Desired Behavior

- On the first rendered frame of Stage 1, the health label displays `20/20`.
- The health gauge is completely filled.
- Taking damage continues to update the label and gauge immediately.
- The result must not depend on Unity's relative `Awake` and `OnEnable` order for
  the two scene objects.

## Design

Add a regression test that opens the real Stage 1 scene in Play Mode and checks
the health UI after one frame. This reproduces the serialized-scene lifecycle
that the existing dynamically constructed UI test does not cover.

Make the health view perform its initial synchronization only after scene
objects have completed their `Awake` initialization. The existing
`HealthChanged` subscription remains responsible for later damage updates.
This keeps the fix local to presentation lifecycle synchronization and avoids
global Script Execution Order settings.

## Alternatives Considered

1. Emit `HealthChanged` from `PlayerHealth.Awake`.
   This can still miss a view that subscribes after the event and makes an
   initialization event dependent on listener timing.
2. Configure Script Execution Order.
   This creates a global project setting dependency for a local UI concern.
3. Re-synchronize the view after initialization.
   This is preferred because the view reads the authoritative health state once
   all `Awake` methods have completed, while retaining event-driven updates.

## Testing

The regression test will:

1. Load `Assets/Scenes/Stage1.unity`.
2. Enter Play Mode and wait one frame.
3. Locate `PlayerHealthBarView`, its text, and its fill image.
4. Assert `20/20`, fill amount `1`, and a full-width fill anchor.

Existing player health and health bar tests will also run to confirm that damage
updates and zero-health behavior remain unchanged.
