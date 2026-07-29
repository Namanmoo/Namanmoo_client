# Slot-One Sword and Compact Hotbar Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Auto-equip the sword in hotbar slot 1, permit firing only while that sword is selected, and fix the hotbar to exactly `432 × 144.3318`.

**Architecture:** The hotbar controller remains the owner of one `PlayerInventory`; startup setup acquires the sword into that inventory and injects the same instance into `PlayerSwordShooter`. The shooter gates its existing input/cooldown path on the equipped sword, while the view keeps normalized slot anchors inside a smaller fixed RectTransform.

**Tech Stack:** Unity 6000.5.5f1, C#, Unity Input System, Unity UI, existing `PlayerInventory`, NUnit EditMode/PlayMode tests.

## Global Constraints

- Starting sword item: ID `sword`, display name `Sword`, kind `Weapon`, icon `Assets/Weapons/sword.png`.
- Sword must occupy and equip slot index 0 exactly once.
- Firing requires selected slot index 0 and equipped item ID `sword`.
- Selecting slots 2–6 blocks firing immediately; returning to slot 1 fires immediately if an arrow is held.
- Hotbar size is exactly `432 × 144.3318`.
- Preserve projectile damage/rate/speed/spin/lifetime, player movement, map, physics, and UI background artwork.

---

### Task 1: Gate Sword Firing with the Shared Inventory

**Files:**
- Modify: `Assets/Scripts/Combat/PlayerSwordShooter.cs`
- Modify: `Assets/Tests/Editor/PlayerSwordShooterTests.cs`

**Interfaces:**
- Consumes: `PlayerInventory.SelectedSlotIndex` and `EquippedItem`.
- Produces: `PlayerSwordShooter.InitializeInventory(PlayerInventory inventory)`.

- [ ] **Step 1: Write failing gating tests**

Create an inventory, acquire a sword item, inject it into the shooter, and hold
right arrow. Assert slot 1 fires. Select slot 2 and assert no new projectile.
Select slot 1 again while right remains held and assert one projectile is
created immediately, regardless of the previous cooldown.

Add negative cases for slot 1 empty and slot 1 containing a non-sword item.

- [ ] **Step 2: Run `PlayerSwordShooterTests` and verify RED**

Expected: missing inventory initialization/gating API or firing while another
slot is selected.

- [ ] **Step 3: Implement minimal inventory gating**

Store the injected inventory. Before direction/cooldown handling, require
selected index 0 and equipped item ID `sword`. When blocked, clear
`firingDirectionActive` so a later valid selection fires immediately.

- [ ] **Step 4: Run focused tests and verify GREEN**

Expected: all shooter tests pass.

### Task 2: Resize and Refit the Item Hotbar

**Files:**
- Modify: `Assets/Scripts/Items/ItemHotbarView.cs`
- Modify: `Assets/Scripts/Items/ItemHotbarUIFactory.cs`
- Modify: `Assets/Tests/Editor/ItemHotbarViewTests.cs`

**Interfaces:**
- Produces: exact `BackgroundWidth = 432f`,
  `BackgroundHeight = 144.3318f`, and contained aspect-preserving icons.
- Consumes: existing normalized `SlotOverlayRects`.

- [ ] **Step 1: Write failing compact-layout tests**

Assert exact `RectTransform.sizeDelta == new Vector2(432f, 144.3318f)`.
Verify background aspect within `0.0001`, all six slots/outlines/icons remain
inside the hotbar, and the slot 1 icon has symmetric positive inset,
`preserveAspect == true`, and a sword Sprite without clipping.

- [ ] **Step 2: Run `ItemHotbarViewTests` and verify RED**

Expected: current width `1728` and height fail.

- [ ] **Step 3: Implement compact constants and proportional insets**

Set the exact size constants. Keep normalized slot anchors. Use compact
selection/icon insets and a crisp border thickness that remain inside each
approximately `72 × 52` safe slot.

- [ ] **Step 4: Run focused hotbar tests and verify GREEN**

Expected: all view/factory tests pass.

### Task 3: Auto-Acquire the Sword and Wire One Inventory

**Files:**
- Modify: `Assets/Scripts/Stage1ItemHotbarSetup.cs`
- Modify: `Assets/Scripts/Items/ItemHotbarController.cs`
- Modify: `Assets/Editor/Stage1SceneBuilder.cs`
- Modify: `Assets/Scripts/Stage1RuntimeBootstrap.cs`
- Modify: `Assets/Tests/Editor/Stage1SceneBuilderTests.cs`
- Modify: `Assets/Tests/PlayMode/ItemHotbarPlayModeTests.cs`
- Modify: `Assets/Scenes/Stage1.unity`

**Interfaces:**
- Consumes: background Sprite and sword Sprite.
- Produces: one `PlayerInventory` shared by controller, view, and shooter, with
  exactly one starting sword in slot 0.

- [ ] **Step 1: Write failing integration tests**

Assert the saved scene player controller inventory has exactly one sword at
slot 0, selected/equipped; the shooter uses the same inventory; slot 1 icon is
the exact sword Sprite; and hotbar size is exact. Add runtime-created player
coverage for the same state.

- [ ] **Step 2: Run `Stage1SceneBuilderTests` and PlayMode coverage for RED**

Expected: empty starting inventory and shooter without inventory.

- [ ] **Step 3: Implement startup sword acquisition**

Add a serialized starting-sword Sprite and configuration API to
`ItemHotbarController`. Its inventory initialization acquires the sword only
when one is not already present, so the saved scene reconstructs the starting
inventory after reopening. Extend `Stage1ItemHotbarSetup.Create` to accept the
sword Sprite, configure the controller before view creation, and inject that
exact inventory into the shooter. Apply the same call shape in builder and
runtime bootstrap.

- [ ] **Step 4: Run focused integration tests and verify GREEN**

Expected: scene and runtime setup tests pass.

- [ ] **Step 5: Rebuild Stage1 and inspect YAML**

Run `Stage1SceneBuilder.Build`. Verify the compact size, shooter Sprite/defaults,
and required serialized references remain valid. Runtime inventory is
non-serialized and is verified through tests.

### Task 4: Full Regression and Final Review

**Files:**
- Verify all Editor and PlayMode tests.

- [ ] **Step 1: Run combat, inventory, and hotbar focused suites**

Expected: zero failures.

- [ ] **Step 2: Run full EditMode**

Expected: zero failures.

- [ ] **Step 3: Run full PlayMode**

Expected: zero failures, including real Physics2D sword damage.

- [ ] **Step 4: Scan logs and inspect scene**

Confirm no compiler errors, null/missing reference exceptions, assertion
failures, duplicate swords, or stale `1728` hotbar size.
