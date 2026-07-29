# Item Hotbar Image Background Design

## Goal

Replace the code-drawn hotbar artwork with the complete `ItemUI.png` image while preserving the existing acquisition, equipment, and number-key behavior.

## Source Image

The complete source image is retained, including:

- its white background,
- all upper and lower whitespace,
- the handwritten black slot lines,
- the handwritten labels `1` through `6`,
- the original image dimensions and aspect ratio.

The blue outline baked into slot 1 is removed from a copied asset. No other part of the source artwork is intentionally changed. The original root-level `ItemUI.png` remains untouched. The edited copy is stored as `Assets/UI/ItemUIBackground.png` and imported as a Unity Sprite.

## UI Composition

The hotbar uses one full-image background rather than recreating the black border, dividers, numbers, or paper background with uGUI components.

The image is displayed bottom-center with its original aspect ratio. Six transparent overlay regions align with the six drawn slot interiors. Each overlay contains:

- one item icon centered within the corresponding drawn box,
- one dynamic blue selection outline inset inside the drawn black boundary.

Only the icons and the selected blue outline render above the source image. No additional labels, black borders, backgrounds, shadows, gradients, or decorative panels are generated.

## Selection

The edited background contains no blue selection mark. Unity creates six selection-outline objects but enables exactly one at a time.

- Slot 1 is selected initially.
- Keys `1` through `6` move the single blue outline to the matching drawn slot.
- The outline uses the existing selection blue and thin four-edge construction.
- Selecting an empty slot still moves the outline and clears equipped equipment.

## Overlay Alignment

Overlay rectangles use normalized coordinates measured from the full source image. This accounts for the large whitespace above and below the drawn boxes and preserves alignment when the image scales.

The six overlay regions:

- match the visible box interiors,
- remain aligned at all supported resolutions,
- preserve the existing icon aspect-ratio behavior,
- keep icons inside the hand-drawn boundaries.

The full background RectTransform retains the source image aspect ratio. Slot positions and sizes are centralized as normalized layout constants so tests can verify them and future artwork replacement has one adjustment point.

## Existing Behavior

The following behavior remains unchanged:

- six non-stacking inventory slots,
- first-empty-slot acquisition through `TryAcquire(ItemData item)`,
- automatic first-item equipment,
- immediate equipment switching with top-row keys `1` through `6`,
- empty-slot selection clearing equipment,
- scene reload reconstruction,
- shared Stage 1 editor/runtime setup.

## Asset Import

`Assets/UI/ItemUIBackground.png` is imported as a single 2D Sprite with alpha support and no slicing. The sprite uses settings appropriate for retaining the source's hand-drawn appearance without introducing filtering artifacts.

The generated Stage 1 scene stores a persistent reference to the imported sprite. Runtime code does not load the image from an external filesystem path.

## Testing

Edit Mode tests verify:

- the factory uses `ItemUIBackground.png` as the background Sprite,
- exactly one full-image background exists,
- no generated number labels or black border/divider objects remain,
- the displayed image preserves the source aspect ratio,
- six overlay slots align to the centralized normalized coordinates,
- only one blue selection outline is active,
- icon acquisition and selection movement continue to refresh.

Play Mode tests verify:

- the image-backed hotbar reconnects after initialization,
- acquisition displays an icon in the matching drawn slot,
- keys/selection move one blue outline between overlay regions,
- equipment state remains synchronized.

## Success Criteria

- The complete edited `ItemUI.png` artwork, including its white background and whitespace, is visible in game.
- The baked slot-1 blue mark is absent.
- Exactly one Unity-rendered blue outline marks the selected slot.
- Item icons align with the six boxes drawn in the image.
- Existing acquisition and `1` through `6` equipment behavior remains functional.
