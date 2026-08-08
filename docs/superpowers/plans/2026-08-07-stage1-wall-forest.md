# Stage1 벽쪽 숲 배치 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Stage1 던전 방의 문이 없는 벽쪽에 `wall_forest_0`~`wall_forest_8` 스프라이트로 빈틈없는 숲 라인을 둘러, 지금은 안 보이는 충돌 경계를 시각적으로 드러낸다.

**Architecture:** `RoomShape`에 벽 폴리라인이 어느 변(남/동/북/서)에 속하는지 알려주는 `WallSides`를 추가한다. 신규 정적 클래스 `WallForestDecorator`가 방 네 꼭짓점에 코너 스프라이트를 고정 배치하고, 각 벽 폴리라인 구간을 따라 방향에 맞는 가장자리 스프라이트를 빈틈없이 채운 뒤 좌우 반전과 높이 미세 변화로 다양성을 준다. `RoomBuilder.Build`가 마지막 단계로 이 데코레이터를 호출하고, `DungeonRunner.Enter`가 이미 계산해 두는 방 시드를 그대로 넘겨준다.

**Tech Stack:** Unity 6000.5.5f1, C# (네임스페이스 `NaManMoo.Dungeon`), NUnit + Unity Test Framework(EditMode/PlayMode).

## Global Constraints

- 플레이어·몬스터·보스·무기의 기존 밸런스 수치는 건드리지 않는다(이 작업과 무관).
- 이번 작업에서 수정한 파일과 직접 관련된 테스트만 실행한다. 전체 테스트 스위트나 관련 없는 회귀 테스트는 실행하지 않는다.
- Unity 테스트는 항상 `-testFilter`로 좁혀서 실행한다(`-runTests` 단독 실행 금지).
- `RoomKind`별 분기를 넣지 않는다 — 모든 방에 동일하게 적용한다.
- 충돌 경계(`OutdoorRoomGeometry.SafetyBounds`, `RoomBuilder.CreateSafetyBoundary`)는 변경하지 않는다. 숲은 순수 시각 요소이며 개별 콜라이더를 갖지 않는다.
- `wall_forest.png`는 이미 `wall_forest_0`~`wall_forest_8`로 슬라이스되어 있다(`Assets/Resources/Stage1/Wall/wall_forest.png`) — 재슬라이싱하지 않는다.
- 스프라이트 인덱스 매핑(3x3 격자, row-major): `0`=좌상단 코너, `1`=위쪽 변, `2`=우상단 코너, `3`=왼쪽 변, `4`=중앙(미사용), `5`=오른쪽 변, `6`=좌하단 코너, `7`=아래쪽 변, `8`=우하단 코너.
- Unity 테스트 실행 커맨드 형식(이 저장소의 기존 관례):
  ```powershell
  & 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Users\myong\NaManMoo\Namanmoo_Client' -runTests -testPlatform EditMode -testFilter '<ClassName>' -testResults 'C:\Users\myong\NaManMoo\Namanmoo_Client\Artifacts\<label>.xml' -logFile 'C:\Users\myong\NaManMoo\Namanmoo_Client\Artifacts\<label>.log'
  ```
  PlayMode 테스트는 `-testPlatform PlayMode`로 바꾼다.

---

## Task 1: `RoomShape.WallSides` 추가

**Files:**
- Modify: `Assets/Scripts/Dungeon/RoomShape.cs:53-127` (생성자, `Build`, `AddSide`)
- Test: `Assets/Tests/Editor/RoomShapeTests.cs`

**Interfaces:**
- Consumes: 없음(기존 `RoomShape` 내부 확장).
- Produces: `RoomShape.WallSides` (`IReadOnlyList<Doors>`) — `Walls`와 인덱스가 1:1로 대응. 이후 태스크(`WallForestDecorator`)가 어느 변의 스프라이트를 쓸지 판단하는 데 쓴다.

- [ ] **Step 1: 실패하는 테스트 작성**

`Assets/Tests/Editor/RoomShapeTests.cs`의 `ARoomWithADoorHasTwoWallSegmentsOnThatSide` 테스트 아래에 추가:

```csharp
    [Test]
    public void WallSidesAlignOneToOneWithWallsWhenNoDoorsExist()
    {
        RoomShape shape = RoomShape.Build(1, Doors.None);

        Assert.That(shape.WallSides.Count, Is.EqualTo(shape.Walls.Count));
        Assert.That(shape.WallSides, Is.EqualTo(new[]
        {
            Doors.South, Doors.East, Doors.North, Doors.West
        }));
    }

    [Test]
    public void WallSidesSplitAlongsideTheirWallWhenThatSideHasADoor()
    {
        RoomShape shape = RoomShape.Build(1, Doors.North);

        Assert.That(shape.WallSides, Is.EqualTo(new[]
        {
            Doors.South, Doors.East, Doors.North, Doors.North, Doors.West
        }));
    }
```

- [ ] **Step 2: 테스트가 실패하는지 확인**

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Users\myong\NaManMoo\Namanmoo_Client' -runTests -testPlatform EditMode -testFilter 'RoomShapeTests' -testResults 'C:\Users\myong\NaManMoo\Namanmoo_Client\Artifacts\roomshape-wallsides-red.xml' -logFile 'C:\Users\myong\NaManMoo\Namanmoo_Client\Artifacts\roomshape-wallsides-red.log'
```
Expected: `WallSides`가 없어서 컴파일 에러로 FAIL.

- [ ] **Step 3: 최소 구현**

`Assets/Scripts/Dungeon/RoomShape.cs`의 생성자(53-63행)를 다음으로 교체:

```csharp
        private RoomShape(
            Rect bounds,
            IReadOnlyList<Vector2> floorOutline,
            IReadOnlyList<IReadOnlyList<Vector2>> walls,
            IReadOnlyList<Doors> wallSides,
            IReadOnlyList<DoorOpening> doors)
        {
            Bounds = bounds;
            FloorOutline = floorOutline;
            Walls = walls;
            WallSides = wallSides;
            DoorOpenings = doors;
        }
