# Player Death Screen Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 플레이어 체력이 0이 되면 게임을 정지하고 플레이어를 숨긴 뒤, 1초 검정 페이드 후 사망 문구와 타이틀/재시작 버튼을 표시한다.

**Architecture:** `PlayerHealth`는 사망 사실만 `Died` 이벤트로 알린다. `PlayerDeathScreen`은 사망 상태, 시간 정지, unscaled-time 페이드와 씬 전환을 담당하고, `PlayerDeathScreenUIFactory`는 기존 코드 기반 UI 패턴에 맞춰 화면 요소를 생성한다.

**Tech Stack:** Unity, C#, uGUI, NUnit, Unity Test Framework

## Global Constraints

- 사망 문구는 정확히 `이번에도 틀렸나...`를 사용한다.
- 검정 페이드 시간은 실시간 기준 정확히 1초다.
- 문구와 버튼은 페이드 완료 후에만 표시한다.
- 사망 직후 플레이어를 숨기고 `Time.timeScale = 0`으로 게임을 멈춘다.
- 타이틀 버튼은 `GameScenes.Title`, 재시작 버튼은 현재 활성 씬을 로드한다.
- 씬 전환 전과 사망 화면 파괴 시 `Time.timeScale`을 1로 복원한다.
- 사용자가 별도로 요청하지 않았으므로 Git 커밋을 만들지 않는다.

---

## File Map

- Modify: `Assets/Scripts/Player/PlayerHealth.cs` — 사망 이벤트의 단일 진실 공급원
- Create: `Assets/Scripts/UI/PlayerDeathScreen.cs` — 사망 흐름, 페이드, 시간/씬 상태 제어
- Create: `Assets/Scripts/UI/PlayerDeathScreenView.cs` — UI 요소 참조와 표시 상태 제어
- Create: `Assets/Scripts/UI/PlayerDeathScreenUIFactory.cs` — 런타임 uGUI 트리 생성
- Modify: `Assets/Scripts/Stage1PlayerHealthSetup.cs` — 플레이어 체력과 사망 화면 연결
- Modify: `Assets/Tests/Editor/PlayerHealthTests.cs` — 사망 이벤트 회귀 테스트
- Create: `Assets/Tests/Editor/PlayerDeathScreenUIFactoryTests.cs` — UI 구조 테스트
- Create: `Assets/Tests/PlayMode/PlayerDeathScreenPlayModeTests.cs` — 정지, 페이드, 표시 흐름 테스트
- Modify: `Assets/Tests/Editor/Stage1PlayerHealthSetupTests.cs` — 스테이지 연결 테스트

### Task 1: PlayerHealth 사망 이벤트

**Files:**
- Modify: `Assets/Scripts/Player/PlayerHealth.cs`
- Modify: `Assets/Tests/Editor/PlayerHealthTests.cs`

**Interfaces:**
- Produces: `public event Action Died`
- Behavior: 체력이 양수에서 0으로 바뀐 유효 피해에서 정확히 한 번 발생

- [ ] **Step 1: 치명적 피해가 사망 이벤트를 한 번만 발생시키는 테스트 작성**

`PlayerHealthTests`에 다음 Unity 테스트를 추가한다.

```csharp
[UnityTest]
public IEnumerator LethalDamage_RaisesDiedExactlyOnce()
{
    yield return new EnterPlayMode();
    var player = new GameObject(nameof(PlayerHealthTests));
    PlayerHealth health = player.AddComponent<PlayerHealth>();
    int deathCount = 0;
    health.Died += () => deathCount++;

    Assert.That(health.TryTakeDamage(20, 0f, 0f), Is.True);
    Assert.That(health.TryTakeDamage(1, 1f, 0f), Is.False);

    Assert.That(health.CurrentHealth, Is.Zero);
    Assert.That(deathCount, Is.EqualTo(1));

    Object.Destroy(player);
    yield return new ExitPlayMode();
}
```

- [ ] **Step 2: 테스트를 실행해 RED 확인**

Unity Test Framework에서
`PlayerHealthTests.LethalDamage_RaisesDiedExactlyOnce`를 실행한다.

Expected: `PlayerHealth`에 `Died`가 없어 컴파일 실패.

- [ ] **Step 3: 최소 구현 추가**

`PlayerHealth`에 이벤트를 선언하고, `TryTakeDamage`에서
`HealthChanged` 직후 0 여부를 확인한다.

```csharp
public event Action Died;

// TryTakeDamage 내부
HealthChanged?.Invoke(CurrentHealth, MaxHealth);
if (CurrentHealth == 0)
{
    Died?.Invoke();
}
return true;
```

기존 `nextHealth == CurrentHealth` 방어 로직은 유지해 0에서 추가 피해가 이벤트를
다시 발생시키지 않게 한다.

