# Task 2 Report

## Status

DONE

## Files changed

- `Assets/Scripts/Combat/SwordProjectile.cs`
- `Assets/Scripts/Combat/SwordProjectile.cs.meta`
- `Assets/Tests/Editor/SwordProjectileTests.cs`
- `Assets/Tests/Editor/SwordProjectileTests.cs.meta`
- `Artifacts/spinning-sword-task2-red.log`
- `Artifacts/spinning-sword-task2-green.xml`
- `Artifacts/spinning-sword-task2-green.log`
- `.superpowers/sdd/2026-07-29-spinning-sword-projectile/task-2-report.md`

## RED

Command:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Users\myong\NaManMoo' -runTests -testPlatform EditMode -testFilter SwordProjectileTests -testResults 'C:\Users\myong\NaManMoo\Artifacts\spinning-sword-task2-red.xml' -logFile 'C:\Users\myong\NaManMoo\Artifacts\spinning-sword-task2-red.log'
```

Result: Unity script compilation failed as intended before a result XML was created. The log reports `CS0246` at `SwordProjectileTests.cs(101,20)`: `SwordProjectile` could not be found. Unity's own log records return code `1`.

## GREEN

Command:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Users\myong\NaManMoo' -runTests -testPlatform EditMode -testFilter SwordProjectileTests -testResults 'C:\Users\myong\NaManMoo\Artifacts\spinning-sword-task2-green.xml' -logFile 'C:\Users\myong\NaManMoo\Artifacts\spinning-sword-task2-green.log'
```

Exact totals from `spinning-sword-task2-green.xml`:

- Total: 5
- Passed: 5
- Failed: 0
- Inconclusive: 0
- Skipped: 0
- Result: Passed

## Self-review

- `Initialize` normalizes its direction and retains the configured damage, speed, spin speed, lifetime, and owner.
- `Advance` moves by direction * speed * delta time, rotates around Z, and destroys at expiry.
- `TryHit` ignores the owner, non-enemies, and any attempt after consumption. It marks the projectile consumed before applying exactly the configured damage through `EnemyHealth.TakeDamage` and schedules projectile destruction.
- `OnTriggerEnter2D` delegates to `TryHit`; shooter construction and scene integration remain outside Task 2.
- Focused Unity tests cover the required cardinal and normalized diagonal motion, spin, expiry, owner/non-enemy exclusion, and exactly-one application from 20 to 15 health.

## Concerns

- The Unity log contains transient LicensingClient handshake/token messages before the test run, but the generated result XML records all five focused tests as passed.

## Fix round 1

### Status

DONE

### Finding addressed

- Made the owner exclusion test independent from the non-enemy exclusion path by giving the owner an `EnemyHealth`.
- The test asserts that hitting the damageable owner returns `false`, leaves owner health at 20, and still allows the projectile to hit a different enemy once for 5 damage (20 to 15).
- The strengthened test exposed that `TryHit` did not call the existing `IsOwnerCollider` helper. Production was minimally changed to reject owner colliders before resolving `EnemyHealth`.

### Files changed

- `Assets/Tests/Editor/SwordProjectileTests.cs`
- `Assets/Scripts/Combat/SwordProjectile.cs`
- `Artifacts/spinning-sword-task2-fix-round1-red.xml`
- `Artifacts/spinning-sword-task2-fix-round1-red.log`
- `Artifacts/spinning-sword-task2-fix-round1-green.xml`
- `Artifacts/spinning-sword-task2-fix-round1-green.log`
- `.superpowers/sdd/2026-07-29-spinning-sword-projectile/task-2-report.md`

### RED

Command:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Users\myong\NaManMoo' -runTests -testPlatform EditMode -testFilter SwordProjectileTests -testResults 'C:\Users\myong\NaManMoo\Artifacts\spinning-sword-task2-fix-round1-red.xml' -logFile 'C:\Users\myong\NaManMoo\Artifacts\spinning-sword-task2-fix-round1-red.log'
```

Exact result from `spinning-sword-task2-fix-round1-red.xml`:

- Result: Failed(Child)
- Total: 6
- Passed: 5
- Failed: 1
- Inconclusive: 0
- Skipped: 0
- Failing test: `TryHit_OwnerEnemy_IsIgnoredAndProjectileCanStillHitAnotherEnemy`
- Failure: `Expected: False`, `But was: True`

### GREEN

Command:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Users\myong\NaManMoo' -runTests -testPlatform EditMode -testFilter SwordProjectileTests -testResults 'C:\Users\myong\NaManMoo\Artifacts\spinning-sword-task2-fix-round1-green.xml' -logFile 'C:\Users\myong\NaManMoo\Artifacts\spinning-sword-task2-fix-round1-green.log'
```

Exact result from `spinning-sword-task2-fix-round1-green.xml`:

- Result: Passed
- Total: 6
- Passed: 6
- Failed: 0
- Inconclusive: 0
- Skipped: 0
- Duration: 2.825918 seconds

### Self-review

- Owner rejection now precedes `EnemyHealth` lookup, so a damageable owner cannot consume the projectile or take damage.
- The projectile remains unconsumed after an owner hit and can damage a different enemy exactly once.
- Non-enemy exclusion remains covered by its separate focused test.

### Concerns

- The initial task report's five-test totals are historical; fix round 1 has six focused tests after splitting owner and non-enemy exclusion coverage.