```

`Walls` 프로퍼티(72행) 바로 아래에 추가:

```csharp
        /// <summary>Walls[i]가 어느 변에 속하는지. Walls와 인덱스가 1:1로 대응한다.</summary>
        public IReadOnlyList<Doors> WallSides { get; }
```

`Build` 메서드(104-127행)를 다음으로 교체:

```csharp
        public static RoomShape Build(int seed, Doors doors)
        {
            var rng = new DeterministicRandom(seed);
            Rect bounds = new Rect(-Size.x * 0.5f, -Size.y * 0.5f, Size.x, Size.y);

            var floor = new List<Vector2>
            {
                new Vector2(bounds.xMin, bounds.yMin),
                new Vector2(bounds.xMax, bounds.yMin),
                new Vector2(bounds.xMax, bounds.yMax),
                new Vector2(bounds.xMin, bounds.yMax)
            };

            var openings = new List<DoorOpening>();
            var walls = new List<IReadOnlyList<Vector2>>();
            var wallSides = new List<Doors>();

            // 네 변을 각각 훑는다. 문이 있으면 그 구간을 비우고 양쪽으로 벽을 나눈다.
            AddSide(walls, wallSides, openings, bounds, doors, Doors.South, ref rng);
            AddSide(walls, wallSides, openings, bounds, doors, Doors.East, ref rng);
            AddSide(walls, wallSides, openings, bounds, doors, Doors.North, ref rng);
            AddSide(walls, wallSides, openings, bounds, doors, Doors.West, ref rng);

            return new RoomShape(bounds, floor, walls, wallSides, openings);
        }

        private static void AddSide(
            List<IReadOnlyList<Vector2>> walls,
            List<Doors> wallSides,
            List<DoorOpening> openings,
            Rect bounds,
            Doors doors,
            Doors side,
            ref DeterministicRandom rng)
        {
            // 변을 따라 진행하는 방향과, 방 안쪽으로 들어가는 방향
            Vector2 start;
            Vector2 along;
            Vector2 inward;
            float length;

            switch (side)
            {
                case Doors.South:
                    start = new Vector2(bounds.xMin, bounds.yMin);
                    along = Vector2.right;
                    inward = Vector2.up;
                    length = bounds.width;
                    break;
                case Doors.East:
                    start = new Vector2(bounds.xMax, bounds.yMin);
                    along = Vector2.up;
                    inward = Vector2.left;
                    length = bounds.height;
                    break;
                case Doors.North:
                    start = new Vector2(bounds.xMax, bounds.yMax);
                    along = Vector2.left;
                    inward = Vector2.down;
                    length = bounds.width;
                    break;
                default:
                    start = new Vector2(bounds.xMin, bounds.yMax);
                    along = Vector2.down;
                    inward = Vector2.right;
                    length = bounds.height;
                    break;
            }

            bool hasDoor = doors.HasFlag(side);
            float middle = length * 0.5f;
            float gapFrom = middle - DoorWidth * 0.5f;
            float gapTo = middle + DoorWidth * 0.5f;

            if (hasDoor)
            {
                Vector2 from = start + along * gapFrom;
                Vector2 to = start + along * gapTo;
                openings.Add(new DoorOpening(side, start + along * middle, from, to));

                walls.Add(WobbleWall(start, along, inward, 0f, gapFrom, middle, ref rng));
                wallSides.Add(side);
                walls.Add(WobbleWall(start, along, inward, gapTo, length, middle, ref rng));
                wallSides.Add(side);
            }
            else
            {
                walls.Add(WobbleWall(start, along, inward, 0f, length, middle, ref rng));
                wallSides.Add(side);
            }
        }
```

(`WobbleWall`은 그대로 둔다 — 변경 없음.)

- [ ] **Step 4: 테스트가 통과하는지 확인**

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Users\myong\NaManMoo\Namanmoo_Client' -runTests -testPlatform EditMode -testFilter 'RoomShapeTests' -testResults 'C:\Users\myong\NaManMoo\Namanmoo_Client\Artifacts\roomshape-wallsides-green.xml' -logFile 'C:\Users\myong\NaManMoo\Namanmoo_Client\Artifacts\roomshape-wallsides-green.log'
```
Expected: 전부 PASS.

- [ ] **Step 5: 커밋**

```bash
git add Assets/Scripts/Dungeon/RoomShape.cs Assets/Tests/Editor/RoomShapeTests.cs
git commit -m "RoomShape에 벽 폴리라인별 변(WallSides) 정보 추가"
```

---

## Task 2: `WallForestDecorator` — 코너 배치

**Files:**
- Create: `Assets/Scripts/Dungeon/WallForestDecorator.cs`
- Create: `Assets/Tests/Editor/WallForestDecoratorTests.cs`

**Interfaces:**
- Consumes: `RoomShape.Bounds` (기존), `DeterministicRandom` (기존, 사용은 Task 4부터).
- Produces: `WallForestDecorator.Decorate(Transform parent, RoomShape shape, int roomSeed)` — Task 5에서 `RoomBuilder.Build`가 호출한다. `parent` 아래 `"Wall Forest"`라는 이름의 자식 오브젝트를 만들고 그 아래에 나무 타일들을 배치한다.

- [ ] **Step 1: 실패하는 테스트 작성**

`Assets/Tests/Editor/WallForestDecoratorTests.cs` 새로 작성:

