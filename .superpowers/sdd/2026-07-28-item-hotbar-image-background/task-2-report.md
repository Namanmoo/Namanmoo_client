# Task 2 Report: Image-Backed Overlay Layout

## Result

Implemented the image-backed hotbar layout without changing Stage 1 integration. The factory now accepts a caller-provided background `Sprite`, renders it once as `Background`, and layers six transparent slot roots over the complete 2170 x 725 source image.

## Files changed

- `Assets/Scripts/Items/ItemHotbarView.cs`
  - Added `BackgroundWidth`, `BackgroundHeight`, and centralized `SlotOverlayRects`.
  - Left serialized controller/icon/selection arrays and lifecycle reconnect/subscription behavior unchanged.
- `Assets/Scripts/Items/ItemHotbarUIFactory.cs`
  - Added `Create` overloads accepting a `Sprite backgroundSprite`.
  - Added one aspect-preserving `Background` Image, removed generated border/divider/number construction, and anchors each slot from the centralized normalized rectangles.
  - Does not use `Resources.Load` or filesystem loading.
- `Assets/Tests/Editor/ItemHotbarViewTests.cs`
  - Added image asset, background, dimensions, normalized overlay, no-static-artwork, selection, and icon coverage.

## Inspected source-image coordinates

The edited `Assets/UI/ItemUIBackground.png` was visually inspected at original resolution. It remains the full 2170 x 725 canvas, including the white whitespace above and below the hand-drawn strip. The drawn slot boxes use top-origin image rows 250 through 568.

Unity anchors are bottom-origin, so the common vertical range is:

| Anchor | Normalized value |
| --- | ---: |
| `yMin = (725 - 568) / 725` | 0.216552 |
| `yMax = (725 - 250) / 725` | 0.655172 |

| Slot | `xMin` | `xMax` | Normalized horizontal range |
| --- | ---: | ---: | --- |
| 1 | 31 | 430 | 0.014286 to 0.198157 |
| 2 | 430 | 803 | 0.198157 to 0.370046 |
| 3 | 803 | 1161 | 0.370046 to 0.535023 |
| 4 | 1161 | 1482 | 0.535023 to 0.682949 |
| 5 | 1482 | 1797 | 0.682949 to 0.828111 |
| 6 | 1797 | 2110 | 0.828111 to 0.972350 |

## RED evidence

Command:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Users\myong\NaManMoo' -runTests -testPlatform EditMode -testFilter 'ItemHotbarViewTests' -testResults 'C:\Users\myong\NaManMoo\Artifacts\task2-red.xml' -logFile 'C:\Users\myong\NaManMoo\Artifacts\task2-red.log'; exit $LASTEXITCODE
```

Result: expected compile RED. `Assets/Tests/Editor/ItemHotbarViewTests.cs(26,36)` reported `CS1501: No overload for method 'Create' takes 3 arguments`. The XML was not produced because the test assembly could not compile. The complete compiler evidence is in `Artifacts/task2-red.log`.

## GREEN evidence

Command:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Users\myong\NaManMoo' -runTests -testPlatform EditMode -testFilter 'ItemHotbarViewTests' -testResults 'C:\Users\myong\NaManMoo\Artifacts\task2-green.xml' -logFile 'C:\Users\myong\NaManMoo\Artifacts\task2-green.log'; exit $LASTEXITCODE
```

Result: passed. `Artifacts/task2-green.xml` records `total=12`, `passed=12`, `failed=0`, with Unity reporting `Test run completed. Exiting with code 0 (Ok).`

## Self-review

- `Background` is created before overlays, fills the hotbar rect, uses the supplied Sprite, preserves aspect, and has raycasts disabled.
- The hotbar rect has the exact source-image dimensions, so aspect preservation cannot crop the supplied image.
- No `Border`, `Divider`, or `Number` game objects are generated.
- `Slot 1` through `Slot 6` are transparent, use centralized normalized anchors, and each owns `Icon` plus `Selection Outline`.
- Existing dynamic icon refresh, aspect preservation, selection exclusivity, and lifecycle reconnection tests pass.
- No Stage 1 builder/setup/scene files were changed and no commit was attempted.

## Original handoff (superseded by fix round 1)

The initial two-argument factory compatibility overloads were subsequently removed. Fix round 1 below propagates the imported Sprite through the current Stage 1 callers so the compile path no longer permits a null background.

---

## Fix round 1: Required background Sprite API

### Changes

- Removed both public two-argument `ItemHotbarUIFactory.Create` overloads.
- The only public factory overloads now accept `(Transform, PlayerInventory, Sprite)` or `(Transform, ItemHotbarController, Sprite)` and throw `ArgumentNullException` with parameter name `backgroundSprite` when it is null.
- `Stage1ItemHotbarSetup.Create` now requires and validates the Sprite before creating UI.
- `Stage1SceneBuilder` loads `Assets/UI/ItemUIBackground.png` through `AssetDatabase.LoadAssetAtPath<Sprite>` and passes it to the setup.
- `Stage1RuntimeBootstrap` now has a serialized `Sprite itemHotbarBackground` field and passes it into shared setup.
- Updated the Play Mode fixture to supply a test Sprite so its source compiles against the required setup API.
- Added focused Edit Mode tests proving null rejection and that every public `Create` method is a three-parameter Sprite-requiring overload.

### RED evidence

Command:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Users\myong\NaManMoo' -runTests -testPlatform EditMode -testFilter 'ItemHotbarViewTests' -testResults 'C:\Users\myong\NaManMoo\Artifacts\task2-fix1-red.xml' -logFile 'C:\Users\myong\NaManMoo\Artifacts\task2-fix1-red.log'; exit $LASTEXITCODE
```

Result: `total=14`, `passed=12`, `failed=2`. The two intended failures were `Create_RejectsANullBackgroundSprite` (no exception) and `Factory_RequiresABackgroundSpriteForEveryPublicCreateOverload` (a public `Create` method had two parameters). XML: `Artifacts/task2-fix1-red.xml`.

### GREEN status

The requested GREEN command was started with `Artifacts/task2-fix1-green.xml` and `Artifacts/task2-fix1-green.log` destinations, but the parent interrupted the command before it produced either artifact. A subsequent process/artifact check found no Unity process and no XML/log output. Per the parent instruction, no further long Unity command was started.

### Review / concern

All known factory and setup call sites now pass a Sprite, and no `Resources.Load` or root-filesystem loading was introduced. The Stage 1 editor/runtime propagation is intentionally minimal for compilation continuity and remains subject to Task 3's full scene/runtime review. Focused GREEN verification is the sole outstanding item.
