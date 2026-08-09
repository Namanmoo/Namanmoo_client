# EnemySpawnMarker Fixed Definition Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let a designer optionally pin a specific `EnemyDefinition` to an
individual `EnemySpawnMarker`, so a `RoomContentTemplate` can mix
designer-chosen enemies with the existing random pool selection.

**Architecture:** `EnemySpawnMarker` gains an optional `EnemyDefinition`
reference. `RoomContentTemplate`'s marker accessor is renamed from
`SpawnMarkerPositions()` to `SpawnMarkers()` and now returns the marker
components themselves (position + fixed definition together) instead of raw
positions. `DungeonEncounter.SpawnEnemies` resolves each marker's definition
individually: fixed markers use their own definition, unfixed markers pull
from the existing `SelectDefinitions` random pool.

**Tech Stack:** Unity (C#), NUnit EditMode tests (`Assets/Tests/Editor`).

## Global Constraints

- Only `RoomKind.Normal` rooms are affected — this plan does not touch the
  `room.Kind != RoomKind.Normal` guard already in `SpawnEnemies`
  (`Assets/Scripts/Dungeon/DungeonEncounter.cs:187-190`).
- A marker's `FixedEnemyDefinition` being `null` means "use the random
  pool," so every template authored before this change (including
  `Assets/Dungeon/RoomTemplates/NormalRoomTemplate_Example.prefab`) keeps
  working with no changes required.
- Do not change any existing balance values (health, damage, speed, etc.) on
  any real enemy/player/boss definition or component. New numeric literals
  introduced purely as new test-fixture values (e.g. a `MaxHealth` on a
  `ScriptableObject` created only inside a new test) are not "balance
  values" in this sense — they are invented for that test's own assertions,
  not edits to an existing tuned value.
- `DungeonLayout`, `RoomShape`, `RoomBuilder`, `IRoomEncounter`,
  `DungeonRunner`, `RoomSpawnPoints` must not be modified.
- Spec: `docs/superpowers/specs/2026-08-08-enemy-spawn-marker-fixed-definition-design.md`

---

### Task 1: EnemySpawnMarker fixed definition + RoomContentTemplate.SpawnMarkers()

**Files:**
- Modify: `Assets/Scripts/Dungeon/EnemySpawnMarker.cs`
- Modify: `Assets/Scripts/Dungeon/RoomContentTemplate.cs`
- Modify: `Assets/Tests/Editor/RoomContentTemplateTests.cs`

**Interfaces:**
- Produces: `NaManMoo.Dungeon.EnemySpawnMarker.FixedEnemyDefinition : EnemyDefinition`
  (public getter, `null` by default), `NaManMoo.Dungeon.EnemySpawnMarker.Configure(EnemyDefinition
  definition)` (public instance method, sets the field — same
  test/scene-builder-friendly pattern as `DungeonEncounter.Configure` and
  `EnemyDefinition.Configure` elsewhere in this codebase), and
  `NaManMoo.Dungeon.RoomContentTemplate.SpawnMarkers() : List<EnemySpawnMarker>`
  (replaces `SpawnMarkerPositions() : List<Vector2>` — no callers outside
  this codebase's own `DungeonEncounter` and its tests reference the old
  name, so this is a clean rename, not a compatibility-preserving addition).

- [ ] **Step 1: Write the failing tests**

Replace the full contents of `Assets/Tests/Editor/RoomContentTemplateTests.cs`
with:

