# Stage1 던전 벽쪽 숲 배치 설계

## 배경

Stage1 던전 방(`RoomShape`)은 이미 흔들리는 벽 폴리라인(`Walls`)을 계산하지만, 현재는
어디에도 그리지 않는다. 플레이어를 실제로 막는 것은 `RoomBuilder.CreateSafetyBoundary`가
만드는 보이지 않는 사각형 `EdgeCollider2D`(방 경계에서 3유닛 바깥, `OutdoorRoomGeometry.SafetyBounds`)
뿐이다. 즉 방 가장자리에는 지금 아무 시각적 벽도 없다.

`Assets/Resources/Stage1/Wall/wall_forest.png`(1024x1024)를 참조해, 문이 없는 벽쪽에
숲(나무 스프라이트)을 배치해서 "여기가 못 지나가는 경계"임을 시각적으로 드러낸다. 이 이미지는
이미 `wall_forest_0`~`wall_forest_8`(3x3 격자: 모서리 4개, 변 4개, 중앙 큰 덤불 1개)로
스프라이트 슬라이스가 되어 있어 추가 슬라이싱 작업은 필요 없다.

## 목표

- 모든 방 종류(시작/일반/보스/보물/상점)에 동일하게 적용한다. `RoomKind`별 분기 없음.
- 문이 있는 구간(통로)에는 숲을 배치하지 않는다. `RoomShape.Walls`가 이미 문 간격을
  정확히 잘라 계산해 두므로 이를 그대로 활용한다.
- 벽을 따라 빈틈없이 이어지는 숲 라인을 만든다(참고 이미지의 울타리 형태).
- 완전히 규칙적인 반복이 아니라, 타일마다 약간의 좌우 반전/크기 변화를 줘서 손그림 느낌을
  유지한다.
- 숲 라인은 방 경계선(`shape.Bounds`, 잔디가 끝나는 자리) 기준으로 배치한다. 실제 충돌
  경계(`SafetyBoundary`, 3유닛 바깥)와는 위치가 다르지만, 카메라가 이미 경계 바깥 2.5유닛을
  더 보여주도록 설계돼 있어(`CameraFollow.overscan`) 자연스럽게 보인다.

## 범위 밖

- `RoomKind`에 따른 숲 스타일/유무 분기.
- 충돌 경계(`SafetyBoundary`, `OutdoorRoomGeometry`) 변경. 숲은 순수 시각 요소이며 개별
  콜라이더를 갖지 않는다.
- `wall_forest.png` 재슬라이싱. 이미 `wall_forest_0`~`_8`로 슬라이스되어 있다.

## 설계

### 1. `RoomShape.WallSides` 추가

`RoomShape.Walls`(`IReadOnlyList<IReadOnlyList<Vector2>>`)는 각 벽 폴리라인이 남/동/북/서
중 어느 변에 속하는지 알려주지 않는다. 기존 `Walls`는 그대로 두고, 인덱스가 1:1로 대응하는
새 프로퍼티를 추가한다.

```csharp
public IReadOnlyList<Doors> WallSides { get; }
```

`AddSide`가 `walls.Add(...)`를 호출하는 자리마다 같은 `side` 값을 `WallSides`에도 추가한다.
순수 추가라 기존 `RoomShapeTests`는 전혀 수정할 필요가 없다.

### 2. `WallForestDecorator` (신규)

`Assets/Scripts/Dungeon/WallForestDecorator.cs`. 정적 클래스, 진입점 하나:

```csharp
public static class WallForestDecorator
{
    public static void Decorate(Transform parent, RoomShape shape, int roomSeed);
}
```

**코너 (4개, 고정)**

`shape.Bounds`의 네 꼭짓점은 문이 변의 중앙에만 생기므로 항상 벽으로 남는다. 각 꼭짓점에
정확히 하나씩 코너 스프라이트를 배치한다.

| 꼭짓점 | 스프라이트 |
|---|---|
| (xMin, yMax) 좌상단 | `wall_forest_0` |
| (xMax, yMax) 우상단 | `wall_forest_2` |
| (xMin, yMin) 좌하단 | `wall_forest_6` |
| (xMax, yMin) 우하단 | `wall_forest_8` |

**변 타일링**

