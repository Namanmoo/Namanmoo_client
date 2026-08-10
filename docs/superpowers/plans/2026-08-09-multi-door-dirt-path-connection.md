# Multi-Door Dirt Path Connection Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 문이 두 개 이상인 던전 방에서 각 문 앞의 기존 8×8 흙길을 방 중앙까지 반복 배치해 직선·L자·T자·십자 형태로 연결한다.

**Architecture:** `DungeonLayout`과 `RoomShape`는 변경하지 않는다. `RoomBuilder`가 이미 계산된 `DoorOpenings` 수를 확인하고, 다중 문 방에서만 기존 문 안쪽 타일과 같은 Sprite를 8유닛 간격으로 중앙축을 덮을 때까지 추가한다. 단일 문 방과 문 바깥 타일은 현재 동작을 유지한다.

**Tech Stack:** Unity 6000.5.5f1, C#, SpriteRenderer, Resources API, NUnit, Unity Test Framework EditMode.

## Global Constraints

- 작업 브랜치는 `feat/stage1-dirt-path`이며 `main`에서 수정하지 않는다.
- `DungeonLayout`, 방 종류, 문 방향, 문 Trigger, 전투 잠금 및 방 전환을 변경하지 않는다.
- `RoomShape.Size` 44×30과 `RoomShape.DoorWidth` 6유닛을 유지한다.
- 문 하나인 방에는 이번 단계에서 Standalone Sprite를 적용하지 않는다.
- `Assets/Dungeon/RoomTemplates/NormalRoomTemplate_Example.prefab`의 사용자 변경을 수정하지 않는다.
- Unity 테스트는 `-testFilter RoomBuilderOutdoorTests`로 범위를 제한한다.
- 프로젝트 `AGENTS.md`에 따라 사용자가 별도로 요청하기 전에는 커밋하거나 푸시하지 않는다.

---

### Task 1: 다중 문 흙길을 중앙까지 연결

**Files:**
- Modify: `Assets/Tests/Editor/RoomBuilderOutdoorTests.cs`
- Modify: `Assets/Scripts/Dungeon/RoomBuilder.cs`

**Interfaces:**
- Consumes: `RoomShape.DoorOpenings`, `RoomShape.Bounds.center`, `DungeonNavigation.Inward(Doors)`, 기존 가로·세로 흙길 Sprite.
- Produces: 기존 이름 `Door Path {Side}` 및 `Door Path {Side} Outer`를 보존하고, 추가 안쪽 타일을 `Door Path {Side} Extension {index}`로 생성한다.

- [x] **Step 1: 다중 문 중앙 연결과 단일 문 보존 테스트 작성**

`RoomBuilderOutdoorTests.cs`에 `System.Linq`을 추가한다. 기존 북·동 테스트는 다음 추가 위치를 검증하도록 확장한다.

```csharp
Assert.That(
    root.transform.Find("Door Path North Extension 1").localPosition,
    Is.EqualTo(new Vector3(0f, 3f, 0f)));
Assert.That(
    root.transform.Find("Door Path East Extension 1").localPosition,
    Is.EqualTo(new Vector3(10f, 0f, 0f)));
Assert.That(
    root.transform.Find("Door Path East Extension 2").localPosition,
    Is.EqualTo(new Vector3(2f, 0f, 0f)));
```

같은 기존 테스트의 비숲 SpriteRenderer 개수는 Ground 1개, 문별 기존 타일 4개, Extension 3개를 합친 8개로 갱신하고, 각 Extension이 기존 방향 Sprite와 Sorting Order 1을 사용하는지도 확인한다.

```csharp
Assert.That(nonForestRenderers, Has.Length.EqualTo(8));
Assert.That(
    root.transform.Find("Door Path North Extension 1")
        .GetComponent<SpriteRenderer>().sprite,
    Is.SameAs(northRenderer.sprite));
Assert.That(
    root.transform.Find("Door Path East Extension 1")
        .GetComponent<SpriteRenderer>().sortingOrder,
    Is.EqualTo(1));
```

문 조합별로 각 방향의 마지막 안쪽 타일이 원점을 덮는지 검증한다.