- [ ] **Step 4: 대상 및 기존 체력 테스트를 실행해 GREEN 확인**

Unity Test Framework에서 `PlayerHealthTests` 전체를 실행한다.

Expected: 모든 테스트 PASS.

### Task 2: 사망 UI View와 팩토리

**Files:**
- Create: `Assets/Scripts/UI/PlayerDeathScreenView.cs`
- Create: `Assets/Scripts/UI/PlayerDeathScreenUIFactory.cs`
- Create: `Assets/Tests/Editor/PlayerDeathScreenUIFactoryTests.cs`

**Interfaces:**
- Produces: `PlayerDeathScreenView PlayerDeathScreenUIFactory.Create(Transform parent)`
- Produces: `Image FadeOverlay`, `GameObject Menu`, `Button TitleButton`, `Button RestartButton`
- Produces: `void SetFadeAlpha(float alpha)`, `void ShowMenu()`

- [ ] **Step 1: UI 구성 테스트 작성**

```csharp
[Test]
public void Create_BuildsHiddenDeathMenuAboveTransparentBlackOverlay()
{
    var root = new GameObject(nameof(PlayerDeathScreenUIFactoryTests));

    PlayerDeathScreenView view =
        PlayerDeathScreenUIFactory.Create(root.transform);

    Canvas canvas = view.GetComponentInParent<Canvas>();
    CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
    Text message = view.Menu.transform.Find("Message").GetComponent<Text>();

    Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
    Assert.That(canvas.sortingOrder, Is.EqualTo(100));
    Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(1920f, 1080f)));
    Assert.That(view.FadeOverlay.color, Is.EqualTo(new Color(0f, 0f, 0f, 0f)));
    Assert.That(view.Menu.activeSelf, Is.False);
    Assert.That(message.text, Is.EqualTo("이번에도 틀렸나..."));
    Assert.That(view.TitleButton.GetComponentInChildren<Text>().text,
        Is.EqualTo("타이틀화면으로 돌아가기"));
    Assert.That(view.RestartButton.GetComponentInChildren<Text>().text,
        Is.EqualTo("처음부터 다시하기"));

    Object.DestroyImmediate(root);
}
```

테스트 픽스처에는 `TearDown`에서 생성된 루트와 남은 `EventSystem`을 제거하는
정리 코드를 둔다.

- [ ] **Step 2: 테스트를 실행해 RED 확인**

Unity Test Framework에서 `PlayerDeathScreenUIFactoryTests`를 실행한다.

Expected: 팩토리와 View 타입이 없어 컴파일 실패.

- [ ] **Step 3: View 최소 구현 작성**

```csharp
public sealed class PlayerDeathScreenView : MonoBehaviour
{
    public Image FadeOverlay { get; private set; }
    public GameObject Menu { get; private set; }
    public Button TitleButton { get; private set; }
    public Button RestartButton { get; private set; }

    public void Initialize(
        Image fadeOverlay,
        GameObject menu,
        Button titleButton,
        Button restartButton)
    {
        FadeOverlay = fadeOverlay;
        Menu = menu;
        TitleButton = titleButton;
        RestartButton = restartButton;
    }

    public void SetFadeAlpha(float alpha)
    {
        Color color = FadeOverlay.color;
        color.a = Mathf.Clamp01(alpha);
        FadeOverlay.color = color;
    }

    public void ShowMenu()
    {
        Menu.SetActive(true);
    }
}
```

- [ ] **Step 4: 팩토리 최소 구현 작성**

`PlayerDeathScreenUIFactory.Create`는 다음 설정을 정확히 적용한다.

```csharp
public static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);
public const int SortingOrder = 100;
```

- `Player Death Canvas`: `Canvas`, `CanvasScaler`, `GraphicRaycaster`
- 전체 stretch 검정 `Fade Overlay`, 초기 알파 0
- 중앙 `Death Menu`, 초기 inactive
- `Message`, 두 `Button`과 각 버튼의 `Text`
- 내장 `LegacyRuntime.ttf` 폰트
- 현재 씬에 `EventSystem`이 없을 때만 `EventSystem`과
  `StandaloneInputModule` 생성
- 생성한 참조를 `PlayerDeathScreenView.Initialize`로 전달

- [ ] **Step 5: UI 테스트를 실행해 GREEN 확인**

Unity Test Framework에서 `PlayerDeathScreenUIFactoryTests`를 실행한다.

Expected: 모든 테스트 PASS.

### Task 3: 사망 흐름과 페이드

**Files:**
- Create: `Assets/Scripts/UI/PlayerDeathScreen.cs`
- Create: `Assets/Tests/PlayMode/PlayerDeathScreenPlayModeTests.cs`

