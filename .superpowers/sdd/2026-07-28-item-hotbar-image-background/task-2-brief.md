### Task 2: Image-Backed Overlay Layout

Modify:
- `Assets/Scripts/Items/ItemHotbarView.cs`
- `Assets/Scripts/Items/ItemHotbarUIFactory.cs`
- `Assets/Tests/Editor/ItemHotbarViewTests.cs`

Asset:
- `Assets/UI/ItemUIBackground.png`

Requirements:
- Use the complete edited image as one `Background` uGUI Image.
- Preserve the image's 2170:725 aspect ratio.
- Remove generated static artwork: no `Border`, `Divider`, or `Number` objects.
- Keep stable overlay roots `Slot 1` through `Slot 6`.
- Each slot is transparent and contains `Icon` and `Selection Outline`.
- Keep the existing serialized controller/icon/selection arrays and lifecycle reconnect behavior.
- Exactly one dynamic blue selection outline is active.
- Icons preserve aspect ratio and remain within drawn slot interiors.
- Centralize normalized overlay coordinates derived from the source image:
  - visible drawn box vertical range is approximately x-independent pixel y 250 through 568 in top-origin image coordinates;
  - slot horizontal boundaries are approximately pixels 31, 430, 803, 1161, 1482, 1797, and 2110;
  - convert to Unity bottom-origin normalized anchors and refine by inspecting the actual image.
- Keep the entire image visible; do not crop whitespace.
- Do not modify inventory/controller behavior or Stage 1 integration in this task.

Tests first:
- Background exists exactly once and uses the provided Sprite.
- Background Image has `preserveAspect = true`.
- Hotbar dimensions match 2170:725.
- No generated `Border`, `Divider`, or `Number` objects exist.
- Six transparent slots use centralized normalized anchors/positions.
- Blue outline single-selection behavior still works.
- Acquired icons still refresh and preserve aspect.

Factory interface:
- Add or adapt overloads so callers provide a `Sprite backgroundSprite`.
- Do not use `Resources.Load` or root-filesystem loading in the factory.

Run focused `ItemHotbarViewTests` RED/GREEN if Unity is available. Git is invalid; do not commit.
