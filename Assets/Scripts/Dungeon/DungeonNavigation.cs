using UnityEngine;

namespace NaManMoo.Dungeon
{
    /// <summary>
    /// 방 사이를 오갈 때 필요한 계산. 유니티 런타임에 의존하지 않아 EditMode 테스트로 덮는다.
    ///
    /// 방은 늘 원점에 하나만 세운다(<see cref="RoomShape"/>). 그래서 "다음 방으로 간다"는
    /// 것은 월드에서 이동하는 게 아니라 <b>이전 방을 지우고 다음 방을 다시 원점에 짓는</b>
    /// 일이다. 플레이어는 들어온 쪽 문 안쪽으로 순간이동한다.
    /// </summary>
    public static class DungeonNavigation
    {
        /// <summary>문 판정 상자가 벽에서 방 안쪽으로 들어오는 깊이.</summary>
        public const float DoorTriggerDepth = 1.2f;

        /// <summary>
        /// 새 방에서 문 안쪽으로 플레이어를 밀어 넣는 거리.
        ///
        /// <see cref="DoorTriggerDepth"/>보다 반드시 커야 한다. 작으면 도착하자마자
        /// 들어온 문 판정에 다시 걸려 방 사이를 무한히 튕긴다.
        /// </summary>
        public const float EntryInset = 3.6f;

        /// <summary>해당 방향으로 한 칸.</summary>
        public static Vector2Int Neighbour(Vector2Int cell, Doors side)
        {
            return side switch
            {
                Doors.North => cell + new Vector2Int(0, 1),
                Doors.South => cell + new Vector2Int(0, -1),
                Doors.East => cell + new Vector2Int(1, 0),
                Doors.West => cell + new Vector2Int(-1, 0),
                _ => cell
            };
        }

        /// <summary>변에서 방 안쪽을 가리키는 방향.</summary>
        public static Vector2 Inward(Doors side)
        {
            return side switch
            {
                Doors.North => Vector2.down,
                Doors.South => Vector2.up,
                Doors.East => Vector2.left,
                Doors.West => Vector2.right,
                _ => Vector2.zero
            };
        }

        /// <summary>
        /// 방마다 다른 시드. 같은 방으로 되돌아왔을 때 벽 모양이 바뀌면 다른 방처럼 보인다.
        /// 층과 칸 좌표를 섞어 층이 달라도 같은 좌표가 겹치지 않게 한다.
        /// </summary>
        public static int RoomSeed(int dungeonSeed, int floor, Vector2Int cell)
        {
            unchecked
            {
                int hash = dungeonSeed;
                hash = (hash * 397) ^ floor;
                hash = (hash * 397) ^ cell.x;
                hash = (hash * 397) ^ cell.y;
                // 0은 DeterministicRandom이 싫어하는 값이라 피한다
                return hash == 0 ? 1 : hash;
            }
        }

        /// <summary>
        /// 들어온 문 안쪽에 설 자리. <paramref name="entrySide"/>는 <b>새 방 기준</b>이다 —
        /// 북문으로 나갔으면 새 방에서는 남문으로 들어온다.
        /// </summary>
        public static Vector2 EntryPoint(RoomShape shape, Doors entrySide)
        {
            if (!shape.TryGetDoor(entrySide, out DoorOpening door))
            {
                // 문이 없는 쪽으로 들어올 수는 없다. 방 중앙이 안전한 대안이다.
                return shape.Bounds.center;
            }

            return door.Center + Inward(entrySide) * EntryInset;
        }

        /// <summary>첫 방에서 시작할 자리. 문과 무관하게 중앙이다.</summary>
        public static Vector2 StartPoint(RoomShape shape)
        {
            return shape.Bounds.center;
        }

        /// <summary>
        /// 문 판정 상자. 벽 구멍만큼 넓고 방 안쪽으로 <see cref="DoorTriggerDepth"/>만큼 깊다.
        /// 문 폭보다 좁으면 비스듬히 지날 때 통과해 버린다.
        /// </summary>
        public static Rect DoorTrigger(DoorOpening door)
        {
            Vector2 inward = Inward(door.Side);
            bool horizontal = door.Side is Doors.North or Doors.South;

            Vector2 size = horizontal
                ? new Vector2(RoomShape.DoorWidth, DoorTriggerDepth)
                : new Vector2(DoorTriggerDepth, RoomShape.DoorWidth);

            // 상자를 벽에 걸치지 않고 안쪽에 붙인다
            Vector2 center = door.Center + inward * (DoorTriggerDepth * 0.5f);

            return new Rect(center - size * 0.5f, size);
        }
    }
}
