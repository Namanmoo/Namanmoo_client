using System.Collections;
using NaManMoo.Dungeon;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

/// <summary>
/// 저장된 던전 씬이 실제로 도는지. 씬 빌더가 코드로 넣은 참조는 [SerializeField]가
/// 없으면 저장할 때 사라진다 — 에디터에서는 잘 돌다가 빌드에서만 조용히 죽는 종류의
/// 버그라, 씬을 진짜로 불러서 확인한다.
/// </summary>
public sealed class DungeonScenePlayModeTests
{
    private const string ScenePath = "Assets/Scenes/Dungeon.unity";

    private static IEnumerator LoadScene()
    {
        SceneManager.LoadScene(ScenePath);
        yield return null;
        yield return null;
    }

    [UnityTest]
    public IEnumerator TheSceneBuildsARoomAroundThePlayer()
    {
        yield return LoadScene();

        var runner = Object.FindFirstObjectByType<DungeonRunner>();
        Assert.That(runner, Is.Not.Null, "씬에 DungeonRunner 가 없다");
        Assert.That(runner.Layout, Is.Not.Null, "층이 만들어지지 않았다");
        Assert.That(runner.CurrentShape, Is.Not.Null, "방이 세워지지 않았다");
        Assert.That(runner.CurrentCell, Is.EqualTo(runner.Layout.StartCell));

        var player = GameObject.FindGameObjectWithTag("Player");
        Assert.That(player, Is.Not.Null);
        Assert.That(
            runner.CurrentShape.Bounds.Contains(player.transform.position),
            Is.True,
            "플레이어가 방 밖에 있다");
    }

    [UnityTest]
    public IEnumerator TheRoomIsDrawnWithVisibleSprites()
    {
        // 실제로 겪은 버그: 메시·라인으로 그렸을 때 렌더러·머티리얼·셰이더·카메라가
        // 전부 정상인데도 WebGL 빌드에서 방이 통째로 보이지 않았다. 그래서 "오브젝트가
        // 있다"가 아니라 "그릴 것이 붙어 있고 크기가 0이 아니다"까지 확인한다.
        yield return LoadScene();

        var floor = GameObject.Find("Room Floor");
        Assert.That(floor, Is.Not.Null, "바닥이 세워지지 않았다");

        var renderer = floor.GetComponent<SpriteRenderer>();
        Assert.That(renderer, Is.Not.Null, "바닥에 SpriteRenderer 가 없다");
        Assert.That(renderer.sprite, Is.Not.Null, "바닥에 스프라이트가 없다");
        Assert.That(renderer.enabled, Is.True);
        Assert.That(renderer.color.a, Is.GreaterThan(0.9f), "바닥이 투명하다");

        var runner = Object.FindFirstObjectByType<DungeonRunner>();
        Rect bounds = runner.CurrentShape.Bounds;
        Assert.That(renderer.bounds.size.x, Is.EqualTo(bounds.width).Within(0.01f));
        Assert.That(renderer.bounds.size.y, Is.EqualTo(bounds.height).Within(0.01f));

        // 벽도 실제로 그려지는 조각이 있어야 한다
        int wallPieces = 0;
        foreach (SpriteRenderer piece in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
        {
            if (piece.gameObject.name.StartsWith("Piece") && piece.sprite != null)
            {
                wallPieces++;
            }
        }

        Assert.That(wallPieces, Is.GreaterThan(0), "벽이 그려지지 않았다");
    }

    [UnityTest]
    public IEnumerator TheEncounterSurvivesSceneSerialisation()
    {
        // 인터페이스 필드는 유니티가 직렬화하지 못한다. 이게 끊기면 모든 방이
        // 즉시 클리어되어 던전이 그냥 빈 방 여러 개가 된다.
        yield return LoadScene();

        var runner = Object.FindFirstObjectByType<DungeonRunner>();
        var encounter = Object.FindFirstObjectByType<DungeonEncounter>();
        Assert.That(encounter, Is.Not.Null, "씬에 DungeonEncounter 가 없다");

        // 시작 방은 적이 없어 바로 클리어다. 싸울 것이 있는 방을 만날 때까지 걸어간다.
        // 씬은 실행마다 시드를 새로 뽑으므로 옆 방 하나만 보면 시드에 따라 결과가 갈린다.
        bool foundFight = false;
        for (int step = 0; step < 12 && !foundFight; step++)
        {
            DungeonRoom here = runner.Layout.RoomAt(runner.CurrentCell);
            Doors side = FirstSide(here.Doors);
            if (side == Doors.None)
            {
                break;
            }

            Pass(side);
            yield return null;

            DungeonRoom arrived = runner.Layout.RoomAt(runner.CurrentCell);
            foundFight = arrived.Kind == RoomKind.Boss
                || RoomSpawnPoints.EnemyCount(arrived.Kind, arrived.DistanceFromStart) > 0;

            if (!foundFight)
            {
                continue;
            }
        }

        Assert.That(foundFight, Is.True, "싸울 방을 하나도 만나지 못했다");

        Assert.That(
            Object.FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None),
            Is.Not.Empty,
            "적이 나오지 않았다 — 인카운터 참조가 끊겼다");
        Assert.That(runner.IsCleared(runner.CurrentCell), Is.False, "문이 잠기지 않았다");
    }

