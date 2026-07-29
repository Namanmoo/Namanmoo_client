# Final Fix Wave Requirements

Address all load-bearing final-review findings without changing the user-visible feature.

## Inventory invariants

- A fresh `PlayerInventory` starts with slot index `0` selected and `EquippedItem == null`.
- Exactly one valid slot is selected at all times.
- First acquisition goes to slot 0 and equips it.
- Any acquisition into the currently selected empty slot immediately updates `EquippedItem` and emits the equipment event.
- Later acquisition into an unselected slot preserves selection/equipment.
- Add regression tests for selecting empty slot 1, acquiring into slot 0 then slot 1, and verifying slot 1 becomes equipped when filled.

## Persistent/reload-safe wiring

- `ItemHotbarController` must recreate a `PlayerInventory` automatically on `Awake`/initial access when its nonserialized runtime model is absent.
- `ItemHotbarView` must keep serialized Unity object references needed to reconnect after scene reload (controller, icon Images, selection outline GameObjects/Images).
- On enable/awake after reload, the view must initialize from `controller.Inventory`, subscribe exactly once, and refresh.
- Explicit factory initialization must remain supported.
- Add tests that simulate losing runtime model/view subscriptions or reload-relevant initialization as closely as Edit Mode permits.

## Thin serializable borders

- Remove generated `HideAndDontSave` Texture2D/Sprite border resources.
- Build black and blue rectangular borders from four thin uGUI `Image` edge children, or another fully serializable approach whose thickness is expressed directly in UI units.
- Keep the required stable `Selection Outline` root name and inset blue rectangle.
- Ensure borders do not cover icons/interiors.
- Centralize and use border thickness/inset constants.
- Add structural tests for four edges, thin dimensions, colors, and transparent interior.

## EventSystem

- The approved design requires an EventSystem suitable for the Input System.
- Create exactly one `EventSystem` with `InputSystemUIInputModule` when none exists.
- Update tests to require it, not forbid it.

## Runtime/bootstrap and scene

- Existing `Generated Stage` persistence must be safe because controller/view reconstruct runtime state on reload.
- Both builder and runtime bootstrap continue using the shared setup.
- Preserve player movement and map behavior.

## Play Mode coverage

- Add a Play Mode test assembly and focused tests for runtime controller/view initialization, acquisition refresh, empty/occupied selection equipment changes, and selection outline movement.
- Do not overbuild beyond these lifecycle/behavior checks.

## Verification constraints

- Unity remains locked. Do not kill or launch Unity.
- Write tests before each production correction where practical and document that execution is blocked.
- Update the Input System test asmdef references as already fixed.
- Write a full report to `final-fix-report.md`.
