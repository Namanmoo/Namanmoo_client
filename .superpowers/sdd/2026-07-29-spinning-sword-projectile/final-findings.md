# Final Review Findings — Single Fix Wave

## Important — must fix

`PlayerSwordShooter` must reject negative values for every numeric Inspector
field, as required by the design. Currently only damage and shotsPerSecond are
validated.

- Add `[Min(0f)]` to `projectileSpeed`, `spinSpeed`, `projectileLifetime`, and
  `spawnOffset`.
- Clamp all four to zero or greater in `OnValidate`.
- Extend the real Inspector validation test to prove negative values for all
  numeric fields are corrected.
- Follow TDD: capture focused RED before production fix and GREEN afterward.

## Minor — fix in the same wave

1. When lifetime expires, `SwordProjectile` schedules destruction but does not
   mark itself consumed. Set consumed before destroy and add a regression test
   proving a same-frame TryHit after expiry returns false and deals no damage.
2. In the spawned-projectile test, explicitly assert the loaded sword Sprite is
   non-null before identity comparison.
3. Add a PlayMode physics integration test using real Rigidbody2D/trigger
   colliders that proves overlap invokes `OnTriggerEnter2D`, deals exactly 5
   damage, and destroys the sword. Do not replace existing unit tests.

## Verification

- Focused shooter/projectile tests.
- New PlayMode combat integration test.
- Rebuild Stage1 only if production serialization changes require it.
- Full EditMode and PlayMode suites with exact totals.
- Append the fix report and evidence paths to
  `.superpowers/sdd/2026-07-29-spinning-sword-projectile/final-fix-report.md`.
