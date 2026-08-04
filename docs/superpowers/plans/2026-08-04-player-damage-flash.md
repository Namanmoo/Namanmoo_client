# Player Damage Flash Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 실제 피해를 받은 플레이어의 몸체를 해당 피격의 무적시간 동안 검정색으로 점멸시킨다.

**Architecture:** `PlayerHealth`는 실제 피해가 승인된 경우에만 적용된 무적 지속시간을 `Damaged` 이벤트로 알린다. 독립된 `PlayerDamageFlash` 컴포넌트가 이벤트를 구독하여 `PlayerFactory`가 명시적으로 전달한 몸체 렌더러의 색만 전환하고 종료 시 원래 색으로 복구한다.

**Tech Stack:** Unity, C#, NUnit, Unity Test Framework

## Global Constraints

- 플레이어 몸체의 `SpriteRenderer`만 점멸한다.
- 손에 든 무기와 UI에는 효과를 적용하지 않는다.
- 플레이어와 몬스터의 체력, 공격력, 이동 속도, 발사체 속도 및 기존 무적시간 값은 변경하지 않는다.
- 피해가 거부되면 점멸하지 않는다.
- 사용자 요청이 없으므로 Git 커밋을 만들지 않는다.

---

## File Map

- Modify: `Assets/Scripts/Player/PlayerHealth.cs` — 승인된 피격 이벤트 제공
- Create: `Assets/Scripts/Player/PlayerDamageFlash.cs` — 몸체 점멸과 색상 복구
- Modify: `Assets/Scripts/Player/PlayerFactory.cs` — 몸체 렌더러를 점멸 컴포넌트에 명시적으로 연결
- Modify: `Assets/Tests/Editor/PlayerHealthTests.cs` — 승인/거부 피격 이벤트 검증
- Create: `Assets/Tests/Editor/PlayerDamageFlashTests.cs` — 점멸 및 복구 검증
- Modify: `Assets/Tests/Editor/Stage1SceneBuilderTests.cs` — 팩토리 연결 검증

### Task 1: 승인된 피격 이벤트

**Files:**
- Modify: `Assets/Scripts/Player/PlayerHealth.cs`
- Modify: `Assets/Tests/Editor/PlayerHealthTests.cs`

**Interfaces:**
- Produces: `public event Action<float> Damaged`
- Event argument: 해당 피격 직후 남은 무적시간(초)

- [ ] **Step 1: 승인된 피해만 이벤트를 발생시키는 실패 테스트 작성**

`PlayerHealthTests`에 다음 Unity 테스트를 추가한다.

```csharp
[UnityTest]
public IEnumerator TryTakeDamage_RaisesDamagedOnlyForAcceptedHit()
{
    yield return new EnterPlayMode();
    var player = new GameObject(nameof(PlayerHealthTests));
    PlayerHealth health = player.AddComponent<PlayerHealth>();
    int eventCount = 0;
    float reportedDuration = -1f;
    health.Damaged += duration =>
    {
        eventCount++;
        reportedDuration = duration;
    };

    Assert.That(health.TryTakeDamage(2, 10f, 1f), Is.True);
    Assert.That(health.TryTakeDamage(2, 10.5f, 1f), Is.False);
    Assert.That(eventCount, Is.EqualTo(1));
    Assert.That(reportedDuration, Is.EqualTo(1f).Within(0.001f));

    Object.Destroy(player);
    yield return new ExitPlayMode();
}
```

- [ ] **Step 2: RED 확인**

Unity EditMode 테스트에서 `PlayerHealthTests.TryTakeDamage_RaisesDamagedOnlyForAcceptedHit`를 실행한다.

Expected: `PlayerHealth.Damaged`가 없어 컴파일 실패.

- [ ] **Step 3: 최소 구현**

`PlayerHealth`에 이벤트를 선언하고 `TryTakeDamage`가 체력을 감소시키고 `GrantInvulnerability`를 호출한 직후 한 번만 발생시킨다.

```csharp
public event Action<float> Damaged;

// TryTakeDamage 내부
GrantInvulnerability(currentTime, invulnerabilityDuration);
Damaged?.Invoke(Mathf.Max(0f, invulnerableUntil - currentTime));
```

