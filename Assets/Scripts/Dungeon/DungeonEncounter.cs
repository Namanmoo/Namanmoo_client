using System.Collections.Generic;
using UnityEngine;

namespace NaManMoo.Dungeon
{
    /// <summary>
    /// 방에 무엇이 나올지 정한다. 보스방에는 보스 로봇 하나, 일반 방에는 크랩 여럿,
    /// 시작·보물·상점 방에는 아무것도 없다.
    ///
    /// 전부 죽으면 <see cref="Stage1EncounterGate"/>가 열리고 <see cref="DungeonRunner"/>가
    /// 그때 문을 연다. 문을 막는 벽은 게이트가 아니라 문 쪽에 있다 — 방마다 문이 여럿이라
    /// 게이트 하나로는 다 막을 수 없어, 게이트에는 장벽을 주지 않고 추적만 맡긴다.
    ///
    /// 보스는 <b>방에 들어서면 바로</b> 나온다. Stage1처럼 따로 진입 트리거를 두지 않는다 —
    /// 던전에서는 방 자체가 이미 경계이고, 문이 잠기는 것으로 "시작했다"가 드러난다.
    /// </summary>
    public sealed class DungeonEncounter : MonoBehaviour, IRoomEncounter
    {
        [SerializeField] private Sprite krabSprite;
        [SerializeField] private Sprite bossSprite;

        public void Configure(Sprite krab, Sprite boss)
        {
            krabSprite = krab;
            bossSprite = boss;
        }

        public Stage1EncounterGate Spawn(
            Transform roomRoot,
            Transform player,
            RoomShape shape,
            DungeonRoom room,
            int roomSeed)
        {
            if (player == null)
            {
                return null;
            }

            List<EnemyHealth> enemies = room.Kind == RoomKind.Boss
                ? SpawnBoss(roomRoot, player, shape)
                : SpawnKrabs(roomRoot, player, shape, room, roomSeed);

            if (enemies == null || enemies.Count == 0)
            {
                return null;
            }

            var gateObject = new GameObject("Room Clear Gate");
            gateObject.transform.SetParent(roomRoot, false);

            Stage1EncounterGate gate = gateObject.AddComponent<Stage1EncounterGate>();
            // 장벽과 표시는 넘기지 않는다. 막는 일은 문이 한다.
            gate.Initialize(enemies, null, new Renderer[0]);
            return gate;
        }

        private List<EnemyHealth> SpawnBoss(
            Transform roomRoot, Transform player, RoomShape shape)
        {
            if (bossSprite == null)
            {
                return null;
            }

            // 체력 바 캔버스를 방 아래에 둔다 — 방을 떠나면 같이 사라진다
            Transform canvas = BossFactory.CreateOverlayCanvas(roomRoot, "Boss Health Canvas");

            // 방 중앙에 세운다. 어느 문으로 들어와도 가장 먼 곳이라, 들어서자마자
            // 몸으로 겹쳐 맞는 일이 없다 (문 안쪽 착지점은 벽에서 3.6).
            return new List<EnemyHealth>
            {
                BossFactory.Create(roomRoot, canvas, player, bossSprite, shape.Bounds.center)
            };
        }

        private List<EnemyHealth> SpawnKrabs(
            Transform roomRoot,
            Transform player,
            RoomShape shape,
            DungeonRoom room,
            int roomSeed)
        {
            if (krabSprite == null)
            {
                return null;
            }

            int count = RoomSpawnPoints.EnemyCount(room.Kind, room.DistanceFromStart);
            if (count <= 0)
            {
                return null;
            }

            // 방 기하와 같은 시드를 쓴다 — 되돌아왔을 때 벽도 배치도 그대로여야 한다
            List<Vector2> spots = RoomSpawnPoints.Inside(shape, count, roomSeed);
            var enemies = new List<EnemyHealth>(spots.Count);
            for (int i = 0; i < spots.Count; i++)
            {
                enemies.Add(KrabFactory.Create(
                    roomRoot, player, krabSprite, spots[i], $"Krab {i + 1}"));
            }

            return enemies;
        }
    }
}
