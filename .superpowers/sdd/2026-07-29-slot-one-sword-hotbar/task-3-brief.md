# Task 3: Auto-Acquire the Sword and Wire One Inventory

## Requirements

- Modify as needed:
  - `Assets/Scripts/Items/ItemHotbarController.cs`
  - `Assets/Scripts/Stage1ItemHotbarSetup.cs`
  - `Assets/Editor/Stage1SceneBuilder.cs`
  - `Assets/Scripts/Stage1RuntimeBootstrap.cs`
  - `Assets/Tests/Editor/Stage1SceneBuilderTests.cs`
  - `Assets/Tests/Editor/ItemHotbarControllerTests.cs`
  - `Assets/Tests/PlayMode/ItemHotbarPlayModeTests.cs`
  - generated `Assets/Scenes/Stage1.unity`
- Use exact sword item values:
  - ID `sword`
  - display name `Sword`
  - kind `ItemKind.Weapon`
  - icon exact Sprite `Assets/Weapons/sword.png`
- `ItemHotbarController` stores a serialized starting-sword Sprite and exposes a
  configuration API. Whenever its inventory is created/ensured and a valid
  starting Sprite is configured, ensure exactly one sword item exists.
- Fresh startup inventory puts the sword in slot index 0, selects it, and equips
  it. Repeated `Awake`, access, or configuration must not duplicate it.
- Persist the starting Sprite in the saved scene so reopening/reloading builds
  the same runtime starting inventory.
- Extend `Stage1ItemHotbarSetup.Create` to accept the sword Sprite, configure the
  controller before creating the view, then inject the exact
  `controller.Inventory` object into the player's `PlayerSwordShooter`.
- Update both `Stage1SceneBuilder` and `Stage1RuntimeBootstrap` to pass the
  already validated sword Sprite through the setup.
- Saved-scene and runtime-created player tests prove:
  - slot 0 contains exactly one sword;
  - selected index 0 and `EquippedItem` is the same sword object;
  - exact icon Sprite;
  - shooter stores/uses the same inventory instance;
  - hotbar slot 1 icon is enabled, preserveAspect, exact Sprite, contained;
  - hotbar exact size `432 × 144.3318`.
- PlayMode must prove:
  - slot 1 permits arrow-key sword firing;
  - selecting another slot blocks firing;
  - reselecting slot 1 restores immediate firing while held.
- Update existing tests that assumed a newly loaded Stage1 inventory was empty.
  Keep standalone `PlayerInventory` unit tests unchanged.
- Strict TDD: focused controller/scene integration RED before production,
  focused GREEN after.
- Rebuild `Assets/Scenes/Stage1.unity`; inspect serialized controller starting
  Sprite, compact hotbar size, shooter defaults/Sprite, and no stale 1728 size.
- Do not alter combat numeric defaults, projectile behavior, map, player visual,
  movement, or physics.

## Existing Interfaces

- `PlayerSwordShooter.InitializeInventory(PlayerInventory)` exists.
- `ItemHotbarView` exact size constants are 432 and 144.3318.
- `Stage1ItemHotbarSetup.Create` currently accepts player, parent, background.

## Report Contract

Write `.superpowers/sdd/2026-07-29-slot-one-sword-hotbar/task-3-report.md`
with status, files changed, RED/GREEN evidence and totals, builder result, saved
YAML inspection, focused PlayMode results, self-review, and concerns.
