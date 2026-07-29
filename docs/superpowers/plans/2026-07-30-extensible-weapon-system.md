# Extensible Weapon System Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add data-driven melee and ranged weapon templates, five working sample weapon types, an empty default hotbar, and a SampleStage-only example loadout.

**Architecture:** `WeaponDefinition` ScriptableObjects hold every tunable weapon value. `PlayerWeaponController` reads the equipped definition and routes cardinal input to reusable melee geometry or configurable ranged projectiles, while SampleStage setup injects five example definitions without changing the empty default inventory.

**Tech Stack:** Unity 6, C#, ScriptableObject, Input System, Physics2D, Unity Test Framework, NUnit

## Global Constraints

- Work primarily in `Assets/Scripts/Combat`, `Assets/Scripts/Items`, and `Assets/Scripts/Enemies`.
- Scene, tests, builders, bootstrap, and reference files may change only where required for SampleStage and verification.
- Default hotbars are empty; only SampleStage receives sample weapons.
- All attacks reject diagonal input.
- Weapon reach, interval, radius, damage, arc, speed, and lifetime remain data-configurable.
- Existing enemy and boss behavior stays unchanged; hits use `EnemyHealth.TakeDamage(int)`.
- Preserve the player-relative camera configuration.

---

### Task 1: Weapon Definitions and Item Association

**Files:**
- Create: `Assets/Scripts/Items/WeaponCategory.cs`
- Create: `Assets/Scripts/Items/WeaponType.cs`
- Create: `Assets/Scripts/Items/WeaponDefinition.cs`
- Modify: `Assets/Scripts/Items/ItemData.cs`
- Create: `Assets/Tests/Editor/WeaponDefinitionTests.cs`

**Interfaces:**
- Produces: `WeaponDefinition`, `WeaponCategory`, `WeaponType`, `ItemData.Weapon`
- Consumes: Unity `Sprite`, `Color`, and ScriptableObject serialization

- [ ] Write failing tests for valid category/type pairs, configurable numeric properties, validation clamps, and `ItemData` carrying a weapon definition.
- [ ] Run `WeaponDefinitionTests` and confirm compilation/test failure because the new types do not exist.
- [ ] Implement enums, definition fields/properties, `Configure(...)` for runtime/sample creation, `OnValidate`, `IsCategoryValid`, and the optional `ItemData` weapon constructor parameter.
- [ ] Run `WeaponDefinitionTests` and existing `PlayerInventoryTests`; expect zero failures.
- [ ] Commit only Task 1 files.

### Task 2: Generic Cardinal Weapon Combat

**Files:**
- Create: `Assets/Scripts/Combat/WeaponAttackGeometry.cs`
- Create: `Assets/Scripts/Combat/WeaponProjectile.cs`
- Create: `Assets/Scripts/Combat/PlayerWeaponController.cs`
- Create: `Assets/Tests/Editor/WeaponAttackGeometryTests.cs`
- Create: `Assets/Tests/Editor/PlayerWeaponControllerTests.cs`
- Create: `Assets/Tests/Editor/WeaponProjectileTests.cs`

**Interfaces:**
- Consumes: equipped `ItemData.Weapon`, `EnemyHealth.TakeDamage(int)`
- Produces: `PlayerWeaponController.ProcessInput(Keyboard, float)`, cardinal direction calculation, melee target filtering, configurable ranged projectiles

- [ ] Write failing geometry tests: diagonal input returns zero; spear accepts only a narrow forward line; sword accepts a 90-degree sector; axe accepts every target inside radius.
- [ ] Write failing controller tests for immediate attack, configured cooldown, no release/reselect bypass, equipped-definition routing, and all five types.
- [ ] Write failing projectile tests for radius, speed, lifetime, owner rejection, one-hit consumption, and damage.
- [ ] Run the three focused fixtures and verify RED.
- [ ] Implement pure geometry functions using distance, dot product, and perpendicular distance.
- [ ] Implement a single controller with one cooldown timestamp and definition-driven melee/ranged spawning.
- [ ] Implement one ranged projectile whose `CircleCollider2D.radius`, motion, lifetime, damage, Sprite, and color come from `WeaponDefinition`.
- [ ] Run focused fixtures and existing enemy health tests; expect zero failures.
- [ ] Commit only Task 2 files.

### Task 3: Empty Hotbar and Sample Loadout

**Files:**
- Modify: `Assets/Scripts/Items/ItemHotbarController.cs`
- Modify: `Assets/Scripts/Items/PlayerInventory.cs`
- Create: `Assets/Scripts/Items/SampleWeaponFactory.cs`
- Modify: `Assets/Scripts/Stage1ItemHotbarSetup.cs`
- Modify: `Assets/Scripts/Stage1RuntimeBootstrap.cs`
- Modify: `Assets/Editor/Stage1SceneBuilder.cs`
- Modify: relevant inventory, hotbar, runtime bootstrap, and scene-builder tests under `Assets/Tests`

**Interfaces:**
- Consumes: `WeaponDefinition.Configure(...)`, `PlayerInventory.EnsureUniqueItemInSlot(...)`
- Produces: empty general inventory and five-item SampleStage loadout in Axe, Projectile, Spear, Sword, Gun order

- [ ] Change/add tests so a plain `ItemHotbarController` has six empty slots.
- [ ] Add tests for `SampleWeaponFactory` producing five independently configurable, valid definitions with optional placeholder Sprites.
- [ ] Add SampleStage setup tests asserting slot order, weapon types/categories, shared inventory, and `PlayerWeaponController` initialization.
- [ ] Run the affected fixtures and verify RED against automatic legacy sword/axe injection.
- [ ] Remove general starting-weapon fields and automatic acquisition from `ItemHotbarController`.
- [ ] Add explicit loadout injection used only by stage setup.
- [ ] Replace `PlayerSwordShooter`/`PlayerAxeAttacker` setup with `PlayerWeaponController`; preserve legacy files only if needed for existing serialized compatibility.
- [ ] Generate simple in-memory solid-color placeholder Sprites when a definition has no art.
- [ ] Run inventory, hotbar, combat, bootstrap, and scene-builder tests; expect zero relevant failures.
- [ ] Commit only Task 3 files.

### Task 4: Rename Stage1 Scene to SampleStage

**Files:**
- Move: `Assets/Scenes/Stage1.unity` to `Assets/Scenes/SampleStage.unity`
- Move: matching `.meta` file with the scene
- Modify: `Assets/Editor/Stage1SceneBuilder.cs`
- Modify: `Assets/Scripts/UI/TitleScreenController.cs`
- Modify: `ProjectSettings/EditorBuildSettings.asset`
- Modify: tests and user-facing build messages that reference `Assets/Scenes/Stage1.unity`

**Interfaces:**
- Produces: canonical scene path `Assets/Scenes/SampleStage.unity`
- Preserves: existing scene GUID, content, player-relative camera hierarchy/offset, and internal Stage1 class names where unrelated

- [ ] Add/update tests expecting `SampleStage.unity`, build settings inclusion, title loading path, sample loadout, and camera parent/local-Y configuration.
- [ ] Run focused scene/title tests and verify RED while references still point to Stage1.
- [ ] Move the scene and `.meta` together, then update every runtime path and test fixture path.
- [ ] Update the builder output path without rebuilding away the user's camera configuration; ensure future builder output reproduces that camera setup.
- [ ] Run scene/title tests, full relevant Edit Mode tests, and Play Mode hotbar/title tests.
- [ ] Build SampleStage in batch mode, inspect the log, and restore only unrelated Unity-generated IDE files.
- [ ] Commit Task 4 files and the implementation plan.
