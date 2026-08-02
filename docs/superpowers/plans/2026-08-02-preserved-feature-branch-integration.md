# Preserved Feature Branch Integration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Merge the outstanding client and backend feature branches into `main` while preserving the requested player animation, outdoor map assets and generation, faster movement values, and all newly added dungeon enemies.

**Architecture:** Use clean integration worktrees based on the current remote `main` branches. Merge the outdoor-ground branch first because it already contains the player-animation branch, retain current `main` values for player/enemy speed and enemy definitions, then merge the weapon-forge metadata fix with explicit Unity GUID resolution. Merge the independent backend stage-slider branch separately and fast-forward each local `main` only after validation.

**Tech Stack:** Unity 6.0 (`6000.5.5f1`), C#, Unity Test Framework/NUnit, Python 3.13, FastAPI, pytest, Git/GitHub.

## Global Constraints

- Preserve the five player sword-idle sprites named `프레임0000.png` through `프레임0004.png` and their looping animation/controller.
- Preserve all `Grass` and `Dirt` Stage 1 ground texture assets and their Unity metadata.
- Preserve dungeon layout generation, room construction, navigation, and door generation while adding outdoor room geometry.
- Keep the faster dungeon player scene value `moveSpeed: 10` from current `main`.
- Keep the faster dungeon enemy definitions from current `main`: Krab `moveSpeed: 5`, Squirrel `moveSpeed: 4`, and keep both enemy definitions wired into `DungeonEncounter`.
- Do not modify either pre-existing dirty client worktree.
- Push only after all relevant checks pass; the user explicitly authorized commits and direct `main` pushes.

---

### Task 1: Integrate Client Feature Branches

**Files:**
- Merge: `feat/stage1-outdoor-ground`
- Merge: `origin/feat/weapon-forge`
- Verify ancestor: `feat/player-animation`
- Verify ancestor: `origin/feat/webgl-build`
- Resolve: `Assets/Plugins.meta`
- Resolve: `Assets/Scripts/UI/ScreenRectOf.cs.meta`
- Resolve: `Assets/Scripts/UI/WebTextInput.cs.meta`
- Resolve: `Assets/Tests/Editor/ScreenRectOfTests.cs.meta`

**Interfaces:**
- Consumes: current client `main` at `694765d`, outdoor branch at `5f120d3`, weapon metadata commit at `976b612`.
- Produces: one client integration branch containing all unique commits with valid Unity GUID references.

- [ ] **Step 1: Merge the outdoor-ground branch with history preserved**

```powershell
git merge --no-ff feat/stage1-outdoor-ground -m "Merge outdoor dungeon and player animation"
```

- [ ] **Step 2: Verify the player-animation branch is now an ancestor**

```powershell
git merge-base --is-ancestor feat/player-animation HEAD
```

Expected: exit code `0`.

- [ ] **Step 3: Merge the weapon-forge metadata fix and stop on its four known add/add conflicts**

```powershell
git merge --no-ff origin/feat/weapon-forge -m "Merge weapon forge metadata fix"
```

Expected: conflicts only in the four `.meta` files listed above.

- [ ] **Step 4: Preserve the weapon branch GUIDs and finish the merge commit**

```powershell
git checkout --theirs -- Assets/Plugins.meta Assets/Scripts/UI/ScreenRectOf.cs.meta Assets/Scripts/UI/WebTextInput.cs.meta Assets/Tests/Editor/ScreenRectOfTests.cs.meta
git add Assets/Plugins.meta Assets/Scripts/UI/ScreenRectOf.cs.meta Assets/Scripts/UI/WebTextInput.cs.meta Assets/Tests/Editor/ScreenRectOfTests.cs.meta
git commit --no-edit
```

Expected: `Assets/Scripts/UI/WebTextInput.cs.meta` contains GUID `e370f83985c374330835d4bec0ce0259`, matching `Assets/Scenes/WeaponForge.unity`.

- [ ] **Step 5: Confirm the WebGL branch is already contained**

```powershell
git merge-base --is-ancestor origin/feat/webgl-build HEAD
```

Expected: exit code `0`; no merge commit is needed.

### Task 2: Verify the Preserved Client Features

**Files:**
- Verify: `Assets/Player/Animation/Sword/Idle/Down/Frames/프레임0000.png` through `프레임0004.png`
- Verify: `Assets/Animation/Sword/Idle/Down/Player_SwordIdle_Down.anim`
- Verify: `Assets/Animation/Sword/Idle/Down/Player Visual.controller`
- Verify: `Assets/Resources/Stage1/Ground/Grass_Base_01.png`
- Verify: `Assets/Resources/Stage1/Ground/Dirt_Path_Horizontal_01.png`
- Verify: `Assets/Resources/Stage1/Ground/Dirt_Path_Vertical_01.png`
- Verify: `Assets/Scripts/Dungeon/OutdoorRoomGeometry.cs`
- Verify: `Assets/Scripts/Dungeon/RoomBuilder.cs`
- Verify: `Assets/Scenes/Dungeon.unity`
- Verify: `Assets/Enemies/DungeonKrab.asset`
- Verify: `Assets/Enemies/DungeonSquirrel.asset`

