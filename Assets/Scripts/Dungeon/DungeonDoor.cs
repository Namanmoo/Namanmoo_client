using UnityEngine;

namespace NaManMoo.Dungeon
{
    /// <summary>
    /// 문 하나. 잠겨 있으면 막대 콜라이더로 길을 막고, 열리면 그것을 끄고 통과를 알린다.
    ///
    /// 방을 넘기는 판단은 <see cref="DungeonRunner"/>가 한다 — 문은 "플레이어가 지나갔다"만
    /// 알린다. 문이 씬 전환까지 알면 방 하나를 테스트하기 위해 던전 전체가 필요해진다.
    /// </summary>
    public sealed class DungeonDoor : MonoBehaviour
    {
        private Collider2D bar;
        private LineRenderer barVisual;

        public Doors Side { get; private set; }

        public bool IsOpen { get; private set; }

        /// <summary>플레이어가 이 문을 지났을 때. 인자는 나가는 방향이다.</summary>
        public event System.Action<Doors> Passed;

        public void Configure(Doors side, Collider2D lockBar, LineRenderer visual)
        {
            Side = side;
            bar = lockBar;
            barVisual = visual;
            SetOpen(false);
        }

        public void SetOpen(bool open)
        {
            IsOpen = open;

            if (bar != null)
            {
                bar.enabled = !open;
            }

            if (barVisual != null)
            {
                barVisual.enabled = !open;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsOpen || !other.CompareTag("Player"))
            {
                return;
            }

            Passed?.Invoke(Side);
        }
    }
}
