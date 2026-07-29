### Task 5: Full Verification

Verify all item hotbar production code, tests, and Stage 1 integration.

Required checks:
1. Inspect active Unity processes and project lock before launching anything.
2. If no active editor owns `C:\Users\myong\NaManMoo`, run all Edit Mode tests in Unity batch mode and save XML/logs under `Artifacts`.
3. Run all Play Mode tests in Unity batch mode and save XML/logs under `Artifacts`.
4. Inspect logs for `error CS`, `NullReferenceException`, `MissingReferenceException`, and `AssertionException`.
5. Rebuild Stage 1 through `Stage1SceneBuilder.Build` if Unity is available, then inspect `Assets/Scenes/Stage1.unity` for controller, canvas, hotbar, six slots, labels, icons, and selection outlines.
6. If the project remains locked, do not kill or interfere with a user-owned Unity editor. Perform the strongest available static review/compilation check and document exactly which authoritative checks remain blocked.
7. Do not modify production behavior merely to make verification easier.

Expected outcome:
- All Edit Mode tests pass with zero failures.
- All Play Mode tests pass with zero failures.
- No relevant compile/runtime errors in logs.
- Saved Stage 1 contains the required serialized item hotbar hierarchy.

Report exact commands, outputs, artifact paths, and limitations in `task-5-report.md`.
