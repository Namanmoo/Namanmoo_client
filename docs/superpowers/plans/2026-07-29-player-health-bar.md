# Player Health Bar Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a compact, cartoon-style, top-left player health bar that responds to real `PlayerHealth` damage and temporary `H`-key test damage.

**Architecture:** `PlayerHealth` owns health state and emits change events. `PlayerHealthBarView` renders that state, while a focused UI factory and Stage 1 setup class construct and connect the screen-space UI in both scene-building paths.

**Tech Stack:** Unity 6, C#, uGUI, Input System, NUnit Unity Test Framework

## Global Constraints

- Default and maximum health are 20.
- The health bar is 260 by 54 reference pixels at a 1920 by 1080 reference resolution.
- The top-left inset is 24 reference pixels.
- `HP_heart.png` is the left-side icon.
- Zero health leaves the player present and displays `0/20`.
- `H` applies one point of temporary test damage.

---

### Task 1: Player Health Domain

**Files:**
- Create: `Assets/Scripts/Player/PlayerHealth.cs`
- Create: `Assets/Tests/Editor/PlayerHealthTests.cs`

**Interfaces:**
- Produces: `int CurrentHealth`, `int MaxHealth`, `event Action<int,int> HealthChanged`, and `void TakeDamage(int amount)`.

- [ ] **Step 1: Write failing health tests**

Create tests that instantiate a real `GameObject`, add `PlayerHealth`, verify the initial `20/20`, apply 5 damage and observe `15/20`, verify zero/negative damage is ignored, verify lethal damage clamps to zero without destroying the object, and verify `HealthChanged` reports the resulting values.

- [ ] **Step 2: Run the focused Edit Mode tests**

Run Unity Edit Mode tests to `Artifacts/player-health-red.xml`. Expected: compilation/test failure because `PlayerHealth` does not exist.

- [ ] **Step 3: Implement the health component**

Implement serialized `maxHealth = 20`, initialize current health in `Awake`, clamp positive damage with `Mathf.Max`, and invoke `HealthChanged(CurrentHealth, MaxHealth)` only when the health value changes.

- [ ] **Step 4: Run the focused Edit Mode tests**

Run the same tests to `Artifacts/player-health-green.xml`. Expected: all `PlayerHealthTests` pass.

### Task 2: Dynamic Cartoon Health UI

**Files:**
- Create: `Assets/Scripts/UI/PlayerHealthBarView.cs`
- Create: `Assets/Scripts/UI/PlayerHealthBarUIFactory.cs`
- Create: `Assets/Tests/Editor/PlayerHealthBarViewTests.cs`

**Interfaces:**
- Consumes: `PlayerHealth`.
- Produces: `PlayerHealthBarView Initialize(PlayerHealth health, Text label, RectTransform fill)`, `Connect()`, and `Disconnect()`.
- Produces: `PlayerHealthBarUIFactory.Create(Transform parent, PlayerHealth health, Sprite heartSprite)`.

- [ ] **Step 1: Write failing UI tests**

Create a real UI hierarchy through the desired factory. Assert the root anchors and pivot are `(0,1)`, position is `(24,-24)`, size is `(260,54)`, the `Heart` image uses the supplied sprite, the initial label is `20/20`, and five damage changes the label to `15/20` and fill width ratio to `0.75`.

- [ ] **Step 2: Run the focused Edit Mode tests**

Run Unity Edit Mode tests to `Artifacts/health-ui-red.xml`. Expected: compilation/test failure because the view and factory do not exist.

- [ ] **Step 3: Implement the view and factory**

Build separate heart, label, cream track, dark four-edge outline, and red fill objects. Use `Image.Type.Filled` with horizontal fill and set `fillAmount` from the health ratio. Use Unity's built-in Arial font, dark text, red fill, cream track, and non-interactive raycast settings. Subscribe/unsubscribe safely and render immediately on connection.

- [ ] **Step 4: Run the focused Edit Mode tests**

Run the same tests to `Artifacts/health-ui-green.xml`. Expected: all view tests pass.

### Task 3: Stage Integration and Test Input

**Files:**
- Create: `Assets/Scripts/Stage1PlayerHealthSetup.cs`
- Modify: `Assets/Editor/Stage1SceneBuilder.cs`
- Modify: `Assets/Scripts/Stage1RuntimeBootstrap.cs`
- Modify: `Assets/Tests/Editor/Stage1SceneBuilderTests.cs`
- Create: `Assets/Tests/PlayMode/PlayerHealthBarPlayModeTests.cs`

**Interfaces:**
- Consumes: `Sprite heartSprite`.
- Produces: `Stage1PlayerHealthSetup.Create(GameObject player, Transform canvasParent, Sprite heartSprite)`.

- [ ] **Step 1: Import the heart sprite**

Copy `HP_heart.png` to `Assets/UI/HP_heart.png`; let Unity generate metadata, then configure it as a single, transparent, clamped, non-mipmapped Sprite through the editor builder/import path.

- [ ] **Step 2: Write failing integration tests**

Extend scene-builder coverage to require `PlayerHealth`, `Player Health Canvas`, the exact sprite asset, scaler settings, and top-left layout. Add a Play Mode test that applies damage to the real health component and verifies `15/20` plus `0.75` fill.

- [ ] **Step 3: Run integration tests red**

Run Edit Mode and Play Mode tests to `Artifacts/health-integration-red-*.xml`. Expected: failure because setup is not integrated.

- [ ] **Step 4: Implement setup and both construction paths**

Create a screen-space overlay canvas with `ScaleWithScreenSize`, reference resolution `(1920,1080)`, and `matchWidthOrHeight = 0`. Add `PlayerHealth`, add an Input System `Keyboard.current.hKey.wasPressedThisFrame` check that calls `TakeDamage(1)`, create the UI, and load/validate the heart sprite in both `Stage1SceneBuilder` and `Stage1RuntimeBootstrap`.

- [ ] **Step 5: Rebuild Stage 1 and run all tests green**

Invoke `Stage1SceneBuilder.Build`, then run relevant Edit Mode and Play Mode suites. Expected: scene rebuild succeeds, all tests pass, and logs contain no compiler errors or exceptions.

### Task 4: Visual Verification

**Files:**
- Verify: `Assets/Scenes/Stage1.unity`
- Verify: `Artifacts/`

- [ ] **Step 1: Open or capture Stage 1 in Play Mode**

Confirm the UI is visibly compact, top-left aligned, uses the heart sprite, and resembles the cream/red/dark cartoon reference.

- [ ] **Step 2: Verify damage interaction**

Press `H` five times and confirm the text reaches `15/20` while the bar reaches 75%; continue to zero and confirm the player remains.

- [ ] **Step 3: Review changed files**

Confirm changes are limited to the approved health/UI feature and generated Unity asset metadata.
