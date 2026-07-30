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
}