```csharp
using NaManMoo.Dungeon;
using NUnit.Framework;
using UnityEngine;

public sealed class WallForestDecoratorTests
{
    [Test]
    public void PlacesACornerSpriteAtEachOfTheFourRoomCorners()
    {
        var root = new GameObject("Test Room Root");

        try
        {
            RoomShape shape = RoomShape.Build(1, Doors.None);
            WallForestDecorator.Decorate(root.transform, shape, roomSeed: 1);

            Transform forest = root.transform.Find("Wall Forest");
            Assert.That(forest, Is.Not.Null);

            AssertCorner(forest, "Corner TL", shape.Bounds.xMin, shape.Bounds.yMax, "wall_forest_0");
            AssertCorner(forest, "Corner TR", shape.Bounds.xMax, shape.Bounds.yMax, "wall_forest_2");
            AssertCorner(forest, "Corner BL", shape.Bounds.xMin, shape.Bounds.yMin, "wall_forest_6");
            AssertCorner(forest, "Corner BR", shape.Bounds.xMax, shape.Bounds.yMin, "wall_forest_8");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void AssertCorner(
        Transform forest, string name, float x, float y, string spriteName)
    {
        Transform corner = forest.Find(name);
        Assert.That(corner, Is.Not.Null, $"{name} 오브젝트가 없다");

        SpriteRenderer renderer = corner.GetComponent<SpriteRenderer>();
        Assert.That(renderer, Is.Not.Null);
        Assert.That(
            renderer.sprite,
            Is.SameAs(LoadWallSprite(spriteName)));
        Assert.That(corner.localPosition, Is.EqualTo(new Vector3(x, y, 0f)));
    }

    // wall_forest.png는 Sprite Mode: Multiple로 슬라이스된 시트라 서브 스프라이트를
    // Resources.Load<Sprite>("경로/이름")로 직접 로드할 수 없다(항상 null). LoadAll로
    // 시트 전체를 불러온 뒤 이름으로 찾는다 — WallForestDecorator.LoadSprite와 동일한 방식.
    private static Sprite LoadWallSprite(string spriteName)
    {
        Sprite[] allSprites = Resources.LoadAll<Sprite>("Stage1/Wall/wall_forest");
        return System.Array.Find(allSprites, s => s.name == spriteName);
    }
}
```

- [ ] **Step 2: 테스트가 실패하는지 확인**

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Users\myong\NaManMoo\Namanmoo_Client' -runTests -testPlatform EditMode -testFilter 'WallForestDecoratorTests' -testResults 'C:\Users\myong\NaManMoo\Namanmoo_Client\Artifacts\wallforest-corners-red.xml' -logFile 'C:\Users\myong\NaManMoo\Namanmoo_Client\Artifacts\wallforest-corners-red.log'
```
Expected: `WallForestDecorator`가 없어서 컴파일 에러로 FAIL.

- [ ] **Step 3: 최소 구현**

`Assets/Scripts/Dungeon/WallForestDecorator.cs` 새로 작성:

```csharp
using System;
using UnityEngine;

namespace NaManMoo.Dungeon
{
    /// <summary>
    /// 문이 없는 벽쪽에 숲 스프라이트를 둘러 플레이어가 못 지나가는 경계를 보여준다.
    /// 순수 시각 요소라 콜라이더는 만들지 않는다 — 실제 충돌은 RoomBuilder가 만드는
    /// Safety Boundary가 담당한다.
    /// </summary>
    public static class WallForestDecorator
    {
        private const string RootName = "Wall Forest";
        private const int ForestOrder = 2;
        private const string SpriteSheetPath = "Stage1/Wall/wall_forest";

        public static void Decorate(Transform parent, RoomShape shape, int roomSeed)
        {
            var root = new GameObject(RootName);
            root.transform.SetParent(parent, false);

            PlaceCorners(root.transform, shape.Bounds);
        }

        private static void PlaceCorners(Transform parent, Rect bounds)
        {
            PlaceTile(parent, "Corner TL", LoadSprite(0),
                new Vector2(bounds.xMin, bounds.yMax), Vector2.one, 0f);
            PlaceTile(parent, "Corner TR", LoadSprite(2),
                new Vector2(bounds.xMax, bounds.yMax), Vector2.one, 0f);
            PlaceTile(parent, "Corner BL", LoadSprite(6),
                new Vector2(bounds.xMin, bounds.yMin), Vector2.one, 0f);
            PlaceTile(parent, "Corner BR", LoadSprite(8),
                new Vector2(bounds.xMax, bounds.yMin), Vector2.one, 0f);
        }

        private static void PlaceTile(
            Transform parent,
            string name,
            Sprite sprite,
            Vector2 center,
            Vector2 scale,
            float angle)
        {
            var tile = new GameObject(name);
            tile.transform.SetParent(parent, false);
            tile.transform.localPosition = new Vector3(center.x, center.y, 0f);
            tile.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            tile.transform.localScale = new Vector3(scale.x, scale.y, 1f);

            SpriteRenderer renderer = tile.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = ForestOrder;
        }

        private static Sprite LoadSprite(int index)
        {
            // wall_forest.png는 Sprite Mode: Multiple로 슬라이스된 시트라 서브 스프라이트를
            // Resources.Load<Sprite>("경로/이름")로 직접 로드할 수 없다(항상 null). LoadAll로
            // 시트 전체를 불러온 뒤 이름으로 찾는다.
            string targetName = "wall_forest_" + index;
            Sprite[] allSprites = Resources.LoadAll<Sprite>(SpriteSheetPath);
            Sprite sprite = Array.Find(allSprites, s => s.name == targetName);
            if (sprite == null)
            {
                throw new InvalidOperationException(
                    $"Missing wall forest sprite: {targetName}");
            }

            return sprite;
        }
    }
}
```

- [ ] **Step 4: 테스트가 통과하는지 확인**

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Users\myong\NaManMoo\Namanmoo_Client' -runTests -testPlatform EditMode -testFilter 'WallForestDecoratorTests' -testResults 'C:\Users\myong\NaManMoo\Namanmoo_Client\Artifacts\wallforest-corners-green.xml' -logFile 'C:\Users\myong\NaManMoo\Namanmoo_Client\Artifacts\wallforest-corners-green.log'
```
Expected: PASS.

