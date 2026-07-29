# Task 2 Report: Compact Item Hotbar

## Status

Complete. The factory now creates an exact `432f x 144.3318f` hotbar while
retaining the full background Sprite, its preserved aspect, and the existing
normalized `SlotOverlayRects`.

## Changed Files

- `Assets/Scripts/Items/ItemHotbarView.cs`
- `Assets/Scripts/Items/ItemHotbarUIFactory.cs`
- `Assets/Tests/Editor/ItemHotbarViewTests.cs`
- `.superpowers/sdd/2026-07-29-slot-one-sword-hotbar/task-2-report.md`

No inventory, shooter gating, Stage1 setup, runtime bootstrap, or saved scene
files were modified.

## TDD Evidence

### RED

Command:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Users\myong\NaManMoo' -runTests -testPlatform EditMode -testFilter 'ItemHotbarViewTests' -testResults 'C:\Users\myong\NaManMoo\Artifacts\task2-hotbar-red.xml' -logFile 'C:\Users\myong\NaManMoo\Artifacts\task2-hotbar-red.log'
```

Exact totals: **17 total, 13 passed, 4 failed, 0 inconclusive, 0 skipped**.

The four expected failures independently caught the old `1728f` width and
derived height, old `5f` selection inset, old `6f` icon inset, and oversized
slot 1.

### GREEN

Command:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Users\myong\NaManMoo' -runTests -testPlatform EditMode -testFilter 'ItemHotbarViewTests' -testResults 'C:\Users\myong\NaManMoo\Artifacts\task2-hotbar-green.xml' -logFile 'C:\Users\myong\NaManMoo\Artifacts\task2-hotbar-green.log'
```

Exact totals: **17 total, 17 passed, 0 failed, 0 inconclusive, 0 skipped**.

## Compact Insets

- `SelectionInset = 2f`
- `IconInset = 4f`
- `BorderThickness = 1f`

The icon offsets are symmetric and positive: `(4f, 4f)` and `(-4f, -4f)`.
Selection offsets are `(2f, 2f)` and `(-2f, -2f)`. These keep every child
inside its own non-overlapping normalized slot while leaving the blue
one-pixel selection outline visible.

## Self-review

- Exact hotbar size is `new Vector2(432f, 144.3318f)`.
- Compact/source aspect difference is approximately
  `0.00000005734`, below the required `0.0001`.
- The original full background Sprite and all six normalized slot rectangles
  remain unchanged.
- Slot 1 measures approximately `71.6682 x 51.7604`.
- The sword test verifies the exact Sprite instance, enabled state,
  `preserveAspect = true`, symmetric positive inset, and containment in slot 1.
- Existing selection, subscription, transparency, and background tests remain
  green.
- No Git command was used.

## Concerns

None within Task 2 scope. Verification was focused on
`ItemHotbarViewTests`, as required by the brief; no scene or runtime bootstrap
was rebuilt or modified.
