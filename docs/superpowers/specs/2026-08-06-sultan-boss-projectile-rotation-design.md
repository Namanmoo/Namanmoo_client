# 술탄 보스 투사체 방향 회전 설계

## 목표

`SultanBossController`의 `AimedShotPattern`, `EightWayShotPattern`이 발사하는 투사체(`FireEnemyProjectile`)가
발사 방향을 `flipX` 좌우 반전이 아니라 실제 회전으로 표현한다. 투사체 스프라이트는 0도일 때
오른쪽을 바라보도록 그려져 있으므로, 방향 벡터의 각도를 그대로 스프라이트 회전에 적용한다.
플레이어·몬스터·보스의 체력, 공격력, 이동 속도 등 기존 밸런스 수치는 변경하지 않는다.

## 현재 동작

`GetProjectileOrientation(direction, out angle, out flipX)`는 세로 방향(`|direction.y| > |direction.x|`)일
때만 각도를 ±90도로 스냅하고, 그 외에는 회전 없이(`angle = 0`) `flipX`로만 좌우를 뒤집는다. 그 결과
대각선처럼 카디널 방향이 아닌 곳으로 조준하면 투사체가 실제 각도를 향하지 않는다.

## 변경할 동작

- `GetProjectileOrientation`을 `GetProjectileRotationAngle(Vector2 direction)`으로 교체한다.
  `Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg`를 그대로 반환하는 순수 함수이며 `flipX`
  출력은 없앤다.
- `FireEnemyProjectile()`에서 `flipX` 계산과 `projectileVisual.flipX = flipX;` 대입을 제거하고,
  새 각도를 그대로 `EnemyProjectile.Initialize(...)`의 `initialRotation` 인자로 넘긴다.
- 좌측 방향으로 회전했을 때 스프라이트가 위아래로 뒤집힌 것처럼 보여도 별도 세로 반전(`flipY`)
  보정은 하지 않는다(사용자 확인 사항).
- `AimedShotPattern`, `EightWayShotPattern`이 같은 `FireEnemyProjectile()`을 공유하므로 이 변경은
  두 패턴 모두에 동일하게 적용된다.

## 영향받지 않는 부분

- `EnemyProjectile.cs`는 이미 임의의 `initialRotation`을 받는 `Initialize` 오버로드가 있어 수정이
  필요 없다.
- `EnemyProjectile`을 재사용하는 다른 적/보스의 투사체 로직은 이 변경과 무관하다.
- 낙하 패턴(`FallArcPattern`)의 카메라 흔들림, 페이즈 전환 등 다른 보스 로직은 건드리지 않는다.

## 테스트

`SultanBossController`에는 기존 테스트가 없고 `GetProjectileRotationAngle`은 `private static`이다.
이 코드베이스의 기존 관례(리플렉션으로 private 메서드를 직접 호출하는 방식, 예:
`CameraFollowTests.cs`, `Stage1SceneBuilderTests.cs`)를 따라 새 EditMode 테스트를 추가한다.

- 우/상/하/좌 방향 벡터가 각각 0/90/-90/180도를 반환하는지 검증한다.
- 대각선 방향(예: 우상단 `(1,1)` 정규화)이 45도처럼 스냅되지 않은 실제 각도를 반환하는지 검증한다.
- `Mathf.DeltaAngle`로 각도 wrap-around(180 vs -180)를 안전하게 비교한다.
