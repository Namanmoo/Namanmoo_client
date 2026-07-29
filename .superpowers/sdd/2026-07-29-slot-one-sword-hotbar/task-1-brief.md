# Task 1: Gate Sword Firing with the Shared Inventory

## Requirements

- Modify `Assets/Scripts/Combat/PlayerSwordShooter.cs`.
- Modify `Assets/Tests/Editor/PlayerSwordShooterTests.cs`.
- Add `InitializeInventory(PlayerInventory inventory)` and store that exact
  inventory reference.
- Firing is allowed only when all are true:
  - inventory is non-null;
  - `SelectedSlotIndex == 0`;
  - `EquippedItem` is non-null;
  - `EquippedItem.Id == "sword"`.
- Before processing direction/cooldown, block invalid inventory state.
- When blocked, clear `firingDirectionActive`, so returning to a valid sword
  selection fires immediately even if an arrow remains held.
- Tests with a real Input System keyboard must prove:
  - acquired sword in slot 0 fires;
  - selecting slot 1 (hotbar number 2) blocks new projectiles;
  - selecting slot 0 again while right arrow remains held fires immediately,
    regardless of previous cooldown;
  - null inventory, empty slot 0, and non-sword equipped item do not fire.
- Preserve all existing direction, automatic cooldown, Inspector validation,
  projectile configuration, and Sprite tests.
- Follow strict TDD with focused RED and GREEN Unity artifacts.
- Do not change hotbar dimensions or Stage1 integration in this task.

## Report Contract

Write `.superpowers/sdd/2026-07-29-slot-one-sword-hotbar/task-1-report.md`
with status, files changed, RED/GREEN commands and exact totals, self-review,
and concerns.
