# Task 3 Report: Stage 1 Asset Wiring and Runtime Verification

## Outcome

Implementation and integration-test updates were made, but Unity verification and scene regeneration were intentionally stopped when the parent task directed that no additional Unity commands be run. The checked-in `Assets/Scenes/Stage1.unity` therefore remains the old serialized scene and is not Task 3 complete.

## Changes made

- `Assets/Scripts/Stage1RuntimeBootstrap.cs`
  - Adds the serialized hotbar background asset path constant.
  - Adds editor-only `Reset` and `OnValidate` hooks that assign the project Sprite through `AssetDatabase.LoadAssetAtPath<Sprite>`.
  - Validates the serialized Sprite before creating `Generated Stage`; a missing Sprite now throws an actionable `InvalidOperationException` before a partial runtime hotbar/stage can be created.
- `Assets/Tests/Editor/Stage1SceneBuilderTests.cs`
  - Replaces the stale `Number` dereference with integration assertions for the imported background Sprite, asset path/name, aspect ratio, transparent normalized slot overlays, static-art absence, and exactly one active outline.
  - Adds coverage of editor assignment and missing-Sprite early failure for `Stage1RuntimeBootstrap`.
- `Assets/Tests/PlayMode/ItemHotbarPlayModeTests.cs`
  - Verifies that runtime setup uses exactly one supplied background Sprite while retaining the existing acquisition icon-refresh and selection/equipment tests.

Already-present propagated wiring was preserved:

- `Stage1SceneBuilder` loads `Assets/UI/ItemUIBackground.png` through `AssetDatabase.LoadAssetAtPath<Sprite>` and forwards it to setup.
- `Stage1ItemHotbarSetup` requires a non-null Sprite and forwards it to the factory.

The root source image was not modified.

## TDD evidence

### RED attempted

Command (intentionally without `-quit`):

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Users\myong\NaManMoo' -runTests -testPlatform EditMode -testFilter 'Stage1SceneBuilderTests' -testResults 'C:\Users\myong\NaManMoo\Artifacts\task3-red-editmode.xml' -logFile 'C:\Users\myong\NaManMoo\Artifacts\task3-red-editmode.log'; exit $LASTEXITCODE
```

First sandboxed attempt returned immediately with exit code 0 and produced neither XML nor log. The escalated retry was interrupted by the user after 730.2 seconds. It also left no `task3-red-editmode.xml` or `task3-red-editmode.log`. Consequently no valid RED result exists.

### GREEN

Not run. The parent directed that no additional Unity commands be issued after the interrupted invocation.

## Scene and log inspection

- `Assets/Scenes/Stage1.unity` was inspected as text. It still contains generated `Number`, `Divider`, and `Border` objects, confirming that it has not been regenerated from the current builder/factory behavior.
- No Task 3 Unity test XML or log exists to inspect.
- `Stage1SceneBuilder.Build` was not run after the stop direction, so the expected regenerated `Background` image serialization cannot be claimed.

## Required remaining verification

1. Run focused EditMode tests and capture a real RED/GREEN XML/log result.
2. Run focused PlayMode tests with a real XML/log result.
3. Run `Stage1SceneBuilder.Build` (without `-quit` as directed), then inspect the saved `Stage1.unity` for the imported Sprite and absence of `Number`, `Border`, and `Divider`.
4. Run the full EditMode and PlayMode suites and inspect both logs.

## Files changed in this task

- `Assets/Scripts/Stage1RuntimeBootstrap.cs`
- `Assets/Tests/Editor/Stage1SceneBuilderTests.cs`
- `Assets/Tests/PlayMode/ItemHotbarPlayModeTests.cs`
- `.superpowers/sdd/2026-07-28-item-hotbar-image-background/task-3-report.md`

## Self-review and concerns

- The runtime bootstrap editor API is enclosed in `#if UNITY_EDITOR`, so player builds do not depend on `UnityEditor` and do not perform filesystem or Resources loading.
- The preflight check occurs before `BuildStage`, so the missing-asset path cannot create `Generated Stage` or a partial hotbar.
- The current saved scene is knowingly stale. This is the material blocker to Task 3 completion, alongside missing Unity test evidence.
- No git operation or source-image modification was performed.

## Compiler fix evidence (round 1/5)

- Parent-reported compiler RED: `Stage1SceneBuilderTests.cs` imported `System` for reflection exceptions, which made the unqualified `Object` calls ambiguous between `System.Object` and `UnityEngine.Object` (`CS0104`).
- Fixed only the two affected calls by fully qualifying `UnityEngine.Object.FindObjectsByType` and `UnityEngine.Object.DestroyImmediate`.
- No Unity command was run in this round; compilation and test verification remain delegated to the parent rerun.

## Importer preservation fix evidence (round 2/5)

- Parent-reported EditMode result: 58 total, 57 passed, 1 failed. The hotbar aspect assertion saw a 2.994152 imported Sprite ratio instead of the 2.993103 source ratio.
- Root cause: the image importer capped the 2170x725 source at 2048 and applied compressed platform settings, introducing rounded imported dimensions.
- Updated `Assets/UI/ItemUIBackground.png.meta` to use a 4096 maximum size and uncompressed texture settings for DefaultTexturePlatform, Standalone, and WebGL. The root PNG itself was not changed.
- Updated `Stage1SceneBuilderTests` to require imported texture dimensions of exactly 2170x725 plus importer maximum size of at least 4096 and `TextureImporterCompression.Uncompressed`. The aspect tolerance is unchanged.
- No Unity command was run in this round; import, compilation, and test verification remain delegated to the parent rerun.
