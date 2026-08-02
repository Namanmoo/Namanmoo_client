# 1스테이지 야외 잔디 바닥과 외곽 경계 설계

## 목표

현재 랜덤 던전의 방 연결 그래프와 전투 흐름을 그대로 유지하면서, 단색 회색 바닥과 보이는 사각 벽을 밝은 손그림 잔디 바닥과 화면 밖의 보이지 않는 안전 경계로 교체한다.

이번 범위는 잔디 바닥과 외곽 경계만 다룬다. 흙길, 나무, 꽃, 바위, 방 종류별 랜드마크는 후속 범위다.

## 변경하지 않는 계약

- `DungeonLayout.Generate`, `TargetRoomCount`, `PlaceChance`, 격자 크기와 방 종류 배정을 수정하지 않는다.
- 같은 던전 시드와 층은 기존과 동일한 방 좌표, 방 종류, `Doors` 연결을 생성한다.
- `RoomShape.Bounds`는 기존 `44×30` 전투·카메라 기준 영역으로 유지한다.
- 중앙 폭 `6`유닛의 `DoorOpening`, 방 전환 Trigger, 반대편 착지 위치를 유지한다.
- 몬스터가 남아 있는 동안 출구의 `BoxCollider2D`가 이동을 막고, 전멸하면 해제되는 `DungeonDoor`/`DungeonRunner` 흐름을 유지한다.
- 한 번에 현재 방 하나만 원점에 생성하고 이전 방을 제거하는 구조를 유지한다.

## 잔디 원화

최종 입력 원화는 다음 파일이다.

`C:/Users/dksco/OneDrive/Desktop/리소스/맵/Stage1/Grass_Base_01_Seamless_OriginalColor_v2.png`

- 원본 크기: `2048×2048`
- 프로젝트 내 안정적인 경로: `Assets/Resources/Stage1/Ground/Grass_Base_01.png`
- Unity PPU: `64`
- 자연 크기: `32×32`유닛
- 프로젝트에서는 버전 접미사를 제거한다. 원본 보관 파일은 버전명을 유지하되 런타임 참조 경로는 바뀌지 않게 한다.
- 좌우·상하 경계 픽셀은 동일한 Seamless 결과다.

## 바닥 렌더링

런타임 방 바닥은 `64×64`유닛의 단일 `SpriteRenderer`로 만든다.

- `SpriteDrawMode.Tiled`로 `32×32` 원화를 2×2 반복한다.
- Transform 스케일로 `44×30`에 강제로 늘이지 않는다.
- 방 중심은 원점이며 잔디 범위는 `(-32,-32)`부터 `(32,32)`까지다.
- 바닥은 기존과 같은 최하단 정렬 순서를 사용한다.
- 21:9 화면과 카메라 Overscan에서도 바닥 끝이 보이지 않아야 한다.
- 에셋 참조는 씬 직렬화를 건드리지 않기 위해 `Resources.Load<Sprite>("Stage1/Ground/Grass_Base_01")`로 가져온다.

## 시각물과 충돌 분리

새 순수 계산 단위 `OutdoorRoomGeometry`가 시각 범위와 충돌 범위를 제공한다.

- `GroundBounds`: 중심 기준 `64×64`
- `SafetyBounds`: `RoomShape.Bounds`를 사방으로 `3`유닛 확장한 `50×36`
- 카메라 Overscan `2.5`유닛보다 안전 경계가 `0.5`유닛 더 바깥에 놓인다.

`RoomBuilder`는 기존 `RoomShape.Walls`를 렌더링하거나 충돌로 사용하지 않는다. 대신 `SafetyBounds`를 한 바퀴 도는 닫힌 `EdgeCollider2D` 하나만 만든다. 이 오브젝트에는 Renderer를 붙이지 않는다.

연결 방향의 중앙 출구 Trigger는 기존 `44×30` 기준 경계에 남는다. 출구가 열린 상태에서는 플레이어가 바깥 안전 경계에 닿기 전에 Trigger가 다음 방으로 전환한다. 연결되지 않은 방향에서는 화면 밖의 안전 경계가 최종 이탈만 막는다.

## 전투 중 출구 잠금

기존 잠금용 `BoxCollider2D`는 유지하되 갈색 `Door Bar` Sprite는 만들지 않는다.

- 전투 중: 중앙 흙길 예정 위치에서 보이지 않는 Collider가 막는다.
- 전멸 후: Collider가 비활성화되고 기존 Trigger가 방 이동을 알린다.
- 안내 문구, 마법 장벽, 덤불 장벽과 효과음은 이번 범위에 넣지 않는다.

## 에셋 임포트

잔디 PNG는 다음 설정을 강제한다.

- Texture Type: Sprite
- Sprite Mode: Single
- Pixels Per Unit: 64
- Mesh Type: Full Rect
- Filter Mode: Bilinear
- Wrap Mode: Repeat
- Mip Maps: Off
- sRGB: On
- Alpha Is Transparency: Off
- Max Size: 2048

Editor 전용 AssetPostprocessor가 정확한 경로에만 이 설정을 적용한다. 테스트가 TextureImporter 값을 직접 검증한다.

## 테스트와 완료 조건

- 기존 `DungeonLayoutTests`는 수정 없이 통과한다.
- 기존 `RoomShapeTests`와 `DungeonNavigationTests`는 수정 없이 통과한다.
- 새 EditMode 테스트가 `GroundBounds`, `SafetyBounds`, 닫힌 경계 점과 21:9 카메라 포함 관계를 검증한다.
- 새 EditMode 테스트가 잔디 임포트 설정을 검증한다.
- PlayMode 테스트가 `64×64` Tiled 잔디, 보이지 않는 외곽 Collider, 보이는 벽 조각 부재를 검증한다.
- 기존 방 전환, 착지, 전멸 전 잠금과 전멸 후 개방 테스트가 통과한다.
- `Assets/Scenes/Dungeon.unity`는 수정하지 않는다.
- 사용자 기존 작업 트리의 미커밋 변경은 포함하거나 덮어쓰지 않는다.

## 범위 밖

- 흙길 및 중앙 공터
- 풀, 꽃, 돌, 나무, 덤불 배치
- 방 종류별 시각 차이
- Sorting Layer 개편
- 몬스터 길찾기
- 잠긴 출구의 UI·시각·음향 피드백
