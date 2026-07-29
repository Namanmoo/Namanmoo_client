# Item Hotbar Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the six-slot `ItemUI.png` hotbar, acquisition API, and immediate `1` through `6` weapon selection.

**Architecture:** A plain C# inventory model owns slot and equipment rules. A MonoBehaviour controller translates Unity input into model selection, while a separate uGUI view renders the six contiguous slots and selected blue inset. Both the editor scene builder and runtime bootstrap use one shared UI factory so their output stays identical.

**Tech Stack:** Unity 6000.5.5f1, C#, Unity Input System 1.19.0, Unity uGUI 2.5.0, Unity Test Framework 1.7.0.

## Global Constraints

- The inventory contains exactly six non-stacking slots.
- `TryAcquire(ItemData item)` inserts into the first empty slot and never replaces an existing item.
- Top-row keys `1` through `6` select slot indices `0` through `5` immediately.
- Selecting an empty slot clears the equipped item.
- The UI reproduces `ItemUI.png`: six contiguous transparent boxes, thin black lines, labels above, and one inset blue selection rectangle.
- World pickups, combat, persistence, drag-and-drop, and item stacking are excluded.

---

### Task 1: Inventory Domain Model

**Files:**
- Create: `Assets/Scripts/Items/ItemData.cs`
- Create: `Assets/Scripts/Items/PlayerInventory.cs`
- Test: `Assets/Tests/Editor/PlayerInventoryTests.cs`

**Interfaces:**
- Produces: `ItemData(string id, string displayName, ItemKind kind, Sprite icon = null)`.
- Produces: `bool PlayerInventory.TryAcquire(ItemData item)`.
- Produces: `bool PlayerInventory.SelectSlot(int index)`.
- Produces: `IReadOnlyList<ItemData> Slots`, `int SelectedSlotIndex`, and `ItemData EquippedItem`.
- Produces: `event Action StateChanged` and `event Action<ItemData> EquippedItemChanged`.

- [ ] **Step 1: Write failing inventory tests**

Create tests that instantiate `PlayerInventory`, verify `Slots.Count == 6`, acquire seven distinct `ItemData` values, assert the first six occupy indices `0..5`, and assert the seventh and a null item return `false` without mutation. Verify the first acquisition selects index `0`, later acquisitions preserve selection, occupied-slot selection changes `EquippedItem`, empty-slot selection clears it, and indices `-1` and `6` return `false`.

- [ ] **Step 2: Run the focused Edit Mode tests and confirm RED**

Run Unity in batch mode with `-runTests -testPlatform EditMode -testFilter PlayerInventoryTests`.

Expected: compilation or test failure because `ItemData` and `PlayerInventory` do not exist.

- [ ] **Step 3: Implement the minimal model**

Create `ItemKind` with `Weapon` and `Item`. Implement immutable `ItemData` properties `Id`, `DisplayName`, `Kind`, and `Icon`; validity requires a non-empty `Id`. Implement a six-element slot array in `PlayerInventory`, first-empty insertion, zero-based selection validation, automatic first-item selection, and events emitted only after state changes.

- [ ] **Step 4: Run the focused Edit Mode tests and confirm GREEN**

Run the same filtered Unity test command.

Expected: all `PlayerInventoryTests` pass.

### Task 2: Number-Key Selection Controller

**Files:**
- Create: `Assets/Scripts/Items/ItemHotbarController.cs`
- Test: `Assets/Tests/Editor/ItemHotbarControllerTests.cs`

**Interfaces:**
- Consumes: `PlayerInventory.SelectSlot(int index)`.
- Produces: `static int SlotIndexForNumber(int number)` returning `0..5` or `-1`.
- Produces: `void Initialize(PlayerInventory inventory)` for generated scene wiring.

- [ ] **Step 1: Write failing input-mapping tests**

Assert `SlotIndexForNumber(1..6)` returns `0..5`, while `0` and `7` return `-1`. Instantiate the controller with an inventory and use Input System keyboard fixtures to press each top-row digit, verifying the matching `SelectedSlotIndex`.

- [ ] **Step 2: Run the focused tests and confirm RED**

Run Unity Edit Mode tests filtered to `ItemHotbarControllerTests`.

Expected: compilation failure because the controller does not exist.

- [ ] **Step 3: Implement key-down selection**

Add a MonoBehaviour that stores the initialized inventory, reads `Keyboard.current.digit1Key` through `digit6Key` in `Update`, and calls `SelectSlot` only on `wasPressedThisFrame`. Keep the number-to-index conversion in the public static method tested above.

- [ ] **Step 4: Run model and controller tests and confirm GREEN**

Run Unity Edit Mode tests filtered to the item test namespace.

Expected: all inventory and input tests pass.

### Task 3: Reference-Matching Hotbar View

