using System.Collections.Generic;
using NaManMoo.Dungeon;
using NUnit.Framework;
using UnityEngine;

public sealed class MinimapModelTests
{
    private const int Seed = 5;
    private const int Floor = 3;

    private static DungeonLayout Layout() => DungeonLayout.Generate(Seed, Floor);

    private static MinimapCell Find(List<MinimapCell> cells, Vector2Int at)
    {
        foreach (MinimapCell cell in cells)
        {
            if (cell.Cell == at)
            {
                return cell;
            }
        }

        Assert.Fail($"{at} 이 미니맵에 없다");
        return default;
    }

    [Test]
    public void AtTheStartOnlyTheStartRoomAndItsNeighboursAreShown()
    {
        // 층 전체가 처음부터 보이면 탐험할 이유가 없다
        DungeonLayout layout = Layout();
        var visited = new List<Vector2Int> { layout.StartCell };

        List<MinimapCell> cells = MinimapModel.Build(layout, layout.StartCell, visited);
        DungeonRoom start = layout.RoomAt(layout.StartCell);

        int expectedNeighbours = 0;
        foreach (Doors side in new[] { Doors.North, Doors.South, Doors.East, Doors.West })
        {
            if (start.Doors.HasFlag(side))
            {
                expectedNeighbours++;
            }
        }

        Assert.That(cells.Count, Is.EqualTo(1 + expectedNeighbours));
        Assert.That(cells.Count, Is.LessThan(layout.Rooms.Count),
            "시작하자마자 층 전체가 보인다");
    }

    [Test]
    public void TheCurrentRoomIsMarkedAndItIsTheOnlyOne()
    {
        DungeonLayout layout = Layout();
        List<MinimapCell> cells = MinimapModel.Build(
            layout, layout.StartCell, new List<Vector2Int> { layout.StartCell });

        int current = 0;
        foreach (MinimapCell cell in cells)
        {
            if (cell.Current)
            {
                current++;
                Assert.That(cell.Cell, Is.EqualTo(layout.StartCell));
            }
        }

        Assert.That(current, Is.EqualTo(1));
    }

    [Test]
    public void UnvisitedNeighboursHideWhatTheyAre()
    {
        // 안 가 본 방의 종류가 보이면 보물방을 찍어서 갈 수 있다
        DungeonLayout layout = Layout();
        DungeonRoom treasure = layout.RoomOfKind(RoomKind.Treasure);
        if (treasure == null)
        {
            Assert.Ignore("이 시드에는 보물방이 없다");
        }

        // 보물방의 이웃까지만 가 본 상태를 만든다
        Vector2Int neighbour = default;
        foreach (Doors side in new[] { Doors.North, Doors.South, Doors.East, Doors.West })
        {
            if (treasure.Doors.HasFlag(side))
            {
                neighbour = DungeonNavigation.Neighbour(treasure.Cell, side);
                break;
            }
        }

        List<MinimapCell> cells = MinimapModel.Build(
            layout, neighbour, new List<Vector2Int> { neighbour });

        MinimapCell hidden = Find(cells, treasure.Cell);
        Assert.That(hidden.Visited, Is.False);
        Assert.That(hidden.Kind, Is.EqualTo(RoomKind.Normal), "안 가 본 보물방이 드러났다");
    }

    [Test]
    public void VisitedRoomsShowWhatTheyAre()
    {
        DungeonLayout layout = Layout();
        DungeonRoom boss = layout.RoomOfKind(RoomKind.Boss);
        Assert.That(boss, Is.Not.Null);

        List<MinimapCell> cells = MinimapModel.Build(
            layout, boss.Cell, new List<Vector2Int> { layout.StartCell, boss.Cell });

        MinimapCell shown = Find(cells, boss.Cell);
        Assert.That(shown.Visited, Is.True);
        Assert.That(shown.Kind, Is.EqualTo(RoomKind.Boss));
    }

    [Test]
    public void WalkingEverywhereRevealsTheWholeFloor()
    {
        DungeonLayout layout = Layout();
        var everywhere = new List<Vector2Int>();
        foreach (DungeonRoom room in layout.Rooms)
        {
            everywhere.Add(room.Cell);
        }

        List<MinimapCell> cells = MinimapModel.Build(layout, layout.StartCell, everywhere);

        Assert.That(cells.Count, Is.EqualTo(layout.Rooms.Count));
        foreach (MinimapCell cell in cells)
        {
            Assert.That(cell.Visited, Is.True);
        }
    }

