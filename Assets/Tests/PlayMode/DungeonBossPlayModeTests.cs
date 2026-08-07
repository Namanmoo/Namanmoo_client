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
    private GameObject root;
    private GameObject player;
    private GameObject cameraObject;
    private DungeonRunner runner;
    private DungeonEncounter encounter;
    private Sprite sprite;
    private Sprite projectileSprite;
    private EnemyDefinition contactDefinition;
    private EnemyDefinition rangedDefinition;

    /// <summary>보스방을 가진 층과 그 방의 좌표를 찾는다.</summary>
    private static (int seed, DungeonRoom boss) FindFloorWithBoss()
    {
        for (int seed = 1; seed <= 40; seed++)
        {
            DungeonLayout layout = DungeonLayout.Generate(seed, 2);
            DungeonRoom boss = layout.RoomOfKind(RoomKind.Boss);
            if (boss != null)
            {
                return (seed, boss);
            }
        }

        return (0, null);
    }

    [SetUp]
    public void SetUp()
    {
        cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 10f;

        player = new GameObject("Player") { tag = "Player" };
        player.AddComponent<Rigidbody2D>().gravityScale = 0f;
        player.AddComponent<CircleCollider2D>().radius = 0.5f;
        player.AddComponent<PlayerHealth>();

        root = new GameObject("Dungeon");
        runner = root.AddComponent<DungeonRunner>();
        encounter = root.AddComponent<DungeonEncounter>();

        sprite = Sprite.Create(
            Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        projectileSprite = Sprite.Create(
            Texture2D.blackTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        contactDefinition =
            ScriptableObject.CreateInstance<EnemyDefinition>();
        contactDefinition.Configure(
            "test-mushroom",
            "Test Mushroom",
            sprite,
            null,
            EnemyBehaviorType.ChaseContact,
            5,
            2.5f,
            2,
            0.75f,
            1f,
            0f,
            0.01f,
            0.01f);
        rangedDefinition = ScriptableObject.CreateInstance<EnemyDefinition>();
        rangedDefinition.Configure(
            "test-squirrel",
            "Test Squirrel",
            sprite,
            projectileSprite,
            EnemyBehaviorType.ApproachAndShoot,
            5,
            2.5f,
            2,
            4f,
            1f,
            7f,
            1f,
            0.25f);
        encounter.Configure(new[] { contactDefinition, rangedDefinition }, sprite);
        runner.SetEncounter(encounter);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(root);
        Object.DestroyImmediate(player);
        Object.DestroyImmediate(cameraObject);
        Object.DestroyImmediate(contactDefinition);
        Object.DestroyImmediate(rangedDefinition);
        Object.DestroyImmediate(sprite);
        Object.DestroyImmediate(projectileSprite);
    }

    /// <summary>보스방까지 문을 따라 걸어간다. 도중의 방은 전부 클리어 처리한다.</summary>
    private IEnumerator WalkTo(Vector2Int target)
    {
        for (int step = 0; step < 40 && runner.CurrentCell != target; step++)
        {
            KillEverything();
            yield return null;

            Doors chosen = Doors.None;
            int best = int.MaxValue;
            foreach (Doors side in new[] { Doors.North, Doors.South, Doors.East, Doors.West })
            {
                Vector2Int next = DungeonNavigation.Neighbour(runner.CurrentCell, side);
                if (!runner.Layout.HasRoom(next))
                {
                    continue;
                }

                int distance = Mathf.Abs(next.x - target.x) + Mathf.Abs(next.y - target.y);
                if (distance < best)
                {
                    best = distance;
                    chosen = side;
                }
            }

            if (chosen == Doors.None)
            {
                break;
            }

            Pass(chosen);
            yield return null;
        }
    }

    private void KillEverything()
    {
        foreach (EnemyHealth enemy in Object.FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None))
        {
            if (enemy != null)
            {
                enemy.TakeDamage(enemy.MaxHealth);
            }
        }
    }

    private void Pass(Doors side)
    {
        foreach (DungeonDoor door in root.GetComponentsInChildren<DungeonDoor>())
        {
            if (door.Side == side)
            {
                door.gameObject.SendMessage(
                    "OnTriggerEnter2D",
                    player.GetComponent<CircleCollider2D>(),
                    SendMessageOptions.DontRequireReceiver);
                return;
            }
        }
    }

    [UnityTest]
    public IEnumerator TheBossAppearsInTheBossRoomAndLocksTheDoors()
    {
        (int seed, DungeonRoom boss) = FindFloorWithBoss();
        Assert.That(boss, Is.Not.Null, "보스방이 있는 층을 찾지 못했다");

        runner.Configure(seed, 2, player.transform);
        runner.Begin();
        yield return null;

        yield return WalkTo(boss.Cell);
        Assert.That(runner.CurrentCell, Is.EqualTo(boss.Cell), "보스방까지 가지 못했다");

        var controller = Object.FindFirstObjectByType<BossRobotController>();
        Assert.That(controller, Is.Not.Null, "보스가 나오지 않았다");
        Assert.That(runner.IsCleared(boss.Cell), Is.False, "보스방 문이 잠기지 않았다");

        foreach (DungeonDoor door in root.GetComponentsInChildren<DungeonDoor>())
        {
            Assert.That(door.IsOpen, Is.False, $"{door.Side} 문이 열려 있다");
        }
    }

    [UnityTest]
    public IEnumerator KillingTheBossOpensTheDoors()
    {
        (int seed, DungeonRoom boss) = FindFloorWithBoss();
        runner.Configure(seed, 2, player.transform);
        runner.Begin();
        yield return null;

        yield return WalkTo(boss.Cell);
        Assert.That(runner.CurrentCell, Is.EqualTo(boss.Cell));

        KillEverything();
        yield return null;

        Assert.That(runner.IsCleared(boss.Cell), Is.True);
        foreach (DungeonDoor door in root.GetComponentsInChildren<DungeonDoor>())
        {
            Assert.That(door.IsOpen, Is.True, $"{door.Side} 문이 아직 잠겼다");
        }
    }

    [UnityTest]
    public IEnumerator TheBossStandsAwayFromEveryDoorway()
    {
        // 문 앞에 서 있으면 방에 들어서는 순간 몸으로 겹쳐 무방비로 맞는다
        (int seed, DungeonRoom boss) = FindFloorWithBoss();
        runner.Configure(seed, 2, player.transform);
        runner.Begin();
        yield return null;

        yield return WalkTo(boss.Cell);
        var controller = Object.FindFirstObjectByType<BossRobotController>();
        Assert.That(controller, Is.Not.Null);

        Vector2 bossPosition = controller.transform.position;
        foreach (DoorOpening door in runner.CurrentShape.DoorOpenings)
        {
            Vector2 landing = DungeonNavigation.EntryPoint(runner.CurrentShape, door.Side);
            Assert.That(
                Vector2.Distance(bossPosition, landing),
                Is.GreaterThan(3f),
                $"{door.Side} 착지 지점에 보스가 너무 가깝다");
        }
    }

    [UnityTest]
    public IEnumerator OrdinaryRoomsStillGetMushroomsNotABoss()
    {
        runner.Configure(7, 2, player.transform);
        runner.Begin();
        yield return null;

        Doors side = Doors.None;
        foreach (Doors candidate in new[] { Doors.North, Doors.South, Doors.East, Doors.West })
        {
            if (runner.Layout.RoomAt(runner.CurrentCell).Doors.HasFlag(candidate))
            {
                side = candidate;
                break;
            }
        }

        Pass(side);
        yield return null;

        DungeonRoom here = runner.Layout.RoomAt(runner.CurrentCell);
        if (here.Kind != RoomKind.Normal)
        {
            Assert.Ignore("옆 방이 일반 방이 아니라 이 시드로는 확인할 수 없다");
        }

        Assert.That(Object.FindFirstObjectByType<BossRobotController>(), Is.Null,
            "일반 방에 보스가 나왔다");
        Assert.That(Object.FindFirstObjectByType<ChaseContactEnemyController>(), Is.Not.Null,
            "일반 방에 크랩이 나오지 않았다");
    }
}