**Files:**
- Create: `Assets/Scripts/Items/ItemHotbarView.cs`
- Create: `Assets/Scripts/Items/ItemHotbarUIFactory.cs`
- Test: `Assets/Tests/Editor/ItemHotbarViewTests.cs`

**Interfaces:**
- Consumes: `PlayerInventory.Slots`, `SelectedSlotIndex`, and `StateChanged`.
- Produces: `ItemHotbarUIFactory.Create(Transform parent, PlayerInventory inventory)`.
- Produces stable names `Item Hotbar`, `Slot 1` through `Slot 6`, `Number`, `Icon`, and `Selection Outline` for verification.

- [ ] **Step 1: Write failing UI structure and state tests**

Create a canvas, inventory, and factory-produced view. Assert there are six contiguous slot RectTransforms of equal size, labels contain `1..6`, backgrounds are transparent, and only slot `0` starts with an enabled blue inset outline. Select slot `3` and assert only its outline is enabled. Acquire an item with a generated sprite and assert slot `0` displays that sprite with preserved aspect ratio.

- [ ] **Step 2: Run focused UI tests and confirm RED**

Run Unity Edit Mode tests filtered to `ItemHotbarViewTests`.

Expected: compilation failure because the view and factory do not exist.

- [ ] **Step 3: Implement the visual factory**

Build a bottom-centered `RectTransform` with a `HorizontalLayoutGroup` using zero spacing. Create six transparent square slot roots, one black outline per slot using uGUI `Outline`, labels centered above the roots, icon images centered with `preserveAspect = true`, and blue selection images inset from all four edges. Use shared constants for slot size, line thickness, number offset, blue color, and blue inset; add no gradients, shadows, rounded corners, or extra labels.

- [ ] **Step 4: Implement event-driven refresh**

Have `ItemHotbarView.Initialize` subscribe to inventory state, update each icon's sprite/enabled state, and enable exactly one selection outline. Unsubscribe on destruction and resubscribe safely after initialization.

- [ ] **Step 5: Run all item Edit Mode tests and confirm GREEN**

Run Unity Edit Mode tests filtered to the item test namespace.

Expected: all model, controller, and view tests pass.

### Task 4: Stage 1 Integration

**Files:**
- Modify: `Assets/Editor/Stage1SceneBuilder.cs`
- Modify: `Assets/Scripts/Stage1RuntimeBootstrap.cs`
- Test: `Assets/Tests/Editor/Stage1SceneBuilderTests.cs`

**Interfaces:**
- Consumes: `PlayerInventory`, `ItemHotbarController.Initialize`, and `ItemHotbarUIFactory.Create`.
- Produces scene objects `Item Hotbar Canvas` and `Item Hotbar`.

- [ ] **Step 1: Extend builder tests and confirm RED**

Add assertions that a built Stage 1 scene contains a player inventory/controller, an overlay canvas, a bottom-centered `Item Hotbar`, six named slots, labels `1..6`, and exactly one active selection outline.

- [ ] **Step 2: Run Stage 1 builder tests**

Run Unity Edit Mode tests filtered to `Stage1SceneBuilderTests`.

Expected: new assertions fail because the scene builder does not create the hotbar.

- [ ] **Step 3: Add shared scene/runtime setup**

Update both player creation paths to construct one `PlayerInventory`, initialize `ItemHotbarController`, create a screen-space overlay canvas with `CanvasScaler`, and call `ItemHotbarUIFactory.Create`. Ensure the scene builder saves the generated hierarchy and the runtime bootstrap creates the same hierarchy when needed.

- [ ] **Step 4: Rebuild Stage 1 and run integration tests**

Run the editor scene builder in Unity batch mode, then rerun `Stage1SceneBuilderTests`.

Expected: the saved scene contains the complete hotbar and all assertions pass.

### Task 5: Full Verification

**Files:**
- Verify: `Assets/Scripts/Items/*.cs`
- Verify: `Assets/Tests/Editor/*Item*.cs`
- Verify: `Assets/Scenes/Stage1.unity`

**Interfaces:**
- Consumes all prior task outputs.
- Produces a verified, compilable feature.

- [ ] **Step 1: Run all Edit Mode tests**

Run Unity in batch mode with `-runTests -testPlatform EditMode`.

Expected: the full Edit Mode suite passes with zero failures.

- [ ] **Step 2: Run all Play Mode tests**

Run Unity in batch mode with `-runTests -testPlatform PlayMode`.

Expected: the full Play Mode suite passes with zero failures.

- [ ] **Step 3: Inspect Unity logs**

Search the generated logs for `error CS`, `NullReferenceException`, `MissingReferenceException`, and `AssertionException`.

Expected: no matching errors.

- [ ] **Step 4: Verify saved-scene content**

Inspect `Assets/Scenes/Stage1.unity` and confirm serialized objects for the player inventory/controller, canvas, six slot roots, labels, icons, and blue selection outlines.

Expected: all required objects are serialized and the selected outline begins on slot `1`.
