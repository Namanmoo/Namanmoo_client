# Dungeon Normal Room Content Template Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let a designer hand-place obstacles and monster spawn points for Normal
dungeon rooms in a prefab, and have `DungeonEncounter` pick one such prefab per
room instead of computing enemy spots with `RoomSpawnPoints`.

**Architecture:** Two new small components (`EnemySpawnMarker`,
`RoomContentTemplate`) plus a `DungeonEncounter.SelectTemplate` static
selector, wired into the existing `SpawnEnemies` path. Nothing outside
`DungeonEncounter` and these two new files changes.

**Tech Stack:** Unity (C#), NUnit EditMode tests (existing `Assets/Tests/Editor`
convention).

## Global Constraints

- Only `RoomKind.Normal` rooms are affected. Boss, Treasure, Shop, and Start
  rooms keep their current behavior untouched.
- `DungeonLayout`, `RoomShape`, `RoomBuilder`, `IRoomEncounter`, and
  `DungeonRunner` must not be modified.
- `RoomSpawnPoints` stays in place unmodified; it simply stops being called
  from the Normal-room path. Do not delete it.
- Distance-based difficulty scaling (`RoomSpawnPoints.EnemyCount`) is
  intentionally removed for Normal rooms — enemy count becomes however many
  `EnemySpawnMarker` children the chosen template has. This was an explicit
  user decision, not an oversight.
- Do not change any existing balance values (health, damage, speed, etc.) on
  any enemy or player component.
- Spec: `docs/superpowers/specs/2026-08-08-dungeon-normal-room-content-template-design.md`

---

### Task 1: EnemySpawnMarker and RoomContentTemplate

**Files:**
- Create: `Assets/Scripts/Dungeon/EnemySpawnMarker.cs`
- Create: `Assets/Scripts/Dungeon/RoomContentTemplate.cs`
- Test: `Assets/Tests/Editor/RoomContentTemplateTests.cs`

**Interfaces:**
- Produces: `NaManMoo.Dungeon.EnemySpawnMarker` (empty marker
  `MonoBehaviour`), `NaManMoo.Dungeon.RoomContentTemplate.SpawnMarkerPositions()
  : List<Vector2>` (world positions of every `EnemySpawnMarker` child).

- [ ] **Step 1: Write the failing test**

Create `Assets/Tests/Editor/RoomContentTemplateTests.cs`:

```csharp
using System.Collections.Generic;
using NaManMoo.Dungeon;
using NUnit.Framework;
using UnityEngine;

public sealed class RoomContentTemplateTests
{
    [Test]
    public void SpawnMarkerPositions_ReturnsWorldPositionOfEveryMarkerChild()
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
            List<Vector2> positions = template.SpawnMarkerPositions();

            Assert.That(positions, Has.Count.EqualTo(2));
            Assert.That(positions, Does.Contain(new Vector2(12f, 3f)));
            Assert.That(positions, Does.Contain(new Vector2(6f, 1f)));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void SpawnMarkerPositions_NoMarkersReturnsEmpty()
    {
        var root = new GameObject("Template");
        RoomContentTemplate template = root.AddComponent<RoomContentTemplate>();

        try
        {
            Assert.That(template.SpawnMarkerPositions(), Is.Empty);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `-testFilter "RoomContentTemplateTests"` (Unity EditMode test runner)
Expected: FAIL to compile — `RoomContentTemplate` and `EnemySpawnMarker` do
not exist yet.

- [ ] **Step 3: Write minimal implementation**

Create `Assets/Scripts/Dungeon/EnemySpawnMarker.cs`:

```csharp
using UnityEngine;

