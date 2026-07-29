# Task 1 Report: Gate Sword Firing with the Shared Inventory

## Status

PASS

`PlayerSwordShooter` now stores the exact initialized `PlayerInventory`
reference and permits firing only for a non-null inventory whose selected slot
is slot 0 and whose equipped item has ID `sword`. Invalid inventory state is
checked before direction/cooldown processing and clears the active firing
direction.

## Files changed

- `Assets/Scripts/Combat/PlayerSwordShooter.cs`
- `Assets/Tests/Editor/PlayerSwordShooterTests.cs`
- `.superpowers/sdd/2026-07-29-slot-one-sword-hotbar/task-1-report.md`

## RED

Command:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Users\myong\NaManMoo' -runTests -testPlatform EditMode -testFilter 'PlayerSwordShooterTests' -testResults 'C:\Users\myong\NaManMoo\Artifacts\slot-one-sword-task1-red.xml' -logFile 'C:\Users\myong\NaManMoo\Artifacts\slot-one-sword-task1-red.log'
```

Artifact totals: 11 total, 4 passed, 7 failed, 0 inconclusive, 0 skipped.

All seven failures stopped at the expected missing public
`InitializeInventory` contract (`Expected: not null; But was: null`). Unity's
test log completed with code 2.

Artifacts:

- `Artifacts/slot-one-sword-task1-red.xml`
- `Artifacts/slot-one-sword-task1-red.log`

## GREEN

Final command:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Users\myong\NaManMoo' -runTests -testPlatform EditMode -testFilter 'PlayerSwordShooterTests' -testResults 'C:\Users\myong\NaManMoo\Artifacts\slot-one-sword-task1-green-final.xml' -logFile 'C:\Users\myong\NaManMoo\Artifacts\slot-one-sword-task1-green-final.log'
```

Artifact totals: 11 total, 11 passed, 0 failed, 0 inconclusive, 0 skipped.
Unity's test log completed with code 0.

Artifacts:

- `Artifacts/slot-one-sword-task1-green-final.xml`
- `Artifacts/slot-one-sword-task1-green-final.log`

## Self-review

- Initialization asserts the shooter stores the same inventory object, not a
  replacement or copy.
- Existing firing, cooldown, Inspector validation, projectile configuration,
  Sprite, and direction tests all create a real slot-0 sword inventory by
  default.
- The slot-switch test uses the real Input System keyboard: Digit2 selects
  slot 1 and blocks firing; Digit1 reselects slot 0 while Right Arrow remains
  held and fires immediately at time 0.2, before the prior 0.5-second cooldown.
- Null inventory, empty slot 0, and a non-sword weapon each produce no
  projectile.
- The inventory gate precedes direction calculation and cooldown processing,
  and resets `firingDirectionActive` on every blocked call.
- No hotbar dimensions or Stage1 integration were changed.

## Concerns

- Verification was intentionally focused on `PlayerSwordShooterTests` as
  required; the full EditMode and PlayMode suites were not run in this task.
