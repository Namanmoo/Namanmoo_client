# Task 1 — Inventory Domain Model Report

## Status

`DONE_WITH_CONCERNS` — the requested code and focused test suite are present, but Unity EditMode GREEN verification could not be run because another Unity instance had the project lock. The secondary `dotnet test` command returned zero, but Unity's generated project files are stale and exclude the new source/test files; it is not treated as evidence that this task's tests passed.

## Implementation

- `Assets/Scripts/Items/ItemData.cs`
  - Adds `ItemKind` (`Weapon`, `Item`) and immutable `ItemData` values: `Id`, `DisplayName`, `Kind`, and `Icon`.
  - Treats only non-empty IDs as valid through `IsValid`.
- `Assets/Scripts/Items/PlayerInventory.cs`
  - Adds a plain C# six-slot inventory with a read-only slot view.
  - Acquires valid items into the first empty slot, auto-selecting/equipping the first item only.
  - Supports selecting occupied and empty slots, clearing the equipped item for empty selection.
  - Rejects invalid/full acquisition and out-of-range selection without mutation.
  - Raises `StateChanged` and `EquippedItemChanged` only after actual relevant changes.
- `Assets/Tests/Editor/PlayerInventoryTests.cs`
  - Covers item validity/values, six-slot initialization, acquisition ordering and rejection, selection boundaries and behavior, and event suppression/change behavior.

## TDD Evidence

### RED command

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath 'C:\Users\myong\NaManMoo' -runTests -testPlatform EditMode -testFilter 'PlayerInventoryTests' -testResults 'C:\Users\myong\NaManMoo\Artifacts\task1-red.xml' -logFile 'C:\Users\myong\NaManMoo\Artifacts\task1-red.log'; exit $LASTEXITCODE
```

### RED output

```text
Exit code: 0 (launcher); Unity log reports return code 1.

Aborting batchmode due to fatal error:
It looks like another Unity instance is running with this project open.

Multiple Unity instances cannot open the same project.

Project: C:/Users/myong/NaManMoo
```

This did not reach compilation: it failed at the Unity project lock, so it is not a valid test RED caused by the missing production types. `Artifacts/task1-red.log` captures the full invocation and lock failure. No XML test result was produced.

### GREEN command attempted

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath 'C:\Users\myong\NaManMoo' -runTests -testPlatform EditMode -testFilter 'PlayerInventoryTests' -testResults 'C:\Users\myong\NaManMoo\Artifacts\task1-green.xml' -logFile 'C:\Users\myong\NaManMoo\Artifacts\task1-green.log'; exit $LASTEXITCODE
```

The command was not allowed to complete after the lock issue was identified. It produced no GREEN log/XML result and no claim of passing tests is made.

### Secondary check

```powershell
dotnet test NaManMoo.EditorTests.csproj --no-restore
```

```text
Exit code: 0
Output: (none)
```

This is not valid GREEN verification. Inspection shows `NaManMoo.EditorTests.csproj` is Unity-generated and its `<Compile>` list omits `Assets\Tests\Editor\PlayerInventoryTests.cs`; the generated runtime project likewise predates the new item files.

## Self-review

- Slots are exposed as `IReadOnlyList<ItemData>` backed by `ReadOnlyCollection<ItemData>`, avoiding an externally mutable array exposure.
- The initial unselected state is explicitly `-1`; this is necessary to distinguish the first automatic selection from selecting an empty slot.
- `SelectSlot` returns success for every in-range slot, including empty slots; repeat selection is successful but emits no event because no state changes.
- Equipping uses reference comparison, so selecting an empty slot does not emit an equip event if the equipped item is already null, while state selection still notifies `StateChanged`.
- Scope remains limited to the requested model and editor tests: no input, UI, pickups, combat, persistence, stacking, or drag-and-drop.

## Concerns

1. Unity was locked by another running editor/process, preventing both genuine RED and GREEN test execution. Re-run the exact GREEN command after the lock owner closes.
2. The unavailable true RED evidence is an environment limitation, not a demonstrated missing-type compiler failure.
3. Git cannot be used: `git status --short` returned `fatal: not a git repository (or any of the parent directories): .git`; no commit was created.

## Fix Round 1 of 5

### Review finding and fix

The original first-acquisition condition checked `SelectedSlotIndex == -1`. That incorrectly preserved a caller's earlier selection of an empty slot, leaving the first acquired item in slot 0 unequipped. The requirement is unconditional: the first successfully acquired item always selects and equips slot 0.

- Added `TryAcquire_FirstValidItem_AfterEmptySlotPreselection_StillSelectsAndEquipsSlotZero` in `Assets/Tests/Editor/PlayerInventoryTests.cs`.
- Expanded `TryAcquire_NullInvalidOrFull_DoesNotMutateInventory` to assert that null, invalid, and full rejections raise neither `StateChanged` nor `EquippedItemChanged`.
- Changed `Assets/Scripts/Items/PlayerInventory.cs` so the automatic selection/equip path is based on insertion into slot 0. Because acquisition has no removal feature and always fills the first empty slot, slot 0 unambiguously represents the first successful acquisition.

### RED/GREEN commands and output

The focused regression test was added before the production change. No Unity command was launched for this round, as explicitly instructed while the project lock is active.

Intended RED/GREEN command (not run):

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath 'C:\Users\myong\NaManMoo' -runTests -testPlatform EditMode -testFilter 'PlayerInventoryTests' -testResults 'C:\Users\myong\NaManMoo\Artifacts\task1-fix1.xml' -logFile 'C:\Users\myong\NaManMoo\Artifacts\task1-fix1.log'; exit $LASTEXITCODE
```

Output: not run; no result XML or log was produced. The prior Unity attempt is documented above as blocked by the project lock.

### Round limitation

The code and tests were reviewed statically, but this round has no executed Unity RED/GREEN evidence. Run the command above after the existing Unity project owner releases the lock.
