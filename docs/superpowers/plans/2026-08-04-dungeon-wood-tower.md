# Dungeon Wood Tower Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a stationary Dungeon enemy that waits 1.5 seconds and then fires the supplied bullet sprite in four cardinal directions every 1.5 seconds.

**Architecture:** Add a dedicated behavior enum value and `StationaryFourWayShooterController`, selected by the shared `EnemyFactory`. Reuse `EnemyProjectile`, adding only an initial visual-angle parameter, and create a third Dungeon enemy definition through the existing asset builder.

**Tech Stack:** Unity, C#, NUnit, Unity Test Framework

## Global Constraints

- Use `Assets/Enemies/enemy_woodtower.png` for the body and `Assets/Enemies/enemy_woodtower_bullet.png` for projectiles.
- Stats are health `10`, move speed `0`, damage `2`, interval `1.5`, projectile speed `8`, lifetime `5`, and radius `0.5`.
- Right/down/left/up movement uses visual Z angles `0/90/180/270`.
- The first volley occurs after one full 1.5-second interval.
- Existing Squirrel and Krab stats and behavior must not change.
- Do not create a Git commit.

---

### Task 1: Projectile Initial Visual Angle

**Files:**
- Modify: `Assets/Scripts/Enemies/EnemyProjectile.cs`
- Modify: `Assets/Tests/Editor/EnemyProjectileTests.cs`

**Interfaces:**
- Produces: `Initialize(..., float collisionRadius, float rotationSpeed = 0f, float initialRotation = 0f)`
- Behavior: initialization sets `transform.localRotation` to the requested Z angle.

- [ ] **Step 1: Write a failing test**

Add `Initialize_AppliesConfiguredInitialVisualRotation` that initializes a real projectile with `initialRotation: 90f` and asserts `Mathf.DeltaAngle(transform.eulerAngles.z, 90f)` is zero.

- [ ] **Step 2: Verify RED**

Run the Unity EditMode `EnemyProjectileTests`.

Expected: compilation fails because the initial-rotation argument does not exist.

- [ ] **Step 3: Implement the minimal API**

Extend the sprite overload with an optional `initialRotation` parameter and set:

```csharp
transform.localRotation = Quaternion.Euler(0f, 0f, initialRotation);
```

Keep the existing optional spin speed and all current callers source-compatible.

- [ ] **Step 4: Verify GREEN**

Run `EnemyProjectileTests`.

Expected: all tests pass.

### Task 2: Stationary Four-Way Controller and Factory Wiring

**Files:**
- Create: `Assets/Scripts/Enemies/StationaryFourWayShooterController.cs`
- Create: `Assets/Scripts/Enemies/StationaryFourWayShooterController.cs.meta`
- Create: `Assets/Tests/Editor/StationaryFourWayShooterControllerTests.cs`
- Create: `Assets/Tests/Editor/StationaryFourWayShooterControllerTests.cs.meta`
- Modify: `Assets/Scripts/Enemies/EnemyBehaviorType.cs`
- Modify: `Assets/Scripts/Enemies/EnemyFactory.cs`
- Modify: `Assets/Tests/Editor/EnemyFactoryTests.cs`

**Interfaces:**
- Produces: `EnemyBehaviorType.StationaryFourWayShoot`
- Produces: `Initialize(EnemyDefinition definition, Transform target, float spawnTime)`
- Produces: `bool TryAttack(float currentTime)`
- Consumes: `EnemyProjectile.Initialize(..., initialRotation)`

- [ ] **Step 1: Write failing controller tests**

Use a real configured definition and verify:

```csharp
Assert.That(controller.TryAttack(1.49f), Is.False);
Assert.That(controller.TryAttack(1.5f), Is.True);
```

Assert exactly four spawned `EnemyProjectile` instances. Advance each by `0.25f` and identify their literal positions `(2,0)`, `(0,-2)`, `(-2,0)`, `(0,2)` for speed `8`. Assert the corresponding Z angles are `0`, `90`, `180`, and `270`, and assert damage `2`, speed `8`, lifetime `5`, and collider radius `0.5`.

