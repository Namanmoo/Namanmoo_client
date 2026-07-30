using NaManMoo.Dungeon;
using NUnit.Framework;
using UnityEngine;

public sealed class DungeonNavigationTests
{
    private static readonly Doors[] Sides = { Doors.North, Doors.South, Doors.East, Doors.West };
    private const Doors AllDoors = Doors.North | Doors.South | Doors.East | Doors.West;

    [Test]
    public void NeighbourMovesOneCellInEachDirection()
    {
        var cell = new Vector2Int(5, 5);

        Assert.That(DungeonNavigation.Neighbour(cell, Doors.North), Is.EqualTo(new Vector2Int(5, 6)));
        Assert.That(DungeonNavigation.Neighbour(cell, Doors.South), Is.EqualTo(new Vector2Int(5, 4)));
        Assert.That(DungeonNavigation.Neighbour(cell, Doors.East), Is.EqualTo(new Vector2Int(6, 5)));
        Assert.That(DungeonNavigation.Neighbour(cell, Doors.West), Is.EqualTo(new Vector2Int(4, 5)));
        Assert.That(DungeonNavigation.Neighbour(cell, Doors.None), Is.EqualTo(cell));
    }

    [Test]
    public void WalkingThroughADoorAndBackReturnsToTheSameCell()
    {
        var cell = new Vector2Int(3, 7);

        foreach (Doors side in Sides)
        {
            Vector2Int next = DungeonNavigation.Neighbour(cell, side);
            Vector2Int back = DungeonNavigation.Neighbour(next, RoomShape.Opposite(side));

            Assert.That(back, Is.EqualTo(cell), $"{side} 로 나갔다 돌아오면 제자리여야 한다");
        }
    }

    [Test]
    public void EntryInsetClearsTheDoorTrigger()
    {
        // 이게 뒤집히면 도착하자마자 들어온 문에 다시 걸려 방 사이를 무한히 튕긴다
        Assert.That(
            DungeonNavigation.EntryInset,
            Is.GreaterThan(DungeonNavigation.DoorTriggerDepth),
            "들어선 자리가 문 판정 안이면 즉시 되돌아간다");
    }

    [Test]
    public void EntryPointSitsInsideTheRoomNotOnTheWall()
    {
        RoomShape shape = RoomShape.Build(4, AllDoors);
        Rect bounds = shape.Bounds;

        foreach (Doors side in Sides)
        {
            Vector2 point = DungeonNavigation.EntryPoint(shape, side);

            Assert.That(bounds.Contains(point), Is.True, $"{side}: {point} 가 방 밖이다");

            // 벽에서 충분히 떨어져 있어야 벽 충돌에 끼지 않는다
            float toWall = Mathf.Min(
                point.x - bounds.xMin, bounds.xMax - point.x,
                point.y - bounds.yMin, bounds.yMax - point.y);
            Assert.That(toWall, Is.GreaterThan(RoomShape.WobbleDepth), $"{side}: 벽에 너무 붙었다");
        }
    }

    [Test]
    public void EntryPointIsNotInsideTheDoorTriggerItCameThrough()
    {
        RoomShape shape = RoomShape.Build(9, AllDoors);

        foreach (Doors side in Sides)
        {
            shape.TryGetDoor(side, out DoorOpening door);
            Rect trigger = DungeonNavigation.DoorTrigger(door);
            Vector2 point = DungeonNavigation.EntryPoint(shape, side);

            Assert.That(trigger.Contains(point), Is.False, $"{side}: 들어선 자리가 문 판정 안이다");
        }
    }

    [Test]
    public void EntryPointLinesUpWithTheDoorItCameThrough()
    {
        // 문 중앙에서 좌우로 벗어나면 벽을 뚫고 들어온 것처럼 보인다
        RoomShape shape = RoomShape.Build(6, AllDoors);

        shape.TryGetDoor(Doors.South, out DoorOpening south);
        Vector2 fromSouth = DungeonNavigation.EntryPoint(shape, Doors.South);
        Assert.That(fromSouth.x, Is.EqualTo(south.Center.x).Within(0.001f));
        Assert.That(fromSouth.y, Is.GreaterThan(south.Center.y));

        shape.TryGetDoor(Doors.West, out DoorOpening west);
        Vector2 fromWest = DungeonNavigation.EntryPoint(shape, Doors.West);
        Assert.That(fromWest.y, Is.EqualTo(west.Center.y).Within(0.001f));
        Assert.That(fromWest.x, Is.GreaterThan(west.Center.x));
    }

    [Test]
    public void EntryThroughAMissingDoorFallsBackToTheCentre()
    {
        RoomShape shape = RoomShape.Build(1, Doors.North);

        Assert.That(
            DungeonNavigation.EntryPoint(shape, Doors.South),
            Is.EqualTo(shape.Bounds.center));
    }

    [Test]
    public void StartPointIsTheRoomCentre()
    {
        RoomShape shape = RoomShape.Build(1, AllDoors);

        Assert.That(DungeonNavigation.StartPoint(shape), Is.EqualTo(shape.Bounds.center));
    }

