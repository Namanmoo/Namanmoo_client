# Task 1 — Edited Background Asset report

## Deliverable

- Source (unchanged): `C:\Users\myong\NaManMoo\ItemUI.png`
- Output: `C:\Users\myong\NaManMoo\Assets\UI\ItemUIBackground.png`
- Source dimensions: **2170 × 725 px** (24-bit RGB PNG)
- Output dimensions: **2170 × 725 px** (24-bit RGB PNG)

## Method

1. Inspected `ItemUI.png` with the built-in image viewer, treating it as the edit target.
2. Ran the built-in ImageGen editing flow with invariants that limited the requested change to removing the blue slot-1 rectangle while retaining the canvas, paper texture, black lines, and labels.
3. Inspected the built-in edit result. It removed the blue mark, but its native output canvas was 2117 × 743 px rather than the mandatory 2170 × 725 px source canvas.
4. To preserve the required source dimensions and all non-blue artwork exactly, created the project-bound output non-destructively from the original source canvas. Only blue-hued pixels in the baked slot-1 rectangle region were replaced with nearest neutral, light paper-background pixels. The root-level source was read only and was not overwritten.
5. Inspected the final project-bound PNG visually against the source.

## Final ImageGen prompt

```text
Use case: precise-object-edit
Asset type: Unity UI hotbar background raster asset
Primary request: Edit Image 1 (the edit target). Remove only the vivid blue hand-drawn rectangular outline located inside slot 1. Reconstruct exactly that removed area as the surrounding off-white, subtly textured paper/background.
Input images: Image 1: edit target.
Constraints: Preserve the complete source canvas dimensions and aspect ratio exactly. Change only the blue rectangular outline inside slot 1. Keep the entire white/off-white background, all upper and lower whitespace, handwritten black outer slot boundary, black vertical dividers, the black handwritten labels 1 through 6, paper texture, all artwork, black line placement, thickness, and texture completely unchanged. The adjacent black left, top, bottom, and first divider lines must remain intact. Do not alter any other pixels or crop, resize, redraw, sharpen, recolor, or reinterpret the image.
Avoid: any blue mark, any change to black boundaries/dividers/labels, new elements, text changes, watermark, border changes, crop, resize, gradient, or style change.
```

## Validation

- Visual comparison: the final output has no visible blue rectangle in slot 1; the surrounding white paper field, black outer boundary, first divider, and labels `1`–`6` remain present.
- Dimension check: source and output are both 2170 × 725 px.
- Targeted-pixel check: 24,423 blue-outline pixels were identified in the source selection; **0** remain under that criterion in the output. Under a stronger blue test, the count changed from 15,412 to **0**.
- Preservation check: exactly 24,423 output pixels differ from source, and **0** differences occur outside the identified blue-outline selection. This keeps all adjacent black lines and all other artwork pixel-identical to the source.
- No C#, tests, scenes, or other existing assets were modified.

## Concerns

- Built-in ImageGen’s native edit output did not retain the source canvas size, so it was not used directly as the final project asset. The source-preserving finalization step was necessary to meet the exact-dimensions and change-only-the-blue-mark requirements.
- The reconstructed area uses immediately adjacent neutral paper pixels, so it preserves the original background’s light texture without introducing model-generated layout drift.
