using System.Collections.Generic;
using NaManMoo.Dungeon;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 층 생성이 지켜야 하는 보장 조건들. 랜덤이라 눈으로 확인할 수 없으니
/// 여러 시드에 걸쳐 성질을 검사한다.
/// </summary>
public sealed class DungeonLayoutTests
{
    private const int SeedCount = 200;

    private static IEnumerable<DungeonLayout> ManyLayouts(int floor = 1)
    {
        for (int seed = 1; seed <= SeedCount; seed++)
        {
            yield return DungeonLayout.Generate(seed, floor);
        }
    }

    private static readonly Vector2Int[] Directions =
    {
        new Vector2Int(0, 1),
        new Vector2Int(0, -1),
        new Vector2Int(1, 0),
        new Vector2Int(-1, 0)
    };

    [Test]
    public void SameSeedGivesTheSameFloor()
    {
        // 이게 깨지면 "시드 하나가 맵 전체"라는 전제가 무너진다
        DungeonLayout first = DungeonLayout.Generate(12345, 2);
        DungeonLayout second = DungeonLayout.Generate(12345, 2);

        Assert.That(first.Rooms.Count, Is.EqualTo(second.Rooms.Count));
        for (int i = 0; i < first.Rooms.Count; i++)
        {
            Assert.That(first.Rooms[i].Cell, Is.EqualTo(second.Rooms[i].Cell));
            Assert.That(first.Rooms[i].Kind, Is.EqualTo(second.Rooms[i].Kind));
            Assert.That(first.Rooms[i].Doors, Is.EqualTo(second.Rooms[i].Doors));
        }
    }

    [Test]
    public void DifferentSeedsMostlyGiveDifferentFloors()
    {
        var shapes = new HashSet<string>();
        foreach (DungeonLayout layout in ManyLayouts())
        {
            var cells = new List<string>();
            foreach (DungeonRoom room in layout.Rooms)
            {
                cells.Add($"{room.Cell.x},{room.Cell.y}");
            }

            shapes.Add(string.Join("|", cells));
        }

        // 완전히 다 달라야 할 이유는 없지만, 대부분 달라야 랜덤이라 할 수 있다
        Assert.That(shapes.Count, Is.GreaterThan(SeedCount * 0.8f));
    }

    [Test]
    public void EveryRoomIsReachableFromTheStart()
    {
        foreach (DungeonLayout layout in ManyLayouts())
        {
            var seen = new HashSet<Vector2Int> { layout.StartCell };
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(layout.StartCell);

            while (queue.Count > 0)
            {
                Vector2Int cell = queue.Dequeue();
                foreach (Vector2Int direction in Directions)
                {
                    Vector2Int next = cell + direction;
                    if (layout.HasRoom(next) && seen.Add(next))
                    {
                        queue.Enqueue(next);
                    }
                }
            }

            Assert.That(
                seen.Count,
                Is.EqualTo(layout.Rooms.Count),
                $"시드 {layout.Seed}: 닿을 수 없는 방이 있다");
        }
    }

    [Test]
    public void NoTwoByTwoBlockOfRooms()
    {
        // "이웃이 정확히 1개일 때만 놓는다"는 규칙의 직접적인 결과다.
        // 뭉치면 미로가 아니라 광장이 되고 막다른 곳이 사라진다.
        foreach (DungeonLayout layout in ManyLayouts())
        {
            foreach (DungeonRoom room in layout.Rooms)
            {
                Vector2Int c = room.Cell;
                bool block = layout.HasRoom(c)
                    && layout.HasRoom(c + new Vector2Int(1, 0))
                    && layout.HasRoom(c + new Vector2Int(0, 1))
                    && layout.HasRoom(c + new Vector2Int(1, 1));

                Assert.That(block, Is.False, $"시드 {layout.Seed}: {c}에 2x2 덩어리");
            }
        }
    }

    [Test]
    public void RoomCountMatchesTheTargetForTheFloor()
    {
        for (int floor = 1; floor <= 5; floor++)
        {
            for (int seed = 1; seed <= 40; seed++)
            {
                DungeonLayout layout = DungeonLayout.Generate(seed, floor);
                int target = DungeonLayout.TargetRoomCount(seed, floor);

                Assert.That(
                    layout.Rooms.Count,
                    Is.EqualTo(target),
                    $"시드 {seed}, {floor}층: 목표 {target}개를 못 채웠다");
            }
        }
    }

    [Test]
    public void DeeperFloorsHaveMoreRooms()
    {
        int shallow = DungeonLayout.Generate(7, 1).Rooms.Count;
        int deep = DungeonLayout.Generate(7, 5).Rooms.Count;

        Assert.That(deep, Is.GreaterThan(shallow));
    }

    [Test]
    public void ThereIsExactlyOneStartAndOneBoss()
    {
        foreach (DungeonLayout layout in ManyLayouts())
        {
            int start = 0;
            int boss = 0;
            foreach (DungeonRoom room in layout.Rooms)
            {
                if (room.Kind == RoomKind.Start) start++;
                if (room.Kind == RoomKind.Boss) boss++;
            }

            Assert.That(start, Is.EqualTo(1), $"시드 {layout.Seed}: 시작 방이 {start}개");
            Assert.That(boss, Is.EqualTo(1), $"시드 {layout.Seed}: 보스방이 {boss}개");
        }
    }

