### Task 3: Reference-Matching Hotbar View

Create:
- `Assets/Scripts/Items/ItemHotbarView.cs`
- `Assets/Scripts/Items/ItemHotbarUIFactory.cs`
- `Assets/Tests/Editor/ItemHotbarViewTests.cs`

Consumes:
- `PlayerInventory.Slots`
- `PlayerInventory.SelectedSlotIndex`
- `PlayerInventory.StateChanged`

Required interface:
- `static ItemHotbarView ItemHotbarUIFactory.Create(Transform parent, PlayerInventory inventory)`
- Stable hierarchy names: `Item Hotbar`, `Slot 1` through `Slot 6`, `Number`, `Icon`, `Selection Outline`.

Visual source of truth is `C:\Users\myong\NaManMoo\ItemUI.png`.

Required visual behavior:
- One bottom-centered horizontal strip with six equal nearly-square slots and zero gaps.
- Six contiguous transparent interiors.
- Thin black outer/slot borders and vertical dividers.
- Labels `1` through `6` centered above corresponding slots.
- No gradients, shadows, rounded corners, rarity colors, decorative panel, or extra text.
- Item icons centered and preserving aspect ratio; empty slots show no icon.
- Exactly one selected slot at all times.
- Selected slot shows a blue rectangular inner outline inset from the black border.
- Initial selected outline is slot 1 (index 0), even when empty.
- Approximate full-strip width-to-height ratio is 6:1.
- Centralize constants for slot size, line thickness, number offset, blue color, and inset.

View behavior:
- `ItemHotbarView.Initialize(PlayerInventory inventory, ...)` subscribes to state changes.
- Refresh icons and exactly one selected outline on initialization and every inventory state change.
- Cleanly unsubscribe when destroyed and avoid duplicate subscription if initialized again.
- Missing icons remain visually empty without removing the item from the model.

Tests:
- Factory creates six stable named slots of equal size and zero horizontal gaps.
- Labels are exactly `1` through `6`.
- Interiors are transparent.
- Only slot 1 blue outline starts enabled.
- Selecting slot index 3 moves the enabled blue outline to slot 4.
- Acquiring an item with a generated sprite displays it in slot 1 with `preserveAspect = true`.
- Empty/missing icon has disabled icon image.

Use uGUI components already in the project. Follow TDD. Do not launch a second Unity process while the project is locked; document execution limitations. Do not modify scene builders yet.
