# Player Dash Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a two-charge Shift dash that lasts 0.6 seconds, travels at three times movement speed, grants invulnerability, leaves fading afterimages, and exposes runtime-adjustable ability values for items.

**Architecture:** Keep ordinary WASD movement in `PlayerMovement` and add a focused `PlayerDash` component that owns activation, movement override, charges, recharge, and visual emission. Extend `PlayerHealth` with composable timed invulnerability, and keep individual afterimage fading in a small `PlayerDashAfterimage` component.

**Tech Stack:** Unity 6, C#, Input System, Rigidbody2D, SpriteRenderer, NUnit, Unity Test Framework.

## Global Constraints

- Either Shift key triggers one dash per key press; holding Shift cannot repeatedly activate it.
- Defaults are duration `0.6f`, speed multiplier `3f`, maximum charges `2`, and sequential recharge duration `5f`.
- The player begins at maximum charges.
- Current movement direction takes priority; otherwise use the last non-zero movement direction. No known direction means no dash.
- Dash direction remains fixed and ordinary movement is suppressed until the dash ends.
- Invulnerability covers the full dash and cannot shorten existing protection.
- `Duration`, `SpeedMultiplier`, `MaxCharges`, `RechargeDuration`, `AfterimageInterval`, and `AfterimageLifetime` are runtime-adjustable instance properties for item effects.
- Do not add charge UI, gamepad input, wall phasing, sound, camera shake, afterimage pooling, or a concrete dash-modifying item.
- Do not create Git commits. The repository instruction reserves commits for an explicit user request.

## File Structure

- Create `Assets/Scripts/Player/PlayerDash.cs`: dash state, Shift input, physics movement, charges, recharge, and afterimage spawning.
- Create `Assets/Scripts/Player/PlayerDashAfterimage.cs`: isolated sprite fade/lifetime behavior.
- Create `Assets/Tests/Editor/PlayerDashTests.cs`: deterministic dash state, direction, configuration, and recharge tests.
- Create `Assets/Tests/Editor/PlayerDashAfterimageTests.cs`: deterministic fade calculation tests.
- Modify `Assets/Scripts/PlayerMovement.cs`: expose movement state/speed and allow dash movement suppression.
- Modify `Assets/Scripts/Player/PlayerHealth.cs`: add composable externally granted invulnerability.
- Modify `Assets/Scripts/Player/PlayerFactory.cs`: attach and configure `PlayerDash`.
- Modify `Assets/Tests/Editor/PlayerMovementTests.cs`: cover direction memory and movement suppression contract.
- Modify `Assets/Tests/Editor/PlayerHealthTests.cs`: cover external invulnerability extension.
- Modify `Assets/Tests/Editor/SampleStageSceneTests.cs`: verify the canonical player includes dash dependencies.
- Unity creates the corresponding `.meta` files for all new assets.

---

### Task 1: Movement and Invulnerability Contracts

**Files:**
- Modify: `Assets/Scripts/PlayerMovement.cs`
- Modify: `Assets/Scripts/Player/PlayerHealth.cs`
- Modify: `Assets/Tests/Editor/PlayerMovementTests.cs`
- Modify: `Assets/Tests/Editor/PlayerHealthTests.cs`

**Interfaces:**
- Produces: `float PlayerMovement.MoveSpeed { get; set; }`
- Produces: `Vector2 PlayerMovement.CurrentDirection { get; }`
- Produces: `Vector2 PlayerMovement.LastMoveDirection { get; }`
- Produces: `bool PlayerMovement.MovementSuppressed { get; set; }`
- Produces: `void PlayerMovement.SetMoveInput(Vector2 rawInput)` for input-independent state updates and tests.
- Produces: `void PlayerHealth.GrantInvulnerability(float currentTime, float duration)`
- Produces: `bool PlayerHealth.IsInvulnerable(float currentTime)`

- [ ] **Step 1: Write failing movement contract tests**

Add tests that instantiate a GameObject with `Rigidbody2D` and `PlayerMovement`, then verify:

