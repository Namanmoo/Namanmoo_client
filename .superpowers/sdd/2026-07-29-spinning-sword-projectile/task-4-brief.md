# Task 4: Integrate with Stage1 and Verify Regressions

## Requirements

- Modify only as needed:
  - `Assets/Editor/Stage1SceneBuilder.cs`
  - `Assets/Scripts/Stage1RuntimeBootstrap.cs`
  - `Assets/Tests/Editor/Stage1SceneBuilderTests.cs`
  - generated `Assets/Scenes/Stage1.unity`
- Use exact asset path `Assets/Weapons/sword.png`.
- `Stage1SceneBuilder` loads the Sprite, fails early with an actionable message
  containing the path when absent, adds `PlayerSwordShooter` to Player, and
  assigns its `SwordSprite`.
- Saved player uses shooter defaults, including damage exactly 5 and positive
  shots per second. Add read-only configuration properties to
  `PlayerSwordShooter` only if scene tests need them.
- `Stage1RuntimeBootstrap` adds serialized `Sprite swordSprite`, populates it in
  editor `Reset`/`OnValidate` beside the other Sprites, validates it before
  building, throws an actionable missing-path error, adds `PlayerSwordShooter`
  to runtime Player, and assigns the same Sprite.
- Extend scene tests to verify:
  - sword asset loads as a non-null Sprite;
  - player has PlayerSwordShooter;
  - exact Sprite reference/path;
  - default damage 5;
  - shots per second is positive;
  - runtime editor validation assigns `swordSprite`;
  - missing runtime sword Sprite rejects before creating Generated Stage and
    message contains the exact path.
- Preserve all existing player movement, player visual, Rigidbody2D,
  CircleCollider2D, hotbar, map 2.5x scale, and camera behavior.
- Follow strict TDD: focused Stage1SceneBuilderTests RED before integration and
  GREEN after integration.
- Rebuild Stage1 via `Stage1SceneBuilder.Build` and confirm exit code 0.
- Inspect saved YAML for player shooter, damage 5, positive rate, and exact
  sword Sprite GUID.
- Run focused combat tests, full EditMode, and full PlayMode suites. Record
  exact totals and scan logs for compiler errors, NullReferenceException,
  MissingReferenceException, and assertion failures.

## Existing Interfaces

- `PlayerSwordShooter.SwordSprite` has public get/set.
- `PlayerSwordShooter` serialized defaults are damage 5 and shotsPerSecond 3.
- The sword asset/importer and focused Task 3 tests are complete.

## Report Contract

Write `.superpowers/sdd/2026-07-29-spinning-sword-projectile/task-4-report.md`
with status, files changed, focused RED/GREEN, builder result, focused combat
totals, full EditMode/PlayMode totals, YAML inspection, self-review, and
concerns.