    [Test]
    public void StartRoomIsAtTheStartCell()
    {
        foreach (DungeonLayout layout in ManyLayouts())
        {
            Assert.That(layout.RoomAt(layout.StartCell).Kind, Is.EqualTo(RoomKind.Start));
        }
    }

    [Test]
    public void BossIsADeadEndAndFarFromTheStart()
    {
        foreach (DungeonLayout layout in ManyLayouts())
        {
            DungeonRoom boss = layout.RoomOfKind(RoomKind.Boss);

            Assert.That(boss.IsDeadEnd, Is.True, $"시드 {layout.Seed}: 보스방이 막다른 곳이 아니다");
            Assert.That(
                boss.DistanceFromStart,
                Is.GreaterThan(0),
                $"시드 {layout.Seed}: 보스방이 시작 방과 같다");

            // 다른 어떤 막다른 방도 보스방보다 멀지 않아야 한다
            foreach (DungeonRoom room in layout.Rooms)
            {
                if (room.IsDeadEnd && room.Kind != RoomKind.Start)
                {
                    Assert.That(
                        room.DistanceFromStart,
                        Is.LessThanOrEqualTo(boss.DistanceFromStart),
                        $"시드 {layout.Seed}: {room.Cell}이 보스방보다 멀다");
                }
            }
        }
    }

    [Test]
    public void SpecialRoomsNeverShareACell()
    {
        foreach (DungeonLayout layout in ManyLayouts())
        {
            var used = new Dictionary<RoomKind, Vector2Int>();
            foreach (DungeonRoom room in layout.Rooms)
            {
                if (room.Kind == RoomKind.Normal)
                {
                    continue;
                }

                Assert.That(
                    used.ContainsKey(room.Kind),
                    Is.False,
                    $"시드 {layout.Seed}: {room.Kind}가 두 개다");
                used[room.Kind] = room.Cell;
            }
        }
    }

    [Test]
    public void DoorsAreSymmetric()
    {
        // A에 동쪽 문이 있으면 그 옆 방에는 서쪽 문이 있어야 한다.
        // 어긋나면 한쪽에서만 지나갈 수 있는 문이 생긴다.
        foreach (DungeonLayout layout in ManyLayouts())
        {
            foreach (DungeonRoom room in layout.Rooms)
            {
                CheckPair(layout, room, Doors.North, new Vector2Int(0, 1), Doors.South);
                CheckPair(layout, room, Doors.South, new Vector2Int(0, -1), Doors.North);
                CheckPair(layout, room, Doors.East, new Vector2Int(1, 0), Doors.West);
                CheckPair(layout, room, Doors.West, new Vector2Int(-1, 0), Doors.East);
            }
        }
    }

    [Test]
    public void DoorsOnlyExistWhereANeighbourDoes()
    {
        foreach (DungeonLayout layout in ManyLayouts())
        {
            foreach (DungeonRoom room in layout.Rooms)
            {
                Assert.That(
                    room.Doors.HasFlag(Doors.North),
                    Is.EqualTo(layout.HasRoom(room.Cell + new Vector2Int(0, 1))));
                Assert.That(
                    room.Doors.HasFlag(Doors.East),
                    Is.EqualTo(layout.HasRoom(room.Cell + new Vector2Int(1, 0))));
            }
        }
    }

    [Test]
    public void RoomsStayInsideTheGrid()
    {
        foreach (DungeonLayout layout in ManyLayouts())
        {
            foreach (DungeonRoom room in layout.Rooms)
            {
                Assert.That(room.Cell.x, Is.InRange(0, layout.Grid.x - 1));
                Assert.That(room.Cell.y, Is.InRange(0, layout.Grid.y - 1));
            }
        }
    }

    [Test]
    public void TinyGridStillProducesAValidFloor()
    {
        // 목표 방 개수가 격자보다 많으면 격자 크기로 잘려야 한다 — 무한 루프에 빠지면 안 된다
        DungeonLayout layout = DungeonLayout.Generate(3, 5, new Vector2Int(2, 2));

        Assert.That(layout.Rooms.Count, Is.LessThanOrEqualTo(4));
        Assert.That(layout.RoomAt(layout.StartCell), Is.Not.Null);
    }

    [Test]
    public void SingleCellGridDegradesGracefully()
    {
        DungeonLayout layout = DungeonLayout.Generate(1, 1, new Vector2Int(1, 1));

        Assert.That(layout.Rooms.Count, Is.EqualTo(1));
        Assert.That(layout.Rooms[0].Kind, Is.EqualTo(RoomKind.Start));
        Assert.That(layout.Rooms[0].Doors, Is.EqualTo(Doors.None));
    }

    [Test]
    public void InvalidGridIsRejected()
    {
        Assert.Throws<System.ArgumentException>(
            () => DungeonLayout.Generate(1, 1, new Vector2Int(0, 5)));
    }

    private static void CheckPair(
        DungeonLayout layout, DungeonRoom room, Doors door, Vector2Int step, Doors opposite)
    {
        if (!room.Doors.HasFlag(door))
        {
            return;
        }

        DungeonRoom neighbour = layout.RoomAt(room.Cell + step);
        Assert.That(neighbour, Is.Not.Null, $"{room.Cell}의 {door} 문 건너에 방이 없다");
        Assert.That(
            neighbour.Doors.HasFlag(opposite),
            Is.True,
            $"{room.Cell}→{neighbour.Cell} 문이 한쪽만 있다");
    }
}
