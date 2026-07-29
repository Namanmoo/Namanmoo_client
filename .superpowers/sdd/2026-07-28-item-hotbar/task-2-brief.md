### Task 2: Number-Key Selection Controller

Create:
- `Assets/Scripts/Items/ItemHotbarController.cs`
- `Assets/Tests/Editor/ItemHotbarControllerTests.cs`

Consumes the existing `PlayerInventory`.

Required interfaces:
- `static int ItemHotbarController.SlotIndexForNumber(int number)` returns slot indices `0..5` for numbers `1..6`, otherwise `-1`.
- `void ItemHotbarController.Initialize(PlayerInventory inventory)`.
- Expose the associated inventory read-only so later scene integration and external acquisition callers can reach the API.

Behavior:
- Read Unity Input System `Keyboard.current`.
- Top-row digit keys `1` through `6` select slot indices `0` through `5`.
- Selection occurs only on `wasPressedThisFrame`.
- Missing keyboard or missing inventory does nothing safely.
- Do not implement numpad bindings.

Tests:
- Verify exact static mapping `1..6 -> 0..5`.
- Verify `0`, `7`, and other out-of-range values map to `-1`.
- Verify each top-row digit selects the matching slot using Input System keyboard fixtures if supported by the current test assembly.
- Verify controller without an initialized inventory does not throw.

TDD is mandatory: tests first, capture RED if Unity can run, minimal implementation, then GREEN. The Unity project is currently locked by an active editor; do not launch duplicate Unity processes. If tests cannot run, document exact limitations and perform static compile checks only when they include the new files.

Do not implement UI, scene builders, pickups, combat, persistence, stacking, drag-and-drop, or numpad input.
