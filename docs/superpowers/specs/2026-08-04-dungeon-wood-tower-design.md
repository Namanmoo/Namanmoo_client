# Dungeon Wood Tower Design

## Goal

Add a stationary wood tower enemy to Dungeon normal rooms. The tower uses
`Assets/Enemies/enemy_woodtower.png` and fires four
`Assets/Enemies/enemy_woodtower_bullet.png` projectiles at fixed intervals.
It participates in the same deterministic normal-enemy selection as Krab and
Squirrel.

## Behavior

- The tower never moves or aims at the player.
- It waits one full attack interval after spawning before its first attack.
- Every attack creates four projectiles simultaneously.
- Projectile movement and visual Z rotations are:
  - Right: direction `(1, 0)`, rotation `0` degrees.
  - Down: direction `(0, -1)`, rotation `90` degrees.
  - Left: direction `(-1, 0)`, rotation `180` degrees.
  - Up: direction `(0, 1)`, rotation `270` degrees.
- The attack repeats every 1.5 seconds.

## Enemy Definition

Create `Assets/Enemies/DungeonWoodTower.asset` with:

- ID: `wood_tower`
- Display name: `Wood Tower`
- Health: `10`
- Move speed: `0`
- Attack damage: `2`
- Attack interval: `1.5`
- Projectile speed: `8`
- Projectile lifetime: `5`
- Projectile collision radius: `0.5`

Attack range is not used by the stationary behavior. It will retain a valid
serialized value so that the shared `EnemyDefinition` remains valid.

The existing Squirrel and Krab definitions and all of their stats remain
unchanged.

## Architecture

Add a new `EnemyBehaviorType` value for stationary four-way shooting and a
dedicated `StationaryFourWayShooterController`. `EnemyFactory` selects this
controller for the wood tower definition while preserving the shared enemy
visual, physics, and health setup.

The controller owns only its firing schedule and the four fixed shot
directions. It creates the existing `EnemyProjectile` type and assigns the
requested visual rotation to each projectile. No player tracking or movement
logic is included.

`DungeonEnemyAssetBuilder` imports both wood tower textures as single sprites
with point filtering, transparency, clamped wrapping, and no mipmaps. It
creates or updates the wood tower definition and returns it alongside the
existing Krab and Squirrel definitions. The builder must not reconfigure the
existing Krab or Squirrel stats as a side effect of this feature.

## Timing

Initialization records the first allowed attack time as one full interval
after spawn. Update checks the current time and fires only when the interval
has elapsed. After firing, it schedules the next attack one interval later.

## Validation and Error Handling

`EnemyFactory` accepts the new behavior only when the definition has a body
sprite and valid projectile sprite, speed, lifetime, and collision radius.
Missing or invalid required projectile data produces the same clear argument
error used for other ranged enemies.

## Testing

Focused editor tests will verify:

- The controller does not fire before 1.5 seconds.
- The first attack fires at 1.5 seconds.
- Each attack creates exactly four projectiles.
- Projectile directions are right, down, left, and up.
- Projectile visual rotations are 0, 90, 180, and 270 degrees.
- Projectile damage, speed, lifetime, and radius come from the wood tower
  definition.
- `EnemyFactory` attaches only the stationary four-way controller for the new
  behavior.
- The asset builder creates the wood tower definition from the requested
  sprites and includes it in the Dungeon normal-enemy pool.
- Existing Squirrel and Krab definition files remain unchanged.

