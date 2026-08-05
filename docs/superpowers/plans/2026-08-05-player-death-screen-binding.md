# Player Death Screen Binding Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 플레이어 체력이 0이 되면 씬 초기화 순서와 관계없이 게임오버 화면이 표시되게 한다.

**Architecture:** `PlayerHealth.Start()`가 게임오버 화면의 존재 여부만 검사하지 않고 실제 바인딩 성공 여부를 확인한다. 씬 화면을 바인딩할 수 없으면 기존 팩토리로 정상 화면을 생성하고 체력 사망 이벤트를 연결한다.

**Tech Stack:** Unity 6, C#, NUnit, Unity Test Framework

## Global Constraints

- 플레이어와 몬스터의 체력, 공격력, 속도, 발사체 속도 수치는 변경하지 않는다.
- Git 커밋은 수행하지 않는다.

---

### Task 1: Player death screen binding fallback

**Files:**
- Modify: `Assets/Scripts/Player/PlayerHealth.cs`
- Modify: `Assets/Scripts/UI/PlayerDeathScreenRuntimeBinder.cs`
- Test: `Assets/Tests/Editor/PlayerDeathExistingSceneRegressionTests.cs`

**Interfaces:**
- Consumes: `PlayerHealth`, `PlayerDeathScreen`, `PlayerDeathScreenView`
- Produces: `PlayerDeathScreenRuntimeBinder.TryBind(PlayerHealth health, PlayerDeathScreen screen) : bool`

- [ ] **Step 1: Write the failing regression test**

기존 게임오버 화면이 씬에 있지만 초기화되지 않은 상태에서 플레이어 시작 바인딩을 실행하고, 치명 피해 후 플레이어 비활성화와 `Time.timeScale == 0`을 확인한다.

- [ ] **Step 2: Run the focused test and verify RED**

Unity Test Framework에서 해당 회귀 테스트만 실행하며, 기존 화면이 있다는 이유로 자동 생성과 바인딩을 모두 건너뛰어 실패하는지 확인한다.

- [ ] **Step 3: Implement minimal binding fallback**

`PlayerDeathScreenRuntimeBinder.TryBind`가 필요한 UI 자식과 컴포넌트를 검증한 뒤 화면을 초기화하고 `PlayerHealth.Died`를 연결하게 한다. `PlayerHealth.Start()`는 기존 화면 바인딩이 성공할 때만 반환하고, 실패하면 새 화면을 생성한다.

- [ ] **Step 4: Run focused and related tests**

게임오버 회귀 테스트, `PlayerHealthTests`, `PlayerDeathScreenPlayModeTests`를 실행하고 실패가 없는지 확인한다.

- [ ] **Step 5: Inspect diff**

게임 수치가 변경되지 않았고 게임오버 바인딩과 테스트만 변경됐는지 확인한다.
