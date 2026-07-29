### Task 3: Stage 1 Asset Wiring and Runtime Verification

Modify:
- `Assets/Editor/Stage1SceneBuilder.cs`
- `Assets/Scripts/Stage1ItemHotbarSetup.cs`
- `Assets/Scripts/Stage1RuntimeBootstrap.cs`
- `Assets/Tests/Editor/Stage1SceneBuilderTests.cs`
- `Assets/Tests/PlayMode/ItemHotbarPlayModeTests.cs`
- Regenerate `Assets/Scenes/Stage1.unity`

Asset:
- `Assets/UI/ItemUIBackground.png`

Existing interface:
- All `ItemHotbarUIFactory.Create` overloads require a non-null `Sprite`.
- `Stage1ItemHotbarSetup.Create` requires and forwards the Sprite.

Requirements:
- Editor builder loads `Assets/UI/ItemUIBackground.png` through `AssetDatabase.LoadAssetAtPath<Sprite>` and fails clearly if unavailable.
- Saved Stage1 serializes one `Background` Image using that Sprite.
- Saved/runtime hotbar contains exactly six transparent overlay slots and exactly one active selection outline.
- No `Number`, `Border`, or `Divider` objects exist.
- Keep Player, movement, canvas, Input System EventSystem, inventory, reload-safe view, and keyboard behavior.
- Runtime bootstrap uses a serialized Sprite reference.
- Add an editor-only `Reset`/`OnValidate` assignment using `AssetDatabase` so adding or loading the bootstrap in the editor receives the project Sprite without runtime filesystem/Resources loading.
- Runtime bootstrap must fail with a clear actionable error if the serialized Sprite is absent instead of creating a partial hotbar.
- Update the stale Stage1 builder test that currently dereferences `Number`.
- Stage1 tests assert the actual imported Sprite path/name on `Background`, absence of generated static art, complete-image aspect ratio, and normalized overlays.
- Play Mode tests use/provide a Sprite and verify acquisition icon refresh plus selection/equipment movement.

TDD:
1. Update integration tests first and run focused RED.
2. Implement minimal wiring/default assignment.
3. Run focused Edit and Play Mode GREEN.
4. Run `Stage1SceneBuilder.Build` and inspect the saved scene.
5. Run the full Edit and Play Mode suites and inspect logs.

Do not modify the root-level source image. Git is invalid; do not commit.
