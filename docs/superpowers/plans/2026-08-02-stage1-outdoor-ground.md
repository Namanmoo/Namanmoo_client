# 1스테이지 야외 잔디 바닥과 외곽 경계 구현 계획

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 랜덤 던전 알고리즘을 바꾸지 않고 현재 회색 바닥과 보이는 사각 벽을 Seamless 잔디와 카메라 밖 보이지 않는 안전 경계로 교체한다.

**Architecture:** `OutdoorRoomGeometry`가 바닥·안전 경계 계산을 순수 데이터로 제공하고, `RoomBuilder`가 이를 SpriteRenderer와 EdgeCollider2D로 옮긴다. 잔디는 안정적인 Resources 경로에서 읽고, Editor 전용 AssetPostprocessor가 임포트 규격을 강제한다.

**Tech Stack:** Unity 6000.5.5f1, C#, URP 2D, Unity Physics 2D, Unity Test Framework 1.7.0

## Global Constraints

- `DungeonLayout.Generate`, 방 개수·종류·연결과 시드 결과를 수정하지 않는다.
- `RoomShape.Bounds`는 `44×30`, 문 폭은 `6`유닛으로 유지한다.
- 잔디 바닥은 `64×64`유닛이고 Transform으로 찌그러뜨리지 않는다.
- 안전 경계는 기존 방 기준보다 사방 `3`유닛 바깥이며 Renderer가 없어야 한다.
- 전투 중 출구 잠금 Collider와 전멸 후 개방 흐름을 유지하되 잠금 시각물은 만들지 않는다.
- 16:9를 기본으로 하고 21:9에서도 바닥 끝과 안전 경계를 보이지 않게 한다.
- `Assets/Scenes/Dungeon.unity`를 수정하지 않는다.
- 모든 작업 완료 후 한 번만 한글 커밋을 만들고 `origin/feat/stage1-outdoor-ground`로 푸시한다.

---

### Task 1: 잔디 에셋과 임포트 계약

**Files:**
- Create: `Assets/Resources/Stage1/Ground/Grass_Base_01.png`
- Create: `Assets/Editor/Stage1GroundTextureImporter.cs`
- Create: `Assets/Tests/Editor/Stage1GroundAssetTests.cs`

**Interfaces:**
- Consumes: 외부 최종 PNG `Grass_Base_01_Seamless_OriginalColor_v2.png`
- Produces: Resources 경로 `Stage1/Ground/Grass_Base_01`, 64 PPU Sprite

- [ ] **Step 1: 최종 PNG를 안정적인 프로젝트 경로로 복사한다**

원본 버전명은 외부 보관에만 두고 프로젝트에서는 `Grass_Base_01.png`로 고정한다. 복사 후 SHA-256을 비교해 바이트가 같은지 확인한다.

- [ ] **Step 2: 실패하는 임포트 테스트를 작성한다**

`Stage1GroundAssetTests`에서 다음을 검증한다.

```csharp
private const string AssetPath =
    "Assets/Resources/Stage1/Ground/Grass_Base_01.png";

[Test]
public void GrassTextureUsesTheOutdoorGroundImportContract()
{
    var importer = (TextureImporter)AssetImporter.GetAtPath(AssetPath);
    Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite));
    Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single));
    Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(64f));
    Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Repeat));
    Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Bilinear));
    Assert.That(importer.mipmapEnabled, Is.False);
    Assert.That(importer.alphaIsTransparency, Is.False);
    Assert.That(importer.sRGBTexture, Is.True);
    Assert.That(importer.maxTextureSize, Is.EqualTo(2048));
}
```

- [ ] **Step 3: 테스트를 실행해 RED를 확인한다**

Run:

```powershell
Unity.exe -batchmode -nographics -projectPath <worktree> `
  -runTests -testPlatform EditMode -testFilter Stage1GroundAssetTests `
  -testResults TestResults-GroundAsset-Red.xml -logFile TestResults-GroundAsset-Red.log -quit
```