**Interfaces:**
- Consumes: `PlayerHealth.Died`
- Consumes: `PlayerDeathScreenView`
- Produces: `void Initialize(GameObject player, PlayerHealth health, PlayerDeathScreenView view)`
- Produces: `void ReturnToTitle()`, `void RestartCurrentScene()`
- Test seam: `public float FadeDuration => 1f`, `public bool IsTransitioning`

- [ ] **Step 1: 사망 직후 상태 테스트 작성**

```csharp
[UnityTest]
public IEnumerator Death_ImmediatelyHidesPlayerPausesGameAndKeepsMenuHidden()
{
    GameObject root = new GameObject(nameof(PlayerDeathScreenPlayModeTests));
    GameObject player = new GameObject("Player");
    player.transform.SetParent(root.transform);
    PlayerHealth health = player.AddComponent<PlayerHealth>();
    PlayerDeathScreenView view =
        PlayerDeathScreenUIFactory.Create(root.transform);
    PlayerDeathScreen screen = view.gameObject.AddComponent<PlayerDeathScreen>();
    screen.Initialize(player, health, view);

    health.TryTakeDamage(20, 0f, 0f);
    yield return null;

    Assert.That(player.activeSelf, Is.False);
    Assert.That(Time.timeScale, Is.Zero);
    Assert.That(view.Menu.activeSelf, Is.False);
    Assert.That(screen.IsTransitioning, Is.True);

    Object.Destroy(root);
    Time.timeScale = 1f;
}
```

- [ ] **Step 2: 테스트를 실행해 RED 확인**

Expected: `PlayerDeathScreen`이 없어 컴파일 실패.

- [ ] **Step 3: 구독과 즉시 사망 처리 최소 구현**

`Initialize`에서 참조를 저장하고 `health.Died += HandleDeath`를 수행한다.
`HandleDeath`는 중복 방지 플래그를 설정하고 플레이어를 비활성화한 뒤
`Time.timeScale = 0f`와 `StartCoroutine(FadeToBlack())`을 실행한다.
`OnDestroy`는 이벤트 구독을 해제하고 `Time.timeScale = 1f`를 복원한다.

- [ ] **Step 4: 즉시 상태 테스트를 실행해 GREEN 확인**

Expected: 대상 테스트 PASS.

- [ ] **Step 5: 페이드 완료 테스트 작성**

```csharp
[UnityTest]
public IEnumerator Death_AfterOneRealtimeSecondShowsOpaqueMenu()
{
    // Step 1과 동일한 Arrange
    health.TryTakeDamage(20, 0f, 0f);

    yield return new WaitForSecondsRealtime(1.05f);

    Assert.That(view.FadeOverlay.color.a, Is.EqualTo(1f).Within(0.01f));
    Assert.That(view.Menu.activeSelf, Is.True);
    Assert.That(screen.IsTransitioning, Is.False);

    Object.Destroy(root);
    Time.timeScale = 1f;
}
```

- [ ] **Step 6: 페이드 테스트를 실행해 RED 확인**

Expected: 오버레이 알파가 1이 아니거나 메뉴가 숨겨져 있어 FAIL.

- [ ] **Step 7: unscaled-time 페이드 구현**

```csharp
private IEnumerator FadeToBlack()
{
    float elapsed = 0f;
    while (elapsed < FadeDuration)
    {
        elapsed += Time.unscaledDeltaTime;
        view.SetFadeAlpha(elapsed / FadeDuration);
        yield return null;
    }

    view.SetFadeAlpha(1f);
    view.ShowMenu();
    IsTransitioning = false;
}
```

- [ ] **Step 8: 페이드 테스트를 실행해 GREEN 확인**

Expected: 두 PlayMode 테스트 PASS.

### Task 4: 버튼 전환과 스테이지 연결

**Files:**
- Modify: `Assets/Scripts/UI/PlayerDeathScreen.cs`
- Modify: `Assets/Scripts/Stage1PlayerHealthSetup.cs`
- Modify: `Assets/Tests/Editor/Stage1PlayerHealthSetupTests.cs`
- Modify: `Assets/Tests/PlayMode/PlayerDeathScreenPlayModeTests.cs`

**Interfaces:**
- Consumes: `GameScenes.Title`
- Consumes: `SceneManager.GetActiveScene().path`
- Produces: `Stage1PlayerHealthSetup.Create`가 `PlayerDeathScreen`까지 구성

- [ ] **Step 1: 스테이지 연결 테스트 작성**

`Stage1PlayerHealthSetupTests.Create_AddsPlayerHealthAndTopLeftScaledCanvas`에 다음
검증을 추가한다.

```csharp
PlayerDeathScreen deathScreen =
    root.GetComponentInChildren<PlayerDeathScreen>(true);
Assert.That(deathScreen, Is.Not.Null);
Assert.That(deathScreen.GetComponent<PlayerDeathScreenView>(), Is.Not.Null);
```

- [ ] **Step 2: 연결 테스트를 실행해 RED 확인**

