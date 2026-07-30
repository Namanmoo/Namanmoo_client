using System.Collections.Generic;
using UnityEngine;

namespace NaManMoo.Dungeon
{
    public enum RoomKind
    {
        Normal,
        Start,
        Boss,
        Treasure,
        Shop
    }

    /// <summary>방에 난 문. 이웃 방이 있는 방향에만 생긴다.</summary>
    [System.Flags]
    public enum Doors
    {
        None = 0,
        North = 1,
        South = 2,
        East = 4,
        West = 8
    }

    public sealed class DungeonRoom
    {
        public DungeonRoom(Vector2Int cell, RoomKind kind, Doors doors, int distanceFromStart)
        {
            Cell = cell;
            Kind = kind;
            Doors = doors;
            DistanceFromStart = distanceFromStart;
        }

        public Vector2Int Cell { get; }
        public RoomKind Kind { get; }
        public Doors Doors { get; }

        /// <summary>시작 방에서 문을 몇 번 지나야 오는가. 보스방을 가장 먼 곳에 두는 데 쓴다.</summary>
        public int DistanceFromStart { get; }

        /// <summary>문이 하나뿐인 방. 특수방은 여기에 배정한다.</summary>
        public bool IsDeadEnd => Doors == Doors.North
            || Doors == Doors.South
            || Doors == Doors.East
            || Doors == Doors.West;
    }

    /// <summary>
    /// 던전 한 층의 방 배치. 바인딩 오브 아이작의 생성 규칙을 따른다.
    ///
    /// 핵심은 <b>"놓을 칸의 이웃 방이 정확히 1개일 때만 놓는다"</b>는 규칙 하나다.
    /// 이게 없으면 방이 2x2 덩어리로 뭉쳐 미로가 아니라 광장이 되고, 막다른 곳이
    /// 안 생겨 보스·보물·상점을 놓을 자리가 사라진다.
    ///
    /// 시드로 완전히 재현된다 — 같은 (시드, 층)이면 항상 같은 층이다. 그래서 맵을
    /// 주고받을 필요가 없다. 시드 하나가 맵 전체다.
    ///
    /// 씬·유니티 런타임에 의존하지 않아 EditMode 테스트로 전부 덮을 수 있다.
    /// </summary>
    public sealed class DungeonLayout
    {
        /// <summary>아이작과 같은 격자 크기.</summary>
        public static readonly Vector2Int DefaultGrid = new Vector2Int(13, 9);

        /// <summary>방을 놓을 확률. 낮추면 층이 길고 가늘어진다.</summary>
        public const float PlaceChance = 0.5f;

        private static readonly Vector2Int[] Directions =
        {
            new Vector2Int(0, 1),
            new Vector2Int(0, -1),
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0)
        };

        private readonly Dictionary<Vector2Int, DungeonRoom> byCell;

        private DungeonLayout(
            int seed,
            int floor,
            Vector2Int grid,
            Vector2Int startCell,
            Dictionary<Vector2Int, DungeonRoom> rooms)
        {
            Seed = seed;
            Floor = floor;
            Grid = grid;
            StartCell = startCell;
            byCell = rooms;

            var ordered = new List<DungeonRoom>(rooms.Values);
            // 좌표 순으로 정렬해 목록 순서까지 재현되게 한다 — Dictionary 순서는 보장이 없다
            ordered.Sort((a, b) => a.Cell.y != b.Cell.y
                ? a.Cell.y.CompareTo(b.Cell.y)
                : a.Cell.x.CompareTo(b.Cell.x));
            Rooms = ordered;
        }

        public int Seed { get; }
        public int Floor { get; }
        public Vector2Int Grid { get; }
        public Vector2Int StartCell { get; }
        public IReadOnlyList<DungeonRoom> Rooms { get; }

        public bool HasRoom(Vector2Int cell) => byCell.ContainsKey(cell);

        public DungeonRoom RoomAt(Vector2Int cell)
        {
            return byCell.TryGetValue(cell, out DungeonRoom room) ? room : null;
        }

        public DungeonRoom RoomOfKind(RoomKind kind)
        {
            foreach (DungeonRoom room in Rooms)
            {
                if (room.Kind == kind)
                {
                    return room;
                }
            }

            return null;
        }

        /// <summary>층 번호로 정해지는 목표 방 개수. 아이작의 식을 따른다.</summary>
        public static int TargetRoomCount(int seed, int floor)
        {
            var rng = new DeterministicRandom(seed ^ 0x5F37);
            return 5 + rng.Next(2) + Mathf.FloorToInt(Mathf.Max(0, floor) * 2.6f);
        }

        public static DungeonLayout Generate(int seed, int floor)
        {
            return Generate(seed, floor, DefaultGrid);
        }

        public static DungeonLayout Generate(int seed, int floor, Vector2Int grid)
        {
            if (grid.x < 1 || grid.y < 1)
            {
                throw new System.ArgumentException($"격자가 너무 작습니다: {grid}", nameof(grid));
            }

            var rng = new DeterministicRandom(seed);
            int target = Mathf.Clamp(TargetRoomCount(seed, floor), 1, grid.x * grid.y);

            Vector2Int start = new Vector2Int(grid.x / 2, grid.y / 2);
            var cells = new HashSet<Vector2Int> { start };
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(start);

            // 큐가 비어도 목표에 못 미치면 기존 방들로 다시 채워 돌린다.
            // 확률 판정 때문에 한 바퀴에 목표를 못 채우는 경우가 흔하다.
            int guard = 0;
            while (cells.Count < target && guard++ < 200)
            {
                if (queue.Count == 0)
                {
                    foreach (Vector2Int cell in cells)
                    {
                        queue.Enqueue(cell);
                    }
                }

                int batch = queue.Count;
                for (int i = 0; i < batch && cells.Count < target; i++)
                {
                    Vector2Int cell = queue.Dequeue();
                    TryGrow(cell, grid, cells, queue, target, ref rng);
                }
            }

            Dictionary<Vector2Int, int> distances = BreadthFirstDistances(start, cells);
            Dictionary<Vector2Int, Doors> doors = DoorsFor(cells);
            Dictionary<Vector2Int, RoomKind> kinds = AssignKinds(start, cells, doors, distances);

            var rooms = new Dictionary<Vector2Int, DungeonRoom>(cells.Count);
            foreach (Vector2Int cell in cells)
            {
                rooms[cell] = new DungeonRoom(cell, kinds[cell], doors[cell], distances[cell]);
            }

            return new DungeonLayout(seed, floor, grid, start, rooms);
        }

