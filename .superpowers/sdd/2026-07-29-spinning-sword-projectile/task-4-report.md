# Task 4 Report: Integrate with Stage1 and Verify Regressions

## Status

DONE

`Stage1SceneBuilder` and `Stage1RuntimeBootstrap` now configure
`PlayerSwordShooter` with the exact Sprite at `Assets/Weapons/sword.png`.
The generated Stage1 scene was rebuilt successfully, and the focused combat,
full EditMode, and full PlayMode regressions all pass.

## Files changed

Production and tests:

- `Assets/Editor/Stage1SceneBuilder.cs`
- `Assets/Scripts/Stage1RuntimeBootstrap.cs`
- `Assets/Tests/Editor/Stage1SceneBuilderTests.cs`
- generated `Assets/Scenes/Stage1.unity`
- `.superpowers/sdd/2026-07-29-spinning-sword-projectile/task-4-report.md`

Verification artifacts:

- `Artifacts/spinning-sword-task4-red.xml`
- `Artifacts/spinning-sword-task4-red.log`
- `Artifacts/spinning-sword-task4-green.xml`
- `Artifacts/spinning-sword-task4-green.log`
- `Artifacts/spinning-sword-task4-green-final.xml`
- `Artifacts/spinning-sword-task4-green-final.log`
- `Artifacts/spinning-sword-task4-runtime-red.xml`
- `Artifacts/spinning-sword-task4-runtime-red.log`
- `Artifacts/spinning-sword-task4-green-final2.xml`
- `Artifacts/spinning-sword-task4-green-final2.log`
- `Artifacts/spinning-sword-task4-builder.log`
- `Artifacts/spinning-sword-task4-combat.xml`
- `Artifacts/spinning-sword-task4-combat.log`
- `Artifacts/spinning-sword-task4-editmode.xml`
- `Artifacts/spinning-sword-task4-editmode.log`
- `Artifacts/spinning-sword-task4-editmode-final.xml`
- `Artifacts/spinning-sword-task4-editmode-final.log`
- `Artifacts/spinning-sword-task4-playmode.xml`
- `Artifacts/spinning-sword-task4-playmode.log`

No Git commands were used.

## Focused TDD RED

Command:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Users\myong\NaManMoo' -runTests -testPlatform EditMode -testFilter Stage1SceneBuilderTests -testResults 'C:\Users\myong\NaManMoo\Artifacts\spinning-sword-task4-red.xml' -logFile 'C:\Users\myong\NaManMoo\Artifacts\spinning-sword-task4-red.log'
```

The tests were changed before the Stage1 production integration. Exact RED
totals from `spinning-sword-task4-red.xml`:

- total: 5
- passed: 2
- failed: 3
- skipped: 0
- inconclusive: 0

The three expected assertion failures were:

- `Build_AssignsSwordShooterSpriteAndDefaultConfigurationToPlayer`: the saved
  Player did not have a `PlayerSwordShooter`.
- `RuntimeBootstrap_EditorValidationAssignsProjectSprites`: the serialized
  runtime `swordSprite` field did not exist.
- `RuntimeBootstrap_MissingSwordSpriteRejectsBeforeBuilding`: the runtime
  `swordSprite` field did not exist, so the required missing-Sprite guard could
  not be exercised.

There were no compiler errors. This was an assertion-level RED caused by the
missing integration.

During self-review, a mutation check found that the runtime-created Player's
shooter attachment was not independently protected. A test was added while
the two runtime attachment lines were temporarily removed. Exact supplemental
RED totals from `spinning-sword-task4-runtime-red.xml`:

- total: 6
- passed: 5
- failed: 1

The sole failure was
`RuntimeBootstrap_BuildsPlayerWithConfiguredSwordShooter`, with a null shooter.
The minimal two-line attachment/Sprite assignment was then restored.

## Focused TDD GREEN

Final command:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Users\myong\NaManMoo' -runTests -testPlatform EditMode -testFilter Stage1SceneBuilderTests -testResults 'C:\Users\myong\NaManMoo\Artifacts\spinning-sword-task4-green-final2.xml' -logFile 'C:\Users\myong\NaManMoo\Artifacts\spinning-sword-task4-green-final2.log'
```

Exact final GREEN totals:

- total: 6
- passed: 6
- failed: 0
- skipped: 0
- inconclusive: 0
- Unity Test Runner exit: code 0

An intermediate GREEN run passed 4/5 because the test retained a Sprite object
loaded before `Stage1SceneBuilder.Build()` called `AssetDatabase.Refresh()`.
The test was corrected to keep the pre-build non-null asset check but reload
the current Sprite after reopening the generated scene for the exact-reference
comparison. The final path assertion remained
`Assets/Weapons/sword.png`.

## Builder result