namespace NaManMoo.Dungeon
{
    /// <summary>몬스터가 설 자리를 표시하는 빈 마커. Transform 위치만 쓴다.</summary>
    public sealed class EnemySpawnMarker : MonoBehaviour
    {
    }
}
```

Create `Assets/Scripts/Dungeon/RoomContentTemplate.cs`:

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace NaManMoo.Dungeon
{
    /// <summary>
    /// 디자이너가 손으로 배치한 방 내용물(장애물 + 몬스터 스폰 자리) 프리팹의 루트.
    /// </summary>
    public sealed class RoomContentTemplate : MonoBehaviour
    {
        /// <summary>이 인스턴스 밑에 있는 모든 EnemySpawnMarker의 월드 위치.</summary>
        public List<Vector2> SpawnMarkerPositions()
        {
            EnemySpawnMarker[] markers = GetComponentsInChildren<EnemySpawnMarker>();
            var positions = new List<Vector2>(markers.Length);
            foreach (EnemySpawnMarker marker in markers)
            {
                positions.Add(marker.transform.position);
            }

            return positions;
        }
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `-testFilter "RoomContentTemplateTests"`
Expected: PASS (both tests).

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Dungeon/EnemySpawnMarker.cs Assets/Scripts/Dungeon/RoomContentTemplate.cs Assets/Tests/Editor/RoomContentTemplateTests.cs
git commit -m "feat: RoomContentTemplate이 EnemySpawnMarker 자식 위치를 모으게 한다"
```

---

### Task 2: DungeonEncounter.SelectTemplate deterministic selection

**Files:**
- Modify: `Assets/Scripts/Dungeon/DungeonEncounter.cs`
- Test: `Assets/Tests/Editor/DungeonEncounterTests.cs`

**Interfaces:**
- Consumes: `NaManMoo.Dungeon.RoomContentTemplate` (Task 1),
  `NaManMoo.Dungeon.DeterministicRandom(int seed)` /
  `.Next(int maxExclusive)` (existing, `Assets/Scripts/Dungeon/DeterministicRandom.cs`).
- Produces: `DungeonEncounter.SelectTemplate(RoomContentTemplate[] templates,
  int seed) : RoomContentTemplate` (public static — same pattern as the
  existing `SelectDefinitions`).

- [ ] **Step 1: Write the failing test**

Add to `Assets/Tests/Editor/DungeonEncounterTests.cs` (inside the
`DungeonEncounterTests` class, alongside the other `SelectDefinitions` tests):

```csharp
    [Test]
    public void SelectTemplate_PicksDeterministicallyFromSeed()
    {
        var templateAObject = new GameObject("TemplateA");
        var templateBObject = new GameObject("TemplateB");
        RoomContentTemplate templateA = templateAObject.AddComponent<RoomContentTemplate>();
        RoomContentTemplate templateB = templateBObject.AddComponent<RoomContentTemplate>();

        try
        {
            RoomContentTemplate[] pool = { templateA, templateB };

            RoomContentTemplate first = DungeonEncounter.SelectTemplate(pool, 42);
            RoomContentTemplate second = DungeonEncounter.SelectTemplate(pool, 42);

            Assert.That(first, Is.Not.Null);
            Assert.That(first, Is.SameAs(second));
            Assert.That(pool, Does.Contain(first));
        }
        finally
        {
            Object.DestroyImmediate(templateAObject);
            Object.DestroyImmediate(templateBObject);
        }
    }

    [Test]
    public void SelectTemplate_EmptyOrNullPoolReturnsNull()
    {
        Assert.That(DungeonEncounter.SelectTemplate(null, 1), Is.Null);
        Assert.That(
            DungeonEncounter.SelectTemplate(new RoomContentTemplate[0], 1),
            Is.Null);
        Assert.That(
            DungeonEncounter.SelectTemplate(new RoomContentTemplate[] { null }, 1),
            Is.Null);
    }
```

- [ ] **Step 2: Run test to verify it fails**

Run: `-testFilter "DungeonEncounterTests.SelectTemplate_PicksDeterministicallyFromSeed"`
Expected: FAIL to compile — `DungeonEncounter.SelectTemplate` does not exist
yet.

- [ ] **Step 3: Write minimal implementation**

