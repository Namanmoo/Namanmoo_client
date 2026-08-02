# Dungeon Squirrel Ranged Enemy Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a five-health ranged Squirrel to Dungeon normal rooms, guarantee at least one Krab and one Squirrel per populated room, and use deterministic 50:50 selection for remaining slots.

**Architecture:** `DungeonEncounter` receives an array of normal enemy definitions and uses a pure deterministic selector before calling `EnemyFactory.Create` for each spawn point. A dedicated editor asset builder creates the Krab definition, Squirrel definition, and temporary blue projectile without rebuilding `Dungeon.unity`; the scene is patched only at the serialized definition-array field.

**Tech Stack:** Unity 6.0 (6000.5.5f1), C#, ScriptableObject, Unity AssetDatabase, NUnit, Unity Test Framework

## Global Constraints

- Squirrel body sprite is `Assets/Enemies/enemy_squirrel.png`.
- Squirrel uses `EnemyBehaviorType.ApproachAndShoot` and the existing `ApproachAndShootEnemyController`.
- Squirrel stats are health 5, movement speed 2, damage 1, range 7, interval 1.5, projectile speed 6, lifetime 3, and radius 0.2.
- The temporary projectile is a persistent blue square.
- Every populated normal room has at least one Krab and one Squirrel.
- Remaining slots use deterministic 50:50 selection from `roomSeed`.
- Dungeon boss spawning and Stage 1 Krab spawning remain unchanged.
- Tests must never call `DungeonSceneBuilder.Build()`.
- `Dungeon.unity` must retain its Hotbar and HotKey data; only the encounter definition field may change.
- Do not stage, commit, push, create branches, or otherwise modify Git state.
- Follow red-green-refactor and preserve unrelated dirty-worktree changes.

---

## File Structure

- `Assets/Scripts/Dungeon/DungeonEncounter.cs`: deterministic multi-definition selection and spawning.
- `Assets/Tests/Editor/DungeonEncounterTests.cs`: pure selection and mixed-controller spawn behavior.
- `Assets/Tests/PlayMode/DungeonBossPlayModeTests.cs`: configure both normal definitions in existing runtime setup.
- `Assets/Editor/DungeonEnemyAssetBuilder.cs`: create/update enemy definitions and temporary projectile without rebuilding a scene.
- `Assets/Tests/Editor/DungeonEnemyAssetBuilderTests.cs`: verify persistent Squirrel configuration and projectile color.
- `Assets/Enemies/DungeonSquirrel.asset`: persistent ranged Squirrel definition.
- `Assets/Enemies/TemporaryBlueProjectile.png`: persistent placeholder projectile.
- `Assets/Scenes/Dungeon.unity`: serialize Krab and Squirrel definition references.
- `Assets/Tests/Editor/DungeonSceneBuilderTests.cs`: assert both definitions are wired without invoking the scene builder.

### Task 1: Deterministic Mixed Enemy Selection

**Files:**
- Modify: `Assets/Scripts/Dungeon/DungeonEncounter.cs`
- Modify: `Assets/Tests/Editor/DungeonEncounterTests.cs`
- Modify: `Assets/Tests/PlayMode/DungeonBossPlayModeTests.cs`

**Interfaces:**
- Produces: `DungeonEncounter.Configure(EnemyDefinition[] normalEnemies, Sprite boss)`.
- Produces: `DungeonEncounter.SelectDefinitions(EnemyDefinition[] definitions, int count, int seed) : EnemyDefinition[]`.
- Consumes: `DeterministicRandom.Next(int)` and `DeterministicRandom.Shuffle<T>(T[])`.

- [ ] **Step 1: Write failing selector tests**

Add tests that create Krab and Squirrel definitions and call the wished-for
pure API:

```csharp
EnemyDefinition[] selected = DungeonEncounter.SelectDefinitions(
    new[] { krab, squirrel },
    count: 6,
    seed: 1234);
```

Assert independently observable requirements:

