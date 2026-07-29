# Stage1 Robot Boss Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a gated upper-room robot boss with chase, radial bullets, dash attacks, rage behavior, and a boss health bar.

**Architecture:** `Stage1BossEncounter` owns the one-shot room transition and gate lifecycle. `BossRobotController` owns a coroutine state machine and pooled `BossBullet` objects. `BossHealthBarView` observes `EnemyHealth` independently of the player health UI.

**Tech Stack:** Unity 6.0, C#, Rigidbody2D, UGUI, NUnit/Unity Test Framework

## Global Constraints

- Boss HP 100; contact and bullet damage 4.
- Chase 1.25, bullet speed 3.75, dash speed 10.
- Three eight-direction waves, 0.15 seconds apart.
- Three seconds between patterns.
- Dash sequence: two-second windup, 0.6-second dash, two-second recovery.

---

### Task 1: Reclosable Gate and Observable Enemy Health

**Files:**
- Modify: `Assets/Scripts/Enemies/Stage1EncounterGate.cs`
- Modify: `Assets/Scripts/Combat/EnemyHealth.cs`
- Modify tests in `Assets/Tests/Editor`

- [ ] Add failing tests for `Close`, `Open`, and enemy health change events.
- [ ] Implement minimal APIs and verify focused tests.

### Task 2: Boss Combat Units

**Files:**
- Create: `Assets/Scripts/Enemies/BossRobotController.cs`
- Create: `Assets/Scripts/Enemies/BossBullet.cs`
- Create: `Assets/Tests/Editor/BossRobotControllerTests.cs`

- [ ] Add failing tests for speeds, radial directions, rage tint/probability, and damage.
- [ ] Implement controller state machine and pooled bullet behavior.
- [ ] Verify focused tests.

### Task 3: Boss Health UI

**Files:**
- Create: `Assets/Scripts/UI/BossHealthBarView.cs`
- Create: `Assets/Scripts/UI/BossHealthBarUIFactory.cs`
- Create: `Assets/Tests/Editor/BossHealthBarViewTests.cs`

- [ ] Add failing tests for 100/100, partial width, and hide-on-death.
- [ ] Implement top-center boss UI.
- [ ] Verify focused tests.

### Task 4: Stage1 Encounter Integration

**Files:**
- Create: `Assets/Scripts/Enemies/Stage1BossEncounter.cs`
- Create: `Assets/Scripts/Stage1BossEncounterSetup.cs`
- Modify: `Assets/Editor/Stage1SceneBuilder.cs`
- Modify: `Assets/Scripts/Stage1RuntimeBootstrap.cs`
- Modify/add Stage1 integration tests.

- [ ] Add failing tests for upper entry, one-shot spawn, HP 100, gate close, and gate reopen on death.
- [ ] Implement shared setup and editor/runtime integrations.
- [ ] Rebuild Stage1 and verify focused tests.

### Task 5: Full Verification

- [ ] Run all EditMode tests.
- [ ] Run all PlayMode tests.
- [ ] Inspect the saved Stage1 boss sprite, trigger, gate, and UI references.