    [Test]
    public void DoorTriggerIsAtLeastAsWideAsTheDoorway()
    {
        // 좁으면 비스듬히 지날 때 판정을 건너뛰어 벽을 통과한 것처럼 보인다
        RoomShape shape = RoomShape.Build(2, AllDoors);

        foreach (Doors side in Sides)
        {
            shape.TryGetDoor(side, out DoorOpening door);
            Rect trigger = DungeonNavigation.DoorTrigger(door);

            float span = side is Doors.North or Doors.South ? trigger.width : trigger.height;
            Assert.That(span, Is.GreaterThanOrEqualTo(RoomShape.DoorWidth), $"{side}");
        }
    }

    [Test]
    public void DoorTriggerSitsInsideTheRoom()
    {
        RoomShape shape = RoomShape.Build(3, AllDoors);
        Rect bounds = shape.Bounds;

        foreach (Doors side in Sides)
        {
            shape.TryGetDoor(side, out DoorOpening door);
            Rect trigger = DungeonNavigation.DoorTrigger(door);

            Assert.That(trigger.xMin, Is.GreaterThanOrEqualTo(bounds.xMin - 0.001f), $"{side}");
            Assert.That(trigger.xMax, Is.LessThanOrEqualTo(bounds.xMax + 0.001f), $"{side}");
            Assert.That(trigger.yMin, Is.GreaterThanOrEqualTo(bounds.yMin - 0.001f), $"{side}");
            Assert.That(trigger.yMax, Is.LessThanOrEqualTo(bounds.yMax + 0.001f), $"{side}");
        }
    }

    [Test]
    public void RoomSeedIsStableForTheSameRoom()
    {
        // 같은 방에 되돌아왔을 때 벽이 달라지면 다른 방처럼 보인다
        var cell = new Vector2Int(6, 4);

        Assert.That(
            DungeonNavigation.RoomSeed(1234, 1, cell),
            Is.EqualTo(DungeonNavigation.RoomSeed(1234, 1, cell)));
    }

    [Test]
    public void RoomSeedDiffersByCellFloorAndDungeon()
    {
        int baseline = DungeonNavigation.RoomSeed(1234, 1, new Vector2Int(6, 4));

        Assert.That(DungeonNavigation.RoomSeed(1234, 1, new Vector2Int(7, 4)), Is.Not.EqualTo(baseline));
        Assert.That(DungeonNavigation.RoomSeed(1234, 1, new Vector2Int(6, 5)), Is.Not.EqualTo(baseline));
        Assert.That(DungeonNavigation.RoomSeed(1234, 2, new Vector2Int(6, 4)), Is.Not.EqualTo(baseline));
        Assert.That(DungeonNavigation.RoomSeed(9999, 1, new Vector2Int(6, 4)), Is.Not.EqualTo(baseline));
    }

    [Test]
    public void RoomSeedIsNeverZero()
    {
        // DeterministicRandom(xorshift)은 0에서 영원히 0을 낸다
        for (int seed = -40; seed <= 40; seed++)
        {
            for (int floor = 0; floor < 6; floor++)
            {
                for (int x = -6; x <= 6; x++)
                {
                    for (int y = -6; y <= 6; y++)
                    {
                        Assert.That(
                            DungeonNavigation.RoomSeed(seed, floor, new Vector2Int(x, y)),
                            Is.Not.Zero);
                    }
                }
            }
        }
    }

    [Test]
    public void EveryRoomInAGeneratedFloorCanBeEnteredFromWhereItCameFrom()
    {
        // 배치와 기하가 서로 맞는지 — 이웃이 있는 방향에는 반드시 양쪽 다 문이 있어야 한다
        for (int seed = 1; seed <= 12; seed++)
        {
            DungeonLayout layout = DungeonLayout.Generate(seed, floor: 2);

            foreach (DungeonRoom room in layout.Rooms)
            {
                foreach (Doors side in Sides)
                {
                    if (!room.Doors.HasFlag(side))
                    {
                        continue;
                    }

                    Vector2Int nextCell = DungeonNavigation.Neighbour(room.Cell, side);
                    DungeonRoom next = layout.RoomAt(nextCell);

                    Assert.That(next, Is.Not.Null,
                        $"시드 {seed}: {room.Cell} 의 {side} 문 뒤에 방이 없다");
                    Assert.That(next.Doors.HasFlag(RoomShape.Opposite(side)), Is.True,
                        $"시드 {seed}: {nextCell} 에 돌아올 문이 없다");

                    // 그 문으로 들어설 자리가 실제로 존재해야 한다
                    RoomShape shape = RoomShape.Build(
                        DungeonNavigation.RoomSeed(seed, 2, nextCell), next.Doors);
                    Vector2 entry = DungeonNavigation.EntryPoint(shape, RoomShape.Opposite(side));

                    Assert.That(entry, Is.Not.EqualTo(shape.Bounds.center),
                        $"시드 {seed}: {nextCell} 의 {RoomShape.Opposite(side)} 입구를 못 찾았다");
                }
            }
        }
    }
}
