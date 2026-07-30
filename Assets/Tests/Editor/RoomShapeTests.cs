using System.Collections.Generic;
using NaManMoo.Dungeon;
using NUnit.Framework;
using UnityEngine;

public sealed class RoomShapeTests
{
    private static readonly Doors[] AllCombinations = BuildCombinations();

    private static Doors[] BuildCombinations()
    {
        var list = new List<Doors>();
        for (int mask = 0; mask < 16; mask++)
        {
            list.Add((Doors)mask);
        }

        return list.ToArray();
    }

    [Test]
    public void EveryRoomHasTheSameBounds()
    {
        // 방 크기가 다르면 문 좌표가 안 맞고 카메라 경계도 방마다 달라진다
        Rect first = RoomShape.Build(1, Doors.North).Bounds;

        foreach (Doors doors in AllCombinations)
        {
            for (int seed = 1; seed <= 20; seed++)
            {
                Assert.That(RoomShape.Build(seed, doors).Bounds, Is.EqualTo(first));
            }
        }
    }

    [Test]
    public void RoomIsBiggerThanTheCameraView()
    {
        // 방이 화면보다 작으면 카메라가 따라갈 이유가 없다 (뷰는 약 35.6x20)
        Rect bounds = RoomShape.Build(1, Doors.None).Bounds;

        Assert.That(bounds.width, Is.GreaterThan(35.6f));
        Assert.That(bounds.height, Is.GreaterThan(20f));
    }

    [Test]
    public void DoorsExistOnlyWhereRequested()
    {
        foreach (Doors doors in AllCombinations)
        {
            RoomShape shape = RoomShape.Build(7, doors);

            foreach (Doors side in new[] { Doors.North, Doors.South, Doors.East, Doors.West })
            {
                bool wanted = doors.HasFlag(side);
                Assert.That(
                    shape.TryGetDoor(side, out _),
                    Is.EqualTo(wanted),
                    $"{doors}: {side} 문이 {(wanted ? "있어야" : "없어야")} 한다");
            }
        }
    }

    [Test]
    public void DoorsSitAtTheCentreOfTheirSide()
    {
        // 이게 어긋나면 옆 방과 문 좌표가 안 맞아 지나갈 때 벽에 박힌다
        RoomShape shape = RoomShape.Build(3, Doors.North | Doors.South | Doors.East | Doors.West);
        Rect bounds = shape.Bounds;

        shape.TryGetDoor(Doors.North, out DoorOpening north);
        shape.TryGetDoor(Doors.South, out DoorOpening south);
        shape.TryGetDoor(Doors.East, out DoorOpening east);
        shape.TryGetDoor(Doors.West, out DoorOpening west);

        Assert.That(north.Center.x, Is.EqualTo(bounds.center.x).Within(0.001f));
        Assert.That(north.Center.y, Is.EqualTo(bounds.yMax).Within(0.001f));
        Assert.That(south.Center.x, Is.EqualTo(bounds.center.x).Within(0.001f));
        Assert.That(south.Center.y, Is.EqualTo(bounds.yMin).Within(0.001f));
        Assert.That(east.Center.x, Is.EqualTo(bounds.xMax).Within(0.001f));
        Assert.That(east.Center.y, Is.EqualTo(bounds.center.y).Within(0.001f));
        Assert.That(west.Center.x, Is.EqualTo(bounds.xMin).Within(0.001f));
        Assert.That(west.Center.y, Is.EqualTo(bounds.center.y).Within(0.001f));
    }

    [Test]
    public void OppositeDoorsLineUpBetweenNeighbouringRooms()
    {
        // 방마다 시드가 달라도 문 좌표는 같아야 한다 — 벽만 다르게 흔들린다
        RoomShape left = RoomShape.Build(11, Doors.East);
        RoomShape right = RoomShape.Build(999, Doors.West);

        left.TryGetDoor(Doors.East, out DoorOpening exit);
        right.TryGetDoor(Doors.West, out DoorOpening entry);

        // 원점 기준으로 짓기 때문에, 나가는 문의 y와 들어오는 문의 y가 같아야 한다
        Assert.That(entry.Center.y, Is.EqualTo(exit.Center.y).Within(0.001f));
        Assert.That(entry.Center.x, Is.EqualTo(-exit.Center.x).Within(0.001f));
    }