Expected: 기본 임포트 값이 64 PPU/Repeat 계약과 달라 실패한다.

- [ ] **Step 4: 해당 경로만 설정하는 AssetPostprocessor를 구현한다**

```csharp
public sealed class Stage1GroundTextureImporter : AssetPostprocessor
{
    public const string GrassAssetPath =
        "Assets/Resources/Stage1/Ground/Grass_Base_01.png";

    private void OnPreprocessTexture()
    {
        if (assetPath != GrassAssetPath)
        {
            return;
        }

        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 64f;
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.filterMode = FilterMode.Bilinear;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = false;
        importer.sRGBTexture = true;
        importer.maxTextureSize = 2048;

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
    }
}
```

- [ ] **Step 5: 재임포트하고 GREEN을 확인한다**

Expected: `Stage1GroundAssetTests`가 통과하고 Sprite 자연 크기가 `32×32`유닛이다.

### Task 2: 야외 방의 순수 기하 계약

**Files:**
- Create: `Assets/Scripts/Dungeon/OutdoorRoomGeometry.cs`
- Create: `Assets/Tests/Editor/OutdoorRoomGeometryTests.cs`

**Interfaces:**
- Consumes: `RoomShape.Bounds`
- Produces: `GroundBounds(RoomShape)`, `SafetyBounds(RoomShape)`, `SafetyBoundary(RoomShape)`

- [ ] **Step 1: 실패하는 기하 테스트를 작성한다**

```csharp
[Test]
public void GroundIsSixtyFourUnitsSquareAtTheRoomCentre()
{
    RoomShape shape = RoomShape.Build(1, Doors.North);
    Rect ground = OutdoorRoomGeometry.GroundBounds(shape);
    Assert.That(ground.center, Is.EqualTo(shape.Bounds.center));
    Assert.That(ground.size, Is.EqualTo(new Vector2(64f, 64f)));
}

[Test]
public void SafetyBoundarySitsThreeUnitsOutsideTheCombatBounds()
{
    RoomShape shape = RoomShape.Build(1, Doors.None);
    Rect safety = OutdoorRoomGeometry.SafetyBounds(shape);
    Assert.That(safety.xMin, Is.EqualTo(shape.Bounds.xMin - 3f));
    Assert.That(safety.xMax, Is.EqualTo(shape.Bounds.xMax + 3f));
    Assert.That(safety.yMin, Is.EqualTo(shape.Bounds.yMin - 3f));
    Assert.That(safety.yMax, Is.EqualTo(shape.Bounds.yMax + 3f));
}
```

경계 점이 다섯 개이며 첫 점과 마지막 점이 같아 닫혀 있는지도 검증한다. 21:9, orthographic size 10, Overscan 2.5에서 카메라가 보여주는 모든 점이 GroundBounds 안이고 SafetyBounds가 화면 바깥에 놓이는 성질을 검증한다.

- [ ] **Step 2: 테스트를 실행해 RED를 확인한다**

Expected: `OutdoorRoomGeometry`가 없어 컴파일 실패한다.

- [ ] **Step 3: 최소 기하 구현을 작성한다**

```csharp
public static class OutdoorRoomGeometry
{
    public const float GroundSize = 64f;
    public const float SafetyPadding = 3f;

    public static Rect GroundBounds(RoomShape shape) =>
        Rect.MinMaxRect(
            shape.Bounds.center.x - GroundSize * 0.5f,
            shape.Bounds.center.y - GroundSize * 0.5f,
            shape.Bounds.center.x + GroundSize * 0.5f,
            shape.Bounds.center.y + GroundSize * 0.5f);

    public static Rect SafetyBounds(RoomShape shape) =>
        Rect.MinMaxRect(
            shape.Bounds.xMin - SafetyPadding,
            shape.Bounds.yMin - SafetyPadding,
            shape.Bounds.xMax + SafetyPadding,
            shape.Bounds.yMax + SafetyPadding);
}
```

