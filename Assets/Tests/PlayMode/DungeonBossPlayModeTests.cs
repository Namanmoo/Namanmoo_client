using System.Collections;
using NaManMoo.Dungeon;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// 보스방에 들어서면 보스가 나오고, 잡으면 문이 열린다.
///
/// Stage1은 방 안에 따로 진입 트리거를 두고 그것을 밟아야 보스가 나왔다. 던전에서는
/// 방 자체가 경계이므로 들어서는 즉시 나온다 — 문이 잠기는 것으로 시작이 드러난다.
/// </summary>
public sealed class DungeonBossPlayModeTests
{
    private DungeonBossRoomHarness harness;

    [SetUp]
    public void SetUp()
    {
        harness = new DungeonBossRoomHarness();
    }

    [TearDown]
    public void TearDown()
    {
        harness.TearDown();
    }

    [UnityTest]
    public IEnumerator TheBossAppearsInTheBossRoomAndLocksTheDoors()
    {
        (int seed, DungeonRoom boss) = DungeonBossRoomHarness.FindFloorWithBoss();
        Assert.That(boss, Is.Not.Null, "보스방이 있는 층을 찾지 못했다");

        harness.Runner.Configure(seed, 2, harness.Player.transform);
        harness.Runner.Begin();
        yield return null;

        yield return harness.WalkTo(boss.Cell);
        Assert.That(harness.Runner.CurrentCell, Is.EqualTo(boss.Cell), "보스방까지 가지 못했다");

        var controller = Object.FindFirstObjectByType<BossRobotController>();
        Assert.That(controller, Is.Not.Null, "보스가 나오지 않았다");
        Assert.That(harness.Runner.IsCleared(boss.Cell), Is.False, "보스방 문이 잠기지 않았다");

        foreach (DungeonDoor door in harness.Root.GetComponentsInChildren<DungeonDoor>())
        {
            Assert.That(door.IsOpen, Is.False, $"{door.Side} 문이 열려 있다");
        }
    }

    [UnityTest]
    public IEnumerator KillingTheBossOpensTheDoors()
    {
        (int seed, DungeonRoom boss) = DungeonBossRoomHarness.FindFloorWithBoss();
        harness.Runner.Configure(seed, 2, harness.Player.transform);
        harness.Runner.Begin();
        yield return null;

        yield return harness.WalkTo(boss.Cell);
        Assert.That(harness.Runner.CurrentCell, Is.EqualTo(boss.Cell));

        harness.KillEverything();
        yield return null;

        Assert.That(harness.Runner.IsCleared(boss.Cell), Is.True);
        foreach (DungeonDoor door in harness.Root.GetComponentsInChildren<DungeonDoor>())
        {
            Assert.That(door.IsOpen, Is.True, $"{door.Side} 문이 아직 잠겼다");
        }
    }

    [UnityTest]
    public IEnumerator TheBossStandsAwayFromEveryDoorway()
    {
        // 문 앞에 서 있으면 방에 들어서는 순간 몸으로 겹쳐 무방비로 맞는다
        (int seed, DungeonRoom boss) = DungeonBossRoomHarness.FindFloorWithBoss();
        harness.Runner.Configure(seed, 2, harness.Player.transform);
        harness.Runner.Begin();
        yield return null;

        yield return harness.WalkTo(boss.Cell);
        var controller = Object.FindFirstObjectByType<BossRobotController>();
        Assert.That(controller, Is.Not.Null);

        Vector2 bossPosition = controller.transform.position;
        foreach (DoorOpening door in harness.Runner.CurrentShape.DoorOpenings)
        {
            Vector2 landing = DungeonNavigation.EntryPoint(harness.Runner.CurrentShape, door.Side);
            Assert.That(
                Vector2.Distance(bossPosition, landing),
                Is.GreaterThan(3f),
                $"{door.Side} 착지 지점에 보스가 너무 가깝다");
        }
    }

    [UnityTest]
    public IEnumerator OrdinaryRoomsStillGetMushroomsNotABoss()
    {
        harness.Runner.Configure(7, 2, harness.Player.transform);
        harness.Runner.Begin();
        yield return null;

        Doors side = Doors.None;
        foreach (Doors candidate in new[] { Doors.North, Doors.South, Doors.East, Doors.West })
        {
            if (harness.Runner.Layout.RoomAt(harness.Runner.CurrentCell).Doors.HasFlag(candidate))
            {
                side = candidate;
                break;
            }
        }

        harness.Pass(side);
        yield return null;

        DungeonRoom here = harness.Runner.Layout.RoomAt(harness.Runner.CurrentCell);
        if (here.Kind != RoomKind.Normal)
        {
            Assert.Ignore("옆 방이 일반 방이 아니라 이 시드로는 확인할 수 없다");
        }

        Assert.That(Object.FindFirstObjectByType<BossRobotController>(), Is.Null,
            "일반 방에 보스가 나왔다");
        Assert.That(Object.FindFirstObjectByType<ChaseContactEnemyController>(), Is.Not.Null,
            "일반 방에 크랩이 나오지 않았다");
    }

    [UnityTest]
    public IEnumerator KillingTheBossFiresBossDefeatedEventExactlyOnce()
    {
        (int seed, DungeonRoom boss) = DungeonBossRoomHarness.FindFloorWithBoss();
        Assert.That(boss, Is.Not.Null, "보스방이 있는 층을 찾지 못했다");

        harness.Runner.Configure(seed, 2, harness.Player.transform);
        harness.Runner.Begin();
        yield return null;

        yield return harness.WalkTo(boss.Cell);
        Assert.That(harness.Runner.CurrentCell, Is.EqualTo(boss.Cell), "보스방까지 가지 못했다");

        int fireCount = 0;
        harness.Runner.BossDefeated += () => fireCount++;

        harness.KillEverything();
        yield return null;

        Assert.That(fireCount, Is.EqualTo(1));
    }

    [UnityTest]
    public IEnumerator ReenteringAClearedBossRoomDoesNotRefireBossDefeated()
    {
        (int seed, DungeonRoom boss) = DungeonBossRoomHarness.FindFloorWithBoss();
        Assert.That(boss, Is.Not.Null, "보스방이 있는 층을 찾지 못했다");

        harness.Runner.Configure(seed, 2, harness.Player.transform);
        harness.Runner.Begin();
        yield return null;

        yield return harness.WalkTo(boss.Cell);
        harness.KillEverything();
        yield return null;
        Assert.That(harness.Runner.IsCleared(boss.Cell), Is.True);

        int fireCount = 0;
        harness.Runner.BossDefeated += () => fireCount++;

        Doors exitSide = Doors.None;
        foreach (Doors side in new[] { Doors.North, Doors.South, Doors.East, Doors.West })
        {
            if (harness.Runner.Layout.RoomAt(boss.Cell).Doors.HasFlag(side))
            {
                exitSide = side;
                break;
            }
        }

        Assert.That(exitSide, Is.Not.EqualTo(Doors.None), "보스방에 나갈 문이 없다");

        harness.Pass(exitSide);
        yield return null;
        harness.Pass(RoomShape.Opposite(exitSide));
        yield return null;

        Assert.That(harness.Runner.CurrentCell, Is.EqualTo(boss.Cell));
        Assert.That(fireCount, Is.EqualTo(0));
    }
}