```csharp
using System.Collections.Generic;
using NaManMoo.Dungeon;
using NUnit.Framework;
using UnityEngine;

public sealed class RoomContentTemplateTests
{
    [Test]
    public void SpawnMarkers_ReturnsEveryMarkerChildWithWorldPosition()
    {
        var root = new GameObject("Template");
        RoomContentTemplate template = root.AddComponent<RoomContentTemplate>();
        root.transform.position = new Vector3(10f, 0f, 0f);

        var markerA = new GameObject("MarkerA");
        markerA.transform.SetParent(root.transform, false);
        markerA.transform.localPosition = new Vector3(2f, 3f, 0f);
        markerA.AddComponent<EnemySpawnMarker>();

        var markerB = new GameObject("MarkerB");
        markerB.transform.SetParent(root.transform, false);
        markerB.transform.localPosition = new Vector3(-4f, 1f, 0f);
        markerB.AddComponent<EnemySpawnMarker>();

        var decoration = new GameObject("Obstacle");
        decoration.transform.SetParent(root.transform, false);

        try
        {
            List<EnemySpawnMarker> markers = template.SpawnMarkers();

            Assert.That(markers, Has.Count.EqualTo(2));
            var positions = new List<Vector2>();
            foreach (EnemySpawnMarker marker in markers)
            {
                positions.Add(marker.transform.position);
            }

            Assert.That(positions, Does.Contain(new Vector2(12f, 3f)));
            Assert.That(positions, Does.Contain(new Vector2(6f, 1f)));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void SpawnMarkers_NoMarkersReturnsEmpty()
    {
        var root = new GameObject("Template");
        RoomContentTemplate template = root.AddComponent<RoomContentTemplate>();

        try
        {
            Assert.That(template.SpawnMarkers(), Is.Empty);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void SpawnMarkers_DefaultFixedEnemyDefinitionIsNull()
    {
        var root = new GameObject("Template");
        RoomContentTemplate template = root.AddComponent<RoomContentTemplate>();
        var markerA = new GameObject("MarkerA");
        markerA.transform.SetParent(root.transform, false);
        markerA.AddComponent<EnemySpawnMarker>();

        try
        {
            List<EnemySpawnMarker> markers = template.SpawnMarkers();

            Assert.That(markers, Has.Count.EqualTo(1));
            Assert.That(markers[0].FixedEnemyDefinition, Is.Null);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void SpawnMarkers_ReturnsMarkerWithFixedEnemyDefinitionSet()
    {
        var root = new GameObject("Template");
        RoomContentTemplate template = root.AddComponent<RoomContentTemplate>();
        EnemyDefinition fixedDefinition = ScriptableObject.CreateInstance<EnemyDefinition>();

        var markerA = new GameObject("MarkerA");
        markerA.transform.SetParent(root.transform, false);
        EnemySpawnMarker marker = markerA.AddComponent<EnemySpawnMarker>();
        marker.Configure(fixedDefinition);

        try
        {
            List<EnemySpawnMarker> markers = template.SpawnMarkers();

            Assert.That(markers, Has.Count.EqualTo(1));
            Assert.That(markers[0].FixedEnemyDefinition, Is.SameAs(fixedDefinition));
        }
        finally
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(fixedDefinition);
        }
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `-testFilter "RoomContentTemplateTests"`
Expected: FAIL to compile — `SpawnMarkers()`, `FixedEnemyDefinition`, and
`Configure` do not exist yet on `RoomContentTemplate`/`EnemySpawnMarker`.

- [ ] **Step 3: Write minimal implementation**

Replace the full contents of `Assets/Scripts/Dungeon/EnemySpawnMarker.cs`
with:

```csharp
using UnityEngine;

namespace NaManMoo.Dungeon
{
    /// <summary>
    /// 몬스터가 설 자리를 표시하는 마커. 위치와, 선택적으로 고정할 몬스터 종류를 쓴다.
    /// </summary>
    public sealed class EnemySpawnMarker : MonoBehaviour
    {
        [SerializeField] private EnemyDefinition fixedEnemyDefinition;

        /// <summary>비어 있으면(null) 스폰 시 무작위 풀에서 종류를 배정한다.</summary>
        public EnemyDefinition FixedEnemyDefinition => fixedEnemyDefinition;

        /// <summary>인스펙터 대신 코드로(테스트·씬 빌더) 고정 종류를 지정한다.</summary>
        public void Configure(EnemyDefinition definition)
        {
            fixedEnemyDefinition = definition;
        }
    }
}
```

In `Assets/Scripts/Dungeon/RoomContentTemplate.cs`, replace the
`SpawnMarkerPositions` method (currently lines 11-22) with:

```csharp
        /// <summary>이 인스턴스 밑에 있는 모든 EnemySpawnMarker.</summary>
        public List<EnemySpawnMarker> SpawnMarkers()
        {
            EnemySpawnMarker[] markers = GetComponentsInChildren<EnemySpawnMarker>();
            return new List<EnemySpawnMarker>(markers);
        }
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `-testFilter "RoomContentTemplateTests"`
Expected: PASS (all 4 tests).