`SafetyBoundary`는 사각형 네 모서리와 첫 점을 다시 포함한 읽기 전용 점 목록을 반환한다.

- [ ] **Step 4: 기하 테스트를 실행해 GREEN을 확인한다**

Expected: 모든 `OutdoorRoomGeometryTests` 통과.

### Task 3: 잔디 렌더링과 보이지 않는 안전 경계

**Files:**
- Modify: `Assets/Scripts/Dungeon/RoomBuilder.cs`
- Create: `Assets/Tests/Editor/RoomBuilderOutdoorTests.cs`

**Interfaces:**
- Consumes: `OutdoorRoomGeometry`, Resources Sprite `Stage1/Ground/Grass_Base_01`
- Produces: `Room Ground` SpriteRenderer, `Safety Boundary` EdgeCollider2D, 기존 `DungeonDoor` 목록

- [ ] **Step 1: 실패하는 RoomBuilder 테스트를 작성한다**

테스트용 부모와 `RoomShape`를 만든 뒤 `RoomBuilder.Build`를 호출하고 다음을 검증한다.

```csharp
SpriteRenderer ground = root.transform.Find("Room Ground")
    .GetComponent<SpriteRenderer>();
Assert.That(ground.sprite, Is.Not.Null);
Assert.That(ground.drawMode, Is.EqualTo(SpriteDrawMode.Tiled));
Assert.That(ground.size, Is.EqualTo(new Vector2(64f, 64f)));
Assert.That(ground.transform.localScale, Is.EqualTo(Vector3.one));

Transform boundary = root.transform.Find("Safety Boundary");
Assert.That(boundary.GetComponent<EdgeCollider2D>(), Is.Not.Null);
Assert.That(boundary.GetComponent<Renderer>(), Is.Null);
Assert.That(root.GetComponentsInChildren<SpriteRenderer>(),
    Has.None.Matches<SpriteRenderer>(r => r.name.StartsWith("Piece")));
```

- [ ] **Step 2: 테스트를 실행해 RED를 확인한다**

Expected: 현재 `Room Floor`와 벽 `Piece`가 생성되므로 실패한다.

- [ ] **Step 3: 단색 바닥을 Tiled 잔디로 교체한다**

`CreateGround`는 Sprite를 Resources에서 읽고 누락 시 정확한 경로를 포함한 `InvalidOperationException`을 던진다. SpriteRenderer는 `drawMode = Tiled`, `tileMode = Continuous`, `size = GroundBounds.size`, `sortingOrder = 0`으로 둔다.

- [ ] **Step 4: 보이는 벽 생성 대신 닫힌 EdgeCollider를 만든다**

`CreateWalls` 호출을 제거하고 `CreateSafetyBoundary`를 호출한다. `OutdoorRoomGeometry.SafetyBoundary(shape)` 점을 `EdgeCollider2D.SetPoints`에 넘기며 Renderer는 만들지 않는다.

- [ ] **Step 5: 잠긴 문 시각물을 제거한다**

잠금용 자식 `Bar`와 `BoxCollider2D`는 유지한다. `Door Bar {side}` SpriteRenderer 생성은 제거하고 `DungeonDoor.Configure(side, barCollider, null)`을 호출한다.

- [ ] **Step 6: RoomBuilder 테스트를 실행해 GREEN을 확인한다**

Expected: 잔디, 경계, 보이지 않는 잠금 계약이 모두 통과한다.

### Task 4: 저장된 던전 흐름과 회귀 테스트

**Files:**
- Modify: `Assets/Tests/PlayMode/DungeonScenePlayModeTests.cs`
- Modify: `Assets/Tests/PlayMode/DungeonRunnerPlayModeTests.cs` only if an assertion must name the new ground object

