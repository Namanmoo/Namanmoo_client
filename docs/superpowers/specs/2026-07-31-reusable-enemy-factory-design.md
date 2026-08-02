# Reusable Enemy Factory Design

## Goal

Provide a single enemy creation API that is independent of stage and room
layout. Enemies with the same behavior can reuse one controller while using
different sprites, projectile visuals, and combat statistics.

Map generation, room placement, and spawn-point authoring are outside this
feature.

## Architecture

### EnemyDefinition

`EnemyDefinition` is a `ScriptableObject` that contains data which varies per
enemy:

- Stable ID and display name
- Body sprite
- Behavior type
- Maximum health
- Movement speed
- Attack damage, range, and interval
- Projectile sprite, speed, lifetime, and collision radius
- Optional runtime Animator Controller for future visual animation

Creating an enemy with different art or statistics but an existing behavior
requires only a new `EnemyDefinition` asset.

### EnemySpawnRequest

`EnemySpawnRequest` contains values that belong to one spawn rather than the
enemy type:

- Parent transform
- Player target
- World position
- Optional instance name

The stage or room owner constructs this request and does not need to know how
the enemy GameObject is assembled.

### EnemyFactory

`EnemyFactory.Create(EnemyDefinition, EnemySpawnRequest)` is the only public
enemy construction entry point. It:

1. Validates the definition and spawn request.
2. Creates the root GameObject, visual child, Rigidbody2D, collider, and
   `EnemyHealth`.
3. Applies sprites and statistics from the definition.
4. Selects and configures the controller identified by the behavior type.
5. Returns the created `EnemyHealth`.

The first supported behavior types are:

- `ChaseContact`: move toward the player and deal contact damage. This replaces
  duplicated Krab assembly while preserving its behavior.
- `ApproachAndShoot`: approach until inside attack range, stop, and fire toward
  the player at the configured interval. Resume pursuit when the player leaves
  range.

Adding a truly new behavior requires one controller and one factory routing
entry. Adding a new enemy that uses an existing behavior requires no new
controller.

### EnemyVisualController

Combat controllers do not manipulate Animator parameters directly. They call
`EnemyVisualController.PlayAttack()` when an attack begins.

- Without an Animator Controller, the enemy remains a static sprite and combat
  still works.
- With an Animator Controller, `PlayAttack()` sends the `Attack` trigger.
- A later animation implementation may use an Animation Event to synchronize
  the exact projectile release frame without changing enemy selection or
  movement logic.

Actual animation clips and attack motions are not part of this implementation.

### EnemyProjectile

Ranged enemies create a shared `EnemyProjectile`. Each projectile uses its
owner's definition for:

- Sprite
- Damage
- Speed
- Lifetime
- Collision radius

The projectile damages the player, ignores its owner, and is destroyed on a
valid hit or when its lifetime expires.

## Data Flow

```text
Stage or room code
  -> EnemyFactory.Create(definition, spawn request)
     -> shared body and visual construction
     -> behavior controller selected from definition
        -> EnemyVisualController.PlayAttack()
        -> EnemyProjectile configured from definition
```

## Compatibility

The existing Krab behavior remains available through a `ChaseContact`
definition. Stage and room placement logic is not redesigned in this work.
Existing stage setup may be adapted only where necessary to call the new
factory without changing spawn positions or encounter rules.

## Validation and Errors

Creation fails with a clear argument exception when:

- The definition is missing.
- The target player is missing.
- The body sprite is missing.
- A ranged definition has no valid projectile settings.
- The behavior type is unsupported.

Numeric definition values are clamped or rejected consistently so invalid
Inspector data cannot create an unusable enemy.

## Testing

Editor tests will verify:

- Definitions preserve body sprite, projectile sprite, and statistics.
- The factory builds shared components and selects the correct controller.
- Two definitions can reuse `ApproachAndShoot` with different statistics and
  sprites.
- Ranged enemies stop inside range, resume pursuit outside range, and respect
  their firing interval.
- Projectiles use definition-specific visuals and combat values.
- `PlayAttack()` is safe without an Animator and triggers an Animator when one
  is configured.
- Existing Krab behavior and encounter integration remain functional.

