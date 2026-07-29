# Visible Health Fill Width Design

## Goal

Make the red health fill visibly shrink from right to left whenever player
health decreases.

## Root Cause

The fill uses `Image.Type.Filled` without a source sprite. Unity renders a
sprite-less Image as a simple rectangle, so changing `fillAmount` does not
change the generated visible geometry.

## Design

- Keep the existing track, border, colors, text, and top-left layout.
- Anchor the fill to the left side of its padded track.
- Set `RectTransform.anchorMax.x` to the clamped health ratio.
- Keep the four-pixel inset on all sides.
- Continue setting `fillAmount` for semantic compatibility, while visible
  width is controlled by the RectTransform.
- Verify full, partial, and empty health widths.

## Testing

- At 20/20, `anchorMax.x` is 1.
- At 15/20, `anchorMax.x` is 0.75.
- At 0/20, `anchorMax.x` is 0.
- Existing contact damage test continues to verify 18/20 and a 0.9 gauge.
- Rebuild Stage1 and run all EditMode and PlayMode tests.