- [ ] **Step 4: GREEN 확인**

Unity EditMode 테스트에서 `PlayerHealthTests` 전체를 실행한다.

Expected: 모든 테스트 PASS.

### Task 2: 몸체 점멸 컴포넌트

**Files:**
- Create: `Assets/Scripts/Player/PlayerDamageFlash.cs`
- Create: `Assets/Tests/Editor/PlayerDamageFlashTests.cs`

**Interfaces:**
- Consumes: `PlayerHealth.Damaged`
- Produces: `public void Initialize(PlayerHealth newHealth, SpriteRenderer newBodyRenderer)`
- Constant behavior: `FlashInterval = 0.1f`, flash color `Color.black`

- [ ] **Step 1: 점멸 시작과 종료 복구 실패 테스트 작성**

`PlayerDamageFlashTests`에 실제 시간 경과를 사용하는 Unity 테스트를 작성한다.

```csharp
[UnityTest]
public IEnumerator AcceptedDamage_FlashesBodyBlackAndRestoresOriginalColor()
{
    var player = new GameObject(nameof(PlayerDamageFlashTests));
    PlayerHealth health = player.AddComponent<PlayerHealth>();
    var body = new GameObject("Body");
    body.transform.SetParent(player.transform);
    SpriteRenderer renderer = body.AddComponent<SpriteRenderer>();
    renderer.color = Color.white;
    PlayerDamageFlash flash = player.AddComponent<PlayerDamageFlash>();
    flash.Initialize(health, renderer);

    Assert.That(health.TryTakeDamage(1, Time.time, 0.25f), Is.True);
    Assert.That(renderer.color, Is.EqualTo(Color.black));

    yield return new WaitForSeconds(0.11f);
    Assert.That(renderer.color, Is.EqualTo(Color.white));

    yield return new WaitForSeconds(0.16f);
    Assert.That(renderer.color, Is.EqualTo(Color.white));

    Object.Destroy(player);
}
```

- [ ] **Step 2: RED 확인**

Unity EditMode 테스트에서 `PlayerDamageFlashTests`를 실행한다.

Expected: `PlayerDamageFlash`가 없어 컴파일 실패.

- [ ] **Step 3: 최소 점멸 구현**

`PlayerDamageFlash`는 초기화 시 이벤트를 구독하고, 피격 이벤트에서 몸체의 현재 색을 원래 색으로 저장한 뒤 즉시 검정색으로 바꾼다. `Update`에서 `Time.deltaTime`으로 남은 지속시간과 다음 전환 시각을 줄이며 0.1초마다 검정/원래 색을 교대한다. 시간이 끝나면 원래 색을 복구한다. 재초기화, 비활성화, 파괴 시 기존 구독을 해제하고 색을 복구한다.

```csharp
public sealed class PlayerDamageFlash : MonoBehaviour
{
    public const float FlashInterval = 0.1f;

    private PlayerHealth health;
    private SpriteRenderer bodyRenderer;
    private Color originalColor;
    private float remainingDuration;
    private float timeUntilToggle;
    private bool flashing;
    private bool showingBlack;

    public void Initialize(PlayerHealth newHealth, SpriteRenderer newBodyRenderer);
    private void HandleDamaged(float duration);
    private void Update();
    private void StopFlashing();
    private void OnDisable();
    private void OnDestroy();
}
```

duration이 0 이하이거나 참조가 없으면 색을 바꾸지 않는다. 재피격 시 진행 중인 효과를 중복 실행하지 않고 지속시간과 전환 타이머를 새로 시작한다.

- [ ] **Step 4: GREEN 확인**

Unity EditMode 테스트에서 `PlayerDamageFlashTests` 전체를 실행한다.

Expected: 모든 테스트 PASS.

- [ ] **Step 5: 거부된 피해가 점멸을 재시작하지 않는 실패 테스트 작성**

```csharp
[UnityTest]
public IEnumerator RejectedDamage_DoesNotRestartFlash()
{
    // Task 2 Step 1과 같은 플레이어, health, renderer, flash 구성
    Assert.That(health.TryTakeDamage(1, Time.time, 0.15f), Is.True);
    yield return new WaitForSeconds(0.11f);
    Assert.That(renderer.color, Is.EqualTo(Color.white));

    Assert.That(health.TryTakeDamage(1, Time.time, 0.15f), Is.False);
    yield return new WaitForSeconds(0.06f);

    Assert.That(renderer.color, Is.EqualTo(Color.white));
    Object.Destroy(player);
}
```