**Interfaces:**
- Consumes: 저장된 `Dungeon.unity`, 런타임 `DungeonRunner`와 새 `RoomBuilder`
- Produces: 실제 씬에서의 잔디·안전 경계·문 잠금 회귀 보장

- [ ] **Step 1: 기존 가시성 테스트를 새 계약으로 바꾼다**

`TheRoomIsDrawnWithVisibleSprites`는 `Room Floor`와 벽 Piece 대신 다음을 검증한다.

```csharp
var ground = GameObject.Find("Room Ground").GetComponent<SpriteRenderer>();
Assert.That(ground.sprite, Is.Not.Null);
Assert.That(ground.drawMode, Is.EqualTo(SpriteDrawMode.Tiled));
Assert.That(ground.bounds.size.x, Is.EqualTo(64f).Within(0.01f));
Assert.That(ground.bounds.size.y, Is.EqualTo(64f).Within(0.01f));

var boundary = GameObject.Find("Safety Boundary");
Assert.That(boundary.GetComponent<EdgeCollider2D>(), Is.Not.Null);
Assert.That(boundary.GetComponent<Renderer>(), Is.Null);
```

벽 Piece와 `Door Bar` 이름의 SpriteRenderer가 하나도 없는지도 검증한다.

- [ ] **Step 2: 관련 PlayMode 테스트를 실행한다**

Run filters:

- `DungeonRunnerPlayModeTests`
- `DungeonBossPlayModeTests`
- `DungeonScenePlayModeTests`

Expected: 방 하나만 존재, 중앙 출구 전환, 착지, 전멸 전 잠금, 전멸 후 개방, 저장된 씬 참조가 모두 통과한다.

- [ ] **Step 3: 기존 순수 알고리즘 회귀 테스트를 실행한다**

Run filters:

- `DungeonLayoutTests`
- `RoomShapeTests`
- `DungeonNavigationTests`
- `RoomSpawnPointsTests`
- `CameraFollowTests`

Expected: 수정 없이 전부 통과하며 같은 시드 알고리즘이 유지된다.

### Task 5: 전체 검증, 시각 확인, 단일 커밋과 푸시

**Files:**
- Verify only: all modified/created files

**Interfaces:**
- Consumes: Tasks 1–4 결과
- Produces: 검증된 `feat/stage1-outdoor-ground` 원격 브랜치

- [ ] **Step 1: 전체 EditMode 테스트를 실행한다**

Expected: 실패 0.

- [ ] **Step 2: 전체 PlayMode 테스트를 실행한다**

Expected: 실패 0.

- [ ] **Step 3: Dungeon 씬을 실행해 시각 검증한다**

확인 항목:

- 잔디가 정사각형 원화 비율을 유지하고 찌그러지지 않는다.
- 카메라를 방 네 가장자리로 움직여도 잔디 끝이 보이지 않는다.
- 검은 벽 조각과 갈색 Door Bar가 보이지 않는다.
- 연결되지 않은 방향에서는 플레이어가 화면 밖으로 완전히 사라지지 않는다.
- 전투 중 중앙 출구는 조용히 막히고 전멸 후 이동된다.

- [ ] **Step 4: Git 변경 범위를 감사한다**

`Dungeon.unity`, 사용자 애니메이션, UI, 생성된 csproj/Library/TestResults가 커밋 대상에 없는지 확인한다. `DungeonLayout.cs`, `RoomShape.cs`, `DungeonNavigation.cs`가 변경되지 않았는지 확인한다.

- [ ] **Step 5: 한글 커밋을 한 번 만든다**

저장소의 기존 문장형 커밋 관례를 따라 다음 메시지를 사용한다.

```text
던전 바닥을 야외 잔디와 보이지 않는 경계로 바꾼다
```

- [ ] **Step 6: 원격 브랜치로 푸시한다**

```powershell
git push -u origin feat/stage1-outdoor-ground
```

Expected: `origin/feat/stage1-outdoor-ground`가 생성되고 로컬 HEAD와 동일하다.
