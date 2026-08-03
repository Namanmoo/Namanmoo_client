# Dungeon Projectile Cleanup Design

## Goal

방을 이동할 때 이전 방에서 발사된 플레이어 및 적 투사체가 다음 방으로 넘어오지 않게 한다.

## Design

`DungeonRunner.Teardown()`이 기존 방을 해체하기 전에 씬에 존재하는 모든 `SwordProjectile`, `WeaponProjectile`, `EnemyProjectile`을 찾고 삭제한다. 검색에는 비활성 오브젝트도 포함한다. 투사체 생성 방식이나 개별 수명 및 충돌 동작은 변경하지 않는다.

## Covered projectiles

- 플레이어 검 투사체: `SwordProjectile`
- 플레이어 범용 원거리 투사체: `WeaponProjectile`
- 적 투사체: `EnemyProjectile`

## Verification

PlayMode 테스트에서 활성 및 비활성 상태의 세 투사체 타입을 생성하고 문을 통과한다. 방 전환 다음 프레임에 모든 투사체가 삭제되었는지 확인하며, 기존 `DungeonRunner` PlayMode 테스트도 함께 실행한다.

## Constraints

- `Nuts.png` 및 해당 메타 파일은 수정하지 않는다.
- Git 커밋은 사용자가 별도로 요청할 때만 수행한다.
