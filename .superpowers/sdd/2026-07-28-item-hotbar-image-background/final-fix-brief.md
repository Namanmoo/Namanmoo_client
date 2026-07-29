# Final Fix Wave

Resolve all final-review Critical and Important findings.

## Full-image viewport fit

- The complete 2170×725 image must be visible at the 1920×1080 reference resolution.
- Display it bottom-center at 90% of reference width: 1728 UI units wide.
- Derive display height from the exact source aspect ratio: `1728 * 725 / 2170`.
- Preserve the full source image and all whitespace; no cropping.
- Width-matched CanvasScaler must keep the complete image visible across ordinary aspect ratios.
- Add tests asserting the hotbar Rect fits within the 1920 reference width and retains the exact source ratio.

## Ink-safe overlay regions

- Move normalized slot anchors from outer black-boundary coordinates to visibly safe interior coordinates.
- Use source pixel interiors measured from the edited 2170×725 image. Refine as needed, but overlays must not overlap black ink:
  - Slot 1 approximately x 44..420
  - Slot 2 approximately x 442..793
  - Slot 3 approximately x 814..1150
  - Slot 4 approximately x 1172..1471
  - Slot 5 approximately x 1493..1786
  - Slot 6 approximately x 1808..2098
  - Common top-origin y approximately 260..558, converted to Unity bottom-origin normalized anchors.
- Dynamic blue outline and icon rects must remain inside these safe overlay roots.
- Add tests for exact centralized normalized interiors and verify every overlay is within the background Rect.

## Real-asset integration coverage

- Stage1 Edit Mode tests must use the actual imported image and assert:
  - Sprite is 2170×725.
  - Display Rect is 1728 wide and fully contained in the 1920 reference canvas.
  - Full-image aspect ratio is preserved.
  - All six normalized overlays are contained and ink-safe.
  - No baked blue pixels are present in the imported texture when readable inspection is practical; otherwise validate the project file non-destructively outside Unity and record it.
- Play Mode keeps behavior tests; add saved/runtime image reference coverage if it can be done without `AssetDatabase` or brittle build-settings assumptions.

## Sprite import

- Configure `ItemUIBackground.png` as one Single Sprite (`spriteMode: 1`), not Multiple.
- Keep max size 4096, uncompressed, clamp, no mipmaps, alpha enabled.
- Rebuild Stage1 after import so the serialized Sprite reference is current.

## Verification

- Follow TDD for new layout/bounds assertions.
- Run full Edit Mode and Play Mode suites.
- Rebuild Stage1, inspect scene/logs, and report exact evidence.
- Do not alter inventory/equipment behavior or root-level `ItemUI.png`.
