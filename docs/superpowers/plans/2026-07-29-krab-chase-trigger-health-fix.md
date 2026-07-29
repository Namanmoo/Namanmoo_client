# Krab Chase, Trigger Contact, and Health UI Fix Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make saved Stage1 krabs chase at half player speed, overlap the player without pushing, and apply damage that visibly reduces the HP number and gauge.

**Architecture:** Persist the chase target in `KrabEnemy`, retain a solid boundary collider, and add a child trigger sensor for player contact. Ignore collision response only between each krab body collider and the player collider.

**Tech Stack:** Unity 6.0, C#, Rigidbody2D, Collider2D, Unity UI, NUnit/Unity Test Framework

## Global Constraints

- Player speed remains 5 units per second; krab speed is 2.5.
- Contact damage remains 2 with one second of global player invulnerability.
- Krabs continue colliding with the Stage1 map boundary.
- Five-enemy gate behavior remains unchanged.

---

### Task 1: Persist Chase Target and Set Half Speed

**Files:**
- Modify: `Assets/Scripts/Enemies/KrabEnemy.cs`
- Modify: `Assets/Tests/Editor/KrabEnemyTests.cs`

**Interfaces:**
- Consumes: `KrabEnemy.Initialize(Transform newTarget)`
- Produces: serialized target and `MoveSpeed`/`Target` read-only diagnostics

- [ ] Add tests asserting initialized target persistence and a 2.5-unit chase velocity.
- [ ] Run focused tests and confirm failure at the old 3-unit speed/missing diagnostics.
- [ ] Serialize `target`, set `moveSpeed = 2.5f`, and expose read-only properties.
- [ ] Run focused tests and confirm they pass.

### Task 2: Overlap Without Losing Boundary Collision

**Files:**
- Modify: `Assets/Scripts/Enemies/KrabEnemy.cs`
- Modify: `Assets/Scripts/Stage1KrabEncounterSetup.cs`
- Modify: `Assets/Tests/Editor/KrabEnemyTests.cs`
- Modify: `Assets/Tests/Editor/Stage1KrabEncounterIntegrationTests.cs`

**Interfaces:**
- Consumes: player and krab colliders created by Stage1 setup
- Produces: solid `CircleCollider2D` body plus child `Krab Contact Sensor` trigger

- [ ] Add failing tests for trigger damage, solid body collider, child trigger sensor, and ignored player/body collider pair.
- [ ] Run focused tests and confirm failure against collision callbacks/current setup.
- [ ] Add trigger enter/stay callbacks, create the child sensor, and call `Physics2D.IgnoreCollision` for the solid player/body pair.
- [ ] Run focused tests and confirm they pass.

### Task 3: Verify HP UI Through Real Contact Damage

**Files:**
- Modify: `Assets/Tests/Editor/KrabEnemyTests.cs`

**Interfaces:**
- Consumes: `PlayerHealthBarUIFactory.Create` and krab trigger damage path
- Produces: regression test proving `20/20` and `1.0` become `18/20` and `0.9`

- [ ] Add a PlayMode overlap/contact regression test with a real `PlayerHealthBarView`.
- [ ] Run it before production changes and confirm it fails because contact is not trigger-driven.
- [ ] Use the existing `PlayerHealth.HealthChanged` path without duplicating UI state.
- [ ] Run focused tests and confirm text and fill update together.

### Task 4: Rebuild and Regress

**Files:**
- Modify generated scene: `Assets/Scenes/Stage1.unity`

**Interfaces:**
- Consumes: `Stage1SceneBuilder.Build`
- Produces: five serialized, chasing Stage1 krabs

- [ ] Rebuild Stage1 in Unity batch mode.
- [ ] Run the focused integration tests.
- [ ] Run all EditMode tests.
- [ ] Run all PlayMode tests.
- [ ] Inspect results for zero failures and confirm the scene contains five krabs and one gate.
