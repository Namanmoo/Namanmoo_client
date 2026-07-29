# Task 2: Resize and Refit the Item Hotbar

## Requirements

- Modify:
  - `Assets/Scripts/Items/ItemHotbarView.cs`
  - `Assets/Scripts/Items/ItemHotbarUIFactory.cs`
  - `Assets/Tests/Editor/ItemHotbarViewTests.cs`
- Set exact constants:
  - `BackgroundWidth = 432f`
  - `BackgroundHeight = 144.3318f`
- `Item Hotbar` RectTransform size must equal exactly
  `new Vector2(432f, 144.3318f)`.
- Keep the full background Sprite and current normalized `SlotOverlayRects`.
- Preserve source aspect within `0.0001`.
- Keep all six slot roots, icons, and selection outlines fully contained in the
  hotbar and their own slot interiors.
- Refit selection inset, icon inset, and border thickness for the compact size.
  Use named constants, symmetric positive icon offsets, and avoid adjacent-slot
  overlap. Maintain a visible blue selection outline.
- Slot 1 safe area is approximately 72 × 52 pixels. Its sword icon must:
  - use the exact sword Sprite in the test;
  - have `preserveAspect = true`;
  - remain enabled and fully inside slot 1;
  - remain unstretched and unclipped via symmetric positive inset.
- Update stale tests expecting width 1728 or derived old height.
- Follow strict TDD: focused `ItemHotbarViewTests` RED before implementation,
  GREEN after.
- Do not modify inventory, shooter gating, Stage1 setup, runtime bootstrap, or
  the saved scene.

## Report Contract

Write `.superpowers/sdd/2026-07-29-slot-one-sword-hotbar/task-2-report.md`
with status, changed files, RED/GREEN commands and exact totals, chosen compact
inset constants, self-review, and concerns.