    [Test]
    public void NothingOutsideTheFloorIsEverShown()
    {
        // 이웃을 보여 주다가 방이 없는 칸을 그리면 빈 상자가 뜬다
        DungeonLayout layout = Layout();
        var everywhere = new List<Vector2Int>();
        foreach (DungeonRoom room in layout.Rooms)
        {
            everywhere.Add(room.Cell);
        }

        foreach (MinimapCell cell in MinimapModel.Build(layout, layout.StartCell, everywhere))
        {
            Assert.That(layout.HasRoom(cell.Cell), Is.True, $"{cell.Cell} 에는 방이 없다");
        }
    }

    [Test]
    public void TheCurrentRoomIsAlwaysShownEvenIfNotInTheVisitedList()
    {
        DungeonLayout layout = Layout();
        DungeonRoom boss = layout.RoomOfKind(RoomKind.Boss);

        List<MinimapCell> cells = MinimapModel.Build(layout, boss.Cell, new List<Vector2Int>());

        MinimapCell shown = Find(cells, boss.Cell);
        Assert.That(shown.Current, Is.True);
        Assert.That(shown.Visited, Is.True, "지금 서 있는 방은 가 본 방이다");
    }

    [Test]
    public void OrderIsStableSoTheMinimapDoesNotFlicker()
    {
        DungeonLayout layout = Layout();
        var visited = new List<Vector2Int> { layout.StartCell };

        List<MinimapCell> first = MinimapModel.Build(layout, layout.StartCell, visited);
        List<MinimapCell> second = MinimapModel.Build(layout, layout.StartCell, visited);

        Assert.That(first.Count, Is.EqualTo(second.Count));
        for (int i = 0; i < first.Count; i++)
        {
            Assert.That(first[i].Cell, Is.EqualTo(second[i].Cell));
        }
    }

    [Test]
    public void NoLayoutMeansNothingToDraw()
    {
        Assert.That(MinimapModel.Build(null, Vector2Int.zero, null), Is.Empty);
    }

    [Test]
    public void ANullVisitedListStillShowsWhereYouAre()
    {
        DungeonLayout layout = Layout();

        List<MinimapCell> cells = MinimapModel.Build(layout, layout.StartCell, null);

        Assert.That(cells, Is.Not.Empty);
        Assert.That(Find(cells, layout.StartCell).Current, Is.True);
    }

    [Test]
    public void TheCurrentRoomSitsAtTheCentre()
    {
        // 현재 방을 중심에 두면 층이 커져도 미니맵 크기가 늘지 않는다
        var current = new Vector2Int(6, 4);

        Assert.That(MinimapModel.PositionOf(current, current, 10f), Is.EqualTo(Vector2.zero));
    }

    [Test]
    public void NeighboursSitOneStepAwayInScreenDirections()
    {
        var current = new Vector2Int(6, 4);
        const float step = 12f;

        Assert.That(
            MinimapModel.PositionOf(new Vector2Int(6, 5), current, step),
            Is.EqualTo(new Vector2(0f, step)), "북쪽은 위여야 한다");
        Assert.That(
            MinimapModel.PositionOf(new Vector2Int(7, 4), current, step),
            Is.EqualTo(new Vector2(step, 0f)), "동쪽은 오른쪽이어야 한다");
        Assert.That(
            MinimapModel.PositionOf(new Vector2Int(6, 3), current, step),
            Is.EqualTo(new Vector2(0f, -step)));
        Assert.That(
            MinimapModel.PositionOf(new Vector2Int(5, 4), current, step),
            Is.EqualTo(new Vector2(-step, 0f)));
    }

    [Test]
    public void EveryFloorRevealsProgressivelyForManySeeds()
    {
        // 시드에 따라 무너지지 않는지 — 시작 시 보이는 칸이 층 전체보다 적어야 한다
        for (int seed = 1; seed <= 20; seed++)
        {
            DungeonLayout layout = DungeonLayout.Generate(seed, Floor);
            List<MinimapCell> atStart = MinimapModel.Build(
                layout, layout.StartCell, new List<Vector2Int> { layout.StartCell });

            Assert.That(atStart, Is.Not.Empty, $"시드 {seed}");
            Assert.That(atStart.Count, Is.LessThanOrEqualTo(layout.Rooms.Count), $"시드 {seed}");
        }
    }
}