- [ ] **Step 5: 커밋**

```bash
git add Assets/Scripts/Dungeon/WallForestDecorator.cs Assets/Tests/Editor/WallForestDecoratorTests.cs
git commit -m "WallForestDecorator 추가: 방 네 꼭짓점에 코너 숲 스프라이트 배치"
```

---

## Task 3: 변 타일링 — 빈틈없이 채우기

**Files:**
- Modify: `Assets/Scripts/Dungeon/WallForestDecorator.cs`
- Modify: `Assets/Tests/Editor/WallForestDecoratorTests.cs`

**Interfaces:**
- Consumes: `RoomShape.Walls`, `RoomShape.WallSides` (Task 1).
- Produces: 동일한 `Decorate` — 이제 코너 사이 벽도 채운다. 타일링 축 크기는 항상 "구간에 정확히 맞춘 값"이라 이후 태스크가 반전/미세 변화를 얹어도 총 길이는 변하지 않는다(겹침/빈틈 없음 보장은 여기서 확정).

- [ ] **Step 1: 실패하는 테스트 작성**

`WallForestDecoratorTests.cs` 맨 아래에 추가 (`using System.Collections.Generic;`을 파일 상단에 추가):

```csharp
using System.Collections.Generic;
```

테스트 클래스 안에 추가:

```csharp
    [Test]
    public void NorthEdgeTilesCoverTheWallLengthWithoutGapsOrOverlap()
    {
        var root = new GameObject("Test Room Root");

        try
        {
            RoomShape shape = RoomShape.Build(1, Doors.None);
            WallForestDecorator.Decorate(root.transform, shape, roomSeed: 1);

            Transform forest = root.transform.Find("Wall Forest");
            Sprite northSprite = LoadWallSprite("wall_forest_1");

            // renderer.bounds는 회전을 반영한 월드 축정렬 박스라 벽이 살짝 기울어진
            // 구간에서는 실제 타일링 폭보다 크게 나온다. 로컬 스케일 기준 폭을 직접 합산한다.
            float totalWidth = 0f;
            int tileCount = 0;
            foreach (Transform child in forest)
            {
                SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
                if (renderer.sprite != northSprite)
                {
                    continue;
                }

                totalWidth += Mathf.Abs(child.localScale.x) * northSprite.bounds.size.x;
                tileCount++;
            }

            float northWallLength = WallLength(shape, Doors.North);

            Assert.That(tileCount, Is.GreaterThan(0));
            Assert.That(totalWidth, Is.EqualTo(northWallLength).Within(0.01f));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void EastEdgeTilesCoverTheWallLengthWithoutGapsOrOverlap()
    {
        var root = new GameObject("Test Room Root");

        try
        {
            RoomShape shape = RoomShape.Build(1, Doors.None);
            WallForestDecorator.Decorate(root.transform, shape, roomSeed: 1);

            Transform forest = root.transform.Find("Wall Forest");
            Sprite eastSprite = LoadWallSprite("wall_forest_5");

            // North 테스트와 동일한 이유로 로컬 스케일 기준 폭을 합산한다. East/West처럼 벽이
            // 세로 방향인 변도 회전 후에는 로컬 X축이 벽을 따라가는 방향이 되므로(PlaceSegment의
            // 회전 공식은 side와 무관하게 항상 로컬 X를 벽 방향에 맞춘다), North와 동일하게
            // localScale.x 기준으로 합산해야 한다.
            float totalWidth = 0f;
            int tileCount = 0;
            foreach (Transform child in forest)
            {
                SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
                if (renderer.sprite != eastSprite)
                {
                    continue;
                }

                totalWidth += Mathf.Abs(child.localScale.x) * eastSprite.bounds.size.x;
                tileCount++;
            }

            float eastWallLength = WallLength(shape, Doors.East);

            Assert.That(tileCount, Is.GreaterThan(0));
            Assert.That(totalWidth, Is.EqualTo(eastWallLength).Within(0.01f));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void EdgeTilesUseTheSpriteMatchingTheirSide()
    {
        var root = new GameObject("Test Room Root");

        try
        {
            RoomShape shape = RoomShape.Build(2, Doors.None);
            WallForestDecorator.Decorate(root.transform, shape, roomSeed: 2);

            Transform forest = root.transform.Find("Wall Forest");
            Sprite south = LoadWallSprite("wall_forest_7");
            Sprite east = LoadWallSprite("wall_forest_5");
            Sprite west = LoadWallSprite("wall_forest_3");

            bool hasSouth = false;
            bool hasEast = false;
            bool hasWest = false;
            foreach (Transform child in forest)
            {
                Sprite sprite = child.GetComponent<SpriteRenderer>().sprite;
                hasSouth |= sprite == south;
                hasEast |= sprite == east;
                hasWest |= sprite == west;
            }

            Assert.That(hasSouth, Is.True, "남쪽 변에 wall_forest_7이 없다");
            Assert.That(hasEast, Is.True, "동쪽 변에 wall_forest_5가 없다");
            Assert.That(hasWest, Is.True, "서쪽 변에 wall_forest_3이 없다");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void NoEdgeTileSitsInsideADoorGap()
    {
        var root = new GameObject("Test Room Root");

        try
        {
            RoomShape shape = RoomShape.Build(3, Doors.North);
            WallForestDecorator.Decorate(root.transform, shape, roomSeed: 3);

            shape.TryGetDoor(Doors.North, out DoorOpening door);
            float half = RoomShape.DoorWidth * 0.5f;

            Transform forest = root.transform.Find("Wall Forest");
            Sprite northSprite = LoadWallSprite("wall_forest_1");

            foreach (Transform child in forest)
            {
                SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
                if (renderer.sprite != northSprite)
                {
                    continue;
                }

                float x = child.localPosition.x;
                Assert.That(
                    Mathf.Abs(x - door.Center.x) >= half,
                    Is.True,
                    $"북쪽 문 간격 안에 나무 타일이 있다 (x={x})");
            }
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static float WallLength(RoomShape shape, Doors side)
    {
        float length = 0f;
        for (int i = 0; i < shape.Walls.Count; i++)
        {
            if (shape.WallSides[i] != side)
            {
                continue;
            }

            IReadOnlyList<Vector2> wall = shape.Walls[i];
            for (int p = 0; p < wall.Count - 1; p++)
            {
                length += Vector2.Distance(wall[p], wall[p + 1]);
            }
        }

        return length;
    }
```