        private static void TryGrow(
            Vector2Int cell,
            Vector2Int grid,
            HashSet<Vector2Int> cells,
            Queue<Vector2Int> queue,
            int target,
            ref DeterministicRandom rng)
        {
            foreach (Vector2Int direction in Directions)
            {
                if (cells.Count >= target)
                {
                    return;
                }

                Vector2Int candidate = cell + direction;

                if (candidate.x < 0 || candidate.x >= grid.x
                    || candidate.y < 0 || candidate.y >= grid.y)
                {
                    continue;
                }

                if (cells.Contains(candidate))
                {
                    continue;
                }

                // ── 이 규칙이 아이작 생성기의 핵심이다 ──
                // 이웃이 2개 이상인 칸을 채우면 방들이 2x2로 뭉친다.
                // 정확히 1개일 때만 놓으므로 2x2 덩어리가 생길 수 없다.
                if (NeighbourCount(candidate, cells) > 1)
                {
                    continue;
                }

                if (!rng.Chance(PlaceChance))
                {
                    continue;
                }

                cells.Add(candidate);
                queue.Enqueue(candidate);
            }
        }

        private static int NeighbourCount(Vector2Int cell, HashSet<Vector2Int> cells)
        {
            int count = 0;
            foreach (Vector2Int direction in Directions)
            {
                if (cells.Contains(cell + direction))
                {
                    count++;
                }
            }

            return count;
        }

        private static Dictionary<Vector2Int, int> BreadthFirstDistances(
            Vector2Int start, HashSet<Vector2Int> cells)
        {
            var distances = new Dictionary<Vector2Int, int> { [start] = 0 };
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                Vector2Int cell = queue.Dequeue();
                foreach (Vector2Int direction in Directions)
                {
                    Vector2Int next = cell + direction;
                    if (cells.Contains(next) && !distances.ContainsKey(next))
                    {
                        distances[next] = distances[cell] + 1;
                        queue.Enqueue(next);
                    }
                }
            }

            return distances;
        }

        private static Dictionary<Vector2Int, Doors> DoorsFor(HashSet<Vector2Int> cells)
        {
            var doors = new Dictionary<Vector2Int, Doors>(cells.Count);
            foreach (Vector2Int cell in cells)
            {
                Doors value = Doors.None;
                if (cells.Contains(cell + new Vector2Int(0, 1))) value |= Doors.North;
                if (cells.Contains(cell + new Vector2Int(0, -1))) value |= Doors.South;
                if (cells.Contains(cell + new Vector2Int(1, 0))) value |= Doors.East;
                if (cells.Contains(cell + new Vector2Int(-1, 0))) value |= Doors.West;
                doors[cell] = value;
            }

            return doors;
        }

        /// <summary>
        /// 막다른 방에 특수방을 배정한다. 가장 먼 곳이 보스, 그다음이 보물·상점.
        /// 막다른 곳이 모자라면 있는 만큼만 둔다 — 보스방은 방이 2개 이상이면 항상 생긴다.
        /// </summary>
        private static Dictionary<Vector2Int, RoomKind> AssignKinds(
            Vector2Int start,
            HashSet<Vector2Int> cells,
            Dictionary<Vector2Int, Doors> doors,
            Dictionary<Vector2Int, int> distances)
        {
            var kinds = new Dictionary<Vector2Int, RoomKind>(cells.Count);
            foreach (Vector2Int cell in cells)
            {
                kinds[cell] = RoomKind.Normal;
            }

            kinds[start] = RoomKind.Start;

            var deadEnds = new List<Vector2Int>();
            foreach (Vector2Int cell in cells)
            {
                if (cell == start)
                {
                    continue;
                }

                if (NeighbourCount(cell, cells) == 1)
                {
                    deadEnds.Add(cell);
                }
            }

            // 먼 곳 우선, 같으면 좌표 순 — Dictionary 순서에 흔들리지 않게 고정한다
            deadEnds.Sort((a, b) =>
            {
                int byDistance = distances[b].CompareTo(distances[a]);
                if (byDistance != 0)
                {
                    return byDistance;
                }

                return a.y != b.y ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x);
            });

            RoomKind[] specials = { RoomKind.Boss, RoomKind.Treasure, RoomKind.Shop };
            for (int i = 0; i < specials.Length && i < deadEnds.Count; i++)
            {
                kinds[deadEnds[i]] = specials[i];
            }

            // 막다른 곳이 아예 없으면(일자로만 자란 경우 등) 가장 먼 방을 보스로 둔다
            if (deadEnds.Count == 0 && cells.Count > 1)
            {
                Vector2Int farthest = start;
                foreach (KeyValuePair<Vector2Int, int> pair in distances)
                {
                    if (pair.Key != start && pair.Value > distances[farthest])
                    {
                        farthest = pair.Key;
                    }
                }

                if (farthest != start)
                {
                    kinds[farthest] = RoomKind.Boss;
                }
            }

            return kinds;
        }
    }
}