```csharp
[Test]
public void SetMoveInput_RemembersLastNonZeroDirection()
{
    movement.SetMoveInput(Vector2.right);
    movement.SetMoveInput(Vector2.zero);

    Assert.That(movement.CurrentDirection, Is.EqualTo(Vector2.zero));
    Assert.That(movement.LastMoveDirection, Is.EqualTo(Vector2.right));
}

[Test]
public void MovementProperties_ClampSpeedAndExposeSuppression()
{
    movement.MoveSpeed = -2f;
    movement.MovementSuppressed = true;

    Assert.That(movement.MoveSpeed, Is.Zero);
    Assert.That(movement.MovementSuppressed, Is.True);
}
```

Use test fixture setup/teardown to destroy the GameObject and avoid leaking physics objects between tests.

- [ ] **Step 2: Run focused movement tests and confirm red**

Run Unity Edit Mode tests with `-testFilter PlayerMovementTests`, writing results to `Artifacts/player-dash-movement-red.xml`.

Expected: compilation failures because the new members do not exist.

- [ ] **Step 3: Implement the movement contract**

Refactor input processing through:

```csharp
public void SetMoveInput(Vector2 rawInput)
{
    moveDirection = CalculateDirection(rawInput);
    if (moveDirection.sqrMagnitude > 0f)
    {
        lastMoveDirection = moveDirection;
    }
}
```

Expose the four properties listed above. `Update` calls `SetMoveInput(rawInput)`, and the no-keyboard path calls `SetMoveInput(Vector2.zero)`. `FixedUpdate` returns without applying ordinary movement while `MovementSuppressed` is true.

- [ ] **Step 4: Run focused movement tests and confirm green**

Run `PlayerMovementTests` again to `Artifacts/player-dash-movement-green.xml`.

Expected: all tests pass.

- [ ] **Step 5: Write failing health invulnerability tests**

Add deterministic tests:

```csharp
[Test]
public void GrantInvulnerability_BlocksDamageUntilGrantedDeadline()
{
    health.GrantInvulnerability(10f, 0.6f);

    Assert.That(health.TryTakeDamage(2, 10.59f, 1f), Is.False);
    Assert.That(health.TryTakeDamage(2, 10.6f, 1f), Is.True);
}

[Test]
public void GrantInvulnerability_DoesNotShortenExistingProtection()
{
    health.GrantInvulnerability(10f, 2f);
    health.GrantInvulnerability(10.5f, 0.1f);

    Assert.That(health.IsInvulnerable(11.99f), Is.True);
    Assert.That(health.IsInvulnerable(12f), Is.False);
}
```

- [ ] **Step 6: Run focused health tests and confirm red**

Run Unity Edit Mode tests with `-testFilter PlayerHealthTests`, writing results to `Artifacts/player-dash-health-red.xml`.

Expected: compilation failures for the missing invulnerability methods.

- [ ] **Step 7: Implement composable invulnerability**

Add:

```csharp
public void GrantInvulnerability(float currentTime, float duration)
{
    invulnerableUntil = Mathf.Max(
        invulnerableUntil,
        currentTime + Mathf.Max(0f, duration));
}

public bool IsInvulnerable(float currentTime)
{
    return currentTime < invulnerableUntil;
}
```

Update `TryTakeDamage` to use `IsInvulnerable(currentTime)` and call `GrantInvulnerability` after successful damage instead of directly overwriting `invulnerableUntil`.

- [ ] **Step 8: Run focused health tests and confirm green**

Run `PlayerHealthTests` again to `Artifacts/player-dash-health-green.xml`.

Expected: old damage immunity behavior and new external immunity tests all pass.

### Task 2: Deterministic Dash State and Sequential Recharge

**Files:**
- Create: `Assets/Scripts/Player/PlayerDash.cs`
- Create: `Assets/Tests/Editor/PlayerDashTests.cs`

**Interfaces:**
- Consumes: Task 1 `PlayerMovement` and `PlayerHealth` interfaces.
- Produces: `float Duration`, `float SpeedMultiplier`, `int MaxCharges`, `float RechargeDuration`, `float AfterimageInterval`, and `float AfterimageLifetime`.
- Produces: `int CurrentCharges { get; }`, `bool IsDashing { get; }`, and `Vector2 DashDirection { get; }`.
- Produces: `bool TryStartDash(float currentTime)`.
- Produces: `void Tick(float deltaTime, float currentTime)` for deterministic duration/recharge tests.

- [ ] **Step 1: Write failing default, direction, and activation tests**