- [ ] **Step 2: 테스트가 실패하는지 확인**

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Users\myong\NaManMoo\Namanmoo_Client' -runTests -testPlatform EditMode -testFilter 'WallForestDecoratorTests' -testResults 'C:\Users\myong\NaManMoo\Namanmoo_Client\Artifacts\wallforest-edges-red.xml' -logFile 'C:\Users\myong\NaManMoo\Namanmoo_Client\Artifacts\wallforest-edges-red.log'
```
Expected: 세 테스트 모두 FAIL (변 타일이 아직 없음 — 코너만 생성됨).

- [ ] **Step 3: 구현 — 변 타일링 추가**

`WallForestDecorator.cs`의 `Decorate` 메서드를 다음으로 교체:

```csharp
        public static void Decorate(Transform parent, RoomShape shape, int roomSeed)
        {
            var root = new GameObject(RootName);
            root.transform.SetParent(parent, false);

            PlaceCorners(root.transform, shape.Bounds);
            PlaceEdges(root.transform, shape);
        }

        private static void PlaceEdges(Transform parent, RoomShape shape)
        {
            for (int i = 0; i < shape.Walls.Count; i++)
            {
                IReadOnlyList<Vector2> wall = shape.Walls[i];
                Doors side = shape.WallSides[i];
                Sprite edgeSprite = LoadSprite(EdgeSpriteIndex(side));

                for (int p = 0; p < wall.Count - 1; p++)
                {
                    PlaceSegment(parent, wall[p], wall[p + 1], edgeSprite);
                }
            }
        }

        private static void PlaceSegment(
            Transform parent,
            Vector2 from,
            Vector2 to,
            Sprite sprite)
        {
            Vector2 delta = to - from;
            float length = delta.magnitude;
            if (length < 0.0001f)
            {
                return;
            }

            // PlaceTile은 tile.transform.localRotation = Quaternion.Euler(0, 0, angle)로
            // angle = Atan2(delta.y, delta.x)만큼 회전시킨다. 이 회전 공식에서는 side(동서남북)와
            // 무관하게 로컬 X축이 항상 delta 방향(벽을 따라가는 방향)과 정확히 일치하게 된다 —
            // 로컬 Y축은 항상 벽에 수직인 두께 방향이 된다. 그래서 타일링(구간에 꽉 채우기) 크기는
            // side에 따라 분기하지 않고 항상 스프라이트의 X축 크기(spriteSize.x) 기준으로 계산하고,
            // fitScale도 항상 로컬 X(scale.x)에 곱한다. 예전에는 side가 East/West일 때 Y축 기준으로
            // 계산했는데, 그 축은 회전 후 벽과 나란해지지 않아 빈틈/겹침이 생겼다(Task 3 리뷰에서 발견).
            Vector2 spriteSize = sprite.bounds.size;
            float tileSize = spriteSize.x;
            int count = Mathf.Max(1, Mathf.RoundToInt(length / tileSize));
            float slot = length / count;
            float fitScale = slot / tileSize;
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            Vector2 direction = delta / length;

            for (int i = 0; i < count; i++)
            {
                Vector2 center = from + direction * (slot * (i + 0.5f));
                Vector2 scale = new Vector2(fitScale, 1f);

                PlaceTile(parent, $"Edge {i}", sprite, center, scale, angle);
            }
        }

        private static int EdgeSpriteIndex(Doors side)
        {
            return side switch
            {
                Doors.North => 1,
                Doors.South => 7,
                Doors.East => 5,
                Doors.West => 3,
                _ => 4
            };
        }
```

`using System.Collections.Generic;`를 `WallForestDecorator.cs` 상단에 추가한다(`IReadOnlyList<Vector2>` 사용).

- [ ] **Step 4: 테스트가 통과하는지 확인**

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Users\myong\NaManMoo\Namanmoo_Client' -runTests -testPlatform EditMode -testFilter 'WallForestDecoratorTests' -testResults 'C:\Users\myong\NaManMoo\Namanmoo_Client\Artifacts\wallforest-edges-green.xml' -logFile 'C:\Users\myong\NaManMoo\Namanmoo_Client\Artifacts\wallforest-edges-green.log'
```
Expected: 전부 PASS.

- [ ] **Step 5: 커밋**

```bash
git add Assets/Scripts/Dungeon/WallForestDecorator.cs Assets/Tests/Editor/WallForestDecoratorTests.cs
git commit -m "WallForestDecorator: 문 간격을 피해 벽을 빈틈없이 나무로 채운다"
```

---

## Task 4: 반전 + 미세 크기 변화로 다양성 추가

**Files:**
- Modify: `Assets/Scripts/Dungeon/WallForestDecorator.cs`
- Modify: `Assets/Tests/Editor/WallForestDecoratorTests.cs`

**Interfaces:**
- Consumes: `DeterministicRandom(int seed)`, `DeterministicRandom.Chance(float)`, `DeterministicRandom.Next(int)` (기존, `Assets/Scripts/Dungeon/DeterministicRandom.cs`).
- Produces: 동일한 `Decorate` — 이제 `roomSeed`를 실제로 사용한다. 타일링 축(구간에 맞추는 크기)은 Task 3에서 확정한 그대로 두고, **높이(비-타일링 축)에만** ±15% 지터를 주고 타일링 축은 부호만 뒤집어(반전) 폭 합계가 절대 변하지 않게 한다 — 그래야 Task 3의 "빈틈없음" 보장이 계속 성립한다.

