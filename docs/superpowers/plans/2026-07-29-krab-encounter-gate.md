# Krab Encounter Gate Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add five lower-area krabs that chase and contact-damage the player, plus a physical middle gate that opens only after all five die.

**Architecture:** `PlayerHealth` owns global timed invulnerability. `KrabEnemy` owns Rigidbody pursuit and contact requests, `EnemyHealth` emits death once, and `Stage1EncounterGate` subscribes to the five enemies and disables its physical/visual barrier when none remain.

**Tech Stack:** Unity 6, C#, Physics 2D, NUnit Unity Test Framework

## Global Constraints

- Exactly five krabs, each with 5 health and speed 3.
- Contact damage is 2 with one second of player invulnerability.
- Krabs and player are constrained by the existing EdgeCollider2D boundary.
- Gate position is `(-4.5, 0.5)` and size is `(13, 0.6)`.
- The gate opens only when all five registered krabs are dead.

---

### Task 1: Global Player Invulnerability

**Files:**
- Modify: `Assets/Scripts/Player/PlayerHealth.cs`
- Modify: `Assets/Scripts/Player/PlayerHealthDebugInput.cs`
- Test: `Assets/Tests/Editor/PlayerHealthTests.cs`

**Interfaces:**
- Produces: `bool TryTakeDamage(int amount, float currentTime, float invulnerabilityDuration)`.

- [ ] Add failing tests for a successful 2-damage hit at time 0, rejection at 0.99, acceptance at 1.0, non-positive rejection, clamping at zero, and UI notification only on successful damage.
- [ ] Run focused Edit Mode tests and require failure because `TryTakeDamage` is absent.
- [ ] Implement a global `invulnerableUntil` deadline and route debug H damage through the protected path.
- [ ] Run focused tests and require zero failures.

### Task 2: Enemy Health Death Contract and Krab Pursuit

**Files:**
- Modify: `Assets/Scripts/Combat/EnemyHealth.cs`
- Create: `Assets/Scripts/Enemies/KrabEnemy.cs`
- Test: `Assets/Tests/Editor/KrabEnemyTests.cs`

**Interfaces:**
- Produces: `EnemyHealth.Configure(int maximumHealth)` and `event Action<EnemyHealth> Died`.
- Produces: `KrabEnemy.Initialize(Transform target)` and `Vector2 CalculateVelocity(Vector2 current, Vector2 target, float speed)`.

- [ ] Add failing tests for configured 5 health, one-shot death notification, target direction and 3-unit speed, absent target, and player contact requesting exactly 2 damage with one-second protection.
- [ ] Run focused tests red.
- [ ] Implement configurable enemy health and Rigidbody2D pursuit/contact behavior.
- [ ] Run focused tests green.

### Task 3: Encounter Gate

**Files:**
- Create: `Assets/Scripts/Enemies/Stage1EncounterGate.cs`
- Test: `Assets/Tests/Editor/Stage1EncounterGateTests.cs`
- Test: `Assets/Tests/PlayMode/KrabEncounterPhysicsPlayModeTests.cs`

**Interfaces:**
- Produces: `Initialize(IReadOnlyList<EnemyHealth> enemies, Collider2D barrier, Renderer[] visuals)`.
- Produces: `int RemainingEnemies` and `bool IsOpen`.

- [ ] Add failing tests that four deaths keep the barrier enabled, the fifth disables collider and visuals, duplicate death does not underflow, and real collision damage respects one-second invulnerability.
- [ ] Run focused Edit Mode and Play Mode tests red.
- [ ] Implement event subscriptions, missing-enemy handling, and one-time gate opening.
- [ ] Run focused tests green.

### Task 4: Stage 1 Construction

**Files:**
- Copy: `enemy_krab.png` to `Assets/Enemies/enemy_krab.png`
- Create: `Assets/Scripts/Stage1KrabEncounterSetup.cs`
- Modify: `Assets/Editor/Stage1SceneBuilder.cs`
- Modify: `Assets/Scripts/Stage1RuntimeBootstrap.cs`
- Test: `Assets/Tests/Editor/Stage1KrabEncounterIntegrationTests.cs`

**Interfaces:**
- Produces: `Stage1KrabEncounterSetup.Create(Transform parent, Transform player, Sprite krabSprite)`.

- [ ] Add failing integration tests for exact asset import, five lower-area krabs, 5 health, speed 3, player target, matching Rigidbody2D settings, gate position/size, and both construction paths.
- [ ] Run integration tests red.
- [ ] Implement shared encounter construction and both Sprite loading paths.
- [ ] Rebuild `Assets/Scenes/Stage1.unity`.
- [ ] Run complete Edit Mode and Play Mode suites and require zero failures and clean logs.