Create a fixture with `Rigidbody2D`, `PlayerMovement`, `PlayerHealth`, and `PlayerDash`. Cover:

```csharp
[Test]
public void Defaults_StartFullyCharged()
{
    Assert.That(dash.Duration, Is.EqualTo(0.6f));
    Assert.That(dash.SpeedMultiplier, Is.EqualTo(3f));
    Assert.That(dash.MaxCharges, Is.EqualTo(2));
    Assert.That(dash.CurrentCharges, Is.EqualTo(2));
    Assert.That(dash.RechargeDuration, Is.EqualTo(5f));
}

[Test]
public void TryStartDash_UsesCurrentThenLastDirection()
{
    movement.SetMoveInput(Vector2.up);
    Assert.That(dash.TryStartDash(0f), Is.True);
    Assert.That(dash.DashDirection, Is.EqualTo(Vector2.up));
}

[Test]
public void TryStartDash_RejectsMissingDirectionWithoutSpendingCharge()
{
    Assert.That(dash.TryStartDash(0f), Is.False);
    Assert.That(dash.CurrentCharges, Is.EqualTo(2));
}
```

Also verify a valid dash consumes one charge, suppresses ordinary movement, grants health immunity, and a second request during the dash is rejected without spending another charge.

- [ ] **Step 2: Run focused dash tests and confirm red**

Run Unity Edit Mode tests with `-testFilter PlayerDashTests`, writing results to `Artifacts/player-dash-state-red.xml`.

Expected: compilation failure because `PlayerDash` does not exist.

- [ ] **Step 3: Implement minimal activation and runtime configuration**

Create `PlayerDash` with required component attributes for `Rigidbody2D`, `PlayerMovement`, and `PlayerHealth`. Cache dependencies in `Awake`, initialize charges to the configured maximum, and implement clamped properties.

`TryStartDash(currentTime)` selects:

```csharp
Vector2 direction = movement.CurrentDirection.sqrMagnitude > 0f
    ? movement.CurrentDirection
    : movement.LastMoveDirection;
```

On success, consume one charge, record direction, set `IsDashing`, reset elapsed dash time, suppress movement, and call `health.GrantInvulnerability(currentTime, Duration)`.

- [ ] **Step 4: Run activation tests and confirm green**

Run `PlayerDashTests` again to `Artifacts/player-dash-state-green.xml`.

Expected: activation and default-value tests pass.

- [ ] **Step 5: Write failing duration, recharge, and mutation tests**

Cover:

```csharp
[Test]
public void Tick_EndsDashAtConfiguredDuration()
{
    StartDashRight();
    dash.Tick(0.59f, 0.59f);
    Assert.That(dash.IsDashing, Is.True);

    dash.Tick(0.01f, 0.6f);
    Assert.That(dash.IsDashing, Is.False);
    Assert.That(movement.MovementSuppressed, Is.False);
}

[Test]
public void Tick_RechargesMissingChargesSequentially()
{
    SpendTwoCharges();

    dash.Tick(4.99f, 4.99f);
    Assert.That(dash.CurrentCharges, Is.Zero);
    dash.Tick(0.01f, 5f);
    Assert.That(dash.CurrentCharges, Is.EqualTo(1));
    dash.Tick(5f, 10f);
    Assert.That(dash.CurrentCharges, Is.EqualTo(2));
}
```

Add tests proving that increasing `MaxCharges` leaves the added capacity empty, decreasing it clamps current charges, changing `RechargeDuration` affects the active cycle, and invalid values are clamped.

- [ ] **Step 6: Run focused tests and confirm red**

Run `PlayerDashTests` to `Artifacts/player-dash-recharge-red.xml`.

Expected: recharge/duration assertions fail.

- [ ] **Step 7: Implement dash duration and sequential recharge**

`Tick` advances dash elapsed time and missing-charge recharge elapsed time using `deltaTime`. End the dash once elapsed duration reaches `Duration`. Use a loop when accumulated recharge time crosses `RechargeDuration` so a long frame can restore multiple sequential charges without losing time:

```csharp
while (currentCharges < MaxCharges &&
       rechargeElapsed >= RechargeDuration)
{
    rechargeElapsed -= RechargeDuration;
    currentCharges++;
}
```

Reset recharge elapsed time to zero whenever charges reach maximum. Preserve accumulated time when a runtime setter changes `RechargeDuration`.