**Interfaces:**
- Consumes: merged Unity assets, scene YAML, and existing editor/play-mode tests.
- Produces: evidence that each user-requested resource and runtime value survived integration.

- [ ] **Step 1: Check asset presence, animation frames, loop rate, scene controller, speed values, enemy definitions, and GUID integrity**

```powershell
$frames = Get-ChildItem 'Assets/Player/Animation/Sword/Idle/Down/Frames' -Filter '프레임000?.png'
if ($frames.Count -ne 5) { throw "Expected five animation frames" }
rg -n 'm_SampleRate: 5|m_LoopTime: 1' 'Assets/Animation/Sword/Idle/Down/Player_SwordIdle_Down.anim'
rg -n 'moveSpeed: 10' 'Assets/Scenes/Dungeon.unity'
rg -n 'moveSpeed: 5' 'Assets/Enemies/DungeonKrab.asset'
rg -n 'moveSpeed: 4' 'Assets/Enemies/DungeonSquirrel.asset'
rg -n 'e370f83985c374330835d4bec0ce0259' 'Assets/Scenes/WeaponForge.unity' 'Assets/Scripts/UI/WebTextInput.cs.meta'
```

- [ ] **Step 2: Run focused EditMode tests for tiles, outdoor geometry, dungeon generation, doors, enemy definitions, and factories**

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath $PWD -runTests -testPlatform EditMode -testFilter 'Stage1GroundAssetTests;Stage1DirtPathAssetTests;OutdoorRoomGeometryTests;RoomBuilderOutdoorTests;DungeonLayoutTests;DungeonNavigationTests;DungeonEnemyAssetBuilderTests;EnemyDefinitionTests;EnemyFactoryTests' -testResults 'C:\Users\dksco\Naman\tmp\merge-integration-validation\client-focused-editmode.xml' -logFile 'C:\Users\dksco\Naman\tmp\merge-integration-validation\client-focused-editmode.log'
```

Expected: all selected tests pass with zero failures.

- [ ] **Step 3: Run the complete EditMode test suite**

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath $PWD -runTests -testPlatform EditMode -testResults 'C:\Users\dksco\Naman\tmp\merge-integration-validation\client-final-editmode.xml' -logFile 'C:\Users\dksco\Naman\tmp\merge-integration-validation\client-final-editmode.log'
```

- [ ] **Step 4: Run the complete PlayMode test suite**

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath $PWD -runTests -testPlatform PlayMode -testResults 'C:\Users\dksco\Naman\tmp\merge-integration-validation\client-final-playmode.xml' -logFile 'C:\Users\dksco\Naman\tmp\merge-integration-validation\client-final-playmode.log'
```

Expected: both complete suites pass with zero failures.

### Task 3: Integrate and Verify the Backend Stage Slider

**Files:**
- Merge: `origin/feat/stage-slider`
- Verify: `app/`
- Test: `tests/`

**Interfaces:**
- Consumes: backend `main` at `7d0317c` and stage-slider branch at `f856b32`.
- Produces: the single-stage forge API, image-provider pipeline, weapon storage API, and its passing Python test suite.

- [ ] **Step 1: Merge the backend feature branch**

```powershell
git merge --no-ff origin/feat/stage-slider -m "Merge stage slider backend"
```

- [ ] **Step 2: Run the backend tests in the isolated virtual environment**

```powershell
& '.\.venv\Scripts\python.exe' -m pytest -q
```

Expected: all tests pass with zero failures.

### Task 4: Publish the Validated Main Branches

**Files:**
- Update ref: client `main`
- Update ref: backend `main`

**Interfaces:**
- Consumes: clean, validated integration branches.
- Produces: updated `origin/main` branches for both GitHub repositories.

- [ ] **Step 1: Inspect both integration diffs and statuses**

```powershell
git status -sb
git log --oneline --decorate main..HEAD
git diff --stat main...HEAD
```

- [ ] **Step 2: Fast-forward the client local main to its integration branch**

```powershell
git branch -f main HEAD
```

- [ ] **Step 3: Fast-forward the checked-out backend main from its clean primary worktree**

```powershell
git -C 'C:\Users\dksco\Naman\Namanmoo_Backend' merge --ff-only agent/integrate-stage-slider
```

- [ ] **Step 4: Push the validated commits to the corresponding remote main branches**

```powershell
git push origin main:main
```

- [ ] **Step 5: Verify each remote main SHA and working-tree cleanliness**

```powershell
git ls-remote --heads origin main
git status -sb
```

Expected: the remote `main` SHA matches the validated integration commit and the integration worktree is clean.
