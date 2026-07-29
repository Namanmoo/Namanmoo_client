### Task 1: Edited Background Asset

Source:
- `C:\Users\myong\NaManMoo\ItemUI.png`

Create:
- `C:\Users\myong\NaManMoo\Assets\UI\ItemUIBackground.png`

Requirements:
- Use the imagegen skill and built-in image editing.
- Inspect the local source with `view_image` before editing.
- Treat the source as the edit target, not a style reference.
- Preserve the complete source canvas dimensions and aspect ratio.
- Preserve the entire white background, upper/lower whitespace, handwritten black slot boundary/dividers, handwritten labels `1` through `6`, paper texture, and all other artwork.
- Remove only the blue rectangular outline baked inside slot 1.
- Reconstruct the removed blue area naturally as the same white paper/background beneath it.
- Do not alter or overwrite the root-level source file.
- Save the final project-bound output at the exact Assets path above.
- Visually inspect the output and compare it to the source.
- Confirm the blue mark is absent and adjacent black lines remain intact.
- Record source/output dimensions, method, final prompt, validation, and concerns in `task-1-report.md`.

Do not modify C# files, tests, scenes, or other assets in this task.
