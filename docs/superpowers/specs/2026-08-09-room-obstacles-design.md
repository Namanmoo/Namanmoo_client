# Room Obstacles Design

## Goal

Add three obstacle types a designer can drop into a `RoomContentTemplate`:
`Obstacle_Lake` and `Obstacle_Rock` block both the player's and monsters'
movement; `Obstacle_Spike` lets everyone walk through it, but damages the
player (not monsters) for 2 when they do.

## Scope

- One new component, `SpikeObstacle`, for the damage-on-pass-through
  behavior.
- Three new prefabs under `Assets/Resources/Stage1/Obstacle/`:
  `Obstacle_Lake.prefab`, `Obstacle_Rock.prefab`, `Obstacle_Spike.prefab`.
  Each has a placeholder (empty) `SpriteRenderer` — the referenced
  `Obstacle_Lake.png` / `Obstacle_Rock.png` / `Obstacle_Spike.png` files
  don't exist in the project yet, so the user assigns them by hand once the
  art is ready.
- No new code for Lake/Rock — a plain non-trigger `Collider2D` already
  blocks both the player and monsters today, the same way the room's
  Safety Boundary wall does (`RoomBuilder.CreateSafetyBoundary`, a
  non-trigger `EdgeCollider2D`).

Out of scope: the actual `.png` art (user-provided later), placing these
prefabs inside any specific `RoomContentTemplate` (that's a per-template
authoring choice, not code), and any change to `DungeonLayout`/`RoomShape`/
`RoomBuilder`/existing balance values.

## Components

### SpikeObstacle (new)

`Assets/Scripts/Dungeon/SpikeObstacle.cs`, namespace `NaManMoo.Dungeon`.

- `[SerializeField] private int damage = 2;` — the amount dealt per hit,
  editable per-instance in the Inspector, defaulting to the requested value.
- `[RequireComponent(typeof(Collider2D))]`; on `Awake`, forces
  `GetComponent<Collider2D>().isTrigger = true` — a spike that isn't a
  trigger would physically block movement, defeating "지나갈 수 있다."
- `OnTriggerEnter2D` and `OnTriggerStay2D` both call a shared
  `TryDamagePlayer(Collider2D other)`, mirroring
  `ChaseContactEnemyController`'s existing pattern exactly: find
  `PlayerHealth` via `other.GetComponentInParent<PlayerHealth>()`, and if
  found, call `TryTakeDamage(damage, Time.time, InvulnerabilityDuration)`
  with `InvulnerabilityDuration = 1f` (same constant value
  `ChaseContactEnemyController` already uses for contact damage, so a spike
  feels consistent with every other damage source in the game).
- Nothing in this component looks for `EnemyHealth` or any enemy component
  — monsters simply never trigger any damage path, which is what makes
  "몬스터는 지나가도 데미지 없음" automatic rather than something to special-case.
- Standing on the spike deals damage again once the 1-second invulnerability
  window from the previous hit expires, matching how every other
  contact-damage source in this game already behaves (not a one-time-only
  trap).

### Obstacle_Lake / Obstacle_Rock prefabs (new, no code)

`SpriteRenderer` (sprite unassigned for now) + `BoxCollider2D` (not a
trigger). A designer replaces the `BoxCollider2D` with a `PolygonCollider2D`
later if the art needs a non-rectangular outline — that's an authoring
choice, not something this task needs to solve without the real art to
shape it against.

### Obstacle_Spike prefab (new)

`SpriteRenderer` (sprite unassigned) + `BoxCollider2D` (`isTrigger: true` —
`SpikeObstacle.Awake` also enforces this defensively) + `SpikeObstacle`.

## Testing

`SpikeObstacle`'s damage logic is tested the same way
`ChaseContactEnemyController`'s contact damage is: build a `PlayerHealth` +
`SpikeObstacle` with overlapping colliders (or call `TryDamagePlayer`
directly, since it's public like `ChaseContactEnemyController.TryDamagePlayer`),
and assert the player's health drops by exactly `damage`, that a second call
within the invulnerability window doesn't deal damage again, and that a
collider without a `PlayerHealth` in its parent chain (standing in for a
monster) causes no damage and no exception.

Lake/Rock prefabs have no behavior to unit test — they're plain Editor
assets, not scripts, so nothing to add there.