```csharp
[TestCase(Doors.North | Doors.South)]
[TestCase(Doors.North | Doors.East | Doors.South)]
[TestCase(Doors.North | Doors.East | Doors.South | Doors.West)]
public void MultipleDoorPathsReachTheRoomCenter(Doors sides)
{
    var root = new GameObject("Test Room Root");
    try
    {
        RoomShape shape = RoomShape.Build(23, sides);
        RoomBuilder.Build(root.transform, shape, RoomKind.Normal, roomSeed: 23);

        foreach (DoorOpening opening in shape.DoorOpenings)
        {
            string prefix = $"Door Path {opening.Side}";
            SpriteRenderer[] innerPaths = root
                .GetComponentsInChildren<SpriteRenderer>()
                .Where(renderer =>
                    renderer.name.StartsWith(prefix) &&
                    !renderer.name.EndsWith("Outer"))
                .ToArray();

            Assert.That(
                innerPaths.Any(renderer => renderer.bounds.Contains(Vector3.zero)),
                Is.True,
                $"{opening.Side} 흙길이 방 중앙에 닿지 않았다");
        }
    }
    finally
    {
        Object.DestroyImmediate(root);
    }
}
```

단일 북쪽 문은 Extension을 만들지 않고 기존 두 타일만 유지하는 테스트를 추가한다.

```csharp
[Test]
public void SingleDoorKeepsOnlyTheExistingInnerAndOuterPaths()
{
    var root = new GameObject("Test Room Root");
    try
    {
        RoomShape shape = RoomShape.Build(29, Doors.North);
        RoomBuilder.Build(root.transform, shape, RoomKind.Normal, roomSeed: 29);

        Assert.That(root.transform.Find("Door Path North"), Is.Not.Null);
        Assert.That(root.transform.Find("Door Path North Outer"), Is.Not.Null);
        Assert.That(root.transform.Find("Door Path North Extension 1"), Is.Null);
    }
    finally
    {
        Object.DestroyImmediate(root);
    }
}
```

- [x] **Step 2: 새 테스트가 실패하는지 확인**

```powershell
New-Item -ItemType Directory -Force 'Artifacts' | Out-Null
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' `
  -batchmode -nographics `
  -projectPath 'C:\Users\dksco\Naman\Namanmoo_client' `
  -runTests -testPlatform EditMode `
  -testFilter 'RoomBuilderOutdoorTests' `
  -testResults 'C:\Users\dksco\Naman\Namanmoo_client\Artifacts\dirt-path-connection-red.xml' `
  -logFile 'C:\Users\dksco\Naman\Namanmoo_client\Artifacts\dirt-path-connection-red.log'
```

Expected: 추가 Extension 오브젝트가 없어 북·동 위치 테스트 또는 중앙 도달 테스트가 실패한다.

- [x] **Step 3: 중앙 연결 타일 최소 구현**

`RoomBuilder.cs`에 타일 간격을 명시한다.

```csharp
private const float DoorPathStep = DoorPathInset * 2f;
```

`CreateDoorPaths`에서 `shape.DoorOpenings.Count >= 2`일 때만 첫 안쪽 타일 이후의 Extension을 생성한다.

```csharp
bool connectToCenter = shape.DoorOpenings.Count >= 2;

CreateDoorPath(
    parent,
    $"Door Path {opening.Side}",
    opening.Center + inward * DoorPathInset,
    sprite);

if (connectToCenter)
{
    float centerDistance = Vector2.Dot(
        shape.Bounds.center - opening.Center,
        inward);
    int extensionIndex = 1;

    for (float inset = DoorPathInset + DoorPathStep;
         inset < centerDistance + DoorPathInset;
         inset += DoorPathStep)
    {
        CreateDoorPath(
            parent,
            $"Door Path {opening.Side} Extension {extensionIndex}",
            opening.Center + inward * inset,
            sprite);
        extensionIndex++;
    }
}
```

문 바깥 타일 생성 코드는 이 블록 뒤에 현재 이름과 위치 그대로 둔다.

- [x] **Step 4: 집중 EditMode 테스트 통과 확인**

Step 2 명령에서 결과 파일명을 `dirt-path-connection-green.xml`, 로그 파일명을 `dirt-path-connection-green.log`로 바꿔 다시 실행한다.

