using System.Collections;
using System.Collections.Generic;
using NaManMoo.Dungeon;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// 방 전환이 실제로 도는지. 기하는 EditMode에서 다 덮었으니 여기서는 유니티가
/// 개입하는 부분만 본다 — 오브젝트가 하나만 남는지, 플레이어와 카메라가 따라오는지.
/// </summary>
public sealed class DungeonRunnerPlayModeTests
{
    private const int Seed = 7;
    private const int Floor = 2;

    private GameObject root;
    private GameObject playerObject;
    private GameObject cameraObject;
    private DungeonRunner runner;

    [SetUp]
    public void SetUp()
    {
        cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 10f;

        playerObject = new GameObject("Player") { tag = "Player" };
        Rigidbody2D body = playerObject.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        playerObject.AddComponent<CircleCollider2D>().radius = 0.5f;

        root = new GameObject("Dungeon");
        runner = root.AddComponent<DungeonRunner>();
        runner.Configure(Seed, Floor, playerObject.transform);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(root);
        Object.DestroyImmediate(playerObject);
        Object.DestroyImmediate(cameraObject);
        DestroyProjectiles<SwordProjectile>();
        DestroyProjectiles<WeaponProjectile>();
        DestroyProjectiles<EnemyProjectile>();
    }

    [UnityTest]
    public IEnumerator StartsInTheStartRoomWithThePlayerAtItsCentre()
    {
        yield return null;

        Assert.That(runner.CurrentCell, Is.EqualTo(runner.Layout.StartCell));
        Assert.That(
            (Vector2)playerObject.transform.position,
            Is.EqualTo(runner.CurrentShape.Bounds.center));
    }

    [UnityTest]
    public IEnumerator OnlyOneRoomExistsAtATime()
    {
        // 방을 지우지 않고 쌓으면 프레임이 갈수록 무거워지고 콜라이더가 겹친다
        yield return null;

        for (int i = 0; i < 4; i++)
        {
            Assert.That(CountRoomRoots(), Is.EqualTo(1), $"{i}번째 이동 후");
            if (!MoveThroughAnyDoor())
            {
                break;
            }

            yield return null;
        }

        Assert.That(CountRoomRoots(), Is.EqualTo(1));
    }

    [UnityTest]
    public IEnumerator WalkingThroughADoorMovesToTheNeighbouringCell()
    {
        yield return null;

        Vector2Int before = runner.CurrentCell;
        Doors side = FirstDoor();
        Assert.That(side, Is.Not.EqualTo(Doors.None), "시작 방에 문이 있어야 한다");

        runner.CurrentShape.TryGetDoor(side, out DoorOpening door);
        TriggerDoor(side);
        yield return null;

        Assert.That(
            runner.CurrentCell,
            Is.EqualTo(DungeonNavigation.Neighbour(before, side)));
    }

    [UnityTest]
    public IEnumerator ChangingRoomsDestroysAllActiveAndInactiveProjectiles()
    {
        yield return null;

        CreateProjectile<SwordProjectile>("Active Sword Projectile", true);
        CreateProjectile<SwordProjectile>("Inactive Sword Projectile", false);
        CreateProjectile<WeaponProjectile>("Active Weapon Projectile", true);
        CreateProjectile<WeaponProjectile>("Inactive Weapon Projectile", false);
        CreateProjectile<EnemyProjectile>("Active Enemy Projectile", true);
        CreateProjectile<EnemyProjectile>("Inactive Enemy Projectile", false);

        TriggerDoor(FirstDoor());
        yield return null;

        Assert.That(FindProjectiles<SwordProjectile>(), Is.Empty);
        Assert.That(FindProjectiles<WeaponProjectile>(), Is.Empty);
        Assert.That(FindProjectiles<EnemyProjectile>(), Is.Empty);
    }