In `Assets/Scripts/Dungeon/DungeonEncounter.cs`, add this method next to the
existing `SelectDefinitions` static method:

```csharp
        public static RoomContentTemplate SelectTemplate(
            RoomContentTemplate[] templates, int seed)
        {
            var valid = new List<RoomContentTemplate>();
            if (templates != null)
            {
                foreach (RoomContentTemplate template in templates)
                {
                    if (template != null)
                    {
                        valid.Add(template);
                    }
                }
            }

            if (valid.Count == 0)
            {
                return null;
            }

            var rng = new DeterministicRandom(seed);
            return valid[rng.Next(valid.Count)];
        }
```

- [ ] **Step 4: Run test to verify it passes**

Run: `-testFilter "DungeonEncounterTests.SelectTemplate_PicksDeterministicallyFromSeed"`
Expected: PASS. Then run
`-testFilter "DungeonEncounterTests.SelectTemplate_EmptyOrNullPoolReturnsNull"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Dungeon/DungeonEncounter.cs Assets/Tests/Editor/DungeonEncounterTests.cs
git commit -m "feat: DungeonEncounter가 시드로 RoomContentTemplate을 고르게 한다"
```

---

### Task 3: Wire SpawnEnemies to use the selected template

**Files:**
- Modify: `Assets/Scripts/Dungeon/DungeonEncounter.cs`
- Test: `Assets/Tests/Editor/DungeonEncounterTests.cs`

**Interfaces:**
- Consumes: `RoomContentTemplate.SpawnMarkerPositions()` (Task 1),
  `DungeonEncounter.SelectTemplate` (Task 2), existing `SelectDefinitions`,
  `EnemyFactory.Create(EnemyDefinition, EnemySpawnRequest)`
  (`Assets/Scripts/Enemies/EnemyFactory.cs`), existing
  `EnemySpawnRequest(Transform parent, Transform target, Vector2 position,
  string instanceName)` (`Assets/Scripts/Enemies/EnemySpawnRequest.cs`).
- Produces: `DungeonEncounter.ConfigureRoomTemplates(RoomContentTemplate[]
  templates)` (public instance method), new private serialized field
  `normalRoomTemplates`.

- [ ] **Step 1: Write the failing test**

Add to `Assets/Tests/Editor/DungeonEncounterTests.cs`:

