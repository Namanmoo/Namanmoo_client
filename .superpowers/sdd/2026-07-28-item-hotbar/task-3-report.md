# Task 3 Report: Reference-Matching Hotbar View

## Changed files

- `Assets/Scripts/Items/ItemHotbarView.cs`
  - Owns inventory state subscription, icon refresh, selection refresh, and teardown unsubscription.
  - Centralizes slot count, slot size, border thickness, label offset, selection inset, and blue selection color.
  - Treats `PlayerInventory.SelectedSlotIndex == -1` as visual slot 1 so the UI always has exactly one outline.
- `Assets/Scripts/Items/ItemHotbarUIFactory.cs`
  - Creates the bottom-centered `Item Hotbar` uGUI hierarchy with `Slot 1` through `Slot 6` and stable `Number`, `Icon`, and `Selection Outline` children.
  - Uses a transparent slot image, shared black outer border, black dividers, centered labels, aspect-preserving icons, and an inset sliced blue outline.
- `Assets/Tests/Editor/ItemHotbarViewTests.cs`
  - Covers hierarchy, equal contiguous slots, 6:1 strip proportion, labels, transparent interiors, initial selection, selection updates, sprite icons, and missing icons.

No scene builder files were changed. No Git commands were used because the repository is invalid as directed.

## TDD evidence and execution limitation

The `ItemHotbarViewTests` suite was added before the production hotbar files. It specifies the requested outcomes with real `PlayerInventory`, uGUI images, and an in-memory generated sprite; it does not mock the view or inventory.

The RED and GREEN Unity test executions were deliberately not started. At verification time, `Temp/UnityLockfile` existed and three Unity editor processes were active (PIDs 7928, 19060, and 31140). Launching another Unity process would violate the task constraint. Consequently, there is no test-run result to claim.

After the lock owner exits, run this targeted EditMode command and inspect its XML/log output before accepting the task:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath 'C:\Users\myong\NaManMoo' -runTests -testPlatform EditMode -testFilter 'ItemHotbarViewTests' -testResults 'C:\Users\myong\NaManMoo\Artifacts\task3-item-hotbar.xml' -logFile 'C:\Users\myong\NaManMoo\Artifacts\task3-item-hotbar.log'
```

## Static self-review

- Stable required names are present: `Item Hotbar`, six `Slot N` objects, and each slot's `Number`, `Icon`, and `Selection Outline`.
- The root is anchored to bottom center; the six 80 by 80 slots directly abut with no horizontal gap and produce a 480 by 80 (6:1) strip.
- The root's sliced black border supplies the outer edge; five thin black divider images supply the inner vertical lines; slot images are `Color.clear`.
- Icon images are centered, inset, `preserveAspect = true`, and disabled when the model item has no sprite.
- `Initialize` first detaches the prior `StateChanged` handler, then subscribes once; `OnDestroy` detaches it.
- A selection change refreshes every outline, enabling exactly one. The no-selection model state intentionally displays slot 1 to meet the always-selected UI requirement.

## Concerns

- Unity compilation, EditMode execution, and visual rendering against `ItemUI.png` remain unverified until the project lock clears.
- The black and blue rectangular outlines are generated at runtime from a 3x3 sliced sprite to avoid adding art assets. The intended result is a thin, unrounded, transparent-center outline, but final visual confirmation requires rendering in Unity.

## Fix round 1/5

- Added `Initialize_ReplacingInventory_UnsubscribesOldInventoryAndSubscribesNewInventoryOnce` to `ItemHotbarViewTests` before changing production code. It initializes with inventory A through the factory, reinitializes twice with inventory B, verifies A has zero `StateChanged` callbacks for this view and B has exactly one, verifies B moves the outline to slot 4, and verifies an A change cannot move it. The event-subscription count uses the real C# event delegate because `PlayerInventory` is sealed and exposes no listener-count API; it directly protects the explicit single-subscription contract.
- Added `Create_PlacesNumberLabelsAboveTheStripWithVisibleClearance` before the layout change. `NumberOffset` now represents a real 12-pixel vertical gap, and `NumberHeight` centralizes the label's 20-pixel height. Labels are therefore centered above, rather than touching, the strip.
- Deferred shared border-sprite caching as a minor cleanup. It would be a non-observable implementation optimization and cannot receive a meaningful behavior-first test in this locked verification cycle. The current implementation remains functionally scoped but creates one transient 3x3 texture/sprite pair for the outer border and each outline.

Unity EditMode RED/GREEN execution remains unperformed: the same `Temp/UnityLockfile` restriction applies. No Unity process or long build command was launched for this fix round.
