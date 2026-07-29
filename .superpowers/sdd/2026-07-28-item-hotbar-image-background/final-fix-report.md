# Final Fix Wave Report

## Status

Implementation and tests were updated for the viewport fit, Single-sprite import, and image-measured ink-safe interiors. The final conservative anchor refinement was made after pixel inspection, but the requested stop arrived before its matching GREEN run and before rebuilding Stage1 a second time. The parent must run the focused test and rebuild once more before making a completion claim.

## Files changed

- `Assets/Scripts/Items/ItemHotbarView.cs`
- `Assets/Scripts/Stage1ItemHotbarSetup.cs`
- `Assets/Tests/Editor/ItemHotbarViewTests.cs`
- `Assets/Tests/Editor/Stage1SceneBuilderTests.cs`
- `Assets/UI/ItemUIBackground.png.meta`
- `Assets/Scenes/Stage1.unity` (rebuilt once before the final anchor refinement)

`Assets/UI/ItemUIBackground.png` and root-level `ItemUI.png` were not edited.

## Root cause

- The hotbar used the source dimensions (`2170 x 725`) as UI dimensions, so its width exceeded the `1920` reference canvas.
- The normalized slot roots used the drawn black boundary coordinates instead of whitespace-only interiors.
- `ItemUIBackground.png` was imported as Multiple (`spriteMode: 2`), leaving the scene on a sliced subasset reference.

## Exact layout constants

- Source size: `2170 x 725`
- Reference canvas: `1920 x 1080`
- Display width: `1728` (`90%` of reference width)
- Display height: `1728 * 725 / 2170 = 577.3271889400921`
- Anchor/pivot/position: bottom-center, `anchorMin = anchorMax = pivot = (0.5, 0)`, `anchoredPosition = (0, 0)`
- CanvasScaler: `ScaleWithScreenSize`, reference `1920 x 1080`, `matchWidthOrHeight = 0`

Final source-pixel safe interiors use common top-origin `y = 275..535`, converted to Unity bottom-origin:

- `yMin = (725 - 535) / 725 = 190 / 725 = 0.26206897`
- `yMax = (725 - 275) / 725 = 450 / 725 = 0.62068963`

| Slot | Source-pixel interior | Normalized x min/max |
|---|---|---|
| 1 | `x 50..410` | `50/2170 .. 410/2170` |
| 2 | `x 450..785` | `450/2170 .. 785/2170` |
| 3 | `x 820..1135` | `820/2170 .. 1135/2170` |
| 4 | `x 1185..1455` | `1185/2170 .. 1455/2170` |
| 5 | `x 1500..1770` | `1500/2170 .. 1770/2170` |
| 6 | `x 1815..2070` | `1815/2170 .. 2070/2170` |

The icon and dynamic selection-outline RectTransforms remain stretched within each safe slot root with positive insets, and tests assert their calculated bounds remain inside the background Rect.

## RED / GREEN evidence

### Initial layout RED

- Results: `Artifacts/final-fix-red.xml`
- Log: `Artifacts/final-fix-red.log`
- Result: `15 total, 13 passed, 2 failed`
- Expected failures:
  - viewport fit expected `(1728.00, 577.33)` but saw `(2170.00, 725.00)`
  - ink-safe coordinates expected the new interior minimum but saw the old boundary anchor

### Initial layout GREEN

- Results: `Artifacts/final-fix-green.xml`
- Log: `Artifacts/final-fix-green.log`
- Result: `15 total, 15 passed, 0 failed`

This GREEN covered the `1728` viewport fit, full-source aspect ratio, contained overlay hierarchy, and the brief's initial approximate safe anchors.

### Pixel-refined anchor RED

Non-destructive inspection showed that the approximate `y 260..558` rectangles still included anti-aliased black boundary pixels in slots 2-6. The bounds were tightened to the exact rectangles listed above. Every final rectangle contains zero pixels with all RGB channels at or below `180`.

- Results: `Artifacts/final-fix-safe-red.xml`
- Log: `Artifacts/final-fix-safe-red.log`
- Result: `1 total, 0 passed, 1 failed`
- Expected failure: refined minimum expected approximately `(0.02, 0.26)` but production still returned approximately `(0.02, 0.23)`

Production was then changed to the refined values. The matching GREEN command was interrupted by the stop request, so no post-refinement GREEN result is claimed.

## Real-asset and import evidence

The imported Sprite was verified through the Stage1 integration test as:

- texture and Sprite rect: `2170 x 725`
- Sprite import mode: Single (`spriteMode: 1`)
- maximum size: `4096`
- compression: Uncompressed
- wrap: Clamp
- mipmaps: disabled
- alpha usage/transparency: enabled
- readable: disabled

Because the texture is intentionally not readable in Unity, the source PNG was inspected non-destructively outside Unity:

- dimensions: `2170 x 725`
- strong blue pixels: `0`, using `B >= 100`, `B-R >= 40`, and `B-G >= 20`
- maximum blue dominance anywhere in the image: `12` at RGB `(175,177,189)`, inconsistent with the removed saturated blue outline
- all six final safe interiors contain `0` pixels at or below the conservative `RGB <= 180` ink threshold

## Scene and log checks

Stage1 was rebuilt after switching the importer to Single:

- Build log: `Artifacts/final-fix-stage1-build.log`
- Unity exit: code `0`, `Exiting batchmode successfully`
- Serialized hotbar size: `{x: 1728, y: 577.3272}`
- Serialized CanvasScaler: reference `{x: 1920, y: 1080}`, match `0`
- Serialized background reference: Single-sprite main reference `fileID: 21300000`
- Old Multiple-sprite subasset reference `fileID: -3036955133447002263`: absent

Focused real-asset integration:

- Results: `Artifacts/final-fix-stage1-integration.xml`
- Log: `Artifacts/final-fix-stage1-integration.log`
- Result: `1 total, 1 passed, 0 failed`

That build/integration run occurred before the final conservative anchor refinement.

## Required final handoff verification

No broad suite should be started solely for this subtask. The remaining required checks are:

1. Run focused `ItemHotbarViewTests` (or at minimum `Create_BuildsSixNamedTransparentSlotsAtInkSafeInteriorCoordinates`) and confirm GREEN with the final bounds.
2. Re-run `Stage1SceneBuilder.Build` so `Assets/Scenes/Stage1.unity` serializes the final conservative anchors.
3. Re-run the focused Stage1 hotbar integration test or inspect the rebuilt scene anchors.

Parent-owned final full Edit Mode and Play Mode verification remains appropriate after those focused checks.

## Concerns

- The saved Stage1 scene is current for Single-sprite import and `1728 x 577.3272` sizing, but stale for the final conservative slot anchors until rebuilt once more.
- The last source edit has only RED evidence because the matching GREEN test invocation was interrupted.
- No inventory, equipment, selection, acquisition, or root-level image behavior was altered.
