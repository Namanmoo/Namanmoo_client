# 던전 보스 처치 후 Stage Clear 화면 설계

## 목표

Dungeon(`Assets/Scenes/Dungeon.unity`)에서 보스방의 보스를 처치하면 게임 진행을
즉시 멈추고 화면 중앙에 `Stage Clear!` 문구와 `타이틀화면으로 돌아가기` 버튼을
표시한다. 플레이어 캐릭터는 숨기지 않고 승리한 모습 그대로 화면에 남긴 채 시간만
멈춘다. 기존 `PlayerDeathScreen`과 최대한 동일한 구조를 재사용한다.

적용 범위는 Dungeon 시스템의 보스방(`RoomKind.Boss`)이며, floor(층)와 무관하게
보스방을 클리어하는 즉시 표시한다. 여러 floor를 거치는 구조가 되더라도 중간 floor
보스를 잡을 때마다 동일하게 화면이 뜬다. Stage1(`SampleStage.unity`)의 고정 보스
(`Stage1BossEncounter`)는 이번 설계의 대상이 아니다.

## 동작 흐름

1. `DungeonRunner`가 방을 최초로 클리어(`cleared.Add`가 성공)했을 때, 그 방이
   `RoomKind.Boss`이면 `BossDefeated` 이벤트를 한 번 발생시킨다. 이미 클리어했던
   보스방을 다시 들어왔다가 나가는 경우(문이 이미 열려 있어 `MarkClearedAndOpen`이
   다시 호출되는 경우)는 `cleared.Add`가 `false`를 반환하므로 이벤트가 다시
   발생하지 않는다.
2. `StageClearScreen`(새 `MonoBehaviour`)이 `DungeonRunner.BossDefeated`를
   구독한다. 이벤트가 오면:
   - `Time.timeScale`을 0으로 설정해 적, 투사체, 이동 등 게임 진행을 즉시
     멈춘다. 플레이어 오브젝트는 비활성화하지 않는다.
   - UI 최상단의 검정 오버레이를 실시간 기준(`Time.unscaledDeltaTime`) 1초
     동안 불투명하게 페이드한다 (`PlayerDeathScreen`과 동일 타이밍).
   - 페이드가 끝난 뒤 중앙에 `Stage Clear!` 문구와
     `타이틀화면으로 돌아가기` 버튼을 활성화한다.
3. 버튼 클릭 시 씬을 로드하기 직전에 `Time.timeScale`을 1로 복원한 뒤
   `GameScenes.Title`을 로드한다. 씬 로드는 기존 `ISceneLoader` 추상화를
   재사용한다.

## 구조

### DungeonRunner

`public event Action BossDefeated;`를 추가한다. `MarkClearedAndOpen()`에서
`cleared.Add(CurrentCell)`의 반환값이 `true`(이번이 최초 클리어)이고
`Layout.RoomAt(CurrentCell).Kind == RoomKind.Boss`일 때 발생시킨다. 일반 방
클리어나 이미 클리어한 방 재방문 시에는 발생하지 않는다.

### StageClearScreen

Stage Clear 상태와 화면 전환을 담당하는 `MonoBehaviour`다. `DungeonRunner`
참조와 `ISceneLoader`를 주입받아 `BossDefeated`를 구독하고, 이벤트 수신 시
시간 정지, 페이드, 문구/버튼 표시를 순서대로 수행한다. `PlayerDeathScreen`과
동일하게 페이드는 `Time.timeScale`이 0이어도 진행되도록
`Time.unscaledDeltaTime`을 사용하며, 컴포넌트가 비활성화되거나 파괴될 때
이벤트 구독을 해제한다. `PlayerDeathScreen`과의 차이는 플레이어 오브젝트를
비활성화하지 않는다는 점뿐이다. 처리는 중복 실행되지 않도록 내부 상태로 한
번만 시작한다.

### StageClearScreenUIFactory / StageClearScreenView

`PlayerDeathScreenUIFactory`/`PlayerDeathScreenView`와 동일한 런타임 uGUI
팩토리 패턴을 따른다. Screen Space Overlay 캔버스는 1920×1080 기준 해상도와
화면 크기 대응 스케일러를 쓰고, 다른 게임 UI보다 높은 정렬 순서를 갖는다.

생성 요소는 다음과 같다.

- 화면 전체를 덮는 검정 `Image` (초기 알파 0)
- 중앙의 `Stage Clear!` 문구 (초기 숨김)
- 문구 아래의 `타이틀화면으로 돌아가기` 버튼 (초기 숨김)

