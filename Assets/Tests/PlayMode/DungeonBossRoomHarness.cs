using System.Collections;
using NaManMoo.Dungeon;
using UnityEngine;

/// <summary>
/// 보스방까지 걸어가서 잡는 PlayMode 테스트가 공유하는 던전 설정과 이동 도우미.
/// 크랩(근접)과 다람쥐(원거리) 정의를 갖춘 던전 하나를 만들고, 보스방을 찾아
/// 걸어가는 로직을 제공한다.
/// </summary>
public sealed class DungeonBossRoomHarness
{
    public GameObject Root { get; }
    public GameObject Player { get; }
    public GameObject CameraObject { get; }
    public DungeonRunner Runner { get; }
    public DungeonEncounter Encounter { get; }

    private readonly Sprite sprite;
    private readonly Sprite projectileSprite;
    private readonly EnemyDefinition contactDefinition;
    private readonly EnemyDefinition rangedDefinition;

    public DungeonBossRoomHarness()
    {
        CameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
        Camera camera = CameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 10f;

        Player = new GameObject("Player") { tag = "Player" };
        Player.AddComponent<Rigidbody2D>().gravityScale = 0f;
        Player.AddComponent<CircleCollider2D>().radius = 0.5f;
        Player.AddComponent<PlayerHealth>();

        Root = new GameObject("Dungeon");
        Runner = Root.AddComponent<DungeonRunner>();
        Encounter = Root.AddComponent<DungeonEncounter>();

        sprite = Sprite.Create(
            Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        projectileSprite = Sprite.Create(
            Texture2D.blackTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        contactDefinition = ScriptableObject.CreateInstance<EnemyDefinition>();
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
        Encounter.Configure(new[] { contactDefinition, rangedDefinition }, sprite);
        Runner.SetEncounter(Encounter);
    }

    public void TearDown()
    {
        Object.DestroyImmediate(Root);
        Object.DestroyImmediate(Player);
        Object.DestroyImmediate(CameraObject);
        Object.DestroyImmediate(contactDefinition);
        Object.DestroyImmediate(rangedDefinition);
        Object.DestroyImmediate(sprite);
        Object.DestroyImmediate(projectileSprite);
    }

    /// <summary>보스방을 가진 층과 그 방의 좌표를 찾는다.</summary>
    public static (int seed, DungeonRoom boss) FindFloorWithBoss()
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

    /// <summary>보스방까지 문을 따라 걸어간다. 도중의 방은 전부 클리어 처리한다.</summary>
    public IEnumerator WalkTo(Vector2Int target)
    {
        for (int step = 0; step < 40 && Runner.CurrentCell != target; step++)
        {
            KillEverything();
            yield return null;

            Doors chosen = Doors.None;
            int best = int.MaxValue;
            foreach (Doors side in new[] { Doors.North, Doors.South, Doors.East, Doors.West })
            {
                Vector2Int next = DungeonNavigation.Neighbour(Runner.CurrentCell, side);
                if (!Runner.Layout.HasRoom(next))
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

    public void KillEverything()
    {
        foreach (EnemyHealth enemy in Object.FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None))
        {
            if (enemy != null)
            {
                enemy.TakeDamage(enemy.MaxHealth);
            }
        }
    }

    public void Pass(Doors side)
    {
        foreach (DungeonDoor door in Root.GetComponentsInChildren<DungeonDoor>())
        {
            if (door.Side == side)
            {
                door.gameObject.SendMessage(
                    "OnTriggerEnter2D",
                    Player.GetComponent<CircleCollider2D>(),
                    SendMessageOptions.DontRequireReceiver);
                return;
            }
        }
    }
}