`shape.Walls[i]`와 `shape.WallSides[i]`를 같이 순회한다. 변에 따라 가장자리 스프라이트를
고른다: 북 → `wall_forest_1`, 남 → `wall_forest_7`, 동 → `wall_forest_5`, 서 → `wall_forest_3`.

각 폴리라인은 흔들림 지점마다 끊긴 여러 구간(짧은 직선)으로 이뤄져 있다. 구간마다:

1. 구간 길이와 방향(각도)을 구한다.
2. 타일링 방향 기준 스프라이트 크기를 구한다 — 남/북(가로 변)은 스프라이트 폭, 동/서(세로
   변)는 스프라이트 높이를 쓴다.
3. 타일 개수 `n = max(1, round(구간길이 / 위 크기))`를 정한다.
4. 그 구간을 `n`등분한 간격으로 타일을 나란히 놓는다 (타일링 방향 크기를 살짝 늘리거나
   줄여 정확히 채운다 — 이렇게 하면 구간 사이에 빈틈이 생기지 않는다).
5. 각 타일은 구간 각도만큼 회전시킨다(기존 `SolidQuad.CreateSegment`와 같은 방식).

**변화 주기**

`RoomShape`가 벽을 흔드는 데 쓰는 rng와는 완전히 독립된 `DeterministicRandom`을
`roomSeed`로 새로 만들어 쓴다(같은 시드값이라도 별개의 상태를 가진 인스턴스라 서로 간섭하지
않는다). 변 타일마다:

- 50% 확률로 좌우 반전(`localScale.x`를 음수로).
- 폭/높이에 각각 ±10~15% 정도의 미세한 크기 배율을 곱한다(3번 단계의 "구간에 맞추기" 배율
  위에 추가로 곱해진다).

코너 타일은 정확한 자리에 맞아야 하므로 반전/크기 변화를 주지 않는다.

**정렬 순서**

바닥(`GroundOrder = 0`), 길(`DoorPathOrder = 1`) 위에 그려지도록 숲은 `sortingOrder = 2`로
고정한다. 개별 콜라이더는 만들지 않는다(플레이어는 이미 `SafetyBoundary`로 막혀 있어 나무
쪽으로 걸어 들어올 수 없다).

**계층 구조**

모든 나무 오브젝트는 `parent` 아래 "Wall Forest"라는 하나의 부모 오브젝트 밑에 정리한다.
기존 "Room Ground"/"Safety Boundary"/문 오브젝트들과 같은 계층 관례를 따른다.

### 3. `RoomBuilder` 연결

`Build`에 `int roomSeed` 파라미터를 추가하고 마지막 단계로 데코레이터를 호출한다.

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

### 4. `DungeonRunner` 연결

`Enter`에서 `DungeonNavigation.RoomSeed(seed, floor, cell)`을 지역 변수로 한 번만 계산해
`RoomShape.Build`와 `RoomBuilder.Build` 양쪽에 전달한다. 같은 방에 다시 들어와도 같은
시드이므로 숲 배치가 재현된다(방 흔들림과 동일한 원칙).

## 테스트 변경 범위

`RoomShape`와 `RoomBuilder`를 직접 수정하므로 이 두 클래스를 직접 테스트하는 기존 파일만
변경한다.

- **`RoomShapeTests.cs`**: `WallSides`가 `Walls`와 개수가 같고 각 항목이 올바른 변을
  가리키는지 확인하는 테스트를 추가한다.
- **`RoomBuilderOutdoorTests.cs`**: `Build` 호출에 `roomSeed` 인자를 추가한다. 기존
  "SpriteRenderer가 정확히 5개"라는 단언은 숲 타일이 늘어나며 깨지므로, ground/door path
  개별 존재 확인은 유지하되 전체 개수 단언은 제거하거나 "Wall Forest" 자식을 제외한 개수로
  좁힌다.
- **신규 `WallForestDecoratorTests.cs`** (EditMode): 다음을 검증한다.
  - 네 꼭짓점에 올바른 코너 스프라이트가 올바른 위치에 생성된다.
  - 각 변의 타일이 올바른 방향의 스프라이트를 쓴다(북/남/동/서).
  - 문이 있는 변에서는 문 간격 안에 타일이 생기지 않는다.
  - 같은 `roomSeed`는 같은 배치(반전/크기)를 재현한다.

전체 테스트 스위트는 실행하지 않는다. 위 세 파일에 대한 좁은 필터만 사용한다.
