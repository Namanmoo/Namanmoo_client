# Spinning Sword Projectile Final Fix Report

## Status

Complete. Every Important and Minor finding in `final-findings.md` was
addressed in one fix wave. No Git commands were used.

## Root Causes and Fixes

1. `PlayerSwordShooter` validated only `damage` and `shotsPerSecond`.
   `projectileSpeed`, `spinSpeed`, `projectileLifetime`, and `spawnOffset`
   lacked both Inspector minimum metadata and runtime editor validation.
   Each field now has `[Min(0f)]`, and `OnValidate` clamps each value to zero
   or greater.
2. `SwordProjectile.Advance` called deferred `Destroy(gameObject)` on expiry
   while leaving `consumed` false. It now marks the projectile consumed before
   scheduling destruction, so same-frame `TryHit` calls cannot deal damage.
3. The spawned-projectile test now proves the loaded sword Sprite is non-null
   before comparing object identity.
4. A PlayMode integration test now uses a real kinematic `Rigidbody2D`,
   trigger `CapsuleCollider2D`, dynamic frozen enemy `Rigidbody2D`, and
   non-trigger `BoxCollider2D`. It never calls `TryHit` directly; a real physics
   overlap invokes `OnTriggerEnter2D`, deals exactly 5 damage, and destroys the
   sword.

## Strict TDD Evidence

### Numeric Inspector validation RED

Command: Unity 6000.5.5f1, EditMode, filter `PlayerSwordShooterTests`.

- Result: `Failed(Child)`
- Total: 9
- Passed: 8
- Failed: 1
- Expected failure:
  `projectileSpeed must reject negative Inspector values; Expected: not null; But was: null`
- Results:
  `Artifacts/spinning-sword-final-fix-numeric-red2.xml`
- Log:
  `Artifacts/spinning-sword-final-fix-numeric-red2.log`

### Numeric Inspector validation GREEN

Command: Unity 6000.5.5f1, EditMode, filter `PlayerSwordShooterTests`.

- Result: `Passed`
- Total: 9
- Passed: 9
- Failed: 0
- Skipped: 0
- Results:
  `Artifacts/spinning-sword-final-fix-numeric-green.xml`
- Log:
  `Artifacts/spinning-sword-final-fix-numeric-green.log`

### Lifetime regression RED

Command: Unity 6000.5.5f1, EditMode, filter `SwordProjectileTests`.

- Result: `Failed(Child)`
- Total: 7
- Passed: 6
- Failed: 1
- Expected failure:
  `Advance_ExpiryImmediatelyPreventsDamageBeforeDeferredDestroy` expected
  `TryHit` to return false, but it returned true.
- Results:
  `Artifacts/spinning-sword-final-fix-lifetime-red.xml`
- Log:
  `Artifacts/spinning-sword-final-fix-lifetime-red.log`

### Lifetime regression GREEN

Command: Unity 6000.5.5f1, EditMode, filter `SwordProjectileTests`.

- Result: `Passed`
- Total: 7
- Passed: 7
- Failed: 0
- Skipped: 0
- Results:
  `Artifacts/spinning-sword-final-fix-lifetime-green.xml`
- Log:
  `Artifacts/spinning-sword-final-fix-lifetime-green.log`

## Final Verification

### Real Physics2D PlayMode integration

Command: Unity 6000.5.5f1, PlayMode, filter
`SwordProjectilePhysicsPlayModeTests`.

- Result: `Passed`
- Total: 1
- Passed: 1
- Failed: 0
- Skipped: 0
- Results:
  `Artifacts/spinning-sword-final-fix-physics-playmode.xml`
- Log:
  `Artifacts/spinning-sword-final-fix-physics-playmode.log`

### Focused shooter/projectile EditMode

Command: Unity 6000.5.5f1, EditMode, filter
`SwordProjectileTests;PlayerSwordShooterTests`.

- Result: `Passed`
- Total: 16
- Passed: 16
- Failed: 0
- Skipped: 0
- Results:
  `Artifacts/spinning-sword-final-fix-combat-focused.xml`
- Log:
  `Artifacts/spinning-sword-final-fix-combat-focused.log`

### Full EditMode

Command: Unity 6000.5.5f1, EditMode, no filter.

- Result: `Passed`
- Total: 83
- Passed: 83
- Failed: 0
- Skipped: 0
- Results:
  `Artifacts/spinning-sword-final-fix-editmode-full.xml`
- Log:
  `Artifacts/spinning-sword-final-fix-editmode-full.log`

### Full PlayMode

Command: Unity 6000.5.5f1, PlayMode, no filter.

- Result: `Passed`
- Total: 3
- Passed: 3
- Failed: 0
- Skipped: 0
- Results:
  `Artifacts/spinning-sword-final-fix-playmode-full.xml`
- Log:
  `Artifacts/spinning-sword-final-fix-playmode-full.log`

The final focused/full logs contain no compiler errors, unhandled exceptions,
assertion exceptions, or failed test-run completion. They contain 12 benign
Unity licensing handshake/access-token retry lines; licensing recovered and
all requested runs produced passing XML results.

## Stage1 Rebuild Decision

No rebuild was run. The production change adds Inspector metadata and
validation logic but does not change serialized field names, types, defaults,
or existing valid Stage1 values, so the serialized scene does not need to be
regenerated.

## Changed Files

- `Assets/Scripts/Combat/PlayerSwordShooter.cs`
- `Assets/Scripts/Combat/SwordProjectile.cs`
- `Assets/Tests/Editor/PlayerSwordShooterTests.cs`
- `Assets/Tests/Editor/SwordProjectileTests.cs`
- `Assets/Tests/PlayMode/SwordProjectilePhysicsPlayModeTests.cs`
- `Assets/Tests/PlayMode/SwordProjectilePhysicsPlayModeTests.cs.meta`
- `.superpowers/sdd/2026-07-29-spinning-sword-projectile/final-fix-report.md`

## Concerns

No functional concern. Final logs contain the benign Unity licensing retry
noise described above.