Expected: `RoomBuilderOutdoorTests` 전체 PASS.

- [x] **Step 5: 변경 범위와 브랜치 확인**

```powershell
git branch --show-current
git diff --check
git status --short
git diff -- Assets/Scripts/Dungeon/RoomBuilder.cs Assets/Tests/Editor/RoomBuilderOutdoorTests.cs
```

Expected: 브랜치는 `feat/stage1-dirt-path`이고, 코드 변경은 `RoomBuilder.cs`와 `RoomBuilderOutdoorTests.cs`에 한정된다. 기존 사용자 프리팹과 Standalone PNG는 보존되며 커밋·푸시하지 않는다.

- [x] **Step 6: 사용자 시각 확인을 위해 중지**

Unity에서 문이 2~4개인 방의 길이 중앙에서 합쳐지는지 사용자가 확인할 수 있도록 결과와 실행 방법을 전달한다. 사용자가 명시적으로 다음 작업을 요청하기 전에는 단일 문 Standalone 적용을 시작하지 않는다.

---

### Task 2: 중앙 합류부 리소스 임포트 계약

**Files:**
- Modify: `Assets/Editor/Stage1GroundTextureImporter.cs`
- Modify: `Assets/Tests/Editor/Stage1DirtPathAssetTests.cs`
- Modify: `Assets/Resources/Stage1/Ground/Dirt_Path_Corner_01.png.meta`
- Modify: `Assets/Resources/Stage1/Ground/Dirt_Path_TJunction_01.png.meta`
- Modify: `Assets/Resources/Stage1/Ground/Dirt_Path_Cross_01.png.meta`

**Interfaces:**
- Consumes: 사용자가 제작한 512×512 Corner, T, Cross PNG.
- Produces: 세 Sprite를 기존 직선 흙길과 동일한 64 PPU 임포트 계약으로 보장한다.

- [ ] **Step 1: 세 리소스 임포트 테스트를 먼저 추가**

`Stage1DirtPathAssetTests`에 세 경로 상수와 `TestCase`를 추가한다.

```csharp
private const string CornerAssetPath =
    "Assets/Resources/Stage1/Ground/Dirt_Path_Corner_01.png";
private const string TJunctionAssetPath =
    "Assets/Resources/Stage1/Ground/Dirt_Path_TJunction_01.png";
private const string CrossAssetPath =
    "Assets/Resources/Stage1/Ground/Dirt_Path_Cross_01.png";

[TestCase(CornerAssetPath)]
[TestCase(TJunctionAssetPath)]
[TestCase(CrossAssetPath)]
```

- [ ] **Step 2: 임포트 테스트 RED 확인**

`Stage1DirtPathAssetTests`를 EditMode로 실행한다. 현재 세 파일은 100 PPU이므로 `Expected: 64f But was: 100f`로 실패해야 한다.

- [ ] **Step 3: 임포터와 Meta를 64 PPU로 교정**

`Stage1GroundTextureImporter`에 세 정확한 경로 상수를 추가하고 `isDirtPath` 조건에 포함한다. 세 `.meta`의 `spritePixelsToUnits`는 64, 기본 `maxTextureSize`는 512로 맞춘다. 기존 Bilinear, Clamp, Mip Map Off, Alpha Is Transparency, sRGB, Full Rect 설정을 재사용한다.

- [ ] **Step 4: 임포트 테스트 GREEN 확인**

`Stage1DirtPathAssetTests` 전체가 통과하는지 확인한다.

### Task 3: 문 조합에 맞는 중앙 합류부 생성

**Files:**
- Modify: `Assets/Scripts/Dungeon/RoomBuilder.cs`
- Modify: `Assets/Tests/Editor/RoomBuilderOutdoorTests.cs`

**Interfaces:**
- Consumes: `RoomShape.DoorOpenings`의 방향 집합과 Task 2의 Corner/T/Cross Sprite.
- Produces: 방 중앙의 `Door Path Junction` SpriteRenderer 한 개, Sorting Order 2.

- [ ] **Step 1: 방향 조합별 Sprite와 회전 테스트 작성**

