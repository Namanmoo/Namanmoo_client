# Item Hotbar Design

## Goal

Implement a six-slot item hotbar based on `ItemUI.png`. Other gameplay systems can add acquired weapons or items through an API. Pressing number keys `1` through `6` immediately selects the matching slot and equips its weapon.

## Scope

Included:

- A six-slot inventory with deterministic first-empty-slot insertion.
- An acquisition API for other gameplay systems.
- Keyboard selection with the top-row number keys `1` through `6`.
- Immediate equipped-item updates when the selected slot changes.
- A bottom-centered hotbar matching the reference layout.
- Automated tests for inventory behavior, selection, equipment, and UI state.

Excluded:

- World pickups and collision-based acquisition.
- Weapon attacks, firing, damage, ammunition, and animations.
- Drag-and-drop slot reordering.
- Persistence between play sessions.
- Item stacking.

## Data Model

`ItemData` is a small immutable runtime value containing a stable identifier, display name, item kind, and optional icon. The initial item kind supports weapons while leaving room for non-weapon items to occupy slots later.

`PlayerInventory` owns exactly six slots and exposes read-only slot state. It provides:

- `TryAcquire(ItemData item)`: places a valid item in the first empty slot and returns `true`; returns `false` without changing state when the item is invalid or all slots are full.
- `SelectSlot(int index)`: selects a zero-based slot from `0` through `5`.
- `SelectedSlotIndex`: the currently selected slot.
- `EquippedItem`: the item in the selected slot, or no item when the selected slot is empty.
- State-change notifications so the UI and future combat code can react without polling.

The first successfully acquired item is automatically selected and equipped when no item is currently equipped. Later acquisitions do not interrupt the player's current selection.

Selecting an empty slot is valid and clears `EquippedItem`. Selecting an out-of-range index is rejected without changing state.

## Input and Equipment Flow

`ItemHotbarController` reads the top-row number keys using Unity's Input System:

- `1` selects slot index `0`.
- `2` selects slot index `1`.
- `3` selects slot index `2`.
- `4` selects slot index `3`.
- `5` selects slot index `4`.
- `6` selects slot index `5`.

Only a key-down transition changes selection, so holding a key does not repeatedly emit equipment changes. Selection updates the inventory first; the inventory then publishes the new selected slot and equipped item. Future weapon-use code can subscribe to the equipment-change notification.

## UI

The hotbar uses Unity uGUI on a screen-space overlay canvas and reproduces `ItemUI.png` as the visual source of truth. It is anchored to the bottom center of the screen.

- Six equal, nearly square slots form one uninterrupted horizontal strip with no gaps.
- One thin black outer rectangle encloses the full strip, and five thin black vertical dividers separate the slots.
- Slot interiors are transparent so the game remains visible behind the UI; no decorative panels, gradients, shadows, rounded corners, rarity colors, or extra text are added.
- Handwritten-style labels `1` through `6` are centered above their corresponding slots, following the spacing and placement in the reference.
- The item icon is centered inside an occupied slot and preserves its aspect ratio.
- Empty slots show no icon.
- The selected slot receives a blue rectangular inner outline inset from the black slot boundary, matching the first-slot example in the reference.
- Exactly one slot is selected at all times, including when that slot is empty.

The complete strip uses the reference image's approximate `6:1` width-to-height proportion. Slot width, slot height, divider placement, number offsets, line thickness, and blue-outline inset are centralized as layout constants so the generated scene and runtime bootstrap remain visually identical. The layout scales uniformly with the canvas reference resolution while retaining its proportions and bottom-center alignment.

`ItemHotbarView` is responsible only for rendering the inventory state. It subscribes to state changes and refreshes icons and selection outlines. Inventory rules do not depend on UI objects, allowing behavior tests to run without a scene.

## Scene Integration

The Stage 1 builder and runtime bootstrap create the same hotbar setup:

- A player inventory/controller associated with the player.
- A screen-space canvas with six generated slot views.
- An event system suitable for the Input System UI module.

The UI is generated from code to match the project's existing scene-builder pattern and avoid requiring manually wired prefab references. No test pickup objects are added to the stage.

## Public Acquisition API

Other systems acquire items through the player inventory:

```csharp
bool accepted = playerInventory.TryAcquire(itemData);
```

The return value is the authoritative result. Callers retain responsibility for removing a world pickup or showing a failure message after checking it. A failed call never replaces or drops an existing slot item.

## Error Handling

- A null or invalid item is rejected.
- A full hotbar rejects additional items.
- An invalid slot index is rejected.
- Missing icons are allowed and render as an empty visual while the item remains present.
- UI refreshes tolerate empty slots and disabled/re-enabled view objects.
- Input does nothing when the inventory dependency is unavailable.

## Testing

Edit Mode tests cover:

- Six-slot initialization.
- First-empty-slot acquisition order.
- Automatic selection/equipment of the first acquired item.
- Later acquisitions preserving the current selection.
- Full-inventory and invalid-item rejection without mutation.
- Slot-index validation.
- Selecting occupied slots changes `EquippedItem`.
- Selecting empty slots clears `EquippedItem`.
- Number-key-to-slot mapping.

Play Mode tests cover:

- The hotbar contains six visible slots and labels `1` through `6`.
- Only the selected slot has the blue outline.
- Selection changes refresh the blue outline and displayed equipped item.
- An acquisition event refreshes the matching slot icon.

## Success Criteria

- External code can add an item with one API call and receive a success result.
- Up to six acquired items remain in stable slot order.
- Pressing `1` through `6` immediately equips the selected slot's weapon or clears equipment for an empty slot.
- The selected slot is visibly identified by a blue outline.
- The hotbar visually matches `ItemUI.png`: six contiguous transparent boxes, thin black outer/divider lines, labels above the boxes, and an inset blue rectangle only on the selected slot.
- Automated tests verify inventory, selection, equipment, and UI synchronization.
