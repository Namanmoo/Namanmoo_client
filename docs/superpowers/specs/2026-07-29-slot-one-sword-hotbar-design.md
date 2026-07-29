# Slot-One Sword and Compact Hotbar Design

## Goal

Place the sword in hotbar slot 1 at game start, allow sword firing only while
that sword is selected, and resize the complete item hotbar to exactly
`432 × 144.3318`.

## Starting Inventory

- Create one `ItemData` for the sword with ID `sword`, display name `Sword`,
  kind `Weapon`, and the Sprite at `Assets/Weapons/sword.png`.
- Acquire it through the existing `PlayerInventory.TryAcquire` API so it enters
  slot index 0 and becomes selected and equipped.
- Apply the same setup in the saved Stage1 scene builder and runtime bootstrap.
- Do not add duplicate swords when setup or connection is repeated.

## Firing Gate

- `PlayerSwordShooter` receives the same `PlayerInventory` used by the
  `ItemHotbarController`.
- Arrow-key input can fire only when:
  - `SelectedSlotIndex == 0`;
  - `EquippedItem` is non-null; and
  - `EquippedItem.Id == "sword"`.
- Selecting slots 2–6 immediately blocks firing.
- Returning to slot 1 while an arrow direction is held fires immediately rather
  than waiting for the previous cooldown.
- Existing damage, firing-rate, speed, spin, lifetime, diagonal, and collision
  behavior remains unchanged.

## Compact Hotbar

- Set the hotbar `RectTransform.sizeDelta` to exactly
  `432 × 144.3318`.
- Keep the full background image and existing normalized slot anchors.
- Keep all slot roots, icons, and selection outlines inside the compact hotbar.
- The sword icon uses `preserveAspect` and stays inside the slot 1 safe interior
  with a small proportional inset so it is neither clipped nor stretched.
- Selection outlines continue to follow the selected slot without overlapping
  adjacent slots.

## Integration

- The saved scene and runtime bootstrap both create one inventory shared by the
  hotbar controller, view, and sword shooter.
- The sword Sprite used for the projectile and slot icon is the same imported
  Sprite reference.
- Rebuild `Assets/Scenes/Stage1.unity`.

## Verification

- Inventory/integration tests verify one sword is acquired into slot 1.
- Shooter tests verify selected sword permits firing, another slot blocks it,
  and returning to slot 1 fires immediately.
- Hotbar tests verify exact `432 × 144.3318` size, unchanged background ratio,
  contained slot geometry, and a non-clipped aspect-preserving sword icon.
- Existing combat, inventory, hotbar, EditMode, and PlayMode tests remain green.

