using System.Collections.Generic;
using UnityEngine;

namespace NaManMoo.Dungeon
{
    /// <summary>벽에 난 문 하나. 지나갈 수 있는 구간과 그 중심.</summary>
    public readonly struct DoorOpening
    {
        public DoorOpening(Doors side, Vector2 center, Vector2 from, Vector2 to)
        {
            Side = side;
            Center = center;
            From = from;
            To = to;
        }

        public Doors Side { get; }

        /// <summary>문 중앙. 방을 넘어올 때 플레이어를 여기 놓는다.</summary>
        public Vector2 Center { get; }

        public Vector2 From { get; }
        public Vector2 To { get; }
    }

    /// <summary>
    /// 방 하나의 기하 구조. 순수 계산이라 EditMode 테스트로 전부 덮을 수 있다.
    ///
    /// 방은 <b>원점에 짓는다.</b> 한 번에 한 방만 존재하므로 격자 좌표를 월드에 늘어놓을
    /// 필요가 없다 — 문을 지나면 이전 방을 파괴하고 다음 방을 다시 원점에 세운다.
    ///
    /// 모든 방의 바깥 사각형은 같다. 그래서 문을 <b>각 변의 중앙</b>에 두면 어떤 방끼리
    /// 붙어도 좌표가 자동으로 맞는다. 벽은 손그림처럼 흔들리게 두지만 문 주변은
    /// 평평하게 남긴다 — 비스듬한 벽에 문이 걸리면 지나갈 때 어색해진다.
    /// </summary>
    public sealed class RoomShape
    {
        /// <summary>방 크기. 카메라 뷰(약 35.6x20)보다 커서 안에서 추적이 필요하다.</summary>
        public static readonly Vector2 Size = new Vector2(44f, 30f);

        /// <summary>문 구간의 폭.</summary>
        public const float DoorWidth = 6f;

        /// <summary>문 양옆으로 이만큼은 벽을 평평하게 둔다.</summary>
        public const float FlatMargin = 2.5f;

        /// <summary>벽이 안쪽으로 들어오는 최대 폭. 방끼리 겹치지 않게 바깥으로는 나가지 않는다.</summary>
        public const float WobbleDepth = 1.2f;

        /// <summary>벽을 이 간격으로 쪼개 흔든다.</summary>
        private const float WobbleStep = 4f;

        private RoomShape(
            Rect bounds,
            IReadOnlyList<Vector2> floorOutline,
            IReadOnlyList<IReadOnlyList<Vector2>> walls,
            IReadOnlyList<DoorOpening> doors)
        {
            Bounds = bounds;
            FloorOutline = floorOutline;
            Walls = walls;
            DoorOpenings = doors;
        }

        /// <summary>카메라를 자를 때 쓰는 사각형. 모든 방이 같다.</summary>
        public Rect Bounds { get; }

        /// <summary>바닥 폴리곤. 문 자리까지 덮어야 지나갈 때 바닥이 끊기지 않는다.</summary>
        public IReadOnlyList<Vector2> FloorOutline { get; }

        /// <summary>벽 폴리라인들. 문 구간에서 끊긴다.</summary>
        public IReadOnlyList<IReadOnlyList<Vector2>> Walls { get; }

        public IReadOnlyList<DoorOpening> DoorOpenings { get; }

        public bool TryGetDoor(Doors side, out DoorOpening opening)
        {
            foreach (DoorOpening door in DoorOpenings)
            {
                if (door.Side == side)
                {
                    opening = door;
                    return true;
                }
            }

            opening = default;
            return false;
        }

        /// <summary>맞은편 문. 북쪽으로 나갔으면 다음 방의 남쪽에서 들어온다.</summary>
        public static Doors Opposite(Doors side)
        {
            return side switch
            {
                Doors.North => Doors.South,
                Doors.South => Doors.North,
                Doors.East => Doors.West,
                Doors.West => Doors.East,
                _ => Doors.None
            };
        }