    [UnityTest]
    public IEnumerator ArrivingPlacesThePlayerInsideTheDoorItCameThrough()
    {
        yield return null;

        Doors side = FirstDoor();
        TriggerDoor(side);
        yield return null;

        Doors entry = RoomShape.Opposite(side);
        Assert.That(
            (Vector2)playerObject.transform.position,
            Is.EqualTo(DungeonNavigation.EntryPoint(runner.CurrentShape, entry)));
    }

    [UnityTest]
    public IEnumerator ArrivingDoesNotImmediatelyBounceBack()
    {
        // 들어선 자리가 문 판정 안이면 방 사이를 무한히 튕긴다. 몇 프레임 두고 본다.
        yield return null;

        Vector2Int start = runner.CurrentCell;
        Doors side = FirstDoor();
        TriggerDoor(side);
        yield return null;

        Vector2Int arrived = runner.CurrentCell;
        Assert.That(arrived, Is.Not.EqualTo(start));

        for (int i = 0; i < 6; i++)
        {
            yield return new WaitForFixedUpdate();
        }

        Assert.That(runner.CurrentCell, Is.EqualTo(arrived), "가만히 있는데 방이 바뀌었다");
    }

    [UnityTest]
    public IEnumerator CameraBoundsFollowTheCurrentRoom()
    {
        yield return null;

        CameraFollow follow = cameraObject.GetComponent<CameraFollow>();
        Assert.That(follow, Is.Not.Null, "런너가 카메라 추적을 붙여야 한다");
        Assert.That(follow.Target, Is.EqualTo(playerObject.transform));
        Assert.That(follow.Bounds, Is.EqualTo(runner.CurrentShape.Bounds));

        TriggerDoor(FirstDoor());
        yield return null;

        Assert.That(follow.Bounds, Is.EqualTo(runner.CurrentShape.Bounds));
    }

    [UnityTest]
    public IEnumerator DoorsAreOpenWhenThereIsNothingToFight()
    {
        // 인카운터를 붙이지 않았으므로 방은 들어서는 즉시 클리어다
        yield return null;

        Assert.That(runner.IsCleared(runner.CurrentCell), Is.True);
        foreach (DungeonDoor door in FindDoors())
        {
            Assert.That(door.IsOpen, Is.True, $"{door.Side} 문이 잠겨 있다");
        }
    }

    [UnityTest]
    public IEnumerator EveryDoorLeadsToARoomThatExists()
    {
        yield return null;

        foreach (DungeonDoor door in FindDoors())
        {
            Vector2Int target = DungeonNavigation.Neighbour(runner.CurrentCell, door.Side);
            Assert.That(
                runner.Layout.HasRoom(target),
                Is.True,
                $"{door.Side} 문 뒤에 방이 없다");
        }
    }

    [UnityTest]
    public IEnumerator ReturningToAClearedRoomKeepsItCleared()
    {
        yield return null;

        Vector2Int start = runner.CurrentCell;
        Doors side = FirstDoor();
        TriggerDoor(side);
        yield return null;

        TriggerDoor(RoomShape.Opposite(side));
        yield return null;

        Assert.That(runner.CurrentCell, Is.EqualTo(start));
        Assert.That(runner.IsCleared(start), Is.True);
    }

    [UnityTest]
    public IEnumerator RoomChangedFiresOnceForEachMove()
    {
        // 미니맵이 이 이벤트를 듣는다. 두 번 울리면 두 칸 움직인 것으로 그려진다.
        yield return null;

        var seen = new List<Vector2Int>();
        runner.RoomChanged += seen.Add;

        TriggerDoor(FirstDoor());
        yield return null;

        Assert.That(seen.Count, Is.EqualTo(1));
        Assert.That(seen[0], Is.EqualTo(runner.CurrentCell));
    }