```csharp
Assert.That(selected, Has.Length.EqualTo(6));
Assert.That(selected, Does.Contain(krab));
Assert.That(selected, Does.Contain(squirrel));
Assert.That(
    DungeonEncounter.SelectDefinitions(
        new[] { krab, squirrel }, 6, 1234),
    Is.EqualTo(selected));
```

Add separate cases for:

- `count == 2` returns exactly one of each.
- null definitions are filtered.
- one valid definition fills every slot.
- zero count or no valid definitions returns an empty array.
- fixed seeds over several remaining slots produce both Krab and Squirrel,
  proving selection is not hardcoded to one type.

The mutation caught is removal of the guarantee, use of global
`UnityEngine.Random`, failure to filter nulls, or biased constant selection.

- [ ] **Step 2: Update the spawn integration test before production code**

Change `Spawn_NormalRoomBuildsEnemiesFromConfiguredDefinition` into
`Spawn_NormalRoomBuildsGuaranteedContactAndRangedEnemies`. Configure a contact
definition and an `ApproachAndShoot` definition with distinct sprites, spawn a
normal room whose calculated count is at least two, and assert:

```csharp
Assert.That(
    roomRootObject.GetComponentsInChildren<ChaseContactEnemyController>(),
    Has.Length.GreaterThanOrEqualTo(1));
Assert.That(
    roomRootObject.GetComponentsInChildren<ApproachAndShootEnemyController>(),
    Has.Length.GreaterThanOrEqualTo(1));
```

Assert both definitions produce five-health enemies and that instance names
start with the selected definition display names rather than the old hardcoded
`Krab`.

- [ ] **Step 3: Run RED**

Run Unity without `-quit`:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' `
  -batchmode -nographics `
  -projectPath 'C:\Users\myong\NaManMoo\Namanmoo_client' `
  -runTests -testPlatform EditMode `
  -testFilter 'DungeonEncounterTests' `
  -testResults 'C:\Users\myong\NaManMoo\Namanmoo_client\Artifacts\dungeon-squirrel-task1-red.xml' `
  -logFile 'C:\Users\myong\NaManMoo\Namanmoo_client\Artifacts\dungeon-squirrel-task1-red.log'
```

Expected: compilation fails because the array `Configure` overload and
`SelectDefinitions` do not exist.

- [ ] **Step 4: Implement minimal selection**

Replace the serialized singular field with:

```csharp
[SerializeField]
private EnemyDefinition[] normalEnemyDefinitions;
```

Implement `SelectDefinitions`:

1. Return empty for `count <= 0`.
2. Copy non-null definitions into a local list.
3. Return empty if no valid definitions exist.
4. Seed a local `DeterministicRandom`.
5. If `count >= valid.Count`, place each valid definition once.
6. Fill remaining slots with `valid[rng.Next(valid.Count)]`.
7. Shuffle the completed array with the same local RNG.

This yields exactly 50:50 choices for the two configured definitions on every
remaining slot without touching Unity's global random state.

- [ ] **Step 5: Spawn selected definitions and derive names**

Call `SelectDefinitions(normalEnemyDefinitions, spots.Count, roomSeed)`. Track
per-definition counts by stable `definition.Id`, falling back to display name
when the ID is empty. Create names with:

```csharp
string baseName = string.IsNullOrEmpty(definition.DisplayName)
    ? "Enemy"
    : definition.DisplayName;
string instanceName = $"{baseName} {nextCount}";
```

Pass each selected definition and corresponding spot to
`EnemyFactory.Create`.

- [ ] **Step 6: Adapt existing PlayMode setup**

In `DungeonBossPlayModeTests`, create one temporary contact definition and one
temporary ranged definition, call `encounter.Configure(new[] { contact,
ranged }, sprite)`, and destroy both definitions in teardown. Preserve all
boss assertions.

- [ ] **Step 7: Run GREEN**

