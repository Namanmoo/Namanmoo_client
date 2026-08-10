# Stone Bullet Blocking Design

## Goal

Make `Obstacle_Rock` ("Stone") stop both the player's ranged-weapon
projectiles and monster bullets on contact — they're destroyed on hit,
regardless of any pierce/bounce effect the player's weapon has.
`Obstacle_Lake` keeps letting all bullets pass through unaffected.

## Background

Today, neither obstacle actually blocks bullets:

- `WeaponProjectile.TryHit` only reacts to colliders with an `EnemyHealth`
  in their parent chain; anything else falls into `TryBounce`, which only
  changes direction if the weapon has bounce charges left, and does nothing
  otherwise — the projectile just keeps flying through.
- `EnemyProjectile`, `BossBullet`, and `SlimeBossProjectile` only react to
  `PlayerHealth`; everything else is ignored entirely.

Both `Obstacle_Rock` and `Obstacle_Lake` already have identical non-trigger
`BoxCollider2D`s, so player/monster *movement* is blocked equally by both —
that part is correct today and isn't changing. This design only adds a
bullet-stopping reaction to Stone.

Since Lake already lets bullets pass through (there's no code path that
stops them), **Lake needs no changes at all** — only Stone gets new
behavior.

## Approach

Add an empty marker component, `BulletBlockingObstacle`, attached only to
`Obstacle_Rock.prefab`. Each projectile script checks for this marker on
the collider it hit and destroys itself unconditionally when present —
independent of pierce/bounce state. This mirrors the existing
`SpikeObstacle` pattern (a small marker/behavior component living in
`Assets/Scripts/Dungeon/Obstacle/`) rather than introducing a new Unity Tag
or Physics2D Layer, which would require editing project-wide
`TagManager.asset`/collision-matrix settings for a single obstacle type.

## Components

### BulletBlockingObstacle (new)

`Assets/Scripts/Dungeon/Obstacle/BulletBlockingObstacle.cs`, namespace
`NaManMoo.Dungeon`.

- Empty marker `MonoBehaviour` — no fields, no logic. Its only purpose is
  to be found via `GetComponentInParent<BulletBlockingObstacle>()`.

### Obstacle_Rock.prefab (modified)

Add a `BulletBlockingObstacle` component to the existing GameObject
(alongside its current `SpriteRenderer` + non-trigger `BoxCollider2D`).
Collider stays non-trigger and unchanged — it already blocks player/monster
movement correctly.

### WeaponProjectile.cs (modified)

In `TryHit`, when the hit collider has no `EnemyHealth`: check for
`BulletBlockingObstacle` first. If present, call `Expire()` unconditionally
and return, skipping `TryBounce` entirely (per user decision: Stone stops
the projectile outright, no ricochet regardless of remaining bounce
charges). If absent, fall through to the existing `TryBounce` call
unchanged — this is exactly today's behavior, so Lake and anything else
(e.g. the Safety Boundary wall) keep bouncing/passing through exactly as
they do now.

### EnemyProjectile.cs / BossBullet.cs / SlimeBossProjectile.cs (modified)

In `OnTriggerEnter2D`, after the existing player-damage check: if the
projectile hasn't already been consumed/deactivated this frame and the hit
collider has a `BulletBlockingObstacle` in its parent chain, destroy
(`EnemyProjectile`/`SlimeBossProjectile`) or deactivate (`BossBullet`,
matching its existing `SetActive(false)` pattern) the bullet — no damage
dealt, since it hit an obstacle, not the player.

## Testing

- `WeaponProjectile`: a test asserting `TryHit` destroys the projectile
  (`gameObject == null` after the call, matching how `Expire()` behaves)
  when the hit collider has a `BulletBlockingObstacle`, even when the
  projectile still has bounce charges remaining — verifying B (unconditional
  stop) over A (bounce-if-charges-remain).
- One test each for `EnemyProjectile.TryDamagePlayer`-adjacent behavior:
  since `TryDamagePlayer` itself doesn't check obstacles, the obstacle
  check lives directly in `OnTriggerEnter2D`, which isn't easily unit
  tested without simulating real trigger events (same limitation noted in
  the prior obstacles plan for `Awake`-dependent state). Cover this by code
  review against the mirrored, already-tested `WeaponProjectile` pattern,
  the same way the prior obstacles plan handled `SpikeObstacle`'s
  `OnTriggerEnter2D`/`OnTriggerStay2D` wiring.
- `BulletBlockingObstacle` itself has no logic to test — it's a marker.

## Out of Scope

- Any change to `Obstacle_Lake.prefab` — it already lets bullets pass
  through with zero code changes.
- Any change to how Stone/Lake block player or monster *movement* — both
  already correctly use non-trigger `BoxCollider2D`s for that, unchanged.
- Any change to `TryBounce`'s existing behavior against non-marked
  non-trigger colliders (e.g. the Safety Boundary wall) — out of scope,
  not requested.
- Balance values (damage, speed, etc.) — none touched.
