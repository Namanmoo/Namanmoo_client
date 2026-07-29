# Task 3: Add Arrow-Key Automatic Firing and Import the Sword

## Requirements

- Preserve root `sword.png`; copy byte-for-byte to `Assets/Weapons/sword.png`.
- Import as transparent Single Sprite: alpha transparency on, mipmaps off,
  clamp wrapping, 100 pixels per unit, max texture size at least 512, and
  uncompressed texture compression.
- Create `Assets/Scripts/Combat/PlayerSwordShooter.cs`.
- Create `Assets/Tests/Editor/PlayerSwordShooterTests.cs`.
- Expose serialized Inspector fields with defaults:
  - `damage = 5` and non-negative validation.
  - `shotsPerSecond = 3` and positive validation.
  - `projectileSpeed = 8`.
  - `spinSpeed = 720`.
  - `projectileLifetime = 4`.
  - `spawnOffset = 0.8`.
  - `swordSprite`.
- Provide `CalculateDirection(Keyboard keyboard)`: arrow keys only, cardinal
  results exact, diagonal normalized to approximately `(0.7071068, 0.7071068)`.
- WASD must not affect firing direction.
- `Update` delegates to `ProcessInput(Keyboard.current, Time.time)`.
- `ProcessInput(Keyboard keyboard, float currentTime)` fires immediately when a
  non-zero direction becomes active, then every `1 / shotsPerSecond` while held.
- With shots per second 2 and held right: calls at times 0, 0.49, 0.5 produce
  projectile counts 1, 1, 2.
- Spawn at player position plus normalized direction times spawn offset.
- Create `Sword Projectile` with:
  - imported Sprite and SpriteRenderer sorting order 5;
  - trigger CapsuleCollider2D;
  - kinematic Rigidbody2D with zero gravity;
  - SwordProjectile initialized with direction, damage, speed, spin, lifetime,
    and player owner.
- Provide a small production configuration API needed by Stage1 integration to
  assign/read the sword Sprite; tests may use a production configuration API or
  SerializedObject to set numeric Inspector values.
- Tests cover import settings, source-copy hash equality, input cardinal and
  diagonal directions, exact cooldown timing, spawned component setup, sprite,
  and configured projectile values.
- Follow strict TDD with focused RED and GREEN artifacts.
- Do not modify Stage1SceneBuilder, Stage1RuntimeBootstrap, or Stage1.unity.

## Existing Interface

`SwordProjectile.Initialize(Vector2 direction, int damage, float speed,
float spinSpeed, float lifetime, GameObject owner)` exists along with
readable initialized values if exposed by the current implementation.

## Report Contract

Write `.superpowers/sdd/2026-07-29-spinning-sword-projectile/task-3-report.md`
with status, files changed, RED command/result, GREEN command/exact totals,
self-review, and concerns.