Run `DungeonEncounterTests`, followed by
`DungeonBossPlayModeTests`, using new `task1-green` result/log paths.
Expected: all selected tests pass.

### Task 2: Persistent Squirrel and Blue Projectile Assets

**Files:**
- Create: `Assets/Editor/DungeonEnemyAssetBuilder.cs`
- Create: `Assets/Tests/Editor/DungeonEnemyAssetBuilderTests.cs`
- Create: `Assets/Enemies/DungeonSquirrel.asset`
- Create: `Assets/Enemies/DungeonSquirrel.asset.meta`
- Create: `Assets/Enemies/TemporaryBlueProjectile.png`
- Create: `Assets/Enemies/TemporaryBlueProjectile.png.meta`
- Modify: `Assets/Editor/DungeonSceneBuilder.cs`

**Interfaces:**
- Produces: `DungeonEnemyAssetBuilder.BuildDefinitions() : EnemyDefinition[]`.
- Produces persistent paths:
  - `Assets/Enemies/DungeonKrab.asset`
  - `Assets/Enemies/DungeonSquirrel.asset`
  - `Assets/Enemies/TemporaryBlueProjectile.png`
- Consumes: `DungeonEncounter.Configure(EnemyDefinition[], Sprite)`.

- [ ] **Step 1: Write failing persistent-asset test**

Create `DungeonEnemyAssetBuilderTests` and call only the asset builder, never
`DungeonSceneBuilder.Build`. Assert the returned array contains definitions
with IDs `krab` and `squirrel`. For Squirrel assert all approved values,
`ApproachAndShoot`, the imported `enemy_squirrel_0` body sprite, and a non-null
projectile sprite.

Read the projectile texture's center pixel after temporarily making the
importer readable and assert:

```csharp
Assert.That(pixel.b, Is.GreaterThan(0.9f));
Assert.That(pixel.r, Is.LessThan(0.1f));
Assert.That(pixel.g, Is.LessThan(0.1f));
```

Restore importer readability in `finally`.

- [ ] **Step 2: Run RED**

Run EditMode with filter `DungeonEnemyAssetBuilderTests`.
Expected: compilation failure because `DungeonEnemyAssetBuilder` is missing.

- [ ] **Step 3: Implement an asset-only builder**

Create `DungeonEnemyAssetBuilder` with public path constants and
`BuildDefinitions`. It must:

- Load the first `Sprite` subasset from `enemy_squirrel.png`.
- Create an 8x8 opaque blue `Texture2D`, encode it to PNG, and import it only
  when the placeholder file is missing.
- Configure its `TextureImporter` as a single Sprite with mipmaps disabled,
  point filtering, clamp wrapping, and transparency enabled.
- Create or load both `EnemyDefinition` assets.
- Configure the Krab with its existing exact values.
- Configure the Squirrel with the approved exact values.
- mark definitions dirty, save assets, and return `{ krab, squirrel }`.

`DungeonSceneBuilder.Build` calls `BuildDefinitions` and passes the returned
array to `CreateRunner`. Remove its duplicate Krab-definition builder.

- [ ] **Step 4: Run GREEN and generate assets**

Run `DungeonEnemyAssetBuilderTests`. The test produces the persistent PNG,
metadata, and Squirrel definition while never opening or saving
`Dungeon.unity`.

- [ ] **Step 5: Verify generated assets**

Run the asset test again without code changes to prove the builder is
idempotent and does not replace asset GUIDs.

### Task 3: Wire Both Definitions Without Rebuilding Dungeon

**Files:**
- Modify: `Assets/Scenes/Dungeon.unity`
- Modify: `Assets/Tests/Editor/DungeonSceneBuilderTests.cs`

**Interfaces:**
- Consumes: generated `DungeonKrab.asset.meta` and
  `DungeonSquirrel.asset.meta` GUIDs.
- Produces: serialized `normalEnemyDefinitions` array with Krab first and
  Squirrel second.

- [ ] **Step 1: Write the failing scene-reference test**

