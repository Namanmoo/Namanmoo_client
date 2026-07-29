# Sword Fire Cooldown Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Limit the slot-one sword to one projectile every 0.5 seconds even when the firing key is rapidly released and pressed.

**Architecture:** Keep `PlayerSwordShooter.ProcessInput(Keyboard, float)` as the single timing boundary. Use its existing `nextShotTime` for every shot, set the default rate to two shots per second, and remove input-reactivation as a cooldown bypass.

**Tech Stack:** Unity 6, C#, Input System, Unity Test Framework, NUnit

## Global Constraints

- The first ready directional input fires immediately.
- Holding a direction fires one sword every 0.5 seconds.
- Releasing and pressing again does not reset the cooldown.
- Switching away from and back to slot one does not reset the cooldown.
- Direction changes affect the next projectile without resetting the cooldown.

---

### Task 1: Enforce the Sword Cooldown Across Input Reactivation

**Files:**
- Modify: `Assets/Tests/Editor/PlayerSwordShooterTests.cs`
- Modify: `Assets/Tests/PlayMode/ItemHotbarPlayModeTests.cs`
- Modify: `Assets/Scripts/Combat/PlayerSwordShooter.cs`

**Interfaces:**
- Consumes: `PlayerSwordShooter.ProcessInput(Keyboard keyboard, float currentTime)`
- Produces: A two-shots-per-second default and an unbypassable `nextShotTime`

- [ ] **Step 1: Write failing tests**

Change the Inspector default expectation to:

```csharp
Assert.That(
    serializedShooter.FindProperty("shotsPerSecond").floatValue,
    Is.EqualTo(2f));
```

Change the release/reactivation test so it verifies no shot at `0.2` and the
second shot at `0.5`:

```csharp
Release(keyboard.rightArrowKey);
shooter.ProcessInput(keyboard, 0.1f);
Press(keyboard.rightArrowKey);
shooter.ProcessInput(keyboard, 0.2f);
Assert.That(ProjectileCount(), Is.EqualTo(1));

shooter.ProcessInput(keyboard, 0.5f);
Assert.That(ProjectileCount(), Is.EqualTo(2));
```

Change the slot reselection test to expect one projectile immediately after
reselection at `0.2`, then two at `0.5`.

- [ ] **Step 2: Run focused tests and verify RED**

Run:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' `
  -batchmode -nographics `
  -projectPath 'C:\Users\myong\NaManMoo' `
  -runTests -testPlatform EditMode `
  -testFilter PlayerSwordShooterTests `
  -testResults 'C:\Users\myong\NaManMoo\Artifacts\sword-cooldown-red.xml' `
  -logFile 'C:\Users\myong\NaManMoo\Artifacts\sword-cooldown-red.log'
```

Expected: the default-rate assertion reports `3` instead of `2`, and the
release/reselection tests report two projectiles instead of one before the
0.5-second boundary.

- [ ] **Step 3: Implement the minimal cooldown change**

In `PlayerSwordShooter`:

```csharp
[SerializeField, Min(0.01f)]
private float shotsPerSecond = 2f;
```

Remove `firingDirectionActive` and fire only when:

```csharp
if (currentTime >= nextShotTime)
{
    SpawnProjectile(direction);
    nextShotTime = currentTime + (1f / shotsPerSecond);
}
```

- [ ] **Step 4: Run focused tests and verify GREEN**

Run `PlayerSwordShooterTests` again. Expect all tests to pass with zero
failures.

- [ ] **Step 5: Run related integration tests**

Run `ItemHotbarPlayModeTests` and `Stage1SceneBuilderTests`. Expect the tests
to pass after updating only assertions that explicitly encode the old
three-shots-per-second default.

- [ ] **Step 6: Verify build and diff**

Run `Stage1SceneBuilder.Build`, confirm batch mode exits with code zero, restore
only Unity-generated scene and IDE project files, and confirm the intentional
diff contains only the shooter, its tests, and this plan.

- [ ] **Step 7: Commit the implementation**

```powershell
git add -- Assets/Scripts/Combat/PlayerSwordShooter.cs Assets/Tests/Editor/PlayerSwordShooterTests.cs Assets/Tests/PlayMode/ItemHotbarPlayModeTests.cs docs/superpowers/plans/2026-07-30-sword-fire-cooldown.md
git commit -m "fix: limit sword firing to two shots per second"
```
