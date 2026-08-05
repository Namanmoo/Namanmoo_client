# Per-Enemy Visual and Collision Size Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make each regular enemy asset expose independently editable visual height and circular body collision radius values.

**Architecture:** Store both values in `EnemyDefinition`, retain the existing gameplay-stat `Configure` signature, and add a focused presentation configuration method. `EnemyFactory` consumes the definition values instead of shared constants, while existing enemy assets receive one-time serialized defaults.

**Tech Stack:** Unity, C#, NUnit, Unity Test Framework

## Global Constraints

- Collider shape remains `CircleCollider2D`.
- Krab and Squirrel start at visual height `2` and body radius `0.7`.
- Wood Tower starts at visual height `3` and body radius `1.1`.
- Existing health, damage, speed, timing, projectile speed, and projectile collision values must not change.
- The asset builder must preserve all values of existing enemy definitions.
- Do not create a Git commit.

---

### Task 1: Enemy Definition Presentation Settings

**Files:**
- Modify: `Assets/Scripts/Enemies/EnemyDefinition.cs`
- Modify: `Assets/Tests/Editor/EnemyDefinitionTests.cs`

**Interfaces:**
- Produces: `float VisualHeight`
- Produces: `float BodyCollisionRadius`
- Produces: `void ConfigurePresentation(float newVisualHeight, float newBodyCollisionRadius)`

- [ ] Add failing tests that assert defaults `2 / 0.7`, configured values, and minimum clamping to `0.01`.
- [ ] Run `EnemyDefinitionTests` and verify compilation fails because the API is absent.
- [ ] Add serialized fields with defaults, properties, `ConfigurePresentation`, and normalization.
- [ ] Run `EnemyDefinitionTests` and verify all tests pass.

### Task 2: Factory Uses Per-Enemy Values

**Files:**
- Modify: `Assets/Scripts/Enemies/EnemyFactory.cs`
- Modify: `Assets/Tests/Editor/EnemyFactoryTests.cs`

**Interfaces:**
- Consumes: `EnemyDefinition.VisualHeight`
- Consumes: `EnemyDefinition.BodyCollisionRadius`

- [ ] Add a failing test that creates definitions configured as `2 / 0.7` and `3 / 1.1`, then asserts their rendered world heights and root collider radii.
- [ ] Run `EnemyFactoryTests` and verify the second enemy still uses shared `2 / 0.7`.
- [ ] Remove shared size constants from factory behavior and consume definition properties.
- [ ] Run `EnemyDefinitionTests;EnemyFactoryTests` and verify all tests pass.

### Task 3: One-Time Asset Values and Builder Preservation

**Files:**
- Modify: `Assets/Enemies/DungeonKrab.asset`
- Modify: `Assets/Enemies/DungeonSquirrel.asset`
- Modify: `Assets/Enemies/DungeonWoodTower.asset`
- Modify: `Assets/Editor/DungeonEnemyAssetBuilder.cs`
- Modify: `Assets/Tests/Editor/DungeonEnemyAssetBuilderTests.cs`

**Interfaces:**
- Existing assets serialize `visualHeight` and `bodyCollisionRadius`.
- Builder creates a missing Wood Tower with approved defaults but never reconfigures an existing definition.

- [ ] Extend the asset test to assert `2 / 0.7`, `2 / 0.7`, and `3 / 1.1`.
- [ ] Add a preservation test that changes an existing Wood Tower presentation value in memory, runs the builder, and confirms it is unchanged before restoring the test value.
- [ ] Run `DungeonEnemyAssetBuilderTests` and verify the presentation API/value assertions fail.
- [ ] Apply the three one-time serialized asset values and make builder configuration conditional on newly creating Wood Tower.
- [ ] Run `DungeonEnemyAssetBuilderTests` and verify all tests pass without changing Krab/Squirrel gameplay values.

### Task 4: Final Verification

**Files:**
- Verify all files above plus prior Wood Tower integration.

**Interfaces:**
- No new interfaces.

- [ ] Run focused Unity EditMode tests: `EnemyDefinitionTests;EnemyFactoryTests;DungeonEnemyAssetBuilderTests;StationaryFourWayShooterControllerTests;DungeonSceneBuilderTests`.
- [ ] Run `dotnet restore NaManMoo.Runtime.csproj` and `dotnet build NaManMoo.Runtime.csproj --no-restore`.
- [ ] Run `git diff --check` and inspect diffs for all three enemy assets to confirm only the two new presentation fields changed on Krab/Squirrel.
- [ ] Confirm no commit was created.

