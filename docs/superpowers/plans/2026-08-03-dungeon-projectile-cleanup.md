# Dungeon Projectile Cleanup Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 방을 이동할 때 활성·비활성 상태의 플레이어 및 적 투사체를 모두 삭제한다.

**Architecture:** `DungeonRunner`가 방 생명주기를 소유하므로 `Teardown()`에서 씬의 세 투사체 컴포넌트를 검색해 삭제한다. 투사체별 생성 및 이동 코드는 수정하지 않는다.

**Tech Stack:** Unity 6, C#, NUnit, Unity Test Framework PlayMode

## Global Constraints

- `SwordProjectile`, `WeaponProjectile`, `EnemyProjectile`을 모두 정리한다.
- 비활성 투사체도 정리한다.
- `Nuts.png` 및 해당 메타 파일은 수정하지 않는다.
- Git 커밋은 사용자가 별도로 요청할 때만 수행한다.

---

### Task 1: 방 전환 투사체 정리

**Files:**
- Modify: `Assets/Tests/PlayMode/DungeonRunnerPlayModeTests.cs`
- Modify: `Assets/Scripts/Dungeon/DungeonRunner.cs`

**Interfaces:**
- Consumes: `DungeonRunner.Teardown()`, Unity `Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None)`
- Produces: `DungeonRunner` 내부의 투사체 일괄 정리 동작

- [ ] **Step 1: 실패하는 PlayMode 테스트 작성**

`ChangingRoomsDestroysAllActiveAndInactiveProjectiles` 테스트에서 세 투사체 타입을 활성·비활성으로 생성하고 문을 통과한 뒤 모두 사라지는지 검사한다.

- [ ] **Step 2: 테스트가 올바른 이유로 실패하는지 확인**

Unity PlayMode에서 해당 테스트만 실행한다. 기존 구현에서는 투사체가 방 루트의 자식이 아니므로 남아 있어야 한다.

- [ ] **Step 3: 최소 구현**

`DungeonRunner.Teardown()`에서 각 투사체 타입을 비활성 포함 검색하고 해당 게임 오브젝트를 `Destroy`하는 전용 내부 메서드를 호출한다.

- [ ] **Step 4: 집중 테스트 통과 확인**

같은 PlayMode 테스트를 다시 실행해 통과를 확인한다.

- [ ] **Step 5: 회귀 테스트**

전체 `DungeonRunnerPlayModeTests`와 세 투사체의 Editor 테스트를 실행해 기존 이동, 수명, 충돌 동작이 유지되는지 확인한다.

- [ ] **Step 6: 변경 범위 검토**

`git diff --check`와 대상 파일 diff를 확인하고 `Nuts.png` 및 메타 파일이 변경되지 않았음을 확인한다. 커밋은 수행하지 않는다.