```csharp
    [Test]
    public void Spawn_NormalRoomUsesConfiguredTemplateMarkerPositions()
    {
        var encounterObject = new GameObject("Encounter");
        var roomRootObject = new GameObject("Room");
        var playerObject = new GameObject("Player");
        var templateObject = new GameObject("Template");
        Sprite sprite = Sprite.Create(
            Texture2D.whiteTexture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            1f);
        EnemyDefinition mushroom = ScriptableObject.CreateInstance<EnemyDefinition>();

        try
        {
            mushroom.Configure(
                "dungeon-mushroom",
                "Dungeon Mushroom",
                sprite,
                null,
                EnemyBehaviorType.ChaseContact,
                5,
                3.5f,
                6,
                0.75f,
                1f,
                0f,
                0.01f,
                0.01f);

            RoomContentTemplate template = templateObject.AddComponent<RoomContentTemplate>();
            var markerA = new GameObject("MarkerA");
            markerA.transform.SetParent(templateObject.transform, false);
            markerA.transform.localPosition = new Vector3(3f, 4f, 0f);
            markerA.AddComponent<EnemySpawnMarker>();
            var markerB = new GameObject("MarkerB");
            markerB.transform.SetParent(templateObject.transform, false);
            markerB.transform.localPosition = new Vector3(-3f, -2f, 0f);
            markerB.AddComponent<EnemySpawnMarker>();

            DungeonEncounter encounter = encounterObject.AddComponent<DungeonEncounter>();
            encounter.Configure(mushroom, null);
            encounter.ConfigureRoomTemplates(new[] { template });

            RoomShape shape = RoomShape.Build(101, Doors.North | Doors.South);
            var room = new DungeonRoom(
                Vector2Int.zero, RoomKind.Normal, Doors.North | Doors.South, 2);

            Stage1EncounterGate gate = encounter.Spawn(
                roomRootObject.transform, playerObject.transform, shape, room, 202);

            Assert.That(gate, Is.Not.Null);
            EnemyHealth[] enemies = roomRootObject.GetComponentsInChildren<EnemyHealth>();
            Assert.That(enemies, Has.Length.EqualTo(2));

            var positions = new Vector2[enemies.Length];
            for (int i = 0; i < enemies.Length; i++)
            {
                positions[i] = enemies[i].transform.position;
            }

            Assert.That(positions, Does.Contain(new Vector2(3f, 4f)));
            Assert.That(positions, Does.Contain(new Vector2(-3f, -2f)));
        }
        finally
        {
            Object.DestroyImmediate(encounterObject);
            Object.DestroyImmediate(roomRootObject);
            Object.DestroyImmediate(playerObject);
            Object.DestroyImmediate(templateObject);
            Object.DestroyImmediate(mushroom);
            Object.DestroyImmediate(sprite);
        }
    }

    [Test]
    public void Spawn_NormalRoomWithNoConfiguredTemplateReturnsNull()
    {
        var encounterObject = new GameObject("Encounter");
        var roomRootObject = new GameObject("Room");
        var playerObject = new GameObject("Player");
        EnemyDefinition mushroom = ScriptableObject.CreateInstance<EnemyDefinition>();

        try
        {
            DungeonEncounter encounter = encounterObject.AddComponent<DungeonEncounter>();
            encounter.Configure(mushroom, null);

            RoomShape shape = RoomShape.Build(101, Doors.North | Doors.South);
            var room = new DungeonRoom(
                Vector2Int.zero, RoomKind.Normal, Doors.North | Doors.South, 2);

            Stage1EncounterGate gate = encounter.Spawn(
                roomRootObject.transform, playerObject.transform, shape, room, 202);

            Assert.That(gate, Is.Null);
            Assert.That(roomRootObject.GetComponentsInChildren<EnemyHealth>(), Is.Empty);
        }
        finally
        {
            Object.DestroyImmediate(encounterObject);
            Object.DestroyImmediate(roomRootObject);
            Object.DestroyImmediate(playerObject);
            Object.DestroyImmediate(mushroom);
        }
    }
```

- [ ] **Step 2: Run test to verify it fails**

Run: `-testFilter "DungeonEncounterTests.Spawn_NormalRoomUsesConfiguredTemplateMarkerPositions"`
Expected: FAIL — `ConfigureRoomTemplates` does not exist yet, and even once
it compiles, the enemy count would still come from `RoomSpawnPoints.EnemyCount`
rather than the two template markers.

- [ ] **Step 3: Write minimal implementation**

In `Assets/Scripts/Dungeon/DungeonEncounter.cs`, add the new field next to the
existing serialized fields:

```csharp
        [SerializeField] private RoomContentTemplate[] normalRoomTemplates;
```

Add this method next to the existing `Configure` overloads:

```csharp
        public void ConfigureRoomTemplates(RoomContentTemplate[] templates)
        {
            normalRoomTemplates = templates;
        }
```

Replace the body of `SpawnEnemies` (currently starting with
`int count = RoomSpawnPoints.EnemyCount(...)` and the
`List<Vector2> spots = RoomSpawnPoints.Inside(...)` line right after it) with:

```csharp
        private List<EnemyHealth> SpawnEnemies(
            Transform roomRoot,
            Transform player,
            RoomShape shape,
            DungeonRoom room,
            int roomSeed)
        {
            RoomContentTemplate template = SelectTemplate(normalRoomTemplates, roomSeed);
            if (template == null)
            {
                return null;
            }

            // 방 기하와 같은 시드를 쓴다 — 되돌아왔을 때 고른 템플릿도 그대로여야 한다
            RoomContentTemplate instance = Instantiate(template, roomRoot);
            instance.transform.localPosition = Vector3.zero;

            List<Vector2> spots = instance.SpawnMarkerPositions();
            EnemyDefinition[] definitions = SelectDefinitions(
                normalEnemyDefinitions,
                spots.Count,
                roomSeed);
            if (definitions.Length == 0)
            {
                return null;
            }

            var enemies = new List<EnemyHealth>(spots.Count);
            var instancesByDefinition = new Dictionary<string, int>();
            for (int i = 0; i < spots.Count; i++)
            {
                EnemyDefinition definition = definitions[i];
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
                        spots[i],
                        $"{baseName} {nextCount}")));
            }

            return enemies;
        }
```

`shape` and `room` stay in the parameter list unchanged (the call site in
`Spawn` passes both) even though this new body no longer reads them — leave
them as-is rather than reshaping the call site for an unrelated cleanup.

- [ ] **Step 4: Run test to verify it passes**

Run: `-testFilter "DungeonEncounterTests.Spawn_NormalRoomUsesConfiguredTemplateMarkerPositions"`
Expected: PASS. Then run
`-testFilter "DungeonEncounterTests.Spawn_NormalRoomWithNoConfiguredTemplateReturnsNull"`
Expected: PASS. Then re-run the pre-existing
`-testFilter "DungeonEncounterTests.Spawn_NormalRoomBuildsGuaranteedContactAndRangedEnemies"`
— **expected to now FAIL**, because that test relies on
`RoomSpawnPoints`-computed spawn counts and never configures a template.

Fix it in this same step. In `Spawn_NormalRoomBuildsGuaranteedContactAndRangedEnemies`,
add a `templateObject` line next to the other GameObjects:

```csharp
        var encounterObject = new GameObject("Encounter");
        var roomRootObject = new GameObject("Room");
        var playerObject = new GameObject("Player");
        var templateObject = new GameObject("Template");
```

Then, right after `encounter.Configure(new[] { contact, ranged }, null);` and
before `RoomShape shape = RoomShape.Build(...)`, add:

```csharp
            RoomContentTemplate template = templateObject.AddComponent<RoomContentTemplate>();
            for (int i = 0; i < 4; i++)
            {
                var marker = new GameObject($"Marker{i}");
                marker.transform.SetParent(templateObject.transform, false);
                marker.transform.localPosition = new Vector3(i, 0f, 0f);
                marker.AddComponent<EnemySpawnMarker>();
            }
            encounter.ConfigureRoomTemplates(new[] { template });
```

Four markers keeps `count >= valid.Count` (2 definitions) in
`SelectDefinitions`, so both `contact` and `ranged` are still guaranteed to
appear at least once — the existing assertions stay valid unchanged. Add
`Object.DestroyImmediate(templateObject);` to the `finally` block alongside
the other `DestroyImmediate` calls. Re-run the test to confirm PASS.