Update `DungeonSceneBuilderTests` to load both assets, open the existing scene,
and inspect `normalEnemyDefinitions`:

```csharp
SerializedProperty definitions =
    serialized.FindProperty("normalEnemyDefinitions");
Assert.That(definitions.arraySize, Is.EqualTo(2));
Assert.That(
    definitions.GetArrayElementAtIndex(0).objectReferenceValue,
    Is.SameAs(krab));
Assert.That(
    definitions.GetArrayElementAtIndex(1).objectReferenceValue,
    Is.SameAs(squirrel));
```

Do not call any builder from this test.

- [ ] **Step 2: Run RED**

Run EditMode with filter `DungeonSceneBuilderTests`.
Expected: failure because the restored scene still has the singular
`normalEnemyDefinition` field.

- [ ] **Step 3: Patch only the serialized encounter field**

Use the generated asset GUIDs to replace:

```yaml
normalEnemyDefinition: ...
```

with:

```yaml
normalEnemyDefinitions:
- {fileID: 11400000, guid: <krab-guid>, type: 2}
- {fileID: 11400000, guid: <squirrel-guid>, type: 2}
```

Do not run `DungeonSceneBuilder.Build` and do not change any other scene line.

- [ ] **Step 4: Run GREEN**

Run `DungeonSceneBuilderTests`. Expected: pass.

- [ ] **Step 5: Verify no Hotbar/HotKey scene changes**

Run:

```powershell
git diff --numstat -- Assets/Scenes/Dungeon.unity
git diff -- Assets/Scenes/Dungeon.unity
```

Expected: the scene diff is confined to the encounter definition field. There
must be no `Item Hotbar`, slot, key, UI hierarchy, or unrelated file-ID
changes.

### Task 4: Final Behavior and Regression Verification

**Files:**
- Test only; no production edits unless a failing test identifies a defect.

- [ ] **Step 1: Verify Squirrel projectile initialization**

Extend the mixed spawn integration test or add a focused test that locates the
spawned `ApproachAndShootEnemyController`, places the player inside range,
calls `TryAttack(0f)`, and asserts the resulting `EnemyProjectile`:

- uses `DungeonSquirrel.ProjectileSprite`;
- has damage 1;
- speed 6;
- lifetime 3;
- collider radius 0.2.

- [ ] **Step 2: Run relevant EditMode suite**

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' `
  -batchmode -nographics `
  -projectPath 'C:\Users\myong\NaManMoo\Namanmoo_client' `
  -runTests -testPlatform EditMode `
  -testFilter 'DungeonEncounterTests;DungeonEnemyAssetBuilderTests;DungeonSceneBuilderTests;EnemyFactoryTests;ApproachAndShootEnemyControllerTests' `
  -testResults 'C:\Users\myong\NaManMoo\Namanmoo_client\Artifacts\dungeon-squirrel-final-editmode.xml' `
  -logFile 'C:\Users\myong\NaManMoo\Namanmoo_client\Artifacts\dungeon-squirrel-final-editmode.log'
```

- [ ] **Step 3: Run Dungeon PlayMode suite**

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' `
  -batchmode -nographics `
  -projectPath 'C:\Users\myong\NaManMoo\Namanmoo_client' `
  -runTests -testPlatform PlayMode `
  -testFilter 'DungeonScenePlayModeTests;DungeonBossPlayModeTests' `
  -testResults 'C:\Users\myong\NaManMoo\Namanmoo_client\Artifacts\dungeon-squirrel-final-playmode.xml' `
  -logFile 'C:\Users\myong\NaManMoo\Namanmoo_client\Artifacts\dungeon-squirrel-final-playmode.log'
```

- [ ] **Step 4: Inspect results and scope**

Confirm both XML files report zero failures. Confirm `Dungeon.unity` still has
only the expected serialized field diff. Report modified files and verification
evidence without performing Git staging, commits, pushes, or branch operations.

