using System.Collections.Generic;
using UnityEngine;

namespace NaManMoo.Dungeon
{
    /// <summary>
    /// 방에 크랩을 채운다. 전부 죽으면 <see cref="Stage1EncounterGate"/>가 열리고
    /// <see cref="DungeonRunner"/>가 그때 문을 연다.
    ///
    /// 문을 막는 벽은 게이트가 아니라 문 쪽에 있다 — 방마다 문이 여럿이라 게이트
    /// 하나로는 다 막을 수 없다. 그래서 게이트에는 장벽을 주지 않고 적 추적만 맡긴다.
    /// </summary>
    public sealed class KrabRoomEncounter : MonoBehaviour, IRoomEncounter
    {
        [SerializeField] private Sprite krabSprite;

        public void Configure(Sprite sprite)
        {
            krabSprite = sprite;
        }

        public Stage1EncounterGate Spawn(
            Transform roomRoot,
            Transform player,
            RoomShape shape,
            DungeonRoom room,
            int roomSeed)
        {
            if (krabSprite == null || player == null)
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
            if (spots.Count == 0)
            {
                return null;
            }

            var enemies = new List<EnemyHealth>(spots.Count);
            for (int i = 0; i < spots.Count; i++)
            {
                enemies.Add(KrabFactory.Create(
                    roomRoot, player, krabSprite, spots[i], $"Krab {i + 1}"));
            }

            var gateObject = new GameObject("Room Clear Gate");
            gateObject.transform.SetParent(roomRoot, false);

            Stage1EncounterGate gate = gateObject.AddComponent<Stage1EncounterGate>();
            // 장벽과 표시는 넘기지 않는다. 막는 일은 문이 한다.
            gate.Initialize(enemies, null, new Renderer[0]);
            return gate;
        }
    }
}