Expected: `PlayerDeathScreen`이 생성되지 않아 FAIL.

- [ ] **Step 3: Setup에서 사망 화면 생성 및 초기화**

`Stage1PlayerHealthSetup.Create`에서 체력/대시 UI 생성 뒤 다음 연결을 추가한다.

```csharp
PlayerDeathScreenView deathView =
    PlayerDeathScreenUIFactory.Create(canvasParent);
PlayerDeathScreen deathScreen =
    deathView.gameObject.AddComponent<PlayerDeathScreen>();
deathScreen.Initialize(player, health, deathView);
```

- [ ] **Step 4: 연결 테스트를 실행해 GREEN 확인**

Expected: `Stage1PlayerHealthSetupTests` PASS.

- [ ] **Step 5: 버튼 리스너 및 시간 복원 테스트 작성**

PlayMode 테스트에서 View 버튼을 클릭해 씬 로드 자체를 직접 일으키지 않도록,
`PlayerDeathScreen`에 다음 순수 선택 메서드를 테스트한다.

```csharp
[Test]
public void SceneChoices_UseTitleAndCurrentScenePaths()
{
    Assert.That(PlayerDeathScreen.TitleScenePath, Is.EqualTo(GameScenes.Title));
    Assert.That(
        PlayerDeathScreen.ResolveRestartScenePath("Assets/Scenes/SampleStage.unity"),
        Is.EqualTo("Assets/Scenes/SampleStage.unity"));
    Assert.That(PlayerDeathScreen.ResolveRestartScenePath(string.Empty), Is.Null);
}
```

그리고 View의 `TitleButton.onClick`과 `RestartButton.onClick`에 각각
`ReturnToTitle`, `RestartCurrentScene`가 연결되었는지 런타임 생성 테스트에서
`GetPersistentEventCount`가 아닌 실제 클릭 후 주입된 씬 로더 기록값으로
검증한다. 이를 위해 컨트롤러 내부 씬 로드는 다음 작은 인터페이스로 격리한다.

```csharp
public interface ISceneLoader
{
    string ActiveScenePath { get; }
    void Load(string scenePath);
}
```

기본 구현은 `SceneManager`를 사용하며, 테스트는 호출 경로를 기록하는
`RecordingSceneLoader`를 주입한다.

- [ ] **Step 6: 버튼 테스트를 실행해 RED 확인**

Expected: 씬 선택 API와 로더 주입이 없어 컴파일 실패.

- [ ] **Step 7: 씬 로더와 버튼 동작 최소 구현**

- `Initialize` 오버로드에서 기본 `UnitySceneLoader`를 사용
- 테스트용 오버로드에서 `ISceneLoader`를 받음
- 두 버튼 리스너 연결
- 전환 중복 방지 플래그 적용
- 유효한 경로일 때 `Time.timeScale = 1f` 복원 후 `Load`
- 빈 재시작 경로는 로드하지 않음

- [ ] **Step 8: 버튼과 전체 대상 테스트 실행**

Unity Test Framework에서 다음을 실행한다.

- `PlayerHealthTests`
- `PlayerDeathScreenUIFactoryTests`
- `PlayerDeathScreenPlayModeTests`
- `Stage1PlayerHealthSetupTests`
- `Stage1PlayerHealthSceneTests`
- `Stage1SceneBuilderTests`

Expected: 모든 테스트 PASS, Console error 없음.

### Task 5: 전체 회귀 검증

**Files:**
- Verify only

**Interfaces:**
- 없음

- [ ] **Step 1: 전체 EditMode 테스트 실행**

Unity Test Runner 또는 프로젝트의 Unity 배치 테스트 명령으로 전체 EditMode
테스트를 실행한다.

Expected: 모든 테스트 PASS.

- [ ] **Step 2: 전체 PlayMode 테스트 실행**

Unity Test Runner 또는 프로젝트의 Unity 배치 테스트 명령으로 전체 PlayMode
테스트를 실행한다.

Expected: 모든 테스트 PASS.

- [ ] **Step 3: 실제 스테이지 수동 확인**

`SampleStage`에서 플레이어 체력을 0으로 만들고 다음을 확인한다.

- 즉시 플레이어가 사라지고 적/투사체/게임 진행이 멈춤
- 1초 동안 검정색으로 부드럽게 페이드
- 페이드가 끝난 뒤에만 정확한 문구와 두 버튼 표시
- 타이틀 버튼으로 Title 씬 이동
- 다시 진입 후 재시작 버튼으로 SampleStage 초기 상태 로드
- 두 이동 후 게임 시간이 정상적으로 흐름

- [ ] **Step 4: 작업 트리 확인**

`git diff --check`와 `git status --short`를 실행해 공백 오류와 변경 파일 범위를
확인한다. 커밋은 만들지 않는다.

