# 적 피격 넉백 설계

## 목표

플레이어의 무기 공격이 적에게 명중하면, 공격 방향의 정반대 방향으로 적이 살짝 밀려난다.

## 범위

- 대상 공격(직접 타격): 근접 판정(`MeleeStrike` — swing/thrust/spin), 도끼(`AxeSwing`), 검 발사체(`SwordProjectile`), 일반 무기 발사체(`WeaponProjectile`).
- 제외: 화상·독 지속피해(`EnemyStatus`), 폭발·연쇄 2차 효과(`AreaActions`), 기존 `ShockwaveAction`의 자체 넉백(방사형이라 이 기능과 방향 기준이 다름 — 변경하지 않는다).
- 대상 적: `ChaseContactEnemyController`, `ApproachAndShootEnemyController`, `KrabEnemy` 중 하나를 가진 "추적형" 적만. 보스(`BossRobotController`, `SlimeBossController`, `SultanBossController`)와 고정형 적(`StationaryFourWayShooterController`)은 넉백 대상에서 제외한다.
- 플레이어·몬스터·보스·무기의 기존 밸런스 수치(체력, 공격력, 이동속도 등)는 변경하지 않는다.

## 구조

### EnemyStatus (확장)

기존에 경직(`ApplyStagger`)·냉기(`ApplyChill`)·화상·독을 관리하던 컴포넌트에 넉백 상태를 추가한다.

- `ApplyKnockback(Vector2 direction, float distance, float duration)`: 방향을 정규화해 슬라이드 속도(`distance / duration`)와 잔여시간을 저장한다. 이미 넉백이 진행 중이면 새 호출로 덮어쓴다(경직·냉기처럼 누적하지 않는다).
- `Tick()`: 잔여시간이 남아 있으면 매 프레임 `transform.position`을 슬라이드 속도만큼 이동시키고 잔여시간을 줄인다.
- `SpeedMultiplier`: 넉백이 진행 중이면 경직과 동일하게 0을 반환해, 추적 이동 로직이 그 사이 넉백 이동과 충돌하지 않도록 멈춘다.

### EnemyKnockback (신규 정적 헬퍼, Combat)

각 히트 지점에서 호출하는 단일 진입점.

- `Apply(EnemyHealth target, Vector2 attackDirection)`
  - `target`이 없거나 이미 죽었거나(`CurrentHealth <= 0`), `attackDirection`이 0벡터면 아무 것도 하지 않는다.
  - `target`의 게임 오브젝트에 `ChaseContactEnemyController` · `ApproachAndShootEnemyController` · `KrabEnemy` 중 아무것도 없으면(보스·고정형 적) 아무 것도 하지 않는다.
  - 그 외의 경우 `EnemyStatus.EnsureOn(target).ApplyKnockback(-attackDirection, KnockbackDistance, KnockbackDuration)`을 호출한다.
  - `KnockbackDistance`(기본 0.3)와 `KnockbackDuration`(기본 0.12초)은 이 클래스의 상수로 관리해 한 곳에서 조정할 수 있게 한다.

### 호출 지점 4곳

각 지점에서 `TakeDamage` 호출 직후, 그 공격이 실제로 사용한 방향으로 `EnemyKnockback.Apply`를 호출한다.

- `MeleeStrike.Execute` (`MeleeDeliveries.cs`): `context.Direction` 사용.
- `AxeSwing.TryHit`: `AxeSwing`은 현재 공격 방향을 들고 있지 않으므로 `Initialize`에 방향 매개변수를 옵션으로 추가한다(기본값 `Vector2.zero` — 넉백 없음). `PlayerAxeAttacker.SpawnSwing`이 이미 계산해 둔 `direction`을 그대로 전달한다. 기존 3-인자 `Initialize` 호출(테스트 포함)은 그대로 컴파일된다.
- `SwordProjectile.TryHit`: 맞는 순간의 `direction`(진행 방향) 사용.
- `WeaponProjectile.TryHit`: 맞는 순간의 `direction`(유도·도탄으로 꺾였을 수 있는 현재 진행 방향) 사용.

## 동작 흐름

1. 플레이어 무기가 적 콜라이더를 맞혀 `EnemyHealth.TakeDamage`가 호출된다.
2. 같은 지점에서 그 공격의 방향 정보로 `EnemyKnockback.Apply`를 호출한다.
3. 적이 죽었거나, 방향이 없거나, 추적형이 아니면 조용히 무시된다.
4. 그 외에는 `EnemyStatus`에 넉백 상태가 설정되고, 공격 방향의 반대로 `KnockbackDuration` 동안 `KnockbackDistance`만큼 부드럽게 슬라이드한다.
5. 슬라이드 도중에는 `SpeedMultiplier`가 0이 되어 추적 이동이 멈추고, 슬라이드가 끝나면 정상 추적 이동이 재개된다.

## 예외 처리

- 이미 처치된 적(`CurrentHealth <= 0`)에는 넉백을 적용하지 않는다(기존 `ShockwaveAction`과 동일한 규칙).
- 보스·고정형 적은 애초에 대상에서 제외되므로 별도 마커 컴포넌트 없이 컨트롤러 타입 확인만으로 충분하다.
- 넉백 도중 같은 적이 다시 맞으면 기존 슬라이드를 최신 공격 기준으로 덮어쓴다(경직 중첩 없이 갱신되는 기존 패턴과 동일).
- `AxeSwing`처럼 방향이 전달되지 않은 기존 호출부는 방향이 0벡터이므로 넉백이 자연히 비활성화된다(하위 호환).

## 테스트

기능과 직접 관련된 좁은 범위만 검증한다(전체 스위트 실행 안 함).

- `EnemyStatus`: `ApplyKnockback` 호출 후 `Tick`을 진행시키면 지정 거리만큼 반대 방향으로 이동하고, 지속시간 동안 `SpeedMultiplier`가 0인지 확인.
- `EnemyKnockback`: 추적형 컨트롤러가 있는 적에게는 넉백이 걸리고, 보스/고정형 컨트롤러가 있는 적이나 이미 죽은 적에게는 걸리지 않는지 확인.
- 기존 `AxeSwingTests`가 새 옵션 매개변수 추가 후에도 그대로 컴파일·통과하는지 확인.