    [UnityTest]
    public IEnumerator CurrentStateIsReadableWithoutHavingHeardTheEvent()
    {
        // Start()가 먼저 돌기 때문에 나중에 붙는 쪽(미니맵)은 첫 RoomChanged를 못 듣는다.
        // 그래서 지금 상태를 언제든 물어볼 수 있어야 한다 — 이게 깨지면 미니맵이
        // 첫 방을 비워 놓고 시작한다.
        yield return null;

        Assert.That(runner.Layout, Is.Not.Null);
        Assert.That(runner.CurrentShape, Is.Not.Null);
        Assert.That(runner.Layout.HasRoom(runner.CurrentCell), Is.True);
        Assert.That(runner.ClearedRooms, Does.Contain(runner.CurrentCell));
    }

    [UnityTest]
    public IEnumerator DoorsStayLockedUntilTheRoomIsCleared()
    {
        // 이게 안 되면 방을 건너뛰며 보스방까지 걸어갈 수 있다
        var encounter = new TestEncounter(enemyCount: 2);
        runner.SetEncounter(encounter);
        runner.Begin();
        yield return null;

        Assert.That(runner.IsCleared(runner.CurrentCell), Is.False);
        Assert.That(encounter.Spawned, Has.Count.EqualTo(2));
        foreach (DungeonDoor door in FindDoors())
        {
            Assert.That(door.IsOpen, Is.False, $"{door.Side} 문이 열려 있다");
        }

        // 문을 지나려 해도 방이 바뀌지 않아야 한다
        Vector2Int before = runner.CurrentCell;
        TriggerDoor(FirstDoor());
        yield return null;
        Assert.That(runner.CurrentCell, Is.EqualTo(before), "잠긴 문으로 넘어갔다");

        encounter.KillAll();
        yield return null;

        Assert.That(runner.IsCleared(before), Is.True);
        foreach (DungeonDoor door in FindDoors())
        {
            Assert.That(door.IsOpen, Is.True, $"{door.Side} 문이 아직 잠겼다");
        }

        TriggerDoor(FirstDoor());
        yield return null;
        Assert.That(runner.CurrentCell, Is.Not.EqualTo(before));
    }

    [UnityTest]
    public IEnumerator AClearedRoomDoesNotSpawnEnemiesAgain()
    {
        // 지나온 방마다 다시 싸우게 하면 되돌아갈 수 없다
        var encounter = new TestEncounter(enemyCount: 1);
        runner.SetEncounter(encounter);
        runner.Begin();
        yield return null;

        Vector2Int start = runner.CurrentCell;
        encounter.KillAll();
        yield return null;

        Doors side = FirstDoor();
        TriggerDoor(side);
        yield return null;

        encounter.KillAll();
        yield return null;

        int spawnsBefore = encounter.SpawnCalls;
        TriggerDoor(RoomShape.Opposite(side));
        yield return null;

        Assert.That(runner.CurrentCell, Is.EqualTo(start));
        Assert.That(encounter.SpawnCalls, Is.EqualTo(spawnsBefore), "클리어한 방에 적이 다시 나왔다");
        foreach (DungeonDoor door in FindDoors())
        {
            Assert.That(door.IsOpen, Is.True);
        }
    }

    [UnityTest]
    public IEnumerator SafeRoomsAreOpenImmediately()
    {
        // 적을 내놓지 않는 방(상점·보물)은 잠기면 안 된다 — 영원히 갇힌다
        var encounter = new TestEncounter(enemyCount: 0);
        runner.SetEncounter(encounter);
        runner.Begin();
        yield return null;

        Assert.That(runner.IsCleared(runner.CurrentCell), Is.True);
        foreach (DungeonDoor door in FindDoors())
        {
            Assert.That(door.IsOpen, Is.True, $"{door.Side}");
        }
    }

    [UnityTest]
    public IEnumerator ClearingANonBossRoomDoesNotFireBossDefeated()
    {
        var encounter = new TestEncounter(enemyCount: 1);
        runner.SetEncounter(encounter);
        runner.Begin();
        yield return null;

        int fireCount = 0;
        runner.BossDefeated += () => fireCount++;

        encounter.KillAll();
        yield return null;

        Assert.That(runner.IsCleared(runner.CurrentCell), Is.True);
        Assert.That(fireCount, Is.EqualTo(0));
    }

