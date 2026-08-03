# Player Dash Steering and Charge UI Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let WASD instantly redirect an active dash and render remaining dash charges as yellow or gray outlined circles below the player health bar.

**Architecture:** `PlayerMovement` continues recording normalized input while ordinary movement is suppressed, and `PlayerDash` updates its active direction from that state. `PlayerDash` publishes charge changes, while a focused `PlayerDashChargeView` builds and recolors runtime-generated circular UI indicators in the existing health overlay canvas.

**Tech Stack:** Unity 6, C#, Input System, Rigidbody2D, Unity UI, NUnit, Unity Test Framework.

## Global Constraints

- Active dash direction changes immediately for every non-zero WASD direction.
- Zero input preserves the most recent dash direction.
- Diagonal steering is normalized and does not increase speed.
- Indicator count equals `MaxCharges`.
- Available indicators have yellow fill and gray outline; spent indicators have gray fill and gray outline.
- Increasing maximum capacity creates spent slots that recharge normally.
- Do not add recharge-progress animation, sound, controller input, or new art assets.
- Do not perform Git operations or create commits.

---

### Task 1: Immediate Dash Steering

**Files:**
- Modify: `Assets/Scripts/Player/PlayerDash.cs`
- Modify: `Assets/Tests/Editor/PlayerDashTests.cs`

**Interfaces:**
- Produces: `void PlayerDash.UpdateDashDirection()`.
- Consumes: `PlayerMovement.CurrentDirection`.

- [ ] **Step 1: Write failing steering tests**

Add tests proving that an active rightward dash changes to `Vector2.up` after `SetMoveInput(Vector2.up)`, diagonal steering becomes a normalized diagonal vector, and zero input preserves the previous dash direction.

- [ ] **Step 2: Run RED**

Run Unity Edit Mode with `-testFilter PlayerDashTests`, saving `Artifacts/player-dash-steering-red.xml`. Expected: missing `UpdateDashDirection` or unchanged-direction assertions fail.

- [ ] **Step 3: Implement minimal steering**

Add `UpdateDashDirection()` that returns unless `IsDashing` and `movement.CurrentDirection` is non-zero, otherwise assigns `DashDirection = movement.CurrentDirection.normalized`. Call it from `Tick` before physics movement can use the next fixed step.

- [ ] **Step 4: Run GREEN**

Run `PlayerDashTests` to `Artifacts/player-dash-steering-green.xml`; require zero failures.

### Task 2: Charge Events and Circle View

**Files:**
- Modify: `Assets/Scripts/Player/PlayerDash.cs`
- Create: `Assets/Scripts/UI/PlayerDashChargeView.cs`
- Create: `Assets/Scripts/UI/PlayerDashChargeUIFactory.cs`
- Modify: `Assets/Tests/Editor/PlayerDashTests.cs`
- Create: `Assets/Tests/Editor/PlayerDashChargeViewTests.cs`

**Interfaces:**
- Produces: `event Action<int, int> PlayerDash.ChargesChanged`.
- Produces: `void PlayerDashChargeView.Initialize(PlayerDash dash)`.
- Produces: `PlayerDashChargeView PlayerDashChargeUIFactory.Create(Transform parent, PlayerDash dash)`.

- [ ] **Step 1: Write failing charge notification tests**

Assert that starting a dash reports `(1, 2)`, a recharge reports `(2, 2)`, and changing `MaxCharges` reports the preserved/clamped current count with the new maximum.

- [ ] **Step 2: Run event RED**

Run `PlayerDashTests` to `Artifacts/player-dash-charge-event-red.xml`. Expected: `ChargesChanged` is missing.

- [ ] **Step 3: Implement charge notifications**

Publish after successful charge spending, each recharge restoration, and effective `MaxCharges` mutation. Add `NotifyChargesChanged()` for initial view rendering without exposing mutable charge state.

- [ ] **Step 4: Write failing circle view tests**

Create a dash with maximum three and current two. Build the view and assert three `Charge Slot` children, first two fill images equal `AvailableColor`, third equals `SpentColor`, every outline equals `OutlineColor`, and `raycastTarget` is false. Spend/recharge and assert colors update; change maximum and assert child count rebuilds.

- [ ] **Step 5: Run view RED**

Run `PlayerDashChargeViewTests` to `Artifacts/player-dash-charge-ui-red.xml`. Expected: missing factory/view types.

- [ ] **Step 6: Implement circular UI**

`PlayerDashChargeUIFactory` creates a top-left anchored root at `(24, -82)`, 22-pixel circles with 8-pixel spacing, and initializes the view. `PlayerDashChargeView` creates a shared 32x32 antialiased circle sprite in memory, builds a gray 22x22 outline and 16x16 fill per slot, subscribes on enable, unsubscribes on disable, rebuilds only when maximum changes, and recolors by index `< currentCharges`.

- [ ] **Step 7: Run event and view GREEN**

Run `PlayerDashTests;PlayerDashChargeViewTests` to `Artifacts/player-dash-charge-ui-green.xml`; require zero failures.

### Task 3: Health Overlay and Saved Scene Integration

**Files:**
- Modify: `Assets/Scripts/Stage1PlayerHealthSetup.cs`
- Modify: `Assets/Tests/Editor/Stage1PlayerHealthSetupTests.cs`
- Modify: `Assets/Tests/Editor/SampleStageSceneTests.cs`
- Modify: `Assets/Tests/Editor/DungeonSceneBuilderTests.cs`
- Regenerate: `Assets/Scenes/SampleStage.unity`
- Regenerate: `Assets/Scenes/Dungeon.unity`

**Interfaces:**
- Consumes: `PlayerDashChargeUIFactory.Create(Transform, PlayerDash)`.
- Produces: every factory-built player overlay contains one bound `PlayerDashChargeView`.

- [ ] **Step 1: Write failing setup and scene tests**

Assert `Stage1PlayerHealthSetup.Create` creates a `PlayerDash` when absent and one `PlayerDashChargeView` under the same canvas. Open SampleStage and Dungeon and assert their player health canvases contain the view.

- [ ] **Step 2: Run integration RED**

Run the three focused fixtures to `Artifacts/player-dash-charge-integration-red.xml`. Expected: charge view assertions fail.

- [ ] **Step 3: Integrate with health setup**

Ensure `PlayerDash` exists after health, call the charge UI factory on the health canvas transform, and keep returning `PlayerHealthBarView` to avoid changing existing callers.

- [ ] **Step 4: Regenerate both scenes**

Execute `Stage1SceneBuilder.Build` and `DungeonSceneBuilder.Build` in Unity batch mode.

- [ ] **Step 5: Run focused GREEN**

Run `PlayerDashTests;PlayerDashChargeViewTests;PlayerMovementTests;PlayerHealthTests;Stage1PlayerHealthSetupTests;SampleStageSceneTests;DungeonSceneBuilderTests` to `Artifacts/player-dash-steering-ui-focused-green.xml`; require zero failures.

- [ ] **Step 6: Verify compilation**

Run `dotnet build NaManMoo.slnx --no-restore`; require zero build errors. Record unrelated pre-existing full-suite failures separately if full Unity suites are run.
