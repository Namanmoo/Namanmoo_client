# Spinning Sword Projectile Design

## Goal

Allow the player to hold the arrow keys to automatically fire `sword.png`
projectiles in cardinal or diagonal directions. Each sword spins in flight and
deals configurable damage to enemies.

## Input and Firing

- Add `PlayerSwordShooter` to the player.
- Read the four arrow keys through Unity's Input System.
- Combine simultaneous horizontal and vertical input and normalize it so
  diagonal projectiles do not move faster.
- Holding a non-zero direction automatically fires at a configurable
  `shotsPerSecond`.
- WASD movement and arrow-key firing remain independent.
- Spawn projectiles slightly outside the player's collider in the chosen
  direction.

## Inspector Configuration

`PlayerSwordShooter` exposes serialized fields:

- `damage`, default `5`.
- `shotsPerSecond`, with a positive default.
- `projectileSpeed`.
- `spinSpeed`, in degrees per second.
- `projectileLifetime`, in seconds.
- `swordSprite`.

All numeric fields reject negative values. Damage and shots per second are
directly editable on the player in the Unity Inspector.

## Sword Projectile

- Add a focused `SwordProjectile` component.
- Initialize it with direction, damage, movement speed, spin speed, lifetime,
  and owner.
- Move in a straight line at constant world speed.
- Rotate continuously around the Z axis while moving.
- Use a trigger `Collider2D`; do not physically push the player, map boundary,
  or enemies.
- Ignore collision with its owner.
- When it reaches an `EnemyHealth` component, apply its damage once and destroy
  the projectile immediately.
- Destroy the projectile when its configured lifetime expires.

## Enemy Health

- Add an `EnemyHealth` MonoBehaviour with serialized `maxHealth`.
- Initialize current health from maximum health when enabled.
- Provide `TakeDamage(int amount)`.
- Ignore zero and negative damage.
- Destroy the enemy GameObject when current health reaches zero.
- Expose read-only current and maximum health values for gameplay code and
  tests.

## Asset and Scene Integration

- Preserve the root `sword.png`.
- Copy it to `Assets/Weapons/sword.png` and import it as a transparent Single
  Sprite.
- Configure both `Stage1SceneBuilder` and `Stage1RuntimeBootstrap` to assign the
  same imported sword Sprite to `PlayerSwordShooter`.
- Missing Sprite references fail early with an actionable error.
- Rebuild `Assets/Scenes/Stage1.unity`.

## Testing

- Unit tests cover cardinal and normalized diagonal direction calculation.
- Shooter tests cover fire-rate timing and projectile configuration.
- Projectile tests cover straight movement, rotation, lifetime, owner
  exclusion, and exactly one damage application.
- Enemy tests cover damage, ignored non-positive values, and death.
- Scene tests verify the shooter and sword Sprite are serialized on the player.
- Existing EditMode and PlayMode tests remain green.

