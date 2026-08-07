using NaManMoo.Dungeon;
using UnityEngine;

namespace NaManMoo.Audio
{
    /// <summary>
    /// 던전의 방 이동과 보스 페이즈에 맞춰 곡을 바꾼다.
    ///
    /// 평소에는 던전 곡, 보스방에 들어서면 1페이즈 곡(재즈), 보스가 2페이즈로 넘어가면
    /// 2페이즈 곡(락). 셋은 리듬·화성이 같은 형제 곡이라(<c>MUSIC_DESIGN.md</c> 부록)
    /// 크로스페이드로 이어도 같은 노래가 계속되는 것처럼 들린다.
    ///
    /// 이미 잡은 보스방에 다시 들어가면 던전 곡을 유지한다 — 싸움이 없는데 전투곡이
    /// 나오면 거짓말이 된다. 보스를 잡은 직후에는 방을 나설 때 던전 곡으로 돌아온다.
    /// </summary>
    public sealed class DungeonBgmDirector : MonoBehaviour
    {
        [SerializeField] private BgmPlayer player;
        [SerializeField] private DungeonRunner runner;
        [SerializeField] private AudioClip dungeonClip;
        [SerializeField] private AudioClip bossPhase1Clip;
        [SerializeField] private AudioClip bossPhase2Clip;

        /// <summary>씬 빌더가 에디터에서 잇는다.</summary>
        public void Configure(
            BgmPlayer bgmPlayer,
            DungeonRunner dungeonRunner,
            AudioClip dungeon,
            AudioClip bossPhase1,
            AudioClip bossPhase2)
        {
            player = bgmPlayer;
            runner = dungeonRunner;
            dungeonClip = dungeon;
            bossPhase1Clip = bossPhase1;
            bossPhase2Clip = bossPhase2;
        }

        private void OnEnable()
        {
            if (runner != null)
            {
                runner.RoomChanged += OnRoomChanged;
            }

            SultanBossController.PhaseTwoStarted += OnBossPhaseTwo;
        }

        private void OnDisable()
        {
            if (runner != null)
            {
                runner.RoomChanged -= OnRoomChanged;
            }

            SultanBossController.PhaseTwoStarted -= OnBossPhaseTwo;
        }

        private void OnRoomChanged(Vector2Int cell)
        {
            if (player == null || runner == null || runner.Layout == null)
            {
                return;
            }

            DungeonRoom room = runner.Layout.RoomAt(cell);
            bool bossFight = room != null
                && room.Kind == RoomKind.Boss
                && !runner.IsCleared(cell);
            player.CrossfadeTo(bossFight ? bossPhase1Clip : dungeonClip);
        }

        private void OnBossPhaseTwo()
        {
            if (player != null)
            {
                player.CrossfadeTo(bossPhase2Clip);
            }
        }
    }
}