- [ ] **Step 1: 실패하는 테스트 작성**

`WallForestDecoratorTests.cs`에 추가:

```csharp
    [Test]
    public void SameRoomSeedProducesTheSameForestLayout()
    {
        var rootA = new GameObject("A");
        var rootB = new GameObject("B");

        try
        {
            RoomShape shape = RoomShape.Build(4, Doors.North | Doors.East);
            WallForestDecorator.Decorate(rootA.transform, shape, roomSeed: 99);
            WallForestDecorator.Decorate(rootB.transform, shape, roomSeed: 99);

            Transform forestA = rootA.transform.Find("Wall Forest");
            Transform forestB = rootB.transform.Find("Wall Forest");

            Assert.That(forestA.childCount, Is.EqualTo(forestB.childCount));

            for (int i = 0; i < forestA.childCount; i++)
            {
                Transform childA = forestA.GetChild(i);
                Transform childB = forestB.GetChild(i);

                Assert.That(childA.localPosition, Is.EqualTo(childB.localPosition));
                Assert.That(childA.localScale, Is.EqualTo(childB.localScale));
            }
        }
        finally
        {
            Object.DestroyImmediate(rootA);
            Object.DestroyImmediate(rootB);
        }
    }

    [Test]
    public void EdgeTileHeightsVaryInsteadOfBeingPerfectlyUniform()
    {
        var root = new GameObject("Test Room Root");

        try
        {
            RoomShape shape = RoomShape.Build(1, Doors.None);
            WallForestDecorator.Decorate(root.transform, shape, roomSeed: 5);

            // 북쪽 변 하나로 좁힌다 — 문이 없는 방에서 북쪽 벽은 한 폴리라인 안에서
            // 구간 길이가 전부 같아 지터가 없으면 높이 배율이 항상 1.0으로 똑같다.
            // 다른 변까지 섞어서 보면 변마다 다른 fitScale 때문에 지터 없이도
            // "값이 2개 이상"으로 보여 테스트가 아무 의미 없이 통과해 버린다.
            Transform forest = root.transform.Find("Wall Forest");
            Sprite northSprite = LoadWallSprite("wall_forest_1");
            var heightScales = new HashSet<float>();

            foreach (Transform child in forest)
            {
                SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
                if (renderer.sprite == northSprite)
                {
                    heightScales.Add(child.localScale.y);
                }
            }

            Assert.That(
                heightScales.Count, Is.GreaterThan(1),
                "북쪽 타일들의 높이 배율이 전부 같다 — 변화가 적용되지 않았다");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void NorthEdgeTilesStillCoverTheWallLengthAfterVariationIsApplied()
    {
        var root = new GameObject("Test Room Root");

        try
        {
            RoomShape shape = RoomShape.Build(1, Doors.None);
            WallForestDecorator.Decorate(root.transform, shape, roomSeed: 5);

            Transform forest = root.transform.Find("Wall Forest");
            Sprite northSprite = LoadWallSprite("wall_forest_1");

            // Task 3의 이유와 동일하게 renderer.bounds가 아니라 로컬 스케일로 폭을 잰다.
            float totalWidth = 0f;
            foreach (Transform child in forest)
            {
                SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
                if (renderer.sprite == northSprite)
                {
                    totalWidth += Mathf.Abs(child.localScale.x) * northSprite.bounds.size.x;
                }
            }

            Assert.That(totalWidth, Is.EqualTo(WallLength(shape, Doors.North)).Within(0.01f));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }
```

- [ ] **Step 2: 테스트가 실패하는지 확인**

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Users\myong\NaManMoo\Namanmoo_Client' -runTests -testPlatform EditMode -testFilter 'WallForestDecoratorTests' -testResults 'C:\Users\myong\NaManMoo\Namanmoo_Client\Artifacts\wallforest-variety-red.xml' -logFile 'C:\Users\myong\NaManMoo\Namanmoo_Client\Artifacts\wallforest-variety-red.log'
```
Expected: `EdgeTileHeightsVaryInsteadOfBeingPerfectlyUniform`는 FAIL(전부 1.0), 나머지는 이미 PASS(아직 변화가 없으므로).

- [ ] **Step 3: 구현 — 반전/미세 변화 추가**

`WallForestDecorator.cs`에서 `RootName`/`ForestOrder`/`SpritePathPrefix` 상수 옆에 추가:

```csharp
        private const float MinHeightJitter = 0.85f;
        private const float HeightJitterRange = 0.30f; // 0.85 ~ 1.15배
```

`Decorate`를 다음으로 교체:

```csharp
        public static void Decorate(Transform parent, RoomShape shape, int roomSeed)
        {
            var root = new GameObject(RootName);
            root.transform.SetParent(parent, false);

            var rng = new DeterministicRandom(roomSeed);

            PlaceCorners(root.transform, shape.Bounds);
            PlaceEdges(root.transform, shape, ref rng);
        }