Apply the identical fix to
`Spawn_SquirrelAttackInitializesProjectileFromDungeonSquirrelDefinition`: add
`var templateObject = new GameObject("Template");` next to its other
GameObjects, insert the same four-marker `RoomContentTemplate` setup and
`encounter.ConfigureRoomTemplates(new[] { template });` call right after its
`encounter.Configure(new[] { mushroom, squirrel }, null);` line (before
`RoomShape shape = RoomShape.Build(...)`), and add
`Object.DestroyImmediate(templateObject);` to its `finally` block. Re-run
`-testFilter "DungeonEncounterTests.Spawn_SquirrelAttackInitializesProjectileFromDungeonSquirrelDefinition"`
to confirm PASS.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Dungeon/DungeonEncounter.cs Assets/Tests/Editor/DungeonEncounterTests.cs
git commit -m "feat: DungeonEncounter가 몬스터를 RoomContentTemplate 마커 위치에 배치한다"
```

---

### Task 4: Authoring gizmos on RoomContentTemplate

**Files:**
- Modify: `Assets/Scripts/Dungeon/RoomContentTemplate.cs`

**Interfaces:**
- Consumes: `RoomShape.Size` (`Assets/Scripts/Dungeon/RoomShape.cs`),
  `RoomSpawnPoints.WallInset` / `DoorClearance` / `CentreClearance`
  (`Assets/Scripts/Dungeon/RoomSpawnPoints.cs`).
- Produces: nothing consumed by other tasks — Editor-only visualization.

This step has no automated test: `OnDrawGizmos` only runs inside the Unity
Editor's Scene view and there is no existing precedent in this codebase for
testing gizmo rendering. Verification is manual, described in Step 3.

- [ ] **Step 1: Add the gizmo drawing code**

Append to the bottom of the `RoomContentTemplate` class body in
`Assets/Scripts/Dungeon/RoomContentTemplate.cs`:

```csharp
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Rect bounds = new Rect(
                -RoomShape.Size.x * 0.5f, -RoomShape.Size.y * 0.5f,
                RoomShape.Size.x, RoomShape.Size.y);
            Rect inner = Rect.MinMaxRect(
                bounds.xMin + RoomSpawnPoints.WallInset,
                bounds.yMin + RoomSpawnPoints.WallInset,
                bounds.xMax - RoomSpawnPoints.WallInset,
                bounds.yMax - RoomSpawnPoints.WallInset);

            Gizmos.color = Color.yellow;
            DrawRect(bounds);

            Gizmos.color = Color.cyan;
            DrawRect(inner);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(new Vector3(0f, bounds.yMax, 0f), RoomSpawnPoints.DoorClearance);
            Gizmos.DrawWireSphere(new Vector3(0f, bounds.yMin, 0f), RoomSpawnPoints.DoorClearance);
            Gizmos.DrawWireSphere(new Vector3(bounds.xMax, 0f, 0f), RoomSpawnPoints.DoorClearance);
            Gizmos.DrawWireSphere(new Vector3(bounds.xMin, 0f, 0f), RoomSpawnPoints.DoorClearance);

            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(Vector3.zero, RoomSpawnPoints.CentreClearance);
        }

        private static void DrawRect(Rect rect)
        {
            var bottomLeft = new Vector3(rect.xMin, rect.yMin, 0f);
            var bottomRight = new Vector3(rect.xMax, rect.yMin, 0f);
            var topRight = new Vector3(rect.xMax, rect.yMax, 0f);
            var topLeft = new Vector3(rect.xMin, rect.yMax, 0f);

            Gizmos.DrawLine(bottomLeft, bottomRight);
            Gizmos.DrawLine(bottomRight, topRight);
            Gizmos.DrawLine(topRight, topLeft);
            Gizmos.DrawLine(topLeft, bottomLeft);
        }
#endif
```

- [ ] **Step 2: Verify compilation**

Confirm Unity recompiles with no errors (open the Editor, or run any single
existing Dungeon EditMode test — a compile error would fail it too).

- [ ] **Step 3: Manual verification**

In the Unity Editor, create an empty GameObject, add `RoomContentTemplate`,
select it, and confirm the Scene view shows: a yellow 44×30 rectangle, a cyan
inset rectangle 5 units in from each side, four red circles (radius 9) at the
midpoint of each edge, and a magenta circle (radius 5) at the center. This is
the reference the designer uses while placing obstacles and
`EnemySpawnMarker`s in a template prefab.

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Dungeon/RoomContentTemplate.cs
git commit -m "feat: RoomContentTemplate에 안전지대 기즈모를 그린다"
```

---

## After This Plan

Content authoring (obstacle art, building actual `RoomContentTemplate`
prefabs, assigning them to the Dungeon scene's `DungeonEncounter.
normalRoomTemplates`) is manual design work for the user in the Unity Editor,
not further code — this plan only builds the mechanism.
