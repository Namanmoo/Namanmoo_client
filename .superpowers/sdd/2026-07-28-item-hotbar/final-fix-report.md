# Item Hotbar Final Fix Report

Date: 2026-07-28 (Asia/Seoul)

## Status

All source and test changes requested by `final-fix-brief.md` were implemented. Unity was not launched, stopped, or otherwise controlled. Authoritative Edit Mode and Play Mode execution, Unity's exact asmdef compilation/import cycle, and a Stage1 scene rebuild remain unverified because the task explicitly required leaving the locked Unity project alone.

Tests were added before the corresponding production corrections. The intended RED and GREEN executions could not be observed because Unity test execution was prohibited.

## Production files changed

- `Assets/Scripts/Items/PlayerInventory.cs`
  - A fresh inventory now selects slot index 0 while leaving `EquippedItem` null.
  - Acquisition still fills the first empty slot.
  - First acquisition selects/equips slot 0.
  - Acquisition into any currently selected empty slot now equips the acquired item and emits `EquippedItemChanged`.
  - Acquisition into an unselected slot preserves the current selection and equipment.

- `Assets/Scripts/Items/ItemHotbarController.cs`
  - Replaced the auto-property runtime model with a nonserialized runtime field.
  - `Awake` and the `Inventory` getter reconstruct a missing `PlayerInventory`.
  - Explicit `Initialize(PlayerInventory)` remains supported and safely handles null.
  - Input polling now uses the guaranteed runtime inventory.

- `Assets/Scripts/Items/ItemHotbarView.cs`
  - Added serialized references for the controller, icon `Image` array, and selection-outline `GameObject` array.
  - Added lifecycle reconnection through `Awake` and `OnEnable`.
  - `Awake` reconnects and refreshes; `OnEnable` reconnects, subscribes exactly once, and refreshes.
  - `OnDisable`/`OnDestroy` unsubscribe without discarding an explicitly initialized runtime inventory.
  - Factory initialization with either an inventory or controller remains supported.
  - Selection is represented by activating exactly one stable `Selection Outline` root.

- `Assets/Scripts/Items/ItemHotbarUIFactory.cs`
  - Removed all generated `Texture2D`, `Sprite`, and `HideAndDontSave` border resources.
  - Replaced outer and selection borders with four serializable thin uGUI `Image` edges named `Top`, `Bottom`, `Left`, and `Right`.
  - Kept `Selection Outline` as the stable root and retained the inset blue rectangle.
  - Uses centralized `BorderThickness` and `SelectionInset` constants.
  - Leaves border interiors transparent and insets icons beyond the blue border edge.
  - Added a controller-aware factory overload while preserving the explicit-inventory overload.

- `Assets/Scripts/Stage1ItemHotbarSetup.cs`
  - Shared setup now passes the controller into the view factory so serialized scene references can reconnect after reload.
  - Creates one `EventSystem` with `InputSystemUIInputModule` when none exists.
  - Adds `InputSystemUIInputModule` to an existing EventSystem when necessary.
  - Both `Stage1SceneBuilder` and `Stage1RuntimeBootstrap` continue to call this shared setup; neither movement nor map code was changed.

- Added missing Unity metadata for the focused scripts:
  - `Assets/Scripts/Items/ItemHotbarUIFactory.cs.meta`
  - `Assets/Scripts/Items/ItemHotbarView.cs.meta`
  - `Assets/Scripts/Stage1ItemHotbarSetup.cs.meta`

No generated `.csproj` file was edited.

## Edit Mode test changes

- `Assets/Tests/Editor/PlayerInventoryTests.cs`
  - Updated the fresh-inventory invariant to selected slot 0 / no equipped item.
  - Added the exact regression flow: acquire into slot 0, select empty slot 1, acquire into slot 1, then verify slot 1 becomes equipped and emits one equipment event.
  - Strengthened unselected-slot acquisition coverage to prove slot 0 and its equipped item are preserved.

- `Assets/Tests/Editor/ItemHotbarControllerTests.cs`
  - Verifies automatic initial inventory creation.
  - Simulates loss of the nonserialized runtime model and verifies first access reconstructs a usable inventory.

- `Assets/Tests/Editor/ItemHotbarViewTests.cs`
  - Uses controller-aware factory setup.
  - Verifies four edges, direct UI-unit thickness, black/blue colors, inset, raycast behavior, and transparent interiors.
  - Verifies exactly one active selection root and outline movement.
  - Simulates lost view subscription/runtime inventory, then verifies re-enable reconnects exactly once and refreshes from `controller.Inventory`.
  - Retains explicit inventory reinitialization/subscription replacement coverage.
  - Added `Assets/Tests/Editor/ItemHotbarViewTests.cs.meta`.

- `Assets/Tests/Editor/Stage1SceneBuilderTests.cs`
  - Updated outline assertions for the serializable four-edge selection root.
  - Now requires exactly one `EventSystem` and an `InputSystemUIInputModule` instead of forbidding an EventSystem.
  - Existing build/open/select assertions continue to exercise serialized controller/view reconnection after scene reload.

- `Assets/Tests/Editor/NaManMoo.EditorTests.asmdef`
  - Existing fixed references to `Unity.InputSystem` and `Unity.InputSystem.TestFramework` were preserved.

## Play Mode coverage added

- `Assets/Tests/PlayMode/NaManMoo.PlayModeTests.asmdef`
- `Assets/Tests/PlayMode/ItemHotbarPlayModeTests.cs`
- Corresponding folder/file `.meta` files

The focused Play Mode tests cover:

- Runtime controller and view initialization through shared Stage1 setup.
- Required Input System EventSystem presence.
- Acquisition-driven icon refresh.
- Selecting an empty slot and observing null equipment.
- Filling the selected empty slot and observing immediate equipment.
- Switching back to an occupied slot.
- Selection-outline movement between slots.

