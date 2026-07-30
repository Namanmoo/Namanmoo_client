using System.Collections.Generic;
using NaManMoo.Dungeon;
using NUnit.Framework;
using UnityEngine;

public sealed class RoomSpawnPointsTests
{
    private const Doors AllDoors = Doors.North | Doors.South | Doors.East | Doors.West;

    private static Doors[] AllCombinations()
    {
        var list = new List<Doors>();
        for (int mask = 0; mask < 16; mask++)
        {
            list.Add((Doors)mask);
        }

        return list.ToArray();
    }

    [Test]
    public void SpawnsStayInsideTheRoomAwayFromWalls()
    {
        foreach (Doors doors in AllCombinations())
        {
            RoomShape shape = RoomShape.Build(5, doors);
            Rect bounds = shape.Bounds;

            foreach (Vector2 point in RoomSpawnPoints.Inside(shape, 6, 11))
            {
                float toWall = Mathf.Min(
                    point.x - bounds.xMin, bounds.xMax - point.x,
                    point.y - bounds.yMin, bounds.yMax - point.y);

                Assert.That(
                    toWall,
                    Is.GreaterThanOrEqualTo(RoomSpawnPoints.WallInset - 0.001f),
                    $"{doors}: {point} 이 벽에 너무 붙었다");
            }
        }
    }

    [Test]
    public void NothingSpawnsInFrontOfADoor()
    {
        // 문 앞에 적이 서 있으면 방에 들어서는 순간 몸으로 겹쳐 바로 맞는다
        foreach (Doors doors in AllCombinations())
        {
            for (int seed = 1; seed <= 15; seed++)
            {
                RoomShape shape = RoomShape.Build(seed, doors);

                foreach (Vector2 point in RoomSpawnPoints.Inside(shape, 8, seed))
                {
                    foreach (DoorOpening door in shape.DoorOpenings)
                    {
                        Assert.That(
                            Vector2.Distance(point, door.Center),
                            Is.GreaterThanOrEqualTo(RoomSpawnPoints.DoorClearance - 0.001f),
                            $"시드 {seed} {doors}: {door.Side} 문 앞에 적이 있다");
                    }
                }
            }
        }
    }

    [Test]
    public void NothingSpawnsWhereThePlayerLands()
    {
        // 들어서는 자리와 겹치면 무방비로 맞는다. 실제 착지 지점으로 확인한다.
        foreach (Doors doors in AllCombinations())
        {
            for (int seed = 1; seed <= 15; seed++)
            {
                RoomShape shape = RoomShape.Build(seed, doors);
                List<Vector2> spawns = RoomSpawnPoints.Inside(shape, 8, seed);

                foreach (DoorOpening door in shape.DoorOpenings)
                {
                    Vector2 landing = DungeonNavigation.EntryPoint(shape, door.Side);

                    foreach (Vector2 point in spawns)
                    {
                        Assert.That(
                            Vector2.Distance(point, landing),
                            Is.GreaterThan(2f),
                            $"시드 {seed} {doors}: {door.Side} 착지 지점에 적이 겹친다");
                    }
                }

                // 시작 방에서는 중앙에 선다
                foreach (Vector2 point in spawns)
                {
                    Assert.That(
                        Vector2.Distance(point, DungeonNavigation.StartPoint(shape)),
                        Is.GreaterThanOrEqualTo(RoomSpawnPoints.CentreClearance - 0.001f));
                }
            }
        }
    }

    [Test]
    public void EnemiesDoNotOverlapEachOther()
    {
        RoomShape shape = RoomShape.Build(3, AllDoors);
        List<Vector2> spawns = RoomSpawnPoints.Inside(shape, 8, 42);

        for (int a = 0; a < spawns.Count; a++)
        {
            for (int b = a + 1; b < spawns.Count; b++)
            {
                Assert.That(
                    Vector2.Distance(spawns[a], spawns[b]),
                    Is.GreaterThanOrEqualTo(RoomSpawnPoints.MinSpacing - 0.001f));
            }
        }
    }

    [Test]
    public void SameSeedGivesTheSamePlacement()
    {
        // 클리어하지 않은 방을 나갔다 다시 들어올 수 있다. 배치가 바뀌면 다른 방처럼 보인다.
        RoomShape shape = RoomShape.Build(8, AllDoors);

        Assert.That(
            RoomSpawnPoints.Inside(shape, 5, 77),
            Is.EqualTo(RoomSpawnPoints.Inside(shape, 5, 77)));
    }

