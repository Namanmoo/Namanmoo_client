# Task 3 Report: Auto-Acquire the Sword and Wire One Inventory

## Status

Complete.

- Stage 1 now starts with exactly one configured sword item in slot index `0`.
- The starting item has ID `sword`, display name `Sword`, kind
  `ItemKind.Weapon`, and the exact Sprite from `Assets/Weapons/sword.png`.
- Slot index `0` is selected and its exact `ItemData` object is equipped.
- The hotbar view and `PlayerSwordShooter` use the same `PlayerInventory`
  instance owned by `ItemHotbarController`.
- Repeated configuration, `Awake`, inventory access, and runtime inventory
  recreation do not add another controller-owned sword.
- The saved and runtime-created hotbars are exactly `432 x 144.3318`.
- The first slot icon is enabled, preserves aspect ratio, uses the exact sword
  Sprite, and stays inside the hotbar bounds.
- No combat numeric defaults, projectile behavior, map definition, player
  artwork, movement, or physics implementation was changed.
- Git was not used.

## Files Changed

- `Assets/Scripts/Items/ItemHotbarController.cs`
  - Added serialized `startingSwordSprite`.
  - Added `ConfigureStartingSword(Sprite)`.
  - Ensures the exact starting sword when an inventory is created or recovered.
  - Reconnects the co-located shooter to the ensured inventory, which restores
    the runtime-only shared reference after a saved scene is reopened.
- `Assets/Scripts/Stage1ItemHotbarSetup.cs`
  - Extended `Create` with the validated sword Sprite.
  - Configures the controller before view creation.
  - Injects the exact `controller.Inventory` instance into
    `PlayerSwordShooter`.
- `Assets/Editor/Stage1SceneBuilder.cs`
  - Passes its already validated sword Sprite through the hotbar setup.
- `Assets/Scripts/Stage1RuntimeBootstrap.cs`
  - Passes its already validated sword Sprite through the hotbar setup.
- `Assets/Tests/Editor/ItemHotbarControllerTests.cs`
  - Added exact starting-item, no-duplicate, and runtime-recreation coverage.
- `Assets/Tests/Editor/Stage1SceneBuilderTests.cs`
  - Added saved-scene and runtime-created shared-inventory coverage.
  - Added exact sword/icon/containment coverage.
  - Updated the old `1728` hotbar expectation to exact `432 x 144.3318`.
- `Assets/Tests/PlayMode/ItemHotbarPlayModeTests.cs`
  - Updated startup assumptions from empty inventory to automatic sword.
  - Added held-arrow firing, other-slot blocking, and immediate first-slot
    reselection firing coverage.
- `Assets/Scenes/Stage1.unity`
  - Rebuilt with `Stage1SceneBuilder.Build`.
  - Persists the starting sword Sprite, compact hotbar, first-slot sword icon,
    and existing shooter defaults/Sprite.
- `.superpowers/sdd/2026-07-29-slot-one-sword-hotbar/task-3-report.md`
  - This report.

`PlayerInventory` and its standalone unit tests were not changed.

## Strict TDD Evidence

### Controller RED

Command scope: `ItemHotbarControllerTests`

Artifact: `Artifacts/task3-controller-red.xml`

- Result: failed as expected
- Total: `21`
- Passed: `18`
- Failed: `3`
- Expected failures:
  - exact starting sword acquisition
  - repeated configuration/access no-duplicate behavior
  - configured sword recreation after runtime model loss
- All three failures reported the missing
  `ConfigureStartingSword(Sprite)` production contract.

### Controller GREEN

Artifact: `Artifacts/task3-controller-green.xml`

- Result: passed
- Total: `21`
- Passed: `21`
- Failed: `0`

### Scene/Runtime Integration RED

Command scope: `Stage1SceneBuilderTests`

Artifact: `Artifacts/task3-scene-red.xml`

- Result: failed as expected
- Total: `7`
- Passed: `5`
- Failed: `2`
- Expected failures:
  - saved scene did not persist/configure the controller starting Sprite
  - runtime-created player inventory did not contain the automatic sword

The first GREEN attempt exposed one additional saved-scene lifecycle issue:
the controller Sprite and shooter settings were serialized, but the runtime-only
`PlayerInventory` reference inside the shooter was not. The failing integration
test reproduced this consistently. The minimal fix reconnects the co-located
shooter whenever the controller ensures its inventory.

