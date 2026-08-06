# Per-Enemy Visual and Collision Size Design

## Goal

Allow every regular enemy definition to configure its visible height and
circular body collision radius independently in the Unity Inspector.

## Configuration

`EnemyDefinition` gains two serialized fields:

- `visualHeight`: the desired world-space height of the rendered enemy.
- `bodyCollisionRadius`: the radius of the root `CircleCollider2D`.

Both values are exposed as read-only public properties for factories and
tests. Unity Inspector serialization lets the user edit each enemy asset
directly without modifying code.

Initial values are:

| Enemy | Visual Height | Body Collision Radius |
|---|---:|---:|
| Krab | 2 | 0.7 |
| Squirrel | 2 | 0.7 |
| Wood Tower | 3 | 1.1 |

All existing health, attack, movement, timing, projectile, and projectile
collision values remain unchanged.

## API and Compatibility

Keep the existing `EnemyDefinition.Configure(...)` signature unchanged so
existing setup code and tests do not risk shifting gameplay-stat arguments.
Add:

```csharp
public void ConfigurePresentation(
    float newVisualHeight,
    float newBodyCollisionRadius)
```

This method updates only the two presentation/body-collision fields.
`OnValidate` and runtime configuration clamp each value to at least `0.01`.
New definitions default to visual height `2` and body radius `0.7`, preserving
the current shared factory behavior when presentation has not been configured.

## Factory Behavior

`EnemyFactory.CreateVisual` calculates scale using
`definition.VisualHeight` rather than the shared `VisualHeight` constant.
`EnemyFactory.ConfigurePhysics` receives the definition and assigns
`definition.BodyCollisionRadius` to the root `CircleCollider2D`.

Collider shape remains `CircleCollider2D` for all regular enemy types. The
Wood Tower's `FreezeAll` rigidbody constraint remains unchanged.

## Asset Builder

`DungeonEnemyAssetBuilder` must not reconfigure any existing enemy definition.
The three current `.asset` files receive the approved presentation values once
as part of this change. If the Wood Tower definition is missing, the builder
may create it with its approved gameplay and presentation defaults.

The serialized `.asset` values are the user-editable source of truth. Running
the builder after the user edits an existing definition must preserve both its
gameplay and presentation values.

## Validation and Testing

Focused tests verify:

- Default presentation values preserve the old `2 / 0.7` behavior.
- `ConfigurePresentation` stores values and clamps zero or negative inputs.
- `EnemyFactory` creates two definitions with different visual heights and
  confirms their rendered world-space heights differ accordingly.
- `EnemyFactory` assigns each definition's collision radius to its root
  `CircleCollider2D`.
- Krab and Squirrel definitions contain `2 / 0.7`.
- Wood Tower contains `3 / 1.1`.
- Existing gameplay stats for Krab, Squirrel, and Wood Tower remain unchanged.