```

`PlaceEdges`와 `PlaceSegment`를 다음으로 교체:

```csharp
        private static void PlaceEdges(
            Transform parent, RoomShape shape, ref DeterministicRandom rng)
        {
            for (int i = 0; i < shape.Walls.Count; i++)
            {
                IReadOnlyList<Vector2> wall = shape.Walls[i];
                Doors side = shape.WallSides[i];
                Sprite edgeSprite = LoadSprite(EdgeSpriteIndex(side));

                for (int p = 0; p < wall.Count - 1; p++)
                {
                    PlaceSegment(parent, wall[p], wall[p + 1], edgeSprite, ref rng);
                }
            }
        }

        private static void PlaceSegment(
            Transform parent,
            Vector2 from,
            Vector2 to,
            Sprite sprite,
            ref DeterministicRandom rng)
        {
            Vector2 delta = to - from;
            float length = delta.magnitude;
            if (length < 0.0001f)
            {
                return;
            }

            // Task 3와 동일한 이유로(PlaceTile의 회전 공식상 로컬 X축이 항상 벽 방향과
            // 일치한다) 타일링 축은 side와 무관하게 항상 로컬 X, 비-타일링(지터) 축은 항상
            // 로컬 Y다.
            Vector2 spriteSize = sprite.bounds.size;
            float tileSize = spriteSize.x;
            int count = Mathf.Max(1, Mathf.RoundToInt(length / tileSize));
            float slot = length / count;
            float fitScale = slot / tileSize;
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            Vector2 direction = delta / length;

            for (int i = 0; i < count; i++)
            {
                Vector2 center = from + direction * (slot * (i + 0.5f));

                // 타일링 축(로컬 X)은 구간에 딱 맞춘 크기를 유지한다 — 반전은 부호만 뒤집어
                // 폭 합은 그대로다. 높이(로컬 Y, 비-타일링 축)만 지터를 줘서 빈틈없음이
                // 깨지지 않는다.
                bool flip = rng.Chance(0.5f);
                float alongScale = flip ? -fitScale : fitScale;
                float offScale = MinHeightJitter + rng.Next(1000) / 1000f * HeightJitterRange;

                Vector2 scale = new Vector2(alongScale, offScale);

                PlaceTile(parent, $"Edge {i}", sprite, center, scale, angle);
            }
        }
```

- [ ] **Step 4: 테스트가 통과하는지 확인**

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Users\myong\NaManMoo\Namanmoo_Client' -runTests -testPlatform EditMode -testFilter 'WallForestDecoratorTests' -testResults 'C:\Users\myong\NaManMoo\Namanmoo_Client\Artifacts\wallforest-variety-green.xml' -logFile 'C:\Users\myong\NaManMoo\Namanmoo_Client\Artifacts\wallforest-variety-green.log'
```
Expected: 전부 PASS(이전 태스크에서 작성한 테스트 포함, `WallForestDecoratorTests` 전체).

- [ ] **Step 5: 커밋**

```bash
git add Assets/Scripts/Dungeon/WallForestDecorator.cs Assets/Tests/Editor/WallForestDecoratorTests.cs
git commit -m "WallForestDecorator: 좌우 반전과 높이 지터로 손그림 느낌 추가"
```

---

## Task 5: `RoomBuilder` 연결

**Files:**
- Modify: `Assets/Scripts/Dungeon/RoomBuilder.cs:26-32` (`Build`)
- Modify: `Assets/Tests/Editor/RoomBuilderOutdoorTests.cs`

**Interfaces:**
- Consumes: `WallForestDecorator.Decorate(Transform, RoomShape, int)` (Task 2-4).
- Produces: `RoomBuilder.Build(Transform parent, RoomShape shape, RoomKind kind, int roomSeed)` — Task 6에서 `DungeonRunner`가 호출한다.

- [ ] **Step 1: 기존 테스트를 새 시그니처에 맞게 수정 (실패 예상)**

`Assets/Tests/Editor/RoomBuilderOutdoorTests.cs`의 `Build` 호출부(17-18행)를 교체:

```csharp
            List<DungeonDoor> doors = RoomBuilder.Build(
                root.transform, shape, RoomKind.Normal, roomSeed: 17);
```

파일 맨 아래쪽의 렌더러 개수 단언(109-116행)을 교체:

```csharp
            Transform forestRoot = root.transform.Find("Wall Forest");
            Assert.That(forestRoot, Is.Not.Null);
            Assert.That(
                forestRoot.GetComponentsInChildren<SpriteRenderer>(),
                Has.Length.GreaterThan(0));

            SpriteRenderer[] nonForestRenderers = System.Array.FindAll(
                root.GetComponentsInChildren<SpriteRenderer>(includeInactive: true),
                r => !r.transform.IsChildOf(forestRoot));
            Assert.That(nonForestRenderers, Has.Length.EqualTo(5));
            Assert.That(nonForestRenderers, Does.Contain(ground));
            Assert.That(nonForestRenderers, Does.Contain(northRenderer));
            Assert.That(nonForestRenderers, Does.Contain(eastRenderer));
            Assert.That(nonForestRenderers, Does.Contain(northOuterRenderer));
            Assert.That(nonForestRenderers, Does.Contain(eastOuterRenderer));
```

- [ ] **Step 2: 테스트가 실패하는지 확인**

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Users\myong\NaManMoo\Namanmoo_Client' -runTests -testPlatform EditMode -testFilter 'RoomBuilderOutdoorTests' -testResults 'C:\Users\myong\NaManMoo\Namanmoo_Client\Artifacts\roombuilder-forest-red.xml' -logFile 'C:\Users\myong\NaManMoo\Namanmoo_Client\Artifacts\roombuilder-forest-red.log'
```
Expected: `RoomBuilder.Build`가 4개 인자를 안 받아서 컴파일 에러로 FAIL.

- [ ] **Step 3: `RoomBuilder.Build`에 숲 연결**

`Assets/Scripts/Dungeon/RoomBuilder.cs:26-32`을 교체:

```csharp
        public static List<DungeonDoor> Build(
            Transform parent, RoomShape shape, RoomKind kind, int roomSeed)
        {
            CreateGround(parent, shape);
            CreateDoorPaths(parent, shape);
            CreateSafetyBoundary(parent, shape);
            WallForestDecorator.Decorate(parent, shape, roomSeed);
            return CreateDoors(parent, shape);
        }
