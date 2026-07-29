### Task 1: Inventory Domain Model

Create:
- `Assets/Scripts/Items/ItemData.cs`
- `Assets/Scripts/Items/PlayerInventory.cs`
- `Assets/Tests/Editor/PlayerInventoryTests.cs`

Required interfaces:
- `ItemData(string id, string displayName, ItemKind kind, Sprite icon = null)`
- `bool PlayerInventory.TryAcquire(ItemData item)`
- `bool PlayerInventory.SelectSlot(int index)`
- `IReadOnlyList<ItemData> Slots`
- `int SelectedSlotIndex`
- `ItemData EquippedItem`
- `event Action StateChanged`
- `event Action<ItemData> EquippedItemChanged`

Behavior:
- `ItemKind` contains `Weapon` and `Item`.
- `ItemData` exposes immutable `Id`, `DisplayName`, `Kind`, and `Icon`.
- An item is valid only when its `Id` is non-empty.
- `PlayerInventory` always contains exactly six slots.
- Acquisition inserts a valid item into the first empty slot.
- Null/invalid items and acquisition when full return false without mutation.
- The first successfully acquired item automatically selects slot 0 and equips it.
- Later acquisitions preserve the current selection.
- Selecting indices 0 through 5 succeeds, including empty slots.
- Selecting an occupied slot changes `EquippedItem`.
- Selecting an empty slot clears `EquippedItem`.
- Selecting indices outside 0 through 5 returns false without mutation.
- Events are emitted only after state changes.

TDD is mandatory:
1. Add focused tests before production files.
2. Run them and capture the expected RED failure.
3. Add the minimal implementation.
4. Re-run and capture GREEN evidence.

Do not implement input, UI, scene integration, pickups, combat, persistence, stacking, or drag-and-drop.