Command:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath 'C:\Users\myong\NaManMoo' -executeMethod Stage1SceneBuilder.Build -logFile 'C:\Users\myong\NaManMoo\Artifacts\spinning-sword-task4-builder.log'
```

`spinning-sword-task4-builder.log` records:

```text
Exiting batchmode successfully now!
Exiting without the bug reporter. Application will terminate with return code 0
```

The generated `Assets/Scenes/Stage1.unity` was imported successfully.

## Focused combat tests

Command:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Users\myong\NaManMoo' -runTests -testPlatform EditMode -testFilter 'SwordProjectileTests;PlayerSwordShooterTests' -testResults 'C:\Users\myong\NaManMoo\Artifacts\spinning-sword-task4-combat.xml' -logFile 'C:\Users\myong\NaManMoo\Artifacts\spinning-sword-task4-combat.log'
```

Exact totals:

- combined: 15 total, 15 passed, 0 failed, 0 skipped, 0 inconclusive
- `SwordProjectileTests`: 6/6 passed
- `PlayerSwordShooterTests`: 9/9 passed
- Unity Test Runner exit: code 0

## Full EditMode

Final command:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Users\myong\NaManMoo' -runTests -testPlatform EditMode -testResults 'C:\Users\myong\NaManMoo\Artifacts\spinning-sword-task4-editmode-final.xml' -logFile 'C:\Users\myong\NaManMoo\Artifacts\spinning-sword-task4-editmode-final.log'
```

Exact totals:

- total: 82
- passed: 82
- failed: 0
- skipped: 0
- inconclusive: 0
- Unity Test Runner exit: code 0

## Full PlayMode

Command:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Users\myong\NaManMoo' -runTests -testPlatform PlayMode -testResults 'C:\Users\myong\NaManMoo\Artifacts\spinning-sword-task4-playmode.xml' -logFile 'C:\Users\myong\NaManMoo\Artifacts\spinning-sword-task4-playmode.log'
```

Exact totals:

- total: 2
- passed: 2
- failed: 0
- skipped: 0
- inconclusive: 0
- Unity Test Runner exit: code 0

## Saved YAML inspection

Source GUIDs:

- `PlayerSwordShooter.cs.meta`:
  `4cd6c7f5fc8da2742af66979557da978`
- `Assets/Weapons/sword.png.meta`:
  `c2b45cf6255b54d4c9cb54f7cd626537`

The Player component block in `Assets/Scenes/Stage1.unity` contains:

```yaml
m_Script: {fileID: 11500000, guid: 4cd6c7f5fc8da2742af66979557da978, type: 3}
m_EditorClassIdentifier: NaManMoo.Runtime::PlayerSwordShooter
damage: 5
shotsPerSecond: 3
projectileSpeed: 8
spinSpeed: 720
projectileLifetime: 4
spawnOffset: 0.8
swordSprite: {fileID: 21300000, guid: c2b45cf6255b54d4c9cb54f7cd626537, type: 3}
```

This confirms the exact shooter script, exact damage 5, positive rate 3, and
the exact sword Sprite GUID.

## Self-review

- `Stage1SceneBuilder.Build` loads the exact sword Sprite before folder
  creation or scene replacement. If absent, it throws an
  `InvalidOperationException` whose actionable message includes
  `Assets/Weapons/sword.png`.
- The builder adds `PlayerSwordShooter` to Player and assigns the loaded Sprite.
  It does not override the shooter's serialized defaults, so the saved scene
  retains damage 5 and shots per second 3.
- `Stage1RuntimeBootstrap` has serialized `Sprite swordSprite`.
  `Reset` and `OnValidate` populate it beside the player and hotbar Sprites.
- Runtime validation checks `swordSprite` before `BuildStage`, throws an
  actionable message containing the exact asset path, and creates no
  `Generated Stage` on that failure.
- The runtime-created Player receives `PlayerSwordShooter` with the same
  validated Sprite.
- No changes were made to `PlayerSwordShooter`; the existing public
  `SwordSprite` getter/setter was sufficient.
- Existing movement, player visual scale/aspect, Rigidbody2D,
  CircleCollider2D, hotbar, map 2.5x coordinates, and camera assertions remain
  in `Stage1SceneBuilderTests` and pass in the focused and full suites.
- The final successful builder, focused GREEN, combat, full EditMode, and full
  PlayMode logs were scanned for compiler errors, `NullReferenceException`,
  `MissingReferenceException`, and assertion failures. None were present.

## Concerns

- Unity emitted transient LicensingClient handshake/access-token messages in
  batch logs. Every required final run produced a valid result XML, the test
  runner reported code 0, and the builder log reported process return code 0,
  so these messages did not affect execution.
- The intermediate 4/5 GREEN artifact is retained for auditability; its sole
  failure was the stale pre-refresh Sprite object identity described above.
  The final focused result is `spinning-sword-task4-green-final2.xml` at 6/6.
- No remaining functional or regression concern was found.