    /// <summary>
    /// 적을 원하는 만큼만 내놓고 마음대로 죽일 수 있는 인카운터. 크랩 스프라이트나
    /// 실제 AI 없이 잠금 규칙만 확인한다.
    /// </summary>
    private sealed class TestEncounter : IRoomEncounter
    {
        private readonly int enemyCount;

        public TestEncounter(int enemyCount)
        {
            this.enemyCount = enemyCount;
        }

        public List<EnemyHealth> Spawned { get; } = new List<EnemyHealth>();

        public int SpawnCalls { get; private set; }

        public Stage1EncounterGate Spawn(
            Transform roomRoot, Transform player, RoomShape shape, DungeonRoom room, int roomSeed)
        {
            SpawnCalls++;
            Spawned.Clear();

            if (enemyCount <= 0)
            {
                return null;
            }

            List<Vector2> spots = RoomSpawnPoints.Inside(shape, enemyCount, roomSeed);
            foreach (Vector2 spot in spots)
            {
                var enemy = new GameObject("Test Enemy");
                enemy.transform.SetParent(roomRoot, false);
                enemy.transform.position = spot;
                EnemyHealth health = enemy.AddComponent<EnemyHealth>();
                health.Configure(1);
                Spawned.Add(health);
            }

            var gateObject = new GameObject("Test Gate");
            gateObject.transform.SetParent(roomRoot, false);
            Stage1EncounterGate gate = gateObject.AddComponent<Stage1EncounterGate>();
            gate.Initialize(Spawned, null, new Renderer[0]);
            return gate;
        }

        public void KillAll()
        {
            foreach (EnemyHealth enemy in Spawned)
            {
                if (enemy != null)
                {
                    enemy.TakeDamage(enemy.MaxHealth);
                }
            }
        }
    }

    private int CountRoomRoots()
    {
        int count = 0;
        foreach (Transform child in root.transform)
        {
            if (child.name == "Current Room")
            {
                count++;
            }
        }

        return count;
    }

    private List<DungeonDoor> FindDoors()
    {
        return new List<DungeonDoor>(root.GetComponentsInChildren<DungeonDoor>());
    }

    private Doors FirstDoor()
    {
        List<DungeonDoor> found = FindDoors();
        return found.Count > 0 ? found[0].Side : Doors.None;
    }

    /// <summary>
    /// 물리로 걸어가는 대신 문 판정을 직접 울린다. 걷는 것은 PlayerMovement의 일이고
    /// 여기서 보려는 것은 "문이 울리면 방이 바뀌는가"다.
    /// </summary>
    private void TriggerDoor(Doors side)
    {
        foreach (DungeonDoor door in FindDoors())
        {
            if (door.Side != side)
            {
                continue;
            }

            door.gameObject.SendMessage(
                "OnTriggerEnter2D",
                playerObject.GetComponent<CircleCollider2D>(),
                SendMessageOptions.DontRequireReceiver);
            return;
        }
    }

    private bool MoveThroughAnyDoor()
    {
        Doors side = FirstDoor();
        if (side == Doors.None)
        {
            return false;
        }

        TriggerDoor(side);
        return true;
    }

    private static void CreateProjectile<T>(string name, bool active)
        where T : Component
    {
        GameObject projectile = new GameObject(name);
        projectile.AddComponent<T>();
        projectile.SetActive(active);
    }

    private static T[] FindProjectiles<T>() where T : Component
    {
        return Object.FindObjectsByType<T>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
    }

    private static void DestroyProjectiles<T>() where T : Component
    {
        foreach (T projectile in FindProjectiles<T>())
        {
            if (projectile != null)
            {
                Object.DestroyImmediate(projectile.gameObject);
            }
        }
    }
}