```

- [ ] **Step 4: 테스트가 통과하는지 확인**

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Users\myong\NaManMoo\Namanmoo_Client' -runTests -testPlatform EditMode -testFilter 'RoomBuilderOutdoorTests' -testResults 'C:\Users\myong\NaManMoo\Namanmoo_Client\Artifacts\roombuilder-forest-green.xml' -logFile 'C:\Users\myong\NaManMoo\Namanmoo_Client\Artifacts\roombuilder-forest-green.log'
```
Expected: PASS.

- [ ] **Step 5: 커밋**

```bash
git add Assets/Scripts/Dungeon/RoomBuilder.cs Assets/Tests/Editor/RoomBuilderOutdoorTests.cs
git commit -m "RoomBuilder가 방을 지을 때 WallForestDecorator를 호출하도록 연결"
```

---

## Task 6: `DungeonRunner`에서 방 시드 전달

**Files:**
- Modify: `Assets/Scripts/Dungeon/DungeonRunner.cs:106-135` (`Enter`)

**Interfaces:**
- Consumes: `RoomBuilder.Build(Transform, RoomShape, RoomKind, int)` (Task 5).
- Produces: 없음(최종 배선 — 이 이후로 다른 태스크가 이어받지 않는다).

- [ ] **Step 1: `Enter` 수정**

`Assets/Scripts/Dungeon/DungeonRunner.cs:106-135`을 다음으로 교체:

```csharp
        private void Enter(Vector2Int cell, Doors entrySide)
        {
            DungeonRoom room = Layout.RoomAt(cell);
            if (room == null)
            {
                Debug.LogError($"{cell} 에는 방이 없습니다. 문 배치와 층 배치가 어긋났습니다.");
                return;
            }

            Teardown();

            CurrentCell = cell;
            int roomSeed = DungeonNavigation.RoomSeed(seed, floor, cell);
            CurrentShape = RoomShape.Build(roomSeed, room.Doors);

            roomRoot = new GameObject(RoomRootName);
            roomRoot.transform.SetParent(transform, false);

            doors.AddRange(
                RoomBuilder.Build(roomRoot.transform, CurrentShape, room.Kind, roomSeed));
            foreach (DungeonDoor door in doors)
            {
                door.Passed += OnDoorPassed;
            }

            PlacePlayer(entrySide);
            UpdateCamera();
            StartEncounter(room);

            RoomChanged?.Invoke(cell);
        }
```

- [ ] **Step 2: 컴파일 확인 + 관련 PlayMode 테스트 실행**

`DungeonRunner.Enter`를 직접 테스트하는 것은 `DungeonRunnerPlayModeTests`뿐이다(방 전환이 실제로 도는지 확인). 이 태스크는 내부 호출 배선만 바꾸는 것이라 새 테스트를 추가하지 않고 기존 테스트로 회귀만 확인한다.

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Users\myong\NaManMoo\Namanmoo_Client' -runTests -testPlatform PlayMode -testFilter 'DungeonRunnerPlayModeTests' -testResults 'C:\Users\myong\NaManMoo\Namanmoo_Client\Artifacts\dungeonrunner-forest-wiring.xml' -logFile 'C:\Users\myong\NaManMoo\Namanmoo_Client\Artifacts\dungeonrunner-forest-wiring.log'
```
Expected: 전부 PASS(동작 변화 없음 — 시드를 두 번 계산하던 걸 한 번으로 합치고 그 값을 `RoomBuilder.Build`에도 넘기는 배선 변경뿐).

- [ ] **Step 3: 커밋**

```bash
git add Assets/Scripts/Dungeon/DungeonRunner.cs
git commit -m "DungeonRunner: 방 시드를 한 번만 계산해 RoomBuilder.Build에도 전달"
```

---

## Task 7: 에디터에서 육안 확인 (수동 QA)

이 기능은 시각 요소라 자동 테스트만으로는 "제대로 보이는지"를 확인할 수 없다. 아래를 직접 확인한다.

- [ ] **Step 1: Unity 에디터에서 `Assets/Scenes/Dungeon.unity` 씬을 열고 Play 모드로 진입한다.**

- [ ] **Step 2: 다음을 육안으로 확인한다.**
  - 문이 없는 벽쪽에는 숲이 빈틈없이 이어져 보인다.
  - 문이 있는 구간(통로)에는 숲이 없고 그대로 지나갈 수 있다.
  - 네 모서리에 코너 스프라이트가 자연스럽게 이어진다(예를 들어 확대해서 봤을 때 심하게 어긋나지 않는다).
  - 나무가 좌우 반전/높이 변화로 완전히 똑같이 반복되지는 않는다.
  - 플레이어가 벽쪽으로 이동했을 때 나무를 밟고 지나가지 않고 자연스럽게 막힌다(기존 Safety Boundary 그대로).
  - 문을 지나 다음 방으로 이동해도 새 방에 숲이 잘 나온다.

- [ ] **Step 3: 문제가 있으면(스프라이트 어긋남, 크기 이상 등) 여기서 발견한 내용을 기록하고 필요한 만큼만 `WallForestDecorator.cs`를 조정한다.** 조정했다면 Task 3/4에서 작성한 `WallForestDecoratorTests`를 다시 돌려 회귀가 없는지 확인한다.

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Users\myong\NaManMoo\Namanmoo_Client' -runTests -testPlatform EditMode -testFilter 'WallForestDecoratorTests' -testResults 'C:\Users\myong\NaManMoo\Namanmoo_Client\Artifacts\wallforest-final-check.xml' -logFile 'C:\Users\myong\NaManMoo\Namanmoo_Client\Artifacts\wallforest-final-check.log'
```

- [ ] **Step 4: 조정한 내용이 있으면 커밋한다.**

```bash
git add Assets/Scripts/Dungeon/WallForestDecorator.cs
git commit -m "WallForestDecorator: 육안 확인 후 미세 조정"
```
(조정이 없었다면 이 태스크는 커밋 없이 종료한다.)
