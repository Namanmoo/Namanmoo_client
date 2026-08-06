# Dungeon Slime Boss Design

## Goal

Replace the Dungeon robot boss with a configurable slime boss using:

- `Assets/Boss/boss_slime.png`
- `Assets/Boss/boss_slime_etc.png`
- `Assets/Boss/boss_slime_fallMaker.png`

The boss chases the player and randomly selects one of two attack patterns.

## Configuration

Create a `SlimeBossDefinition` ScriptableObject so the user can edit all
slime-specific values in the Inspector.

| Setting | Initial value |
|---|---:|
| Maximum health | 100 |
| Chase speed | 3 |
| Pattern interval | 2 seconds |
| Boss visual height | 6 |
| Contact sensor radius | 1.2 |
| Boss contact damage | 4 |
| Spit projectile speed | 8 |
| Spit projectile lifetime | 5 seconds |
| Projectile visual height | 0.8 |
| Projectile collision radius | 0.4 |
| Projectile damage | 3 |
| Burrow windup | 0.75 seconds |
| Fall Maker tracking duration | 2 seconds |
| Fall Maker tracking speed | 2.5 |
| Fall Maker visual height | 2 |
| Reappearance projectile distance | 6 |
| Reappearance projectile duration | 1 second |
| Reappearance arc height | 1.5 |

Existing player, regular-enemy, and robot-boss stats remain unchanged.

## Dungeon Integration

The Dungeon boss room creates only the slime boss. The existing boss health
bar, `EnemyHealth`, death event, room-clear gate, and boss-room spawn position
remain in use. Robot boss code remains available for other scenes but is no
longer selected by `DungeonEncounter`.

The Dungeon scene references the slime definition instead of a single robot
sprite. The asset builder configures the three supplied PNGs as sprites and
creates the definition only when it is missing. Existing Inspector-edited
slime settings are never overwritten by rebuilding the scene.

## Components

### SlimeBossFactory

Creates:

- A root object with `Rigidbody2D` and no solid collider.
- A trigger-only circular contact sensor.
- A child `SpriteRenderer` scaled to the configured boss height.
- `EnemyHealth` configured to 100.
- A `SlimeBossController`.
- The existing boss health bar when a UI parent is supplied.

The contact trigger does not physically block or push the player.

### SlimeBossController

Uses explicit states:

- `Chasing`
- `Spitting`
- `BurrowWindup`
- `Hidden`
- `Reappearing`

During `Chasing`, the boss follows the player at speed 3. After two seconds it
selects Spit or Burrow with equal probability. A spit is instantaneous and the
boss resumes chasing. A burrow sequence completes before the next two-second
pattern interval begins.

### SlimeBossProjectile

Supports two launch modes:

- Straight spit: moves toward the player's position at launch, speed 8, and
  expires after 5 seconds.
- Reappearance arc: moves its collision root along one cardinal ground path
  for 6 units over 1 second. Its child visual follows a sine-shaped local
  vertical offset with a maximum height of 1.5, then the projectile disappears.

Both modes use `boss_slime_etc.png`, a circular trigger of radius 0.4, and deal
3 damage. A projectile deactivates or destroys itself after hitting the player.

### SlimeFallMaker

Has only a transform and sprite renderer. It has no Collider or Rigidbody and
therefore cannot block, damage, or be damaged. It follows the player at speed
2.5 for two seconds.

## Pattern Details

### Pattern 1: Spit

1. Capture the direction from the boss to the player's current position.
2. Spawn one straight projectile.
3. Resume chasing immediately.
4. Begin the next two-second pattern timer.

### Pattern 2: Burrow and Reappear

1. Stop moving for 0.75 seconds while still visible and damageable.
2. Hide the boss renderer and set `EnemyHealth` invulnerable.
3. Create the Fall Maker at the boss's current position.
4. Track the player with the marker for two seconds at speed 2.5.
5. Move the boss root to the marker position.
6. Remove the marker, reveal the boss, and clear invulnerability.
7. Launch four arc projectiles right, down, left, and up.
8. Resume chasing and begin the next two-second pattern timer.

Only the hidden interval is invulnerable. The windup and reappeared boss can
take damage.

## Damage and Contact

- Boss contact sensor: 4 damage.
- Any slime projectile: 3 damage.
- Player damage uses the existing player invulnerability system so overlapping
  trigger callbacks cannot deal damage every physics frame.
- The boss root has no solid collision response.

## EnemyHealth Extension

Add an explicit invulnerability state to `EnemyHealth`. `TakeDamage` ignores
positive damage while invulnerable. The state is cleared by configuration and
can be changed by the slime controller. Existing enemies remain vulnerable by
default.

## Testing

Focused tests verify:

- Definition defaults and normalization.
- Factory health, sprite scaling, trigger-only contact, and no solid collider.
- Chase movement at configured speed.
- Equal random-pattern selection boundary behavior.
- Straight spit direction, speed, lifetime, radius, sprite, and 3 damage.
- Windup remains visible and damageable.
- Hidden state disables the renderer and rejects damage.
- Fall Maker has no Collider or Rigidbody and tracks for the configured time.
- Reappearance occurs at the marker position and clears invulnerability.
- Four cardinal arc projectiles travel 6 units, reach 1.5 visual arc height,
  expire at one second, and deal 3 damage.
- Boss contact deals 4 damage without physical collision.
- Dungeon boss creation uses the slime definition and preserves the existing
  health bar and room-clear behavior.
- Rebuilding does not overwrite an existing slime definition.