- [ ] **Step 8: Run all dash state tests and confirm green**

Run `PlayerDashTests` to `Artifacts/player-dash-recharge-green.xml`.

Expected: all dash state and recharge tests pass.

### Task 3: Physics Movement, Shift Input, and Afterimages

**Files:**
- Modify: `Assets/Scripts/Player/PlayerDash.cs`
- Create: `Assets/Scripts/Player/PlayerDashAfterimage.cs`
- Create: `Assets/Tests/Editor/PlayerDashAfterimageTests.cs`
- Modify: `Assets/Tests/Editor/PlayerDashTests.cs`

**Interfaces:**
- Consumes: Task 2 dash state and configuration.
- Produces: `static Color PlayerDashAfterimage.EvaluateColor(Color startColor, float normalizedAge)`.
- Produces: `void PlayerDash.ConfigureVisual(SpriteRenderer renderer)`.

- [ ] **Step 1: Write failing physics and input-edge tests**

Add tests for a pure movement delta helper:

```csharp
[Test]
public void CalculateDashDelta_UsesMoveSpeedMultiplierAndFixedDelta()
{
    Assert.That(
        PlayerDash.CalculateDashDelta(Vector2.right, 5f, 3f, 0.02f),
        Is.EqualTo(new Vector2(0.3f, 0f)));
}
```

Keep keyboard polling thin: `Update` checks `leftShiftKey.wasPressedThisFrame || rightShiftKey.wasPressedThisFrame` and delegates to `TryStartDash(Time.time)`. This explicitly prevents held-key repeat.

- [ ] **Step 2: Run focused dash tests and confirm red**

Run `PlayerDashTests` to `Artifacts/player-dash-motion-red.xml`.

Expected: missing `CalculateDashDelta` failure.

- [ ] **Step 3: Implement physics movement and Shift input**

Add `Update` for edge-triggered Shift activation and `Tick(Time.deltaTime, Time.time)`. Add `FixedUpdate` that calls `Rigidbody2D.MovePosition` with:

```csharp
public static Vector2 CalculateDashDelta(
    Vector2 direction,
    float moveSpeed,
    float speedMultiplier,
    float fixedDeltaTime)
{
    return direction * Mathf.Max(0f, moveSpeed)
        * Mathf.Max(0f, speedMultiplier)
        * Mathf.Max(0f, fixedDeltaTime);
}
```

Only apply this movement while `IsDashing`.

- [ ] **Step 4: Run focused dash tests and confirm green**

Run `PlayerDashTests` to `Artifacts/player-dash-motion-green.xml`.

Expected: all dash tests pass.

- [ ] **Step 5: Write failing afterimage fade tests**

Create:

```csharp
[TestCase(0f, 1f)]
[TestCase(0.5f, 0.5f)]
[TestCase(1f, 0f)]
public void EvaluateColor_FadesAlphaLinearly(float age, float alpha)
{
    Color result = PlayerDashAfterimage.EvaluateColor(
        new Color(0.2f, 0.4f, 0.6f, 1f), age);

    Assert.That(result.a, Is.EqualTo(alpha).Within(0.0001f));
}
```

Also test clamping below zero and above one.

- [ ] **Step 6: Run afterimage tests and confirm red**

Run Unity Edit Mode tests with `-testFilter PlayerDashAfterimageTests`, writing results to `Artifacts/player-dash-afterimage-red.xml`.

Expected: compilation failure because the component is missing.

- [ ] **Step 7: Implement afterimage fading and emission**

`PlayerDashAfterimage` stores its renderer, starting color, lifetime, and elapsed age. `Update` evaluates a clamped normalized age, applies `EvaluateColor`, and destroys the GameObject when lifetime expires.

`PlayerDash.ConfigureVisual` stores the player `SpriteRenderer`. While dashing, accumulate emission time and spawn a renderer at each `AfterimageInterval`. Copy sprite, world transform, flip flags, renderer color with reduced initial alpha, sorting layer, and a sorting order immediately behind the player. Add no collider or Rigidbody2D.

If the renderer is null, skip emission without affecting the dash. Clamp interval and lifetime to small positive values.

- [ ] **Step 8: Run afterimage and dash tests and confirm green**

Run `PlayerDashAfterimageTests;PlayerDashTests` to `Artifacts/player-dash-visual-green.xml`.

