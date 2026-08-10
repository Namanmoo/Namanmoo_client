using UnityEngine;

namespace NaManMoo.Dungeon
{
    /// <summary>
    /// 원거리 무기 투사체와 몬스터 bullet을 막는 장애물임을 표시하는 마커.
    /// 로직은 없다 — WeaponProjectile/EnemyProjectile 등이 이 컴포넌트가
    /// 붙어 있는 콜라이더를 맞았는지만 확인한다.
    /// </summary>
    public sealed class BulletBlockingObstacle : MonoBehaviour
    {
    }
}