        public static RoomShape Build(int seed, Doors doors)
        {
            var rng = new DeterministicRandom(seed);
            Rect bounds = new Rect(-Size.x * 0.5f, -Size.y * 0.5f, Size.x, Size.y);

            var floor = new List<Vector2>
            {
                new Vector2(bounds.xMin, bounds.yMin),
                new Vector2(bounds.xMax, bounds.yMin),
                new Vector2(bounds.xMax, bounds.yMax),
                new Vector2(bounds.xMin, bounds.yMax)
            };

            var openings = new List<DoorOpening>();
            var walls = new List<IReadOnlyList<Vector2>>();

            // 네 변을 각각 훑는다. 문이 있으면 그 구간을 비우고 양쪽으로 벽을 나눈다.
            AddSide(walls, openings, bounds, doors, Doors.South, ref rng);
            AddSide(walls, openings, bounds, doors, Doors.East, ref rng);
            AddSide(walls, openings, bounds, doors, Doors.North, ref rng);
            AddSide(walls, openings, bounds, doors, Doors.West, ref rng);

            return new RoomShape(bounds, floor, walls, openings);
        }

        private static void AddSide(
            List<IReadOnlyList<Vector2>> walls,
            List<DoorOpening> openings,
            Rect bounds,
            Doors doors,
            Doors side,
            ref DeterministicRandom rng)
        {
            // 변을 따라 진행하는 방향과, 방 안쪽으로 들어가는 방향
            Vector2 start;
            Vector2 along;
            Vector2 inward;
            float length;

            switch (side)
            {
                case Doors.South:
                    start = new Vector2(bounds.xMin, bounds.yMin);
                    along = Vector2.right;
                    inward = Vector2.up;
                    length = bounds.width;
                    break;
                case Doors.East:
                    start = new Vector2(bounds.xMax, bounds.yMin);
                    along = Vector2.up;
                    inward = Vector2.left;
                    length = bounds.height;
                    break;
                case Doors.North:
                    start = new Vector2(bounds.xMax, bounds.yMax);
                    along = Vector2.left;
                    inward = Vector2.down;
                    length = bounds.width;
                    break;
                default:
                    start = new Vector2(bounds.xMin, bounds.yMax);
                    along = Vector2.down;
                    inward = Vector2.right;
                    length = bounds.height;
                    break;
            }

            bool hasDoor = doors.HasFlag(side);
            float middle = length * 0.5f;
            float gapFrom = middle - DoorWidth * 0.5f;
            float gapTo = middle + DoorWidth * 0.5f;

            if (hasDoor)
            {
                Vector2 from = start + along * gapFrom;
                Vector2 to = start + along * gapTo;
                openings.Add(new DoorOpening(side, start + along * middle, from, to));

                walls.Add(WobbleWall(start, along, inward, 0f, gapFrom, middle, ref rng));
                walls.Add(WobbleWall(start, along, inward, gapTo, length, middle, ref rng));
            }
            else
            {
                walls.Add(WobbleWall(start, along, inward, 0f, length, middle, ref rng));
            }
        }

        /// <summary>
        /// 벽 한 구간을 흔들리는 폴리라인으로 만든다.
        ///
        /// 흔들림은 <b>안쪽으로만</b> 넣는다. 바깥으로 나가면 옆 방과 겹친다.
        /// 문 중앙 근처(<see cref="FlatMargin"/> 안)는 흔들지 않는다 — 비스듬한 벽에
        /// 문이 걸리면 지나갈 때 걸린다.
        /// </summary>
        private static List<Vector2> WobbleWall(
            Vector2 start,
            Vector2 along,
            Vector2 inward,
            float from,
            float to,
            float doorMiddle,
            ref DeterministicRandom rng)
        {
            var points = new List<Vector2>();
            float span = to - from;
            int steps = Mathf.Max(1, Mathf.RoundToInt(span / WobbleStep));

            for (int i = 0; i <= steps; i++)
            {
                float t = from + span * (i / (float)steps);
                float depth = 0f;

                bool ends = i == 0 || i == steps;
                bool nearDoor = Mathf.Abs(t - doorMiddle) < DoorWidth * 0.5f + FlatMargin;

                // 구간 양 끝은 모서리·문에 맞물리므로 건드리지 않는다
                if (!ends && !nearDoor)
                {
                    depth = rng.Chance(0.55f)
                        ? WobbleDepth * (rng.Next(1000) / 1000f)
                        : 0f;
                }

                points.Add(start + along * t + inward * depth);
            }

            return points;
        }
    }
}
