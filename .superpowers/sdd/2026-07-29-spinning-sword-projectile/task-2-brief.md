# Task 2: Add the Spinning Sword Projectile

## Requirements

- Create `Assets/Scripts/Combat/SwordProjectile.cs`.
- Create `Assets/Tests/Editor/SwordProjectileTests.cs`.
- Consume `EnemyHealth.TakeDamage(int)`.
- Provide `Initialize(Vector2 direction, int damage, float speed, float spinSpeed, float lifetime, GameObject owner)`.
- Provide deterministic `Advance(float deltaTime)` and `TryHit(Collider2D other)`.
- Normalize direction during initialization.
- Move by `direction * speed * deltaTime`.
- Rotate around Z by `spinSpeed * deltaTime`.
- Count down lifetime and destroy the projectile on expiry.
- Ignore owner colliders and colliders without `EnemyHealth`.
- On the first valid enemy hit, set a consumed flag before applying damage, deal exactly the configured damage, and destroy the projectile.
- A second hit attempt must return false and never deal damage again.
- Tests use literal values: direction right, damage 5, speed 8, spin 720, lifetime as selected by the test; `Advance(0.5f)` moves exactly 4 units and rotates one full turn modulo 360.
- Test normalized diagonal travel, lifetime expiry, owner exclusion, non-enemy exclusion, and exactly one damage application reducing default health from 20 to 15.
- Follow strict TDD with recorded RED and GREEN Unity test evidence.
- Do not implement shooter input or scene integration in this task.

## Existing Interface

`EnemyHealth` is complete at `Assets/Scripts/Combat/EnemyHealth.cs` with
`CurrentHealth`, `MaxHealth`, and `TakeDamage(int)`.

## Report Contract

Write `.superpowers/sdd/2026-07-29-spinning-sword-projectile/task-2-report.md` with status, files changed, RED command/result, GREEN command/exact totals, self-review, and concerns.