다음 입력과 기대값을 리터럴 `TestCase`로 추가한다.

```csharp
[TestCase(Doors.North | Doors.South,
    "Stage1/Ground/Dirt_Path_Vertical_01", 0f)]
[TestCase(Doors.East | Doors.West,
    "Stage1/Ground/Dirt_Path_Horizontal_01", 0f)]
[TestCase(Doors.North | Doors.East,
    "Stage1/Ground/Dirt_Path_Corner_01", 0f)]
[TestCase(Doors.North | Doors.West,
    "Stage1/Ground/Dirt_Path_Corner_01", 90f)]
[TestCase(Doors.South | Doors.West,
    "Stage1/Ground/Dirt_Path_Corner_01", 180f)]
[TestCase(Doors.South | Doors.East,
    "Stage1/Ground/Dirt_Path_Corner_01", 270f)]
[TestCase(Doors.North | Doors.East | Doors.West,
    "Stage1/Ground/Dirt_Path_TJunction_01", 0f)]
[TestCase(Doors.North | Doors.South | Doors.West,
    "Stage1/Ground/Dirt_Path_TJunction_01", 90f)]
[TestCase(Doors.South | Doors.East | Doors.West,
    "Stage1/Ground/Dirt_Path_TJunction_01", 180f)]
[TestCase(Doors.North | Doors.South | Doors.East,
    "Stage1/Ground/Dirt_Path_TJunction_01", 270f)]
[TestCase(Doors.North | Doors.South | Doors.East | Doors.West,
    "Stage1/Ground/Dirt_Path_Cross_01", 0f)]
```

각 경우 `Door Path Junction`이 원점에 하나만 존재하고, 기대 Sprite, Z 회전, Sorting Order 2를 사용하는지 검증한다. 기존 단일 문 테스트에는 Junction이 없다는 검증을 추가하고, 북+동 기존 테스트의 비숲 Renderer 수를 9개로 갱신한다.

- [ ] **Step 2: 중앙 합류부 테스트 RED 확인**

`RoomBuilderOutdoorTests`를 EditMode로 실행한다. 현재 `Door Path Junction`이 없어 실패해야 한다.

- [ ] **Step 3: 최소 중앙 합류부 선택·생성 구현**

`RoomBuilder`에 다음 리소스 경로와 Order를 추가한다.

```csharp
private const int DoorJunctionOrder = 2;
private const string CornerPathResourcePath =
    "Stage1/Ground/Dirt_Path_Corner_01";
private const string TJunctionPathResourcePath =
    "Stage1/Ground/Dirt_Path_TJunction_01";
private const string CrossPathResourcePath =
    "Stage1/Ground/Dirt_Path_Cross_01";
```

`CreateDoorPaths`가 기존 문별 타일을 만든 뒤 `CreateDoorJunction(parent, shape)`를 호출한다. 이 메서드는 문 방향을 OR로 합쳐 위 TestCase 표와 같은 Sprite·회전을 선택하고, 문이 하나면 반환한다. Sprite가 없으면 기존 흙길과 같은 `InvalidOperationException`을 발생시킨다.

`CreateDoorPath`에는 선택 매개변수 `float rotationDegrees = 0f`, `int sortingOrder = DoorPathOrder`를 추가한다. Junction은 `shape.Bounds.center`, 선택 회전, Order 2로 생성한다.

- [ ] **Step 4: 중앙 합류부와 회귀 테스트 GREEN 확인**

`RoomBuilderOutdoorTests`와 `DungeonRunnerPlayModeTests`가 모두 통과하는지 확인한다.

- [ ] **Step 5: 범위 확인 후 사용자 시각 검토를 위해 중지**

브랜치가 `feat/stage1-dirt-path`인지, `git diff --check`가 통과하는지 확인한다. 단일 문 Standalone 적용, 사용자 프리팹, 씬·UI·적 에셋 변경은 건드리지 않고 커밋·푸시하지 않는다.

---

### Task 4: 단일 문 직선 흙길 위에 Standalone 끝 조각 겹치기