## Static checks performed

- Parsed the runtime, Edit Mode test, and Play Mode test asmdefs as JSON successfully.
- Confirmed no script or asmdef under `Assets` was missing a `.meta` after the additions.
- Confirmed no `HideAndDontSave`, `CreateBorderSprite`, or generated 3x3 border texture remained in the hotbar source.
- Confirmed the three serialized view links and Input System references/modules were present.
- Confirmed the package source defines `UnityEngine.InputSystem.UI.InputSystemUIInputModule`.
- A direct Roslyn compilation using Unity's cached runtime response file plus the new runtime sources completed with exit code 0 and no diagnostics.
- A direct Roslyn compilation using Unity's cached editor response file plus the new runtime UI/setup sources completed with exit code 0 and no diagnostics.
- A direct Roslyn compilation of the current Edit Mode sources plus the new Play Mode source, cached Unity test references, and explicit Input System references completed with exit code 0 and no diagnostics.
- The first generic `dotnet build --no-restore` attempt was not a valid verification: it failed with `NETSDK1004` because Unity's generated `Temp/obj/NaManMoo.Runtime/project.assets.json` was absent. No restore was attempted.

The three successful direct compilations occurred before the final one-line addition of `Refresh()` in `ItemHotbarView.Awake`. Per the final stop instruction, no further command was run after that adjustment. Therefore the final filesystem state does not have a fresh compilation claim, although the final adjustment only invokes an existing parameterless method in the same class.

## Verification limitations and remaining actions

The following requirements are implemented in source but could not be authoritatively verified:

1. Edit Mode tests were not executed, so RED/GREEN runtime evidence and an Edit Mode result XML do not exist.
2. Play Mode tests were not executed, so a Play Mode result XML does not exist.
3. Unity did not perform a fresh exact asmdef import/compile after the changes.
4. `Stage1SceneBuilder.Build` was not run, so the saved `Assets/Scenes/Stage1.unity` was not regenerated with the new serialized border edges and EventSystem.
5. Visual rendering and real frame lifecycle behavior were not inspected in the Unity Editor.

Once the current Unity owner releases the project, the required follow-up is:

- Allow Unity to import/compile all scripts and new metadata.
- Run the complete Edit Mode suite.
- Run the complete Play Mode suite.
- Rebuild Stage1 through `Stage1SceneBuilder.Build`.
- Reopen the saved scene and verify one Input System EventSystem, serialized controller/view references, six slot outlines with four thin edges each, icon clearance, and selection/acquisition behavior.

## Repository state

Git is not valid in this workspace, so no commit was created. Existing unrelated files were not intentionally changed.

## Authoritative Edit Mode failure follow-up

An external Unity Edit Mode run after the initial fix wave reported 54 total tests: 45 passed and 9 failed. Unity was not run by this fix follow-up.

### Failure evidence and corrections

1. Seven `ItemHotbarControllerTests` cases failed because invoking the private Unity `Update` callback through `SendMessage("Update")` in Edit Mode triggered Unity's internal `ShouldRunBehaviour()` assertion rather than exercising hotbar behavior.
   - Added public `ProcessKeyboard(Keyboard keyboard)` as the directly testable input boundary.
   - Runtime `Update` now delegates to `ProcessKeyboard(Keyboard.current)`.
   - The six top-row digit tests call `ProcessKeyboard` directly and still use Input System `wasPressedThisFrame` state.
   - The no-keyboard/lost-runtime-model test now calls `ProcessKeyboard(null)` directly and verifies safe inventory reconstruction without relying on Edit Mode callback dispatch.

2. `ItemHotbarViewTests.Connect_AfterRuntimeSubscriptionIsLost_ReconnectsOnceAndRefreshesFromController` failed because toggling `view.enabled` did not dispatch `OnDisable` in that Edit Mode test context.
   - Added idempotent public `Connect()` and `Disconnect()` lifecycle boundaries.
   - `Awake` and `OnEnable` delegate to `Connect`.
   - `OnDisable` and `OnDestroy` delegate to `Disconnect`.
   - The regression test explicitly disconnects, simulates loss of the nonserialized inventory link, reconnects, and verifies one subscription plus refreshed outline state.

3. `Stage1SceneBuilderTests` failed at the post-reopen outline assertion because Edit Mode reopening did not invoke the runtime `OnEnable` connection path.
   - The test now retrieves the serialized `ItemHotbarView`, calls the same public `Connect()` boundary used by runtime lifecycle callbacks, then selects slot 3 and verifies the outline moves.
   - This keeps the test focused on reload-safe serialized references and event wiring without depending on Edit Mode callback scheduling.

### Follow-up files changed

- `Assets/Scripts/Items/ItemHotbarController.cs`
- `Assets/Scripts/Items/ItemHotbarView.cs`
- `Assets/Tests/Editor/ItemHotbarControllerTests.cs`
- `Assets/Tests/Editor/ItemHotbarViewTests.cs`
- `Assets/Tests/Editor/Stage1SceneBuilderTests.cs`

### Follow-up static verification

Compiler-only Roslyn checks were run using Unity's cached response files; Unity itself was not invoked.

- Current runtime sources compiled with exit code 0.
- Current editor sources compiled with exit code 0.
- Current Edit Mode sources plus the Play Mode test source and the newly extracted input/lifecycle boundaries compiled with exit code 0 and no diagnostics. The test-only static check supplied current runtime sources explicitly because the cached `Library/ScriptAssemblies/NaManMoo.Runtime.dll` predates this follow-up.

The authoritative Unity Edit Mode suite was not rerun by this agent. A fresh Unity run is still required to confirm the nine failures are resolved.
