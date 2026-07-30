using System.Collections.Generic;
using UnityEngine;

namespace NaManMoo.Dungeon
{
    /// <summary>미니맵에 그릴 칸 하나.</summary>
    public readonly struct MinimapCell
    {
        public MinimapCell(Vector2Int cell, RoomKind kind, bool visited, bool current)
        {
            Cell = cell;
            Kind = kind;
            Visited = visited;
            Current = current;
        }

        public Vector2Int Cell { get; }
        public RoomKind Kind { get; }

        /// <summary>가 본 방. 안 가 본 방은 종류를 숨긴다.</summary>
        public bool Visited { get; }

        public bool Current { get; }
    }

    /// <summary>
    /// 미니맵에 무엇을 그릴지 정한다. 유니티 UI와 떼어 놓아 테스트로 덮는다.
    ///
    /// <b>가 본 방과 그 이웃만</b> 보여 준다. 층 전체를 처음부터 보여 주면
    /// 탐험할 이유가 없어지고, 이웃조차 안 보여 주면 어디로 갈지 고를 수 없다.
    /// 안 가 본 이웃은 있다는 것만 알리고 종류는 숨긴다.
    /// </summary>
    public static class MinimapModel
    {
        private static readonly Doors[] Sides =
            { Doors.North, Doors.South, Doors.East, Doors.West };

        /// <summary>
        /// 그릴 칸들. 좌표 순으로 정렬해 돌려주므로 같은 상태면 항상 같은 순서다.
        /// </summary>
        public static List<MinimapCell> Build(
            DungeonLayout layout, Vector2Int current, IReadOnlyCollection<Vector2Int> visited)
        {
            var cells = new List<MinimapCell>();
            if (layout == null)
            {
                return cells;
            }

            var seenSet = new HashSet<Vector2Int>(visited ?? new List<Vector2Int>());
            seenSet.Add(current);

            var shown = new HashSet<Vector2Int>(seenSet);
            foreach (Vector2Int cell in seenSet)
            {
                DungeonRoom room = layout.RoomAt(cell);
                if (room == null)
                {
                    continue;
                }

                foreach (Doors side in Sides)
                {
                    if (room.Doors.HasFlag(side))
                    {
                        shown.Add(DungeonNavigation.Neighbour(cell, side));
                    }
                }
            }

            foreach (Vector2Int cell in shown)
            {
                DungeonRoom room = layout.RoomAt(cell);
                if (room == null)
                {
                    continue;
                }

                bool wasVisited = seenSet.Contains(cell);
                cells.Add(new MinimapCell(
                    cell,
                    // 안 가 본 방의 종류를 보여 주면 보물방을 찍어서 갈 수 있다
                    wasVisited ? room.Kind : RoomKind.Normal,
                    wasVisited,
                    cell == current));
            }

            cells.Sort((a, b) => a.Cell.y != b.Cell.y
                ? a.Cell.y.CompareTo(b.Cell.y)
                : a.Cell.x.CompareTo(b.Cell.x));

            return cells;
        }

        /// <summary>
        /// 칸을 미니맵 안에서 어디에 놓을지. 현재 방을 중심에 두면 층이 커도
        /// 미니맵 크기가 늘지 않는다.
        /// </summary>
        public static Vector2 PositionOf(Vector2Int cell, Vector2Int current, float step)
        {
            Vector2Int offset = cell - current;
            return new Vector2(offset.x * step, offset.y * step);
        }
    }
}