- [ ] **Step 6: RED/GREEN 확인**

테스트가 이벤트를 잘못 구독하거나 거부된 피해에서 효과를 재시작하는 구현을 탐지하는지 확인하고 `PlayerDamageFlashTests` 전체가 PASS할 때까지 최소 구현만 조정한다.

### Task 3: PlayerFactory 연결

**Files:**
- Modify: `Assets/Scripts/Player/PlayerFactory.cs`
- Modify: `Assets/Tests/Editor/Stage1SceneBuilderTests.cs`

**Interfaces:**
- Consumes: `PlayerDamageFlash.Initialize(PlayerHealth, SpriteRenderer)`
- Produces: `PlayerFactory.Create(...)`로 만든 플레이어에 초기화된 `PlayerDamageFlash`

- [ ] **Step 1: 팩토리 연결 실패 테스트 작성**

`Stage1SceneBuilderTests`의 플레이어 검증에 다음 내용을 추가한다.

```csharp
PlayerDamageFlash damageFlash = player.GetComponent<PlayerDamageFlash>();
Assert.That(damageFlash, Is.Not.Null);

PlayerHealth health = player.GetComponent<PlayerHealth>();
SpriteRenderer bodyRenderer =
    player.transform.Find("Player Visual").GetComponent<SpriteRenderer>();
Assert.That(health.TryTakeDamage(1, Time.time, 0.2f), Is.True);
Assert.That(bodyRenderer.color, Is.EqualTo(Color.black));

PlayerWeaponVisual weaponVisual = player.GetComponent<PlayerWeaponVisual>();
if (weaponVisual != null && weaponVisual.Renderer != null)
{
    Assert.That(weaponVisual.Renderer.color, Is.Not.EqualTo(Color.black));
}
```

- [ ] **Step 2: RED 확인**

Unity EditMode 테스트에서 `Stage1SceneBuilderTests`를 실행한다.

Expected: 생성된 플레이어에 `PlayerDamageFlash`가 없어 FAIL.

- [ ] **Step 3: 최소 팩토리 연결**

`Stage1PlayerHealthSetup.Create`가 반환하거나 플레이어에 추가한 `PlayerHealth`를 얻은 뒤 다음을 `PlayerFactory.Create`에 추가한다.

```csharp
PlayerHealth health = playerObject.GetComponent<PlayerHealth>();
PlayerDamageFlash damageFlash =
    playerObject.AddComponent<PlayerDamageFlash>();
damageFlash.Initialize(health, renderer);
```

- [ ] **Step 4: GREEN 확인**

Unity EditMode 테스트에서 다음을 실행한다.

- `Stage1SceneBuilderTests`
- `PlayerDamageFlashTests`
- `PlayerHealthTests`
- `PlayerDashTests`

Expected: 모든 테스트 PASS, 무기 렌더러와 대시 구성 회귀 없음.

### Task 4: 전체 검증

**Files:**
- Verify only

**Interfaces:**
- 없음

- [ ] **Step 1: 관련 EditMode 테스트 실행**

Unity 배치 테스트 명령으로 `PlayerHealthTests`, `PlayerDamageFlashTests`, `Stage1SceneBuilderTests`, `PlayerDashTests`를 실행한다.

Expected: 모든 테스트 PASS, Console error 없음.

- [ ] **Step 2: 전체 EditMode 테스트 실행**

Unity Test Framework의 전체 EditMode 테스트를 실행한다.

Expected: 모든 테스트 PASS.

- [ ] **Step 3: 전체 PlayMode 테스트 실행**

Unity Test Framework의 전체 PlayMode 테스트를 실행한다.

Expected: 모든 테스트 PASS.

- [ ] **Step 4: 작업 트리 검사**

`git diff --check`, `git diff -- Assets/Scripts/Player Assets/Tests/Editor`, `git status --short`를 실행한다.

Expected: 공백 오류 없음, 요청 범위의 파일만 변경, Git 커밋 없음.

