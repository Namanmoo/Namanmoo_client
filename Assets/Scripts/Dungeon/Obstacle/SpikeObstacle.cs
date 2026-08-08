using UnityEngine;

namespace NaManMoo.Dungeon
{
    /// <summary>
    /// 지나갈 수는 있지만 플레이어에게만 데미지를 주는 장애물. 몬스터는 그냥 지나간다 —
    /// 몬스터 쪽 컴포넌트를 아예 찾지 않아서, 특별히 막을 필요가 없다.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class SpikeObstacle : MonoBehaviour
    {
        private const float PlayerInvulnerabilityDuration = 1f;

        [SerializeField, Min(0)] private int damage = 2;

        private void Awake()
        {
            // 트리거가 아니면 물리적으로 막혀버려 "지나갈 수 있다"가 깨진다
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryDamagePlayer(other, Time.time);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryDamagePlayer(other, Time.time);
        }

        public bool TryDamagePlayer(Collider2D other, float currentTime)
        {
            if (other == null)
            {
                return false;
            }

            PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
            if (health == null)
            {
                return false;
            }

            return health.TryTakeDamage(damage, currentTime, PlayerInvulnerabilityDuration);
        }
    }
}