**Files:**
- Modify: `Assets/Editor/Stage1GroundTextureImporter.cs`
- Modify: `Assets/Tests/Editor/Stage1DirtPathAssetTests.cs`
- Modify: `Assets/Resources/Stage1/Ground/Dirt_Path_Horizontal_Standalone_01.png.meta`
- Modify: `Assets/Resources/Stage1/Ground/Dirt_Path_Vertical_Standalone_01.png.meta`
- Modify: `Assets/Scripts/Dungeon/RoomBuilder.cs`
- Modify: `Assets/Tests/Editor/RoomBuilderOutdoorTests.cs`

**Interfaces:**
- Consumes: `RoomShape.DoorOpenings`, 기존 가로·세로 문 흙길 Sprite, 두 Standalone Sprite.
- Produces: 문이 정확히 하나인 방에서 기존 직선 안쪽·바깥쪽 타일을 유지하고, 추가 `Door Path {Side} Standalone`을 안쪽 9유닛 위치에 겹친다. 다중 문 Extension/Junction, 문 Trigger와 방 생성 알고리즘은 유지한다.

- [x] **Step 1: Standalone 리소스 임포트 계약 테스트 작성**

`Stage1DirtPathAssetTests`에 두 PNG를 TestCase로 추가하고 기존 흙길과 같은 Sprite Single, 64 PPU, Clamp, Bilinear, Mipmap Off, Alpha Transparency, sRGB, 최대 512px, Full Rect 계약을 검증한다.

- [x] **Step 2: 임포트 테스트 RED 확인**

`Stage1DirtPathAssetTests`를 실행해 새 Standalone 리소스가 현재 100 PPU/Multiple 설정 때문에 실패하는지 확인한다.

- [x] **Step 3: Standalone 임포트 규격 최소 구현 및 GREEN 확인**

`Stage1GroundTextureImporter`의 정확한 경로 목록에 두 파일을 추가하고 두 `.meta`를 기존 흙길 계약으로 맞춘 뒤 임포트 테스트를 다시 실행한다.

- [x] **Step 4: 단일 문 직선 유지와 Standalone 겹침 테스트 작성**

`RoomBuilderOutdoorTests`에 상·하·좌·우 TestCase를 둔다. 각 경우 안쪽과 바깥쪽은 기존 직선 Sprite를 유지하고, Standalone의 이름·Sprite·위치·Scale·Sorting Order를 다음 리터럴로 검증한다.

```csharp
[TestCase(Doors.North,
    "Stage1/Ground/Dirt_Path_Vertical_01",
    "Stage1/Ground/Dirt_Path_Vertical_Standalone_01",
    0f, 11f, 0f, 6f, 1.2f, 1f, 0f, 19f)]
[TestCase(Doors.South,
    "Stage1/Ground/Dirt_Path_Vertical_01",
    "Stage1/Ground/Dirt_Path_Vertical_Standalone_01",
    0f, -11f, 0f, -6f, 1.2f, 1f, 0f, -19f)]
[TestCase(Doors.East,
    "Stage1/Ground/Dirt_Path_Horizontal_01",
    "Stage1/Ground/Dirt_Path_Horizontal_Standalone_01",
    18f, 0f, 13f, 0f, 1f, 1.2f, 26f, 0f)]
[TestCase(Doors.West,
    "Stage1/Ground/Dirt_Path_Horizontal_01",
    "Stage1/Ground/Dirt_Path_Horizontal_Standalone_01",
    -18f, 0f, -13f, 0f, 1f, 1.2f, -26f, 0f)]
```

각 TestCase는 다음 동작을 검증한다.

