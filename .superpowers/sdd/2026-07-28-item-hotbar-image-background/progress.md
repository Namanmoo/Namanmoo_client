# SDD ledger — plan: docs/superpowers/plans/2026-07-28-item-hotbar-image-background.md

Setup: Git worktree unavailable because `.git` is not recognized as a repository. Work proceeds in the current workspace with task-scoped reports and reviews.

Task 1: minor (deferred): report describes 24-bit RGB while independent metadata reports 32-bit ARGB; all pixels are opaque.
Task 1: complete (image/spec review approved; 2170x725, blue outline removed, original source preserved)
Task 2: fix round 1/5 (required non-null Sprite factory API addressed; focused tests 14/14 passed)
Task 2: Important forwarded to Task 3: Stage1SceneBuilderTests still expects removed Number objects and saved scene is stale.
Task 2: complete (forwarded integration issue addressed by Task 3)
Task 3: fix round 1/5 (CS0104 test ambiguity addressed)
Task 3: fix round 2/5 (2170x725 source-preserving 4096/uncompressed import addressed)
Task 3: minor (deferred): task report still describes pre-verification stale scene state.
Task 3: complete (builder exit 0; EditMode 58/58; PlayMode 2/2; review clean)
Final review fix: complete (display fitted to 1728x577.327 on 1920 reference canvas; slot overlays moved inside artwork-safe bounds; Sprite importer set to Single)
Final verification: complete (Stage1 rebuilt with exit 0; focused EditMode 15/15; full EditMode 59/59; PlayMode 2/2; final review approved)