페이드가 끝났을 때만 문구와 버튼을 표시한다. 클릭 입력을 받을 수 있도록
필요한 경우 `EventSystem`과 입력 모듈을 한 번만 생성한다.

### 기존 코드와 공유하는 부분

`PlayerDeathScreen` 계열과 겹치는 로직은 각 파일에 따로 복제하지 않고 공용
코드로 뽑아 함께 쓴다.

- **uGUI 조립 도우미** (`CreateButton`/`CreateText`/`CreateImage`/`Stretch`/
  `SetCenteredRect`/`EnsureEventSystem`, 버튼·잉크 색상)를
  `RuntimeMenuUIFactory`라는 공용 정적 클래스로 뽑는다. `PlayerDeathScreenUIFactory`도
  이 클래스를 쓰도록 고치고, `StageClearScreenUIFactory`도 처음부터 이 클래스를
  쓴다.
- **씬 로더** (`ISceneLoader` 인터페이스와 `UnitySceneLoader` 구현)를
  `PlayerDeathScreen.cs`에서 분리해 별도 파일로 뽑는다. 두 화면이 동일한 타입을
  참조한다.
- **페이드 진행** (실시간 1초, `Time.unscaledDeltaTime` 누적, 끝나면 오버레이
  불투명 + 메뉴 표시)을 `ScreenFade.Run(IFadeOverlay view, float duration)`라는
  공용 코루틴으로 뽑는다. `PlayerDeathScreenView`와 `StageClearScreenView`가
  공통 인터페이스 `IFadeOverlay`(`SetFadeAlpha`/`ShowMenu`)를 구현해 이 코루틴을
  같이 쓴다.
- **씬 전환 가드** (중복 클릭 방지 + 전환 직전 `Time.timeScale` 복원)를
  `SceneTransitionGuard`라는 작은 공용 클래스로 뽑는다.

이 네 가지 외의 나머지 로직(플레이어 숨김 여부, 버튼 개수, 이벤트 소스 등
화면마다 다른 부분)은 각 화면 컴포넌트에 그대로 남긴다 — 억지로 하나의
베이스 클래스로 합치지 않는다.

### 연결 지점

`DungeonSceneBuilder.cs`(에디터 스크립트)가 `Dungeon.unity`를 빌드할 때,
`DungeonBgmDirector`를 붙이는 자리와 같은 흐름으로 `StageClearScreen`을
생성하고 `Configure(runner, ...)`로 `DungeonRunner`와 씬 로더 등 필요한
의존성을 연결한다. 별도 씬 수작업 없이 `DungeonSceneBuilder` 실행만으로
Dungeon 씬에 Stage Clear 화면이 갖춰진다.

## 예외 및 상태 복원

- `DungeonRunner.BossDefeated`는 최초 클리어 시에만 발생하므로 화면 쪽에서는
  중복 이벤트를 걱정할 필요가 없지만, 방어적으로 내부 상태 플래그로 한 번만
  처리를 시작하도록 한다.
- 버튼을 연속 클릭해도 씬 전환 처리는 한 번만 실행한다.
- Stage Clear 화면이 처리 도중 파괴되면 `Time.timeScale`을 1로 복원해 에디터와
  다음 씬이 정지 상태로 남지 않게 한다.
- 현재 활성 씬이 유효하지 않은 테스트 환경에서는 씬 전환 요청을 무시하고
  오류를 발생시키지 않는다 (`PlayerDeathScreen`과 동일한 방어 로직 재사용).

## 테스트

- `DungeonRunner`가 보스방을 최초 클리어할 때만 `BossDefeated`를 정확히 한 번
  발생시키는지 검증한다 (이미 클리어한 보스방 재방문 시 재발생하지 않는지
  포함).
- 일반 방(비보스)을 클리어할 때는 `BossDefeated`가 발생하지 않는지 검증한다.
- Stage Clear 화면 생성 시 전체 화면 오버레이, `Stage Clear!` 문구, 버튼
  하나와 캔버스 설정이 구성되는지 검증한다.
- 보스 처치 직후 `Time.timeScale`이 0이 되고 플레이어 오브젝트는 계속
  활성 상태이며 메뉴는 아직 숨겨져 있는지 검증한다.
- 실시간 1초의 페이드 뒤 검정 오버레이가 불투명하고 문구와 버튼이 표시되는지
  검증한다.
- `타이틀화면으로 돌아가기` 버튼이 `GameScenes.Title`을 선택하고 전환 전에
  `Time.timeScale`을 1로 복원하는지 검증한다.