### Scene/Runtime Integration GREEN

Artifact: `Artifacts/task3-scene-green-final.xml`

- Result: passed
- Total: `7`
- Passed: `7`
- Failed: `0`

### PlayMode RED

Command scope: `ItemHotbarPlayModeTests`

Artifact: `Artifacts/task3-playmode-red.xml`

- Result: failed as expected
- Total: `3`
- Passed: `0`
- Failed: `3`
- All three setup failures identified the missing four-argument
  `Stage1ItemHotbarSetup.Create(..., swordSprite)` contract.

### PlayMode GREEN

Artifact: `Artifacts/task3-playmode-green.xml`

- Result: passed
- Total: `3`
- Passed: `3`
- Failed: `0`

Focused firing coverage proves:

- a held right-arrow input fires immediately with slot index `0` selected;
- selecting slot index `1` blocks firing;
- reselecting slot index `0` while the arrow remains held fires immediately,
  without waiting for the previous cooldown.

## Final Test Totals

Fresh post-refactor verification:

- EditMode: `91/91` passed, `0` failed, `0` skipped
  - XML: `Artifacts/task3-final-editmode.xml`
  - Log: `Artifacts/task3-final-editmode.log`
- PlayMode: `4/4` passed, `0` failed, `0` skipped
  - XML: `Artifacts/task3-final-playmode.xml`
  - Log: `Artifacts/task3-final-playmode.log`
- Combined: `95/95` passed

## Builder Result

Command:

`Unity.exe -batchmode -nographics -quit -projectPath C:\Users\myong\NaManMoo -executeMethod Stage1SceneBuilder.Build`

Artifact: `Artifacts/task3-stage1-build.log`

- Result: success
- Log terminal evidence:
  `Batchmode quit successfully invoked - shutting down!`
- Generated scene:
  - path: `Assets/Scenes/Stage1.unity`
  - current size after the final EditMode builder verification: `116218` bytes

Unity emitted transient license-channel warnings during some launches, then
resolved the entitlement and produced complete XML/log artifacts. No run was
accepted without its terminal XML result.

## Saved YAML Inspection

Inspected `Assets/Scenes/Stage1.unity` directly after the final builder run.

- Sword asset GUID:
  `c2b45cf6255b54d4c9cb54f7cd626537`
- Serialized controller starting Sprite:
  - exact sword GUID matches: `1`
- Compact hotbar:
  - `m_SizeDelta: {x: 432, y: 144.3318}` matches: `1`
  - stale `1728` matches: `0`
- Shooter:
  - `damage: 5`
  - `shotsPerSecond: 3`
  - `projectileSpeed: 8`
  - `spinSpeed: 720`
  - `projectileLifetime: 4`
  - `spawnOffset: 0.8`
  - `swordSprite` uses the exact sword GUID
- First-slot sword icon:
  - `m_Enabled: 1`
  - exact sword GUID
  - `m_PreserveAspect: 1`
- Total sword GUID references in the scene: `3`
  - controller starting Sprite
  - shooter Sprite
  - first-slot icon Sprite

The saved-scene test also opens the generated scene and proves that the icon is
contained by the hotbar rectangle and that the shooter receives the same
inventory instance after runtime inventory reconstruction.

## Self-Review

- The controller creates the starting item only when a valid Sprite is
  configured and no item with ID `sword` already exists.
- Exact item values are literal and covered independently by tests.
- Repeated calls retain the original item object and one-sword count.
- The view is created only after the controller has its starting inventory, so
  the first icon is serialized and visible immediately.
- The builder and runtime bootstrap both reuse their validated sword Sprite;
  neither performs a second asset lookup in the setup path.
- The shared inventory assertion uses object identity, not equivalent data.
- Public setup/configuration APIs are exercised directly in final tests.
- No test-only behavior was added to production classes.
- Existing combat values and stage/player setup lines were left unchanged
  except for passing the sword Sprite into the hotbar setup.

## Concerns

No open functional concerns.

The nonserialized inventory model is intentionally recreated when a scene is
loaded. `ItemHotbarController` is the reconstruction owner and re-injects that
same object into the shooter, which is covered for both saved and
runtime-created players.