Expected: all tests pass.

### Task 4: Player Factory and Scene Integration

**Files:**
- Modify: `Assets/Scripts/Player/PlayerFactory.cs`
- Modify: `Assets/Tests/Editor/SampleStageSceneTests.cs`
- Modify if required by scene inspection: `Assets/Editor/Stage1SceneBuilder.cs`
- Modify if required by scene inspection: `Assets/Editor/DungeonSceneBuilder.cs`
- Modify if required by runtime inspection: `Assets/Scripts/Stage1RuntimeBootstrap.cs`

**Interfaces:**
- Consumes: `PlayerDash.ConfigureVisual(SpriteRenderer renderer)`.
- Produces: every `PlayerFactory.Create` player has one configured `PlayerDash`.

- [ ] **Step 1: Write a failing integration test**

Open the canonical SampleStage scene and assert:

```csharp
[Test]
public void SampleStagePlayer_HasConfiguredDash()
{
    EditorSceneManager.OpenScene(Stage1SceneBuilder.ScenePath);
    GameObject player = GameObject.Find("Player");
    PlayerDash dash = player.GetComponent<PlayerDash>();

    Assert.That(dash, Is.Not.Null);
    Assert.That(player.GetComponent<PlayerMovement>(), Is.Not.Null);
    Assert.That(player.GetComponent<PlayerHealth>(), Is.Not.Null);
    Assert.That(player.transform.Find("Player Visual")
        .GetComponent<SpriteRenderer>(), Is.Not.Null);
}
```

- [ ] **Step 2: Run integration test and confirm red**

Run `SampleStageSceneTests` to `Artifacts/player-dash-integration-red.xml`.

Expected: the canonical player has no `PlayerDash`.

- [ ] **Step 3: Integrate the dash in `PlayerFactory`**

After the player visual renderer and required gameplay components exist, add one `PlayerDash` and call `ConfigureVisual(renderer)`. Ensure `PlayerHealth` exists before dash initialization, either by adding health before dash or allowing the existing health setup to reuse the required component.

Inspect the three `PlayerFactory.Create` call paths. Since they share the factory, do not duplicate dash setup in scene builders unless a path replaces/removes the component.

- [ ] **Step 4: Regenerate or update canonical scenes if their serialized player objects require it**

Run the existing scene builder flow only if the integration test proves the scene stores a prebuilt player rather than creating it through runtime bootstrap. Preserve unrelated serialized scene content.

- [ ] **Step 5: Run focused integration tests and confirm green**

Run `SampleStageSceneTests;PlayerDashTests;PlayerMovementTests;PlayerHealthTests;PlayerDashAfterimageTests` to `Artifacts/player-dash-focused-green.xml`.

Expected: all focused tests pass.

### Task 5: Full Verification

**Files:**
- Verify only; modify production or test files only if failures reveal an in-scope regression.

**Interfaces:**
- Consumes: all previous task outputs.
- Produces: test and compilation evidence for handoff.

- [ ] **Step 1: Run the complete Edit Mode suite**

Use the installed Unity editor version from `ProjectSettings/ProjectVersion.txt`:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' `
  -batchmode -nographics -projectPath $PWD `
  -runTests -testPlatform EditMode `
  -testResults 'Artifacts/player-dash-final-editmode.xml' `
  -logFile 'Artifacts/player-dash-final-editmode.log'
```

Expected: Unity exits successfully and the XML reports zero failed tests.

- [ ] **Step 2: Run the complete Play Mode suite**

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' `
  -batchmode -nographics -projectPath $PWD `
  -runTests -testPlatform PlayMode `
  -testResults 'Artifacts/player-dash-final-playmode.xml' `
  -logFile 'Artifacts/player-dash-final-playmode.log'
```

Expected: Unity exits successfully and the XML reports zero failed tests.

- [ ] **Step 3: Run a project compilation check**

Run `dotnet build NaManMoo.slnx --no-restore`.

Expected: zero build errors. Treat Unity test compilation as authoritative if generated solution metadata is stale.

- [ ] **Step 4: Inspect the final diff**

Run `git status --short` and `git diff --check`, then inspect `git diff` for only the planned dash, health, movement, integration, test, and documentation changes.

Expected: no whitespace errors, no unexpected generated assets, and no commit.
