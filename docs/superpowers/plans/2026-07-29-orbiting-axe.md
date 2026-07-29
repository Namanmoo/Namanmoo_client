# Orbiting Axe Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a slot-2 axe that performs a player-centered 360-degree melee swing for 10 damage at most once per second while an arrow key is pressed or held.

**Architecture:** `PlayerAxeAttacker` validates the shared inventory and input cooldown, then creates an `AxeSwing`. `AxeSwing` renders and rotates the bottom-pivoted Sprite, owns a blade trigger, and deduplicates damaged enemies for one revolution.

**Tech Stack:** Unity 6, C#, Input System, Physics 2D, NUnit Unity Test Framework

## Global Constraints

- Preserve the existing slot-1 sword and projectile behavior.
- Put the exact `weapon_axe.png` Sprite in slot 2.
- Damage is 10 and attack interval is one second.
- Each swing rotates clockwise exactly 360 degrees over 0.45 seconds.
- Each enemy is damaged at most once per swing.
- The axe handle bottom is the player-centered pivot.

---

### Task 1: Axe Inventory Configuration

**Files:**
- Copy: `weapon_axe.png` to `Assets/Weapons/weapon_axe.png`
- Modify: `Assets/Scripts/Items/PlayerInventory.cs`
- Modify: `Assets/Scripts/Items/ItemHotbarController.cs`
- Modify: `Assets/Scripts/Stage1ItemHotbarSetup.cs`
- Test: `Assets/Tests/Editor/ItemHotbarControllerTests.cs`

**Interfaces:**
- Produces: `EnsureUniqueItemInSlot(int slotIndex, ItemData requiredItem)`.
- Produces: `ConfigureStartingWeapons(Sprite swordSprite, Sprite axeSprite)`.

- [ ] **Step 1: Add failing tests**

Assert that configuration produces the exact sword in slot index 0, exact axe in slot index 1, no duplicates, and the axe icon appears in `Slot 2/Icon`.

- [ ] **Step 2: Run focused Edit Mode tests red**

Run `ItemHotbarControllerTests` and `ItemHotbarViewTests`; expect failures for the missing slot-specific inventory and axe configuration behavior.

- [ ] **Step 3: Implement minimal inventory support**

Generalize unique-item normalization to a requested slot, preserve other valid items, configure both starting Sprites, and keep the default selection on slot 1.

- [ ] **Step 4: Run focused Edit Mode tests green**

Expect all focused inventory and hotbar view tests to pass.

### Task 2: Axe Swing Damage and Rotation

**Files:**
- Create: `Assets/Scripts/Combat/AxeSwing.cs`
- Create: `Assets/Tests/Editor/AxeSwingTests.cs`
- Create: `Assets/Tests/PlayMode/AxeSwingPhysicsPlayModeTests.cs`

**Interfaces:**
- Produces: `Initialize(GameObject owner, int damage, float duration)`.
- Produces: `Advance(float deltaTime)` and `TryHit(Collider2D other)`.

- [ ] **Step 1: Add failing swing tests**

Assert 0.225 seconds advances a 0.45-second swing by -180 degrees, 0.45 seconds completes -360 degrees, owner/non-enemy colliders are ignored, and one enemy receives 10 damage only once.

- [ ] **Step 2: Run focused tests red**

Expect compilation failure because `AxeSwing` does not exist.

- [ ] **Step 3: Implement minimal swing**

Track elapsed time, rotate clockwise from the initial direction, destroy after one revolution, keep a `HashSet<EnemyHealth>` of damaged targets, and route valid hits through `EnemyHealth.TakeDamage(10)`.

- [ ] **Step 4: Run Edit Mode and physics Play Mode tests green**

Expect all swing behavior and real trigger collision tests to pass.

### Task 3: Axe Attacker Input and Cooldown

**Files:**
- Create: `Assets/Scripts/Combat/PlayerAxeAttacker.cs`
- Create: `Assets/Tests/Editor/PlayerAxeAttackerTests.cs`

**Interfaces:**
- Consumes: `PlayerInventory`.
- Produces: `InitializeInventory(PlayerInventory inventory)`.
- Produces: `ProcessInput(Keyboard keyboard, float currentTime)`.
- Produces: `CalculateDirection(Keyboard keyboard)`.

- [ ] **Step 1: Add failing attacker tests**

Assert each arrow direction is recognized, slot 2 axe gating works, a held key attacks at times 0 and 1 but not 0.99, release/repress still respects cooldown, and created hierarchy uses the axe Sprite with its bottom pivot at the player origin.

- [ ] **Step 2: Run focused Edit Mode tests red**

Expect compilation failure because `PlayerAxeAttacker` does not exist.

- [ ] **Step 3: Implement minimal attacker**

Validate the equipped item ID `axe`, calculate arrow direction, create a temporary pivot and axe visual, place an approximate blade trigger over the head, and schedule the next start time as `currentTime + 1f`.

- [ ] **Step 4: Run focused Edit Mode tests green**

Expect all attacker tests to pass without changing sword tests.

### Task 4: Stage 1 Integration

**Files:**
- Modify: `Assets/Editor/Stage1SceneBuilder.cs`
- Modify: `Assets/Scripts/Stage1RuntimeBootstrap.cs`
- Modify: `Assets/Scripts/Stage1ItemHotbarSetup.cs`
- Modify: `Assets/Tests/Editor/Stage1SceneBuilderTests.cs`

**Interfaces:**
- Both construction paths consume `Assets/Weapons/weapon_axe.png`.

- [ ] **Step 1: Add failing integration tests**

Assert Single Sprite bottom-center pivot import settings, player `PlayerAxeAttacker`, slot 2 `axe`, exact icon Sprite, attack damage 10, interval one second, and runtime bootstrap Sprite assignment.

- [ ] **Step 2: Run integration tests red**

Expect failures because neither Stage 1 construction path loads or wires the axe.

- [ ] **Step 3: Implement both construction paths**

Normalize the axe importer, load the Sprite, create/configure `PlayerAxeAttacker`, and pass the exact Sprite through the shared hotbar setup.

- [ ] **Step 4: Rebuild Stage 1**

Invoke `Stage1SceneBuilder.Build` and require exit code 0.

- [ ] **Step 5: Run full regression suites**

Run all Edit Mode and Play Mode tests and require zero failures and no compiler errors or unhandled exceptions.