- [ ] **Step 2: Write a failing factory test**

Configure a definition with `StationaryFourWayShoot`, call `EnemyFactory.Create`, and assert the root has `StationaryFourWayShooterController` but neither existing movement controller.

- [ ] **Step 3: Verify RED**

Run `StationaryFourWayShooterControllerTests` and `EnemyFactoryTests`.

Expected: compilation fails because the enum value and controller do not exist.

- [ ] **Step 4: Implement the enum and controller**

Add the enum value. The controller stores its definition, initializes
`nextAttackTime = spawnTime + definition.AttackInterval`, and creates four
projectiles using literal direction/angle pairs:

```csharp
(Vector2.right, 0f)
(Vector2.down, 90f)
(Vector2.left, 180f)
(Vector2.up, 270f)
```

`Update` calls `TryAttack(Time.time)`. `TryAttack` returns false before the
scheduled time, otherwise creates one volley and advances the schedule by one
definition interval.

- [ ] **Step 5: Wire and validate the factory**

Add the stationary case to `EnemyFactory.Create`. Treat both ranged behavior
types as requiring valid projectile sprite, speed, lifetime, and radius.

- [ ] **Step 6: Verify GREEN**

Run `EnemyProjectileTests`, `StationaryFourWayShooterControllerTests`, and
`EnemyFactoryTests`.

Expected: all pass.

### Task 3: Wood Tower Asset and Dungeon Pool

**Files:**
- Modify: `Assets/Editor/DungeonEnemyAssetBuilder.cs`
- Modify: `Assets/Tests/Editor/DungeonEnemyAssetBuilderTests.cs`
- Create: `Assets/Enemies/DungeonWoodTower.asset`
- Create: `Assets/Enemies/DungeonWoodTower.asset.meta`

**Interfaces:**
- Produces: `DungeonEnemyAssetBuilder.WoodTowerDefinitionPath`
- Produces: a returned definition with ID `wood_tower`

- [ ] **Step 1: Write a failing asset-builder test**

Change the expected IDs to `krab`, `squirrel`, and `wood_tower`. Assert the
wood tower body/projectile sprites use the requested files, behavior is
`StationaryFourWayShoot`, and its literal stats are `10, 0, 2, 1.5, 8, 5,
0.5`.

- [ ] **Step 2: Verify RED**

Run `DungeonEnemyAssetBuilderTests`.

Expected: the returned definitions do not contain `wood_tower`.

- [ ] **Step 3: Implement texture setup and definition creation**

Configure both supplied PNG importers as single sprites, Point filtered,
Clamp wrapped, transparency enabled, and mipmaps disabled. Create/update only
`DungeonWoodTower.asset` with the approved stats and return it after the
existing definitions.

Do not call `Configure` on the existing Krab or Squirrel assets in this task;
load and return them unchanged.

- [ ] **Step 4: Verify GREEN and Dungeon integration**

Run `DungeonEnemyAssetBuilderTests`, `DungeonEncounterTests`, and
`DungeonSceneBuilderTests`.

Expected: all pass and the generated Dungeon encounter contains all three
normal-enemy definitions.

### Task 4: Final Verification

**Files:**
- Verify all files above and protected assets.

**Interfaces:**
- No new interfaces.

- [ ] **Step 1: Run focused tests**

Run the Unity EditMode tests:

- `EnemyProjectileTests`
- `StationaryFourWayShooterControllerTests`
- `EnemyFactoryTests`
- `DungeonEnemyAssetBuilderTests`
- `DungeonEncounterTests`
- `DungeonSceneBuilderTests`

Expected: zero failures.

- [ ] **Step 2: Verify protected stats and diff hygiene**

```powershell
git diff --check
git diff -- Assets/Enemies/DungeonKrab.asset Assets/Enemies/DungeonSquirrel.asset
git status --short
```

Expected: no diff for either protected definition, no whitespace errors, and
only intended new changes in addition to the user's pre-existing worktree
changes.