Then run `-testFilter "DungeonEncounterTests"` — **expected to FAIL to
compile**, because `DungeonEncounter.SpawnEnemies`
(`Assets/Scripts/Dungeon/DungeonEncounter.cs:202`) still calls the
now-removed `instance.SpawnMarkerPositions()`. This is expected and is
fixed in Task 2 — do not attempt to fix `DungeonEncounter.cs` in this task.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Dungeon/EnemySpawnMarker.cs Assets/Scripts/Dungeon/RoomContentTemplate.cs Assets/Tests/Editor/RoomContentTemplateTests.cs
git commit -m "feat: EnemySpawnMarker에 고정 몬스터 종류를 추가하고 RoomContentTemplate이 마커 자체를 돌려주게 한다"
```

---

### Task 2: DungeonEncounter resolves fixed and random markers per-slot

**Files:**
- Modify: `Assets/Scripts/Dungeon/DungeonEncounter.cs`
- Modify: `Assets/Tests/Editor/DungeonEncounterTests.cs`

**Interfaces:**
- Consumes: `RoomContentTemplate.SpawnMarkers() : List<EnemySpawnMarker>`,
  `EnemySpawnMarker.FixedEnemyDefinition : EnemyDefinition`,
  `EnemySpawnMarker.Configure(EnemyDefinition)` (all from Task 1); existing
  `DungeonEncounter.SelectDefinitions`, `EnemyFactory.Create`,
  `EnemySpawnRequest`.
- Produces: updated `DungeonEncounter.SpawnEnemies` behavior only — no new
  public members.

- [ ] **Step 1: Write the failing tests**

Add to `Assets/Tests/Editor/DungeonEncounterTests.cs`, inside the
`DungeonEncounterTests` class (after `Spawn_NonNormalRoomWithConfiguredTemplateReturnsNull`,
before `Spawn_NormalRoomBuildsGuaranteedContactAndRangedEnemies`):

```csharp
    [Test]
    public void Spawn_MarkerWithFixedDefinitionAlwaysSpawnsThatDefinition()
    {
        var encounterObject = new GameObject("Encounter");
        var roomRootObject = new GameObject("Room");
        var playerObject = new GameObject("Player");
        var templateObject = new GameObject("Template");
        Sprite pooledSprite = Sprite.Create(
            Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        Sprite fixedSprite = Sprite.Create(
            Texture2D.blackTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        EnemyDefinition pooledMushroom = ScriptableObject.CreateInstance<EnemyDefinition>();
        EnemyDefinition fixedEnemy = ScriptableObject.CreateInstance<EnemyDefinition>();

        try
        {
            pooledMushroom.Configure(
                "dungeon-mushroom", "Dungeon Mushroom", pooledSprite, null,
                EnemyBehaviorType.ChaseContact, 5, 3.5f, 6, 0.75f, 1f, 0f, 0.01f, 0.01f);
            fixedEnemy.Configure(
                "dungeon-fixed", "Fixed Enemy", fixedSprite, null,
                EnemyBehaviorType.ChaseContact, 9, 3.5f, 6, 0.75f, 1f, 0f, 0.01f, 0.01f);

            RoomContentTemplate template = templateObject.AddComponent<RoomContentTemplate>();
            var markerObject = new GameObject("FixedMarker");
            markerObject.transform.SetParent(templateObject.transform, false);
            markerObject.transform.localPosition = new Vector3(5f, 0f, 0f);
            EnemySpawnMarker marker = markerObject.AddComponent<EnemySpawnMarker>();
            marker.Configure(fixedEnemy);

            DungeonEncounter encounter = encounterObject.AddComponent<DungeonEncounter>();
            encounter.Configure(pooledMushroom, null);
            encounter.ConfigureRoomTemplates(new[] { template });

            RoomShape shape = RoomShape.Build(101, Doors.North | Doors.South);
            var room = new DungeonRoom(
                Vector2Int.zero, RoomKind.Normal, Doors.North | Doors.South, 2);

            Stage1EncounterGate gate = encounter.Spawn(
                roomRootObject.transform, playerObject.transform, shape, room, 202);

            Assert.That(gate, Is.Not.Null);
            EnemyHealth[] enemies = roomRootObject.GetComponentsInChildren<EnemyHealth>();
            Assert.That(enemies, Has.Length.EqualTo(1));
            Assert.That(enemies[0].MaxHealth, Is.EqualTo(9));
            Assert.That(enemies[0].gameObject.name, Does.StartWith("Fixed Enemy"));
        }
        finally
        {
            Object.DestroyImmediate(encounterObject);
            Object.DestroyImmediate(roomRootObject);
            Object.DestroyImmediate(playerObject);
            Object.DestroyImmediate(templateObject);
            Object.DestroyImmediate(pooledMushroom);
            Object.DestroyImmediate(fixedEnemy);
            Object.DestroyImmediate(pooledSprite);
            Object.DestroyImmediate(fixedSprite);
        }
    }

    [Test]
    public void Spawn_MixedFixedAndUnfixedMarkersEachResolveCorrectly()
    {
        var encounterObject = new GameObject("Encounter");
        var roomRootObject = new GameObject("Room");
        var playerObject = new GameObject("Player");
        var templateObject = new GameObject("Template");
        Sprite pooledSprite = Sprite.Create(
            Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        Sprite fixedSprite = Sprite.Create(
            Texture2D.blackTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        EnemyDefinition pooledMushroom = ScriptableObject.CreateInstance<EnemyDefinition>();
        EnemyDefinition fixedEnemy = ScriptableObject.CreateInstance<EnemyDefinition>();

        try
        {
            pooledMushroom.Configure(
                "dungeon-mushroom", "Dungeon Mushroom", pooledSprite, null,
                EnemyBehaviorType.ChaseContact, 5, 3.5f, 6, 0.75f, 1f, 0f, 0.01f, 0.01f);
            fixedEnemy.Configure(
                "dungeon-fixed", "Fixed Enemy", fixedSprite, null,
                EnemyBehaviorType.ChaseContact, 9, 3.5f, 6, 0.75f, 1f, 0f, 0.01f, 0.01f);

            RoomContentTemplate template = templateObject.AddComponent<RoomContentTemplate>();
            var fixedMarkerObject = new GameObject("FixedMarker");
            fixedMarkerObject.transform.SetParent(templateObject.transform, false);
            fixedMarkerObject.transform.localPosition = new Vector3(5f, 0f, 0f);
            EnemySpawnMarker fixedMarker = fixedMarkerObject.AddComponent<EnemySpawnMarker>();
            fixedMarker.Configure(fixedEnemy);

            var randomMarkerObject = new GameObject("RandomMarker");
            randomMarkerObject.transform.SetParent(templateObject.transform, false);
            randomMarkerObject.transform.localPosition = new Vector3(-5f, 0f, 0f);
            randomMarkerObject.AddComponent<EnemySpawnMarker>();

            DungeonEncounter encounter = encounterObject.AddComponent<DungeonEncounter>();
            encounter.Configure(pooledMushroom, null);
            encounter.ConfigureRoomTemplates(new[] { template });

            RoomShape shape = RoomShape.Build(101, Doors.North | Doors.South);
            var room = new DungeonRoom(
                Vector2Int.zero, RoomKind.Normal, Doors.North | Doors.South, 2);

            Stage1EncounterGate gate = encounter.Spawn(
                roomRootObject.transform, playerObject.transform, shape, room, 202);

            Assert.That(gate, Is.Not.Null);
            EnemyHealth[] enemies = roomRootObject.GetComponentsInChildren<EnemyHealth>();
            Assert.That(enemies, Has.Length.EqualTo(2));
            int fixedCount = 0;
            int pooledCount = 0;
            foreach (EnemyHealth enemy in enemies)
            {
                if (enemy.MaxHealth == 9)
                {
                    fixedCount++;
                }
                else if (enemy.MaxHealth == 5)
                {
                    pooledCount++;
                }
            }

            Assert.That(fixedCount, Is.EqualTo(1));
            Assert.That(pooledCount, Is.EqualTo(1));
        }
        finally
        {
            Object.DestroyImmediate(encounterObject);
            Object.DestroyImmediate(roomRootObject);
            Object.DestroyImmediate(playerObject);
            Object.DestroyImmediate(templateObject);
            Object.DestroyImmediate(pooledMushroom);
            Object.DestroyImmediate(fixedEnemy);
            Object.DestroyImmediate(pooledSprite);
            Object.DestroyImmediate(fixedSprite);
        }
    }

    [Test]
    public void Spawn_UnfixedMarkerSkippedWhenPoolEmptyButFixedMarkerStillSpawns()
    {
        var encounterObject = new GameObject("Encounter");
        var roomRootObject = new GameObject("Room");
        var playerObject = new GameObject("Player");
        var templateObject = new GameObject("Template");
        Sprite fixedSprite = Sprite.Create(
            Texture2D.blackTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        EnemyDefinition fixedEnemy = ScriptableObject.CreateInstance<EnemyDefinition>();

        try
        {
            fixedEnemy.Configure(
                "dungeon-fixed", "Fixed Enemy", fixedSprite, null,
                EnemyBehaviorType.ChaseContact, 9, 3.5f, 6, 0.75f, 1f, 0f, 0.01f, 0.01f);

            RoomContentTemplate template = templateObject.AddComponent<RoomContentTemplate>();
            var fixedMarkerObject = new GameObject("FixedMarker");
            fixedMarkerObject.transform.SetParent(templateObject.transform, false);
            fixedMarkerObject.transform.localPosition = new Vector3(5f, 0f, 0f);
            EnemySpawnMarker fixedMarker = fixedMarkerObject.AddComponent<EnemySpawnMarker>();
            fixedMarker.Configure(fixedEnemy);

            var randomMarkerObject = new GameObject("RandomMarker");
            randomMarkerObject.transform.SetParent(templateObject.transform, false);
            randomMarkerObject.transform.localPosition = new Vector3(-5f, 0f, 0f);
            randomMarkerObject.AddComponent<EnemySpawnMarker>();

            DungeonEncounter encounter = encounterObject.AddComponent<DungeonEncounter>();
            encounter.Configure(new EnemyDefinition[0], null);
            encounter.ConfigureRoomTemplates(new[] { template });

            RoomShape shape = RoomShape.Build(101, Doors.North | Doors.South);
            var room = new DungeonRoom(
                Vector2Int.zero, RoomKind.Normal, Doors.North | Doors.South, 2);

            Stage1EncounterGate gate = encounter.Spawn(
                roomRootObject.transform, playerObject.transform, shape, room, 202);

            Assert.That(gate, Is.Not.Null);
            EnemyHealth[] enemies = roomRootObject.GetComponentsInChildren<EnemyHealth>();
            Assert.That(enemies, Has.Length.EqualTo(1));
            Assert.That(enemies[0].MaxHealth, Is.EqualTo(9));
        }
        finally
        {
            Object.DestroyImmediate(encounterObject);
            Object.DestroyImmediate(roomRootObject);
            Object.DestroyImmediate(playerObject);
            Object.DestroyImmediate(templateObject);
            Object.DestroyImmediate(fixedEnemy);
            Object.DestroyImmediate(fixedSprite);
        }
    }
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `-testFilter "DungeonEncounterTests"`
Expected: at minimum the 3 new tests FAIL — `Spawn_MarkerWithFixedDefinitionAlwaysSpawnsThatDefinition`
gets 0 enemies instead of 1 (current code assigns every marker a random
definition and ignores any concept of "fixed"), and this file currently
fails to compile at all (see Task 1 Step 4) until this task's Step 3 lands,
since `SpawnEnemies` still calls the removed `SpawnMarkerPositions()`.

- [ ] **Step 3: Write minimal implementation**

In `Assets/Scripts/Dungeon/DungeonEncounter.cs`, replace the body of
`SpawnEnemies` (currently lines 180-237, from `private List<EnemyHealth>
SpawnEnemies(...)` through its closing brace) with:

```csharp
        private List<EnemyHealth> SpawnEnemies(
            Transform roomRoot,
            Transform player,
            RoomShape shape,
            DungeonRoom room,
            int roomSeed)
        {
            if (room.Kind != RoomKind.Normal)
            {
                return null;
            }

            RoomContentTemplate template = SelectTemplate(normalRoomTemplates, roomSeed);
            if (template == null)
            {
                return null;
            }

            // 방 기하와 같은 시드를 쓴다 — 되돌아왔을 때 고른 템플릿도 그대로여야 한다
            RoomContentTemplate instance = Instantiate(template, roomRoot);
            instance.transform.localPosition = Vector3.zero;

            List<EnemySpawnMarker> markers = instance.SpawnMarkers();

            int randomCount = 0;
            foreach (EnemySpawnMarker marker in markers)
            {
                if (marker.FixedEnemyDefinition == null)
                {
                    randomCount++;
                }
            }

            EnemyDefinition[] randomDefinitions = SelectDefinitions(
                normalEnemyDefinitions,
                randomCount,
                roomSeed);

            var enemies = new List<EnemyHealth>(markers.Count);
            var instancesByDefinition = new Dictionary<string, int>();
            int randomIndex = 0;
            foreach (EnemySpawnMarker marker in markers)
            {
                EnemyDefinition definition = marker.FixedEnemyDefinition;
                if (definition == null)
                {
                    // 랜덤 풀이 모자라면 이 마커는 그냥 비운다 — 억지로 채우지 않는다
                    if (randomIndex >= randomDefinitions.Length)
                    {
                        continue;
                    }

                    definition = randomDefinitions[randomIndex++];
                }

                string key = string.IsNullOrEmpty(definition.Id)
                    ? definition.DisplayName
                    : definition.Id;
                instancesByDefinition.TryGetValue(key, out int currentCount);
                int nextCount = currentCount + 1;
                instancesByDefinition[key] = nextCount;

                string baseName = string.IsNullOrEmpty(definition.DisplayName)
                    ? "Enemy"
                    : definition.DisplayName;
                enemies.Add(EnemyFactory.Create(
                    definition,
                    new EnemySpawnRequest(
                        roomRoot,
                        player,
                        marker.transform.position,
                        $"{baseName} {nextCount}")));
            }

            return enemies.Count > 0 ? enemies : null;
        }
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `-testFilter "DungeonEncounterTests"`
Expected: PASS for all tests in the class, including the 3 new ones and
every pre-existing test in this file (`Spawn_NormalRoomUsesConfiguredTemplateMarkerPositions`,
`Spawn_NormalRoomWithNoConfiguredTemplateReturnsNull`,
`Spawn_NonNormalRoomWithConfiguredTemplateReturnsNull`,
`Spawn_NormalRoomBuildsGuaranteedContactAndRangedEnemies`, all
`SelectDefinitions_*`/`SelectTemplate_*` tests) — none of their templates
configure a `FixedEnemyDefinition` on any marker, so every marker in those
tests still resolves exactly as before (100% of markers draw from the
random pool, same as today).

`Spawn_SquirrelAttackInitializesProjectileFromDungeonSquirrelDefinition` is
expected to keep FAILING — this is the pre-existing, unrelated coroutine/
`WaitForSeconds` timing bug in `ApproachAndShootEnemyController` documented
in the previous plan's final review; this task does not touch that
controller and must not attempt to fix it.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Dungeon/DungeonEncounter.cs Assets/Tests/Editor/DungeonEncounterTests.cs
git commit -m "feat: SpawnEnemies가 고정 마커는 지정된 종류로, 나머지는 랜덤 풀에서 배정한다"
```