    [Test]
    public void DoorOpeningIsTheExpectedWidth()
    {
        RoomShape shape = RoomShape.Build(5, Doors.North);
        shape.TryGetDoor(Doors.North, out DoorOpening door);

        Assert.That(
            Vector2.Distance(door.From, door.To),
            Is.EqualTo(RoomShape.DoorWidth).Within(0.001f));
    }

    [Test]
    public void WallsNeverPokeOutsideTheBounds()
    {
        // 바깥으로 나가면 옆 방과 겹친다
        foreach (Doors doors in AllCombinations)
        {
            for (int seed = 1; seed <= 30; seed++)
            {
                RoomShape shape = RoomShape.Build(seed, doors);
                Rect bounds = shape.Bounds;

                foreach (IReadOnlyList<Vector2> wall in shape.Walls)
                {
                    foreach (Vector2 point in wall)
                    {
                        Assert.That(point.x, Is.InRange(bounds.xMin - 0.001f, bounds.xMax + 0.001f));
                        Assert.That(point.y, Is.InRange(bounds.yMin - 0.001f, bounds.yMax + 0.001f));
                    }
                }
            }
        }
    }

    [Test]
    public void NoWallPointBlocksADoorway()
    {
        // 흔들린 벽이 문 구간을 가로막으면 지나갈 수 없다
        foreach (Doors doors in AllCombinations)
        {
            for (int seed = 1; seed <= 30; seed++)
            {
                RoomShape shape = RoomShape.Build(seed, doors);

                foreach (DoorOpening door in shape.DoorOpenings)
                {
                    float half = RoomShape.DoorWidth * 0.5f;

                    foreach (IReadOnlyList<Vector2> wall in shape.Walls)
                    {
                        foreach (Vector2 point in wall)
                        {
                            bool insideGap = door.Side is Doors.North or Doors.South
                                ? Mathf.Abs(point.x - door.Center.x) < half - 0.001f
                                    && Mathf.Abs(point.y - door.Center.y) < 0.001f
                                : Mathf.Abs(point.y - door.Center.y) < half - 0.001f
                                    && Mathf.Abs(point.x - door.Center.x) < 0.001f;

                            Assert.That(
                                insideGap,
                                Is.False,
                                $"시드 {seed} {doors}: {door.Side} 문에 벽 점 {point}");
                        }
                    }
                }
            }
        }
    }

    [Test]
    public void WallIsFlatBesideEachDoor()
    {
        // 비스듬한 벽에 문이 걸리면 지나갈 때 걸린다
        for (int seed = 1; seed <= 30; seed++)
        {
            RoomShape shape = RoomShape.Build(seed, Doors.North | Doors.East);

            foreach (DoorOpening door in shape.DoorOpenings)
            {
                float flatSpan = RoomShape.DoorWidth * 0.5f + RoomShape.FlatMargin;

                foreach (IReadOnlyList<Vector2> wall in shape.Walls)
                {
                    foreach (Vector2 point in wall)
                    {
                        bool nearDoor = door.Side is Doors.North or Doors.South
                            ? Mathf.Abs(point.x - door.Center.x) < flatSpan
                            : Mathf.Abs(point.y - door.Center.y) < flatSpan;

                        if (!nearDoor)
                        {
                            continue;
                        }

                        // 문 옆 구간은 벽이 안쪽으로 들어오지 않아야 한다
                        float depth = door.Side is Doors.North or Doors.South
                            ? Mathf.Abs(Mathf.Abs(point.y) - Mathf.Abs(door.Center.y))
                            : Mathf.Abs(Mathf.Abs(point.x) - Mathf.Abs(door.Center.x));

                        Assert.That(
                            depth,
                            Is.LessThan(0.001f),
                            $"시드 {seed}: {door.Side} 문 옆 벽이 평평하지 않다 ({point})");
                    }
                }
            }
        }
    }