    [Test]
    public void DifferentSeedsGiveDifferentPlacement()
    {
        RoomShape shape = RoomShape.Build(8, AllDoors);

        Assert.That(
            RoomSpawnPoints.Inside(shape, 5, 1),
            Is.Not.EqualTo(RoomSpawnPoints.Inside(shape, 5, 2)));
    }

    [Test]
    public void ARoomThatIsAskedForFourEnemiesUsuallyGetsThem()
    {
        // 자리를 못 찾아 빈 방이 되면 문이 열려 던전이 아니게 된다
        int shortfall = 0;
        for (int seed = 1; seed <= 30; seed++)
        {
            RoomShape shape = RoomShape.Build(seed, AllDoors);
            if (RoomSpawnPoints.Inside(shape, 4, seed).Count < 4)
            {
                shortfall++;
            }
        }

        Assert.That(shortfall, Is.Zero, "문 4개인 방에서도 적 4마리 자리는 나와야 한다");
    }

    [Test]
    public void ZeroOrNegativeCountsGiveNothing()
    {
        RoomShape shape = RoomShape.Build(1, AllDoors);

        Assert.That(RoomSpawnPoints.Inside(shape, 0, 1), Is.Empty);
        Assert.That(RoomSpawnPoints.Inside(shape, -3, 1), Is.Empty);
    }

    [Test]
    public void SafeRoomsHaveNoEnemies()
    {
        // 상점과 보물방에서 싸우게 하면 아이작이 아니다
        Assert.That(RoomSpawnPoints.EnemyCount(RoomKind.Start, 0), Is.Zero);
        Assert.That(RoomSpawnPoints.EnemyCount(RoomKind.Treasure, 5), Is.Zero);
        Assert.That(RoomSpawnPoints.EnemyCount(RoomKind.Shop, 5), Is.Zero);
    }

    [Test]
    public void NormalRoomsAlwaysHaveSomethingToFight()
    {
        // 하나라도 없으면 문이 즉시 열려 클리어 개념이 사라진다
        for (int difficulty = 0; difficulty < 20; difficulty++)
        {
            Assert.That(
                RoomSpawnPoints.EnemyCount(RoomKind.Normal, difficulty),
                Is.GreaterThan(0),
                $"난이도 {difficulty}");
        }
    }

    [Test]
    public void DeeperRoomsAreNotEasierAndStayWithinTheCap()
    {
        int previous = 0;
        for (int difficulty = 0; difficulty < 20; difficulty++)
        {
            int count = RoomSpawnPoints.EnemyCount(RoomKind.Normal, difficulty);

            Assert.That(count, Is.GreaterThanOrEqualTo(previous), $"난이도 {difficulty}");
            Assert.That(count, Is.LessThanOrEqualTo(6), "방 크기에 비해 너무 많다");
            previous = count;
        }
    }

    [Test]
    public void BossRoomsAreHarderThanNormalOnes()
    {
        for (int difficulty = 0; difficulty < 12; difficulty++)
        {
            Assert.That(
                RoomSpawnPoints.EnemyCount(RoomKind.Boss, difficulty),
                Is.GreaterThan(RoomSpawnPoints.EnemyCount(RoomKind.Normal, difficulty)),
                $"난이도 {difficulty}");
        }
    }

    [Test]
    public void EveryRoomOfAGeneratedFloorCanBeFilled()
    {
        // 배치·기하·적 배치가 서로 맞는지 한 번에 본다
        for (int seed = 1; seed <= 10; seed++)
        {
            DungeonLayout layout = DungeonLayout.Generate(seed, floor: 3);

            foreach (DungeonRoom room in layout.Rooms)
            {
                int wanted = RoomSpawnPoints.EnemyCount(room.Kind, room.DistanceFromStart);
                if (wanted == 0)
                {
                    continue;
                }

                int roomSeed = DungeonNavigation.RoomSeed(seed, 3, room.Cell);
                RoomShape shape = RoomShape.Build(roomSeed, room.Doors);

                Assert.That(
                    RoomSpawnPoints.Inside(shape, wanted, roomSeed).Count,
                    Is.EqualTo(wanted),
                    $"시드 {seed} {room.Cell} ({room.Kind}, 문 {room.Doors})");
            }
        }
    }
}