```csharp
public void SingleDoorKeepsStraightPathAndOverlapsStandaloneEnd(
    Doors side,
    string straightResourcePath,
    string standaloneResourcePath,
    float innerX,
    float innerY,
    float standaloneX,
    float standaloneY,
    float scaleX,
    float scaleY,
    float outerX,
    float outerY)
{
Transform inner = root.transform.Find($"Door Path {side}");
Transform outer = root.transform.Find($"Door Path {side} Outer");
Transform standalone = root.transform.Find($"Door Path {side} Standalone");

Assert.That(inner.GetComponent<SpriteRenderer>().sprite,
    Is.SameAs(Resources.Load<Sprite>(straightResourcePath)));
Assert.That(inner.localPosition,
    Is.EqualTo(new Vector3(innerX, innerY, 0f)));
Assert.That(outer.GetComponent<SpriteRenderer>().sprite,
    Is.SameAs(Resources.Load<Sprite>(straightResourcePath)));
Assert.That(outer.localPosition,
    Is.EqualTo(new Vector3(outerX, outerY, 0f)));
Assert.That(standalone.localPosition,
    Is.EqualTo(new Vector3(standaloneX, standaloneY, 0f)));
Assert.That(standalone.localScale,
    Is.EqualTo(new Vector3(scaleX, scaleY, 1f)));
Assert.That(standalone.GetComponent<SpriteRenderer>().sprite,
    Is.SameAs(Resources.Load<Sprite>(standaloneResourcePath)));
Assert.That(standalone.GetComponent<SpriteRenderer>().sortingOrder,
    Is.EqualTo(2));
Assert.That(standalone.GetComponent<Collider2D>(), Is.Null);
Assert.That(root.transform.Find($"Door Path {side} Extension 1"), Is.Null);
Assert.That(root.transform.Find("Door Path Junction"), Is.Null);
}
```

기존 북+동 다중 문 테스트에는 `Door Path North Standalone`과 `Door Path East Standalone`이 모두 없다는 검증을 추가한다.

- [x] **Step 5: 겹침 테스트 RED 확인**

`RoomBuilderOutdoorTests`를 실행한다. 현재 구현은 안쪽 직선을 Standalone으로 교체하고 별도 Standalone 오브젝트를 만들지 않으므로, 직선 Sprite 유지와 추가 오브젝트 검증이 실패해야 한다.

- [x] **Step 6: `RoomBuilder` 겹침 최소 구현**

`RoomBuilder`에 다음 상수를 추가한다.

```csharp
private const float StandaloneDoorPathInset = 9f;
private const float StandaloneDoorPathCrossScale = 1.2f;
private const int StandaloneDoorPathOrder = 2;
```

`CreateDoorPaths`는 안쪽 `Door Path {Side}`를 항상 기존 `sprite`로 생성한다. `connectToCenter`가 false일 때만 방향별 Standalone Sprite를 로드하고 다음 값으로 추가 생성한다.

```csharp
string standaloneResourcePath = isVertical
    ? VerticalStandaloneDoorPathResourcePath
    : HorizontalStandaloneDoorPathResourcePath;
Sprite standaloneSprite = Resources.Load<Sprite>(standaloneResourcePath);
if (standaloneSprite == null)
{
    throw new InvalidOperationException(
        "Missing standalone door path sprite at " +
        $"Resources/{standaloneResourcePath}");
}

float scaleX = isVertical ? StandaloneDoorPathCrossScale : 1f;
float scaleY = isVertical ? 1f : StandaloneDoorPathCrossScale;

CreateDoorPath(
    parent,
    $"Door Path {opening.Side} Standalone",
    opening.Center + inward * StandaloneDoorPathInset,
    standaloneSprite,
    sortingOrder: StandaloneDoorPathOrder,
    scaleX: scaleX,
    scaleY: scaleY);
```

`CreateDoorPath`에 `float scaleX = 1f`, `float scaleY = 1f` 선택 매개변수를 추가하고 `path.transform.localScale = new Vector3(scaleX, scaleY, 1f)`을 설정한다. 문이 둘 이상인 경우의 Extension과 중앙 Junction 선택은 변경하지 않는다.

- [x] **Step 7: 겹침 테스트 GREEN 확인**

`RoomBuilderOutdoorTests`를 다시 실행해 모든 방향 조합이 통과하는지 확인한다. `Stage1DirtPathAssetTests`도 다시 실행해 Standalone 임포트 계약이 유지되는지 확인한다.

- [x] **Step 8: 회귀 검증 후 사용자 시각 확인을 위해 중지**

`Stage1DirtPathAssetTests`, `RoomBuilderOutdoorTests`, `DungeonRunnerPlayModeTests`를 실행한다. `feat/stage1-dirt-path` 브랜치와 변경 범위를 확인하고 커밋·푸시하지 않은 상태로 Unity 시각 확인 방법을 전달한다.