    [UnityTest]
    public IEnumerator TheMinimapSurvivesSceneSerialisationAndDrawsCells()
    {
        yield return LoadScene();

        var view = Object.FindFirstObjectByType<MinimapView>();
        Assert.That(view, Is.Not.Null, "씬에 MinimapView 가 없다");

        Image[] cells = view.GetComponentsInChildren<Image>(includeInactive: true);
        int drawn = 0;
        foreach (Image cell in cells)
        {
            if (cell.enabled && cell.gameObject.name.StartsWith("Minimap Cell"))
            {
                drawn++;
            }
        }

        Assert.That(drawn, Is.GreaterThan(0), "미니맵 칸이 하나도 그려지지 않았다");
    }

    [UnityTest]
    public IEnumerator TheCameraStaysInsideTheRoom()
    {
        yield return LoadScene();

        var runner = Object.FindFirstObjectByType<DungeonRunner>();
        Camera camera = Camera.main;
        Assert.That(camera, Is.Not.Null);

        CameraFollow follow = camera.GetComponent<CameraFollow>();
        Assert.That(follow, Is.Not.Null);
        Assert.That(follow.Bounds, Is.EqualTo(runner.CurrentShape.Bounds));

        float halfHeight = camera.orthographicSize;
        float halfWidth = halfHeight * camera.aspect;
        Rect bounds = runner.CurrentShape.Bounds;

        // 뷰가 방보다 작은 축에서는 방 밖 여백이 보이면 안 된다
        if (bounds.width > halfWidth * 2f)
        {
            Assert.That(camera.transform.position.x - halfWidth,
                Is.GreaterThanOrEqualTo(bounds.xMin - 0.01f));
            Assert.That(camera.transform.position.x + halfWidth,
                Is.LessThanOrEqualTo(bounds.xMax + 0.01f));
        }

        if (bounds.height > halfHeight * 2f)
        {
            Assert.That(camera.transform.position.y - halfHeight,
                Is.GreaterThanOrEqualTo(bounds.yMin - 0.01f));
            Assert.That(camera.transform.position.y + halfHeight,
                Is.LessThanOrEqualTo(bounds.yMax + 0.01f));
        }
    }

    [UnityTest]
    public IEnumerator EachRunGeneratesADifferentFloor()
    {
        // 시드가 씬에 구워져 있으면 매번 같은 층이 나온다
        yield return LoadScene();
        int first = Object.FindFirstObjectByType<DungeonRunner>().Seed;

        yield return LoadScene();
        int second = Object.FindFirstObjectByType<DungeonRunner>().Seed;

        Assert.That(second, Is.Not.EqualTo(first), "매번 같은 층이 나온다");
    }

    private static Doors FirstSide(Doors doors)
    {
        foreach (Doors side in new[] { Doors.North, Doors.South, Doors.East, Doors.West })
        {
            if (doors.HasFlag(side))
            {
                return side;
            }
        }

        return Doors.None;
    }

    private static void Pass(Doors side)
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        foreach (DungeonDoor door in Object.FindObjectsByType<DungeonDoor>(FindObjectsSortMode.None))
        {
            if (door.Side == side)
            {
                door.gameObject.SendMessage(
                    "OnTriggerEnter2D",
                    player.GetComponent<Collider2D>(),
                    SendMessageOptions.DontRequireReceiver);
                return;
            }
        }
    }
}