    [Test]
    public void FloorCoversTheWholeRoomIncludingDoorways()
    {
        // 바닥이 문 자리를 안 덮으면 지나갈 때 바닥이 끊긴다
        RoomShape shape = RoomShape.Build(2, Doors.North | Doors.West);
        Rect bounds = shape.Bounds;

        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;
        foreach (Vector2 point in shape.FloorOutline)
        {
            minX = Mathf.Min(minX, point.x);
            maxX = Mathf.Max(maxX, point.x);
            minY = Mathf.Min(minY, point.y);
            maxY = Mathf.Max(maxY, point.y);
        }

        Assert.That(minX, Is.EqualTo(bounds.xMin).Within(0.001f));
        Assert.That(maxX, Is.EqualTo(bounds.xMax).Within(0.001f));
        Assert.That(minY, Is.EqualTo(bounds.yMin).Within(0.001f));
        Assert.That(maxY, Is.EqualTo(bounds.yMax).Within(0.001f));
    }

    [Test]
    public void SameSeedGivesTheSameWalls()
    {
        RoomShape first = RoomShape.Build(42, Doors.North | Doors.South);
        RoomShape second = RoomShape.Build(42, Doors.North | Doors.South);

        Assert.That(first.Walls.Count, Is.EqualTo(second.Walls.Count));
        for (int w = 0; w < first.Walls.Count; w++)
        {
            Assert.That(first.Walls[w].Count, Is.EqualTo(second.Walls[w].Count));
            for (int p = 0; p < first.Walls[w].Count; p++)
            {
                Assert.That(first.Walls[w][p], Is.EqualTo(second.Walls[w][p]));
            }
        }
    }

    [Test]
    public void DifferentSeedsGiveDifferentWalls()
    {
        RoomShape a = RoomShape.Build(1, Doors.None);
        RoomShape b = RoomShape.Build(2, Doors.None);

        bool anyDifference = false;
        for (int w = 0; w < a.Walls.Count && !anyDifference; w++)
        {
            for (int p = 0; p < a.Walls[w].Count; p++)
            {
                if (a.Walls[w][p] != b.Walls[w][p])
                {
                    anyDifference = true;
                    break;
                }
            }
        }

        Assert.That(anyDifference, Is.True, "시드가 달라도 벽이 같으면 방이 다 똑같이 보인다");
    }

    [Test]
    public void ARoomWithADoorHasTwoWallSegmentsOnThatSide()
    {
        // 문이 있으면 그 변의 벽이 둘로 끊긴다. 없으면 하나로 이어진다.
        Assert.That(RoomShape.Build(1, Doors.None).Walls.Count, Is.EqualTo(4));
        Assert.That(RoomShape.Build(1, Doors.North).Walls.Count, Is.EqualTo(5));
        Assert.That(
            RoomShape.Build(1, Doors.North | Doors.South | Doors.East | Doors.West).Walls.Count,
            Is.EqualTo(8));
    }

    [Test]
    public void OppositeMapsEachSideToItsCounterpart()
    {
        Assert.That(RoomShape.Opposite(Doors.North), Is.EqualTo(Doors.South));
        Assert.That(RoomShape.Opposite(Doors.South), Is.EqualTo(Doors.North));
        Assert.That(RoomShape.Opposite(Doors.East), Is.EqualTo(Doors.West));
        Assert.That(RoomShape.Opposite(Doors.West), Is.EqualTo(Doors.East));
        Assert.That(RoomShape.Opposite(Doors.None), Is.EqualTo(Doors.None));
    }
}
