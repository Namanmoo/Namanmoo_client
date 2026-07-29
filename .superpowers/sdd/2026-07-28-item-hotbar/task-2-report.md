# Task 2: Number-Key Selection Controller

## Status

Implemented. No commit was created because git is invalid for this workspace.

## Files changed

- `Assets/Scripts/Items/ItemHotbarController.cs`
- `Assets/Tests/Editor/ItemHotbarControllerTests.cs`
- `Assets/Tests/Editor/NaManMoo.EditorTests.asmdef`

## Implementation

- `SlotIndexForNumber` maps `1..6` to `0..5` and all other values to `-1`.
- `Initialize` associates a `PlayerInventory`, exposed read-only through `Inventory`.
- `Update` safely exits when `Keyboard.current` or `Inventory` is absent.
- Only top-row `Key.Digit1` through `Key.Digit6` are evaluated via `wasPressedThisFrame`; numpad keys are not handled.

## TDD evidence

### RED

`ItemHotbarControllerTests.cs` was written before `ItemHotbarController.cs`. The intended initial RED state was the missing `ItemHotbarController` type.

Unity tests were not run: the project is locked by active Unity Editor processes (PIDs 7928 and 15848), and the task explicitly prohibits launching a duplicate Unity process.

The current compiler log later provided concrete RED evidence: `ItemHotbarControllerTests.cs` could not resolve `UnityEngine.InputSystem` and `InputTestFixture`. The root cause was that `NaManMoo.EditorTests.asmdef` referenced only `NaManMoo.Runtime` and `NaManMoo.Editor`, while the tests directly consume Input System test APIs.

### GREEN

Added the minimal controller implementation after the tests. Unity GREEN execution remains unverified for the same project-lock limitation.

Added direct test-assembly references to `Unity.InputSystem` and `Unity.InputSystem.TestFramework`, matching the package-provided assemblies required by the test source. No generated `.csproj` file was modified.

## Verification

- Static source review completed: the runtime assembly already references `Unity.InputSystem`; the test uses `InputTestFixture`, `Keyboard`, `Key.Digit1..Digit6`, and `Press` for the required top-row keyboard fixtures.
- Static assembly-definition review completed: `NaManMoo.EditorTests.asmdef` now directly references both `Unity.InputSystem` and `Unity.InputSystem.TestFramework`.
- Generated `NaManMoo.EditorTests.csproj` includes the test assembly and therefore the new test source through Unity's project generation.
- A static `dotnet build --no-restore` did not reach compilation because `Temp/obj/NaManMoo.EditorTests/project.assets.json` is absent. A restore-enabled retry was blocked by sandbox access to the user NuGet configuration; no further build or Unity process was started.

## Self-review

- Mapping exactly covers the six requested slots and rejects out-of-range input.
- Input handling uses `wasPressedThisFrame`, avoiding repeated selection while a key is held.
- Missing keyboard and uninitialized inventory are no-throw paths.
- No UI, scene, pickup, combat, persistence, stacking, drag/drop, or numpad work was added.

## Concerns

- Unity EditMode tests and compilation must be run in the already-open Unity Editor after its current lock-owning work is available. The static build could not complete due environment restore/config access, not source diagnostics.
- GREEN remains unexecuted after the assembly-reference fix because launching another Unity process under the active project lock is prohibited.
