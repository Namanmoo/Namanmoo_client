# Dungeon Slime Boss Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the Dungeon robot boss with a configurable slime boss that chases, spits, burrows invulnerably, follows with a marker, and reappears with four arcing projectiles.

**Architecture:** A `SlimeBossDefinition` asset owns all user-adjustable values. Dedicated factory, controller, projectile, and marker components implement the behavior while reusing `EnemyHealth`, the existing boss health bar, and Dungeon room-clear flow.

**Tech Stack:** Unity, C#, NUnit, Unity Test Framework

## Global Constraints

- Use the three supplied `Assets/Boss/boss_slime*.png` sprites.
- Health 100, contact damage 4, projectile damage 3.
- Chase speed 3; random pattern interval 2 seconds.
- Burrow windup 0.75 seconds; hidden marker tracking 2 seconds at speed 2.5.
- Arc shots travel 6 units over 1 second with visual height 1.5.
- All slime values remain Inspector-editable and existing definitions are never overwritten.
- Existing player, normal-enemy, and robot-boss stats remain unchanged.
- Do not create a Git commit.

---

### Task 1: Definition and Health Invulnerability

**Files:** create `SlimeBossDefinition.cs`; modify `EnemyHealth.cs`; create focused tests.

- [ ] Write failing tests for all normalized definition defaults and `EnemyHealth.SetInvulnerable`.
- [ ] Verify RED, then add serialized definition fields and an invulnerability gate cleared by `Configure`.
- [ ] Run definition and health tests to GREEN.

### Task 2: Projectile and Marker

**Files:** create `SlimeBossProjectile.cs`, `SlimeFallMaker.cs`, and focused tests.

- [ ] Write failing tests for straight movement/lifetime/damage and cardinal arc distance, duration, visual height, and no marker physics.
- [ ] Verify RED, implement straight/arc projectile modes and marker movement.
- [ ] Run focused tests to GREEN.

### Task 3: Factory and Controller State Machine

**Files:** create `SlimeBossFactory.cs`, `SlimeBossController.cs`, and focused tests.

- [ ] Write failing tests for trigger-only boss contact, health 100, scaling, chase movement, random choice boundary, contact damage, windup, hidden invulnerability, marker tracking, reappearance, and four arc shots.
- [ ] Verify RED, implement the minimal factory and explicit combat states.
- [ ] Run controller/factory tests to GREEN.

### Task 4: Assets and Dungeon Replacement

**Files:** create slime definition asset and metas; add editor builder; modify `DungeonEncounter`, `DungeonSceneBuilder`, Dungeon scene, and integration tests.

- [ ] Write failing tests that require the slime definition and Dungeon boss creation path.
- [ ] Verify RED, configure sprite importers, create the definition only if missing, and replace the Dungeon robot reference.
- [ ] Run asset, scene, encounter, and boss integration tests to GREEN.

### Task 5: Final Verification

- [ ] Run all slime-focused tests plus existing boss, Dungeon encounter, Dungeon scene, health, and projectile tests.
- [ ] Run Runtime build.
- [ ] Run `git diff --check` and inspect protected enemy/player/robot stats.
- [ ] Request read-only code review and fix all Critical/Important issues.

