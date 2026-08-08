# 던전 보스 처치 후 Stage Clear 화면 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Dungeon 보스방의 보스를 처치하면 화면 중앙에 `Stage Clear!` 문구와 `타이틀화면으로 돌아가기` 버튼을 표시하고, 플레이어는 그대로 보인 채 시간만 멈춘다.

**Architecture:** `DungeonRunner`가 보스방을 최초로 클리어할 때 새 `BossDefeated` 이벤트를 발생시킨다. `PlayerDeathScreen`을 본뜬 `StageClearScreen`(+`StageClearScreenView`/`StageClearScreenUIFactory`)이 이 이벤트를 구독해 시간 정지 → 페이드 → 문구/버튼 표시 → 씬 전환을 수행한다. `PlayerDeathScreen`과 겹치는 로직(uGUI 조립 도우미, 씬 로더, 페이드 진행, 씬 전환 가드)은 공용 코드로 뽑아 두 화면이 함께 쓴다. 보스방까지 걸어가서 잡는 PlayMode 테스트 하네스도 마찬가지로 공용 클래스로 뽑아 기존 `DungeonBossPlayModeTests`와 새 테스트가 함께 쓴다. `DungeonSceneBuilder`가 던전 씬을 지을 때 `StageClearScreen`을 붙인다.

**Tech Stack:** Unity 6000.5.5f1, C#, uGUI(Canvas/Text/Button), NUnit + Unity Test Framework(EditMode/PlayMode)

## Global Constraints

- 문구는 정확히 `Stage Clear!`. 버튼은 `타이틀화면으로 돌아가기` 하나만 (재시작 버튼 없음).
- 보스 처치 시 플레이어 오브젝트는 숨기지 않는다 — `Time.timeScale`만 0으로 만든다.
- 페이드는 실시간 1초(`Time.unscaledDeltaTime` 누적), `FadeDuration = 1f`.
- `BossDefeated`는 보스방을 **최초로** 클리어할 때만 발생한다. 이미 클리어한 보스방을 재방문했다가 나가는 경우 다시 발생하지 않는다. 보스방이 아닌 방을 클리어할 때는 발생하지 않는다.
- 버튼 클릭 시 씬을 로드하기 직전에 `Time.timeScale`을 1로 복원한다. 대상 씬은 `GameScenes.Title`.
- **`PlayerDeathScreen`과 겹치는 로직은 복제하지 않고 공용 코드로 뽑는다** (uGUI 조립 도우미 → `RuntimeMenuUIFactory`, 씬 로더 → `ISceneLoader`/`UnitySceneLoader` 전용 파일, 페이드 진행 → `ScreenFade`/`IFadeOverlay`, 씬 전환 가드 → `SceneTransitionGuard`). 화면마다 다른 부분(플레이어 숨김 여부, 버튼 개수, 이벤트 소스)만 각 컴포넌트에 남긴다.
- 보스방까지 걸어가서 잡는 PlayMode 테스트 설정도 복제하지 않고 `DungeonBossRoomHarness` 공용 클래스로 뽑아 기존/신규 테스트가 함께 쓴다.
- 대상은 Dungeon 시스템의 보스방(`RoomKind.Boss`)뿐이다. Stage1(`Stage1BossEncounter`)은 이번 작업 범위 밖이다.
- 이 계획은 기존 `PlayerDeathScreen`/`PlayerDeathScreenView`/`PlayerDeathScreenUIFactory`/`DungeonBossPlayModeTests`를 리팩터링한다 — 동작은 그대로 유지해야 하므로 각 리팩터링 직후 해당 기존 테스트를 돌려 회귀가 없는지 확인한다.
- Unity 테스트는 항상 `-testFilter`로 좁혀서 실행한다(`-runTests` 단독 실행 금지). 사용할 명령 형태:
  `& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Users\myong\NaManMoo\Namanmoo_client' -runTests -testPlatform <EditMode|PlayMode> -testFilter '<ClassName>' -testResults 'C:\Users\myong\NaManMoo\Namanmoo_client\Artifacts\<label>.xml' -logFile 'C:\Users\myong\NaManMoo\Namanmoo_client\Artifacts\<label>.log'`

---

## Task 1: 보스방 PlayMode 테스트 공용 하네스

**Files:**
- Create: `Assets/Tests/PlayMode/DungeonBossRoomHarness.cs`
- Modify: `Assets/Tests/PlayMode/DungeonBossPlayModeTests.cs`

**Interfaces:**
- Produces: `DungeonBossRoomHarness` — 생성자에서 카메라/플레이어/`DungeonRunner`/`DungeonEncounter`(크랩+다람쥐 정의)를 만들어 `Root`/`Player`/`CameraObject`/`Runner`/`Encounter` 프로퍼티로 노출한다. `void TearDown()`, `static (int seed, DungeonRoom boss) FindFloorWithBoss()`, `IEnumerator WalkTo(Vector2Int target)`, `void KillEverything()`, `void Pass(Doors side)`. Task 2와 Task 4가 이 타입을 그대로 쓴다.

이 태스크는 동작을 바꾸지 않는 순수 리팩터링이다 — 새 실패 테스트를 쓰는 대신, 하네스를 만들고 기존 테스트가 이를 쓰도록 고친 뒤 기존 테스트가 그대로 통과하는지로 검증한다.

- [ ] **Step 1: 하네스를 만들기 전에 기존 `DungeonBossPlayModeTests`가 통과하는 상태인지 기준선을 확인한다**

Run:
```
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Users\myong\NaManMoo\Namanmoo_client' -runTests -testPlatform PlayMode -testFilter 'DungeonBossPlayModeTests' -testResults 'C:\Users\myong\NaManMoo\Namanmoo_client\Artifacts\bossharness-baseline.xml' -logFile 'C:\Users\myong\NaManMoo\Namanmoo_client\Artifacts\bossharness-baseline.log'
```
Expected: PASS (리팩터링 전 기준선)

- [ ] **Step 2: `DungeonBossRoomHarness`를 만든다**

`Assets/Tests/PlayMode/DungeonBossRoomHarness.cs`:

```csharp
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
```

- [ ] **Step 3: `DungeonBossPlayModeTests.cs`가 하네스를 쓰도록 고친다**

`Assets/Tests/PlayMode/DungeonBossPlayModeTests.cs` 전체를 다음으로 교체한다 (테스트 로직은 동일하고, 필드 접근만 `harness.`를 거친다):

```csharp
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
}
```

- [ ] **Step 4: 리팩터링 후에도 같은 테스트가 통과하는지 확인한다 (회귀 없음)**

Run:
```
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Users\myong\NaManMoo\Namanmoo_client' -runTests -testPlatform PlayMode -testFilter 'DungeonBossPlayModeTests' -testResults 'C:\Users\myong\NaManMoo\Namanmoo_client\Artifacts\bossharness-after.xml' -logFile 'C:\Users\myong\NaManMoo\Namanmoo_client\Artifacts\bossharness-after.log'
```
Expected: PASS, Step 1과 같은 4개 테스트 전부 통과.

- [ ] **Step 5: 커밋**

```bash
git add Assets/Tests/PlayMode/DungeonBossRoomHarness.cs Assets/Tests/PlayMode/DungeonBossPlayModeTests.cs
git commit -m "test: 보스방 PlayMode 테스트 설정을 DungeonBossRoomHarness로 공용화"
```

---

## Task 2: `DungeonRunner.BossDefeated` 이벤트

**Files:**
- Modify: `Assets/Scripts/Dungeon/DungeonRunner.cs`
- Modify: `Assets/Tests/PlayMode/DungeonBossPlayModeTests.cs`
- Modify: `Assets/Tests/PlayMode/DungeonRunnerPlayModeTests.cs`

**Interfaces:**
- Consumes: `DungeonBossRoomHarness`(Task 1) — `harness.Runner`, `harness.Player`, `DungeonBossRoomHarness.FindFloorWithBoss()`, `harness.WalkTo(cell)`, `harness.KillEverything()`, `harness.Pass(side)`.
- Produces: `public event System.Action BossDefeated;` on `NaManMoo.Dungeon.DungeonRunner`. 보스방(`RoomKind.Boss`)을 처음 클리어하는 프레임에 정확히 한 번 발생. 이미 클리어된 방 재방문이나 비보스방 클리어 시에는 발생하지 않음.

- [ ] **Step 1: `DungeonBossPlayModeTests.cs`에 실패하는 테스트 두 개를 추가한다**

`Assets/Tests/PlayMode/DungeonBossPlayModeTests.cs`의 `OrdinaryRoomsStillGetMushroomsNotABoss` 테스트 뒤, 클래스 닫는 중괄호 앞에 추가한다:

```csharp
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
```

- [ ] **Step 2: `DungeonRunnerPlayModeTests.cs`에 실패하는 테스트 하나를 추가한다**

`Assets/Tests/PlayMode/DungeonRunnerPlayModeTests.cs`의 `SafeRoomsAreOpenImmediately` 테스트 뒤에 추가한다 (시작 방은 `RoomKind.Start`이므로 비보스방 클리어 케이스로 쓴다):

```csharp
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
```

- [ ] **Step 3: 컴파일 실패(빨강)로 실패하는지 확인한다**

`BossDefeated`가 아직 없으므로 컴파일 오류로 실패해야 한다.

Run:
```
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Users\myong\NaManMoo\Namanmoo_client' -runTests -testPlatform PlayMode -testFilter 'DungeonBossPlayModeTests' -testResults 'C:\Users\myong\NaManMoo\Namanmoo_client\Artifacts\bossdefeated-red.xml' -logFile 'C:\Users\myong\NaManMoo\Namanmoo_client\Artifacts\bossdefeated-red.log'
```
Expected: 실행 실패, 로그에 `BossDefeated`를 찾을 수 없다는 컴파일 오류.

- [ ] **Step 4: `DungeonRunner`에 이벤트를 추가하고 최초 클리어 시에만 발생시킨다**

`Assets/Scripts/Dungeon/DungeonRunner.cs`에서 `RoomChanged` 선언 바로 아래에 추가한다:

```csharp
        /// <summary>방이 바뀌었을 때. 미니맵이 여기에 붙는다.</summary>
        public event System.Action<Vector2Int> RoomChanged;

        /// <summary>보스방을 처음 클리어했을 때. Stage Clear 화면이 여기에 붙는다.</summary>
        public event System.Action BossDefeated;
```

`MarkClearedAndOpen()`을 다음으로 교체한다:

```csharp
        private void MarkClearedAndOpen()
        {
            bool firstClear = cleared.Add(CurrentCell);
            gate = null;
            SetDoorsOpen(true);

            if (firstClear && Layout.RoomAt(CurrentCell).Kind == RoomKind.Boss)
            {
                BossDefeated?.Invoke();
            }
        }
```

- [ ] **Step 5: 테스트가 통과(초록)하는지 확인한다**

Run:
```
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Users\myong\NaManMoo\Namanmoo_client' -runTests -testPlatform PlayMode -testFilter 'DungeonBossPlayModeTests' -testResults 'C:\Users\myong\NaManMoo\Namanmoo_client\Artifacts\bossdefeated-green.xml' -logFile 'C:\Users\myong\NaManMoo\Namanmoo_client\Artifacts\bossdefeated-green.log'
```
Expected: PASS (전체 `DungeonBossPlayModeTests`)

```
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Users\myong\NaManMoo\Namanmoo_client' -runTests -testPlatform PlayMode -testFilter 'DungeonRunnerPlayModeTests' -testResults 'C:\Users\myong\NaManMoo\Namanmoo_client\Artifacts\dungeonrunner-bossdefeated-green.xml' -logFile 'C:\Users\myong\NaManMoo\Namanmoo_client\Artifacts\dungeonrunner-bossdefeated-green.log'
```
Expected: PASS (전체 `DungeonRunnerPlayModeTests`)

- [ ] **Step 6: 커밋**

```bash
git add Assets/Scripts/Dungeon/DungeonRunner.cs Assets/Tests/PlayMode/DungeonBossPlayModeTests.cs Assets/Tests/PlayMode/DungeonRunnerPlayModeTests.cs
git commit -m "feat: DungeonRunner가 보스방 최초 클리어 시 BossDefeated를 발생시키게 한다"
```

---

## Task 3: 공용 UI 조립 도우미 + `StageClearScreenView`/`StageClearScreenUIFactory`

**Files:**
- Create: `Assets/Scripts/UI/RuntimeMenuUIFactory.cs`
- Modify: `Assets/Scripts/UI/PlayerDeathScreenUIFactory.cs`
- Create: `Assets/Scripts/UI/StageClearScreenView.cs`
- Create: `Assets/Scripts/UI/StageClearScreenUIFactory.cs`
- Test: `Assets/Tests/Editor/StageClearScreenUIFactoryTests.cs`

**Interfaces:**
- Produces: `RuntimeMenuUIFactory` (정적 클래스) — `Button CreateButton(Transform parent, string name, string label, Vector2 position)`, `Text CreateText(Transform parent, string name, string content, int fontSize, Color color)`, `Image CreateImage(Transform parent, string name, Color color)`, `void Stretch(RectTransform rect)`, `void SetCenteredRect(RectTransform rect, Vector2 position, Vector2 size)`, `void EnsureEventSystem()`. `PlayerDeathScreenUIFactory`와 `StageClearScreenUIFactory` 둘 다 이 메서드들을 쓴다.
- Produces: `StageClearScreenView` — `Image FadeOverlay`, `GameObject Menu`, `Button TitleButton`(읽기 전용 프로퍼티), `void Initialize(Image fadeOverlay, GameObject menu, Button titleButton)`, `void SetFadeAlpha(float alpha)`, `void ShowMenu()`. (Task 4에서 `IFadeOverlay` 구현이 추가된다.)
- Produces: `StageClearScreenUIFactory.Create(Transform parent) : StageClearScreenView` — Screen Space Overlay 캔버스(정렬 순서 100, 1920x1080 기준 해상도)를 만들고 `Stage Clear!` 문구 + `타이틀화면으로 돌아가기` 버튼이 든 숨겨진 메뉴를 붙인다.
- Task 4가 `StageClearScreenView`/`StageClearScreenUIFactory`를 그대로 쓴다.

- [ ] **Step 1: 리팩터링 전 기준선으로 기존 `PlayerDeathScreenUIFactoryTests`가 통과하는지 확인한다**

Run:
```
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Users\myong\NaManMoo\Namanmoo_client' -runTests -testPlatform EditMode -testFilter 'PlayerDeathScreenUIFactoryTests' -testResults 'C:\Users\myong\NaManMoo\Namanmoo_client\Artifacts\uifactory-baseline.xml' -logFile 'C:\Users\myong\NaManMoo\Namanmoo_client\Artifacts\uifactory-baseline.log'
```
Expected: PASS (리팩터링 전 기준선)

- [ ] **Step 2: 실패하는 `StageClearScreenUIFactoryTests`를 작성한다**

`Assets/Tests/Editor/StageClearScreenUIFactoryTests.cs`를 만든다:

```csharp
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public sealed class StageClearScreenUIFactoryTests
{
    private GameObject root;

    [TearDown]
    public void TearDown()
    {
        if (root != null)
        {
            Object.DestroyImmediate(root);
        }

        EventSystem eventSystem = Object.FindAnyObjectByType<EventSystem>();
        if (eventSystem != null)
        {
            Object.DestroyImmediate(eventSystem.gameObject);
        }
    }

    [Test]
    public void Create_BuildsHiddenStageClearMenuAboveTransparentBlackOverlay()
    {
        root = new GameObject(nameof(StageClearScreenUIFactoryTests));

        StageClearScreenView view = StageClearScreenUIFactory.Create(root.transform);

        Canvas canvas = view.GetComponentInParent<Canvas>();
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        Text message = view.Menu.transform.Find("Message").GetComponent<Text>();

        Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
        Assert.That(canvas.sortingOrder, Is.EqualTo(100));
        Assert.That(
            scaler.referenceResolution,
            Is.EqualTo(new Vector2(1920f, 1080f)));
        Assert.That(
            view.FadeOverlay.color,
            Is.EqualTo(new Color(0f, 0f, 0f, 0f)));
        Assert.That(view.Menu.activeSelf, Is.False);
        Assert.That(message.text, Is.EqualTo("Stage Clear!"));
        Assert.That(
            view.TitleButton.GetComponentInChildren<Text>().text,
            Is.EqualTo("타이틀화면으로 돌아가기"));
        EventSystem eventSystem = Object.FindAnyObjectByType<EventSystem>();
        Assert.That(eventSystem, Is.Not.Null);
        Assert.That(
            eventSystem.GetComponent<InputSystemUIInputModule>(),
            Is.Not.Null);
    }
}
```

- [ ] **Step 3: 컴파일 실패(빨강)로 실패하는지 확인한다**

Run:
```
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Users\myong\NaManMoo\Namanmoo_client' -runTests -testPlatform EditMode -testFilter 'StageClearScreenUIFactoryTests' -testResults 'C:\Users\myong\NaManMoo\Namanmoo_client\Artifacts\stageclearfactory-red.xml' -logFile 'C:\Users\myong\NaManMoo\Namanmoo_client\Artifacts\stageclearfactory-red.log'
```
Expected: 실행 실패, `StageClearScreenView`/`StageClearScreenUIFactory`를 찾을 수 없다는 컴파일 오류.

- [ ] **Step 4: `RuntimeMenuUIFactory` 공용 도우미를 만든다**

`Assets/Scripts/UI/RuntimeMenuUIFactory.cs`:

```csharp
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// 사망 화면·Stage Clear 화면 같은 전체 화면 오버레이 메뉴가 함께 쓰는 uGUI 조립 도우미.
/// </summary>
public static class RuntimeMenuUIFactory
{
    public static readonly Color ButtonColor = new Color(0.92f, 0.86f, 0.7f, 1f);
    public static readonly Color Ink = new Color(0.12f, 0.1f, 0.09f, 1f);

    public static Button CreateButton(
        Transform parent,
        string name,
        string label,
        Vector2 position)
    {
        var buttonObject = new GameObject(
            name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.GetComponent<Image>();
        image.color = ButtonColor;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        SetCenteredRect(rect, position, new Vector2(420f, 72f));

        Text text = CreateText(buttonObject.transform, "Text", label, 30, Ink);
        Stretch(text.rectTransform);
        return button;
    }

    public static Text CreateText(
        Transform parent,
        string name,
        string content,
        int fontSize,
        Color color)
    {
        var textObject = new GameObject(
            name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Text));
        textObject.transform.SetParent(parent, false);
        Text text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = content;
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    public static Image CreateImage(
        Transform parent,
        string name,
        Color color)
    {
        var imageObject = new GameObject(
            name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        return image;
    }

    public static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
    }

    public static void SetCenteredRect(
        RectTransform rect,
        Vector2 position,
        Vector2 size)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    public static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() != null)
        {
            return;
        }

        new GameObject(
            "EventSystem",
            typeof(EventSystem),
            typeof(InputSystemUIInputModule));
    }
}
```

- [ ] **Step 5: `PlayerDeathScreenUIFactory`가 공용 도우미를 쓰도록 고친다**

`Assets/Scripts/UI/PlayerDeathScreenUIFactory.cs` 전체를 다음으로 교체한다 (겉으로 보이는 결과물은 동일하다 — 오브젝트 이름, 계층 구조, 색상, 정렬 순서 모두 그대로):

```csharp
using UnityEngine;
using UnityEngine.UI;

public static class PlayerDeathScreenUIFactory
{
    public static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);
    public const int SortingOrder = 100;

    public static PlayerDeathScreenView Create(Transform parent)
    {
        var canvasObject = new GameObject(
            "Player Death Canvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(PlayerDeathScreenView));
        if (parent != null)
        {
            canvasObject.transform.SetParent(parent, false);
        }

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortingOrder;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = ReferenceResolution;
        scaler.matchWidthOrHeight = 0.5f;

        Image overlay = RuntimeMenuUIFactory.CreateImage(
            canvasObject.transform,
            "Fade Overlay",
            new Color(0f, 0f, 0f, 0f));
        RuntimeMenuUIFactory.Stretch(overlay.rectTransform);
        // 투명한 채로 살아 있는 풀스크린 이미지다 — 레이캐스트를 받으면
        // 게임 내내 모든 마우스 이벤트(핫바 툴팁 등)를 삼킨다.
        overlay.raycastTarget = false;

        var menu = new GameObject("Death Menu", typeof(RectTransform));
        menu.transform.SetParent(canvasObject.transform, false);
        RectTransform menuRect = menu.GetComponent<RectTransform>();
        RuntimeMenuUIFactory.SetCenteredRect(menuRect, Vector2.zero, new Vector2(620f, 360f));

        Text message = RuntimeMenuUIFactory.CreateText(
            menu.transform,
            "Message",
            "이번에도 틀렸나...",
            48,
            Color.white);
        RuntimeMenuUIFactory.SetCenteredRect(
            message.rectTransform, new Vector2(0f, 100f), new Vector2(620f, 80f));

        Button titleButton = RuntimeMenuUIFactory.CreateButton(
            menu.transform,
            "Return To Title Button",
            "타이틀화면으로 돌아가기",
            new Vector2(0f, -10f));
        Button restartButton = RuntimeMenuUIFactory.CreateButton(
            menu.transform,
            "Restart Button",
            "처음부터 다시하기",
            new Vector2(0f, -105f));

        PlayerDeathScreenView view =
            canvasObject.GetComponent<PlayerDeathScreenView>();
        view.Initialize(overlay, menu, titleButton, restartButton);
        menu.SetActive(false);

        RuntimeMenuUIFactory.EnsureEventSystem();
        return view;
    }
}
```

- [ ] **Step 6: 리팩터링 후에도 `PlayerDeathScreenUIFactoryTests`가 통과하는지 확인한다 (회귀 없음)**

Run:
```
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Users\myong\NaManMoo\Namanmoo_client' -runTests -testPlatform EditMode -testFilter 'PlayerDeathScreenUIFactoryTests' -testResults 'C:\Users\myong\NaManMoo\Namanmoo_client\Artifacts\uifactory-after.xml' -logFile 'C:\Users\myong\NaManMoo\Namanmoo_client\Artifacts\uifactory-after.log'
```
Expected: PASS

- [ ] **Step 7: `StageClearScreenView`를 만든다**

`Assets/Scripts/UI/StageClearScreenView.cs`:

```csharp
using UnityEngine;
using UnityEngine.UI;

public sealed class StageClearScreenView : MonoBehaviour
{
    public Image FadeOverlay { get; private set; }
    public GameObject Menu { get; private set; }
    public Button TitleButton { get; private set; }

    public void Initialize(
        Image fadeOverlay,
        GameObject menu,
        Button titleButton)
    {
        FadeOverlay = fadeOverlay;
        Menu = menu;
        TitleButton = titleButton;

        if (FadeOverlay != null)
        {
            // 투명한 채로 항상 살아 있는 풀스크린 이미지다 — 레이캐스트를 받으면
            // 게임 내내 모든 마우스 이벤트를 삼킨다.
            FadeOverlay.raycastTarget = false;
        }
    }

    public void SetFadeAlpha(float alpha)
    {
        Color color = FadeOverlay.color;
        color.a = Mathf.Clamp01(alpha);
        FadeOverlay.color = color;
    }

    public void ShowMenu()
    {
        Menu.SetActive(true);
    }
}
```

- [ ] **Step 8: `StageClearScreenUIFactory`를 만든다**

`Assets/Scripts/UI/StageClearScreenUIFactory.cs`:

```csharp
using UnityEngine;
using UnityEngine.UI;

public static class StageClearScreenUIFactory
{
    public static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);
    public const int SortingOrder = 100;

    public static StageClearScreenView Create(Transform parent)
    {
        var canvasObject = new GameObject(
            "Stage Clear Canvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(StageClearScreenView));
        if (parent != null)
        {
            canvasObject.transform.SetParent(parent, false);
        }

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortingOrder;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = ReferenceResolution;
        scaler.matchWidthOrHeight = 0.5f;

        Image overlay = RuntimeMenuUIFactory.CreateImage(
            canvasObject.transform,
            "Fade Overlay",
            new Color(0f, 0f, 0f, 0f));
        RuntimeMenuUIFactory.Stretch(overlay.rectTransform);
        overlay.raycastTarget = false;

        var menu = new GameObject("Stage Clear Menu", typeof(RectTransform));
        menu.transform.SetParent(canvasObject.transform, false);
        RectTransform menuRect = menu.GetComponent<RectTransform>();
        RuntimeMenuUIFactory.SetCenteredRect(menuRect, Vector2.zero, new Vector2(620f, 220f));

        Text message = RuntimeMenuUIFactory.CreateText(
            menu.transform,
            "Message",
            "Stage Clear!",
            48,
            Color.white);
        RuntimeMenuUIFactory.SetCenteredRect(
            message.rectTransform, new Vector2(0f, 55f), new Vector2(620f, 80f));

        Button titleButton = RuntimeMenuUIFactory.CreateButton(
            menu.transform,
            "Return To Title Button",
            "타이틀화면으로 돌아가기",
            new Vector2(0f, -40f));

        StageClearScreenView view =
            canvasObject.GetComponent<StageClearScreenView>();
        view.Initialize(overlay, menu, titleButton);
        menu.SetActive(false);

        RuntimeMenuUIFactory.EnsureEventSystem();
        return view;
    }
}
```

- [ ] **Step 9: `StageClearScreenUIFactoryTests`가 통과(초록)하는지 확인한다**

Run:
```
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Users\myong\NaManMoo\Namanmoo_client' -runTests -testPlatform EditMode -testFilter 'StageClearScreenUIFactoryTests' -testResults 'C:\Users\myong\NaManMoo\Namanmoo_client\Artifacts\stageclearfactory-green.xml' -logFile 'C:\Users\myong\NaManMoo\Namanmoo_client\Artifacts\stageclearfactory-green.log'
```
Expected: PASS

- [ ] **Step 10: 커밋**

```bash
git add Assets/Scripts/UI/RuntimeMenuUIFactory.cs Assets/Scripts/UI/PlayerDeathScreenUIFactory.cs Assets/Scripts/UI/StageClearScreenView.cs Assets/Scripts/UI/StageClearScreenUIFactory.cs Assets/Tests/Editor/StageClearScreenUIFactoryTests.cs
git commit -m "feat: uGUI 조립 도우미를 RuntimeMenuUIFactory로 공용화하고 Stage Clear 화면 뷰/팩토리 추가"
```

---

## Task 4: 공용 씬 전환/페이드 + `StageClearScreen` 동작

**Files:**
- Create: `Assets/Scripts/UI/ISceneLoader.cs`
- Create: `Assets/Scripts/UI/IFadeOverlay.cs`
- Create: `Assets/Scripts/UI/ScreenFade.cs`
- Create: `Assets/Scripts/UI/SceneTransitionGuard.cs`
- Modify: `Assets/Scripts/UI/PlayerDeathScreen.cs`
- Modify: `Assets/Scripts/UI/PlayerDeathScreenView.cs`
- Modify: `Assets/Scripts/UI/StageClearScreenView.cs`
- Create: `Assets/Scripts/UI/StageClearScreen.cs`
- Test: `Assets/Tests/PlayMode/StageClearScreenPlayModeTests.cs`

**Interfaces:**
- Consumes: `NaManMoo.Dungeon.DungeonRunner.BossDefeated`(Task 2), `StageClearScreenView`/`StageClearScreenUIFactory.Create`(Task 3), `DungeonBossRoomHarness`(Task 1), `GameScenes.Title`/`GameScenes.Dungeon`.
- Produces: `ISceneLoader`(인터페이스, `Assets/Scripts/UI/ISceneLoader.cs`로 이동) + `UnitySceneLoader`(같은 파일). `IFadeOverlay`(`void SetFadeAlpha(float)`, `void ShowMenu()`). `ScreenFade.Run(IFadeOverlay view, float duration) : IEnumerator`. `SceneTransitionGuard.Load(ISceneLoader sceneLoader, string scenePath)`.
- Produces: `StageClearScreen` — `bool IsTransitioning`, `void Configure(DungeonRunner runner, StageClearScreenView view)`, `void Configure(DungeonRunner runner, StageClearScreenView view, ISceneLoader sceneLoader)`, `void ReturnToTitle()`, `public const float FadeDuration = 1f`. Task 5가 `Configure`를 호출해 씬에 붙인다.

- [ ] **Step 1: 리팩터링 전 기준선으로 기존 `PlayerDeathScreenPlayModeTests`가 통과하는지 확인한다**

Run:
```
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Users\myong\NaManMoo\Namanmoo_client' -runTests -testPlatform PlayMode -testFilter 'PlayerDeathScreenPlayModeTests' -testResults 'C:\Users\myong\NaManMoo\Namanmoo_client\Artifacts\deathscreen-baseline.xml' -logFile 'C:\Users\myong\NaManMoo\Namanmoo_client\Artifacts\deathscreen-baseline.log'
```
Expected: PASS (리팩터링 전 기준선)

- [ ] **Step 2: 실패하는 `StageClearScreenPlayModeTests`를 작성한다**

`Assets/Tests/PlayMode/StageClearScreenPlayModeTests.cs`를 만든다 (`DungeonBossRoomHarness`를 그대로 쓴다):

```csharp
using System.Collections;
using NaManMoo.Dungeon;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

/// <summary>
/// 보스를 잡으면 Stage Clear 화면이 뜨는지. 보스 처치 흐름은
/// <see cref="DungeonBossRoomHarness"/>로 재현한다.
/// </summary>
public sealed class StageClearScreenPlayModeTests
{
    private DungeonBossRoomHarness harness;

    [SetUp]
    public void SetUp()
    {
        Time.timeScale = 1f;
        harness = new DungeonBossRoomHarness();
    }

    [TearDown]
    public void TearDown()
    {
        Time.timeScale = 1f;
        harness.TearDown();

        EventSystem eventSystem = Object.FindAnyObjectByType<EventSystem>();
        if (eventSystem != null)
        {
            Object.DestroyImmediate(eventSystem.gameObject);
        }
    }

    private StageClearScreenView BuildScreen()
    {
        StageClearScreenView view = StageClearScreenUIFactory.Create(harness.Root.transform);
        StageClearScreen screen = view.gameObject.AddComponent<StageClearScreen>();
        screen.Configure(harness.Runner, view);
        return view;
    }

    private StageClearScreenView BuildScreen(ISceneLoader sceneLoader)
    {
        StageClearScreenView view = StageClearScreenUIFactory.Create(harness.Root.transform);
        StageClearScreen screen = view.gameObject.AddComponent<StageClearScreen>();
        screen.Configure(harness.Runner, view, sceneLoader);
        return view;
    }

    private IEnumerator EnterAndReachBossRoom()
    {
        (int seed, DungeonRoom boss) = DungeonBossRoomHarness.FindFloorWithBoss();
        Assert.That(boss, Is.Not.Null, "보스방이 있는 층을 찾지 못했다");

        harness.Runner.Configure(seed, 2, harness.Player.transform);
        harness.Runner.Begin();
        yield return null;

        yield return harness.WalkTo(boss.Cell);
        Assert.That(harness.Runner.CurrentCell, Is.EqualTo(boss.Cell), "보스방까지 가지 못했다");
    }

    [UnityTest]
    public IEnumerator BossDefeat_ImmediatelyPausesGameKeepsPlayerActiveAndMenuHidden()
    {
        StageClearScreenView view = BuildScreen();
        yield return EnterAndReachBossRoom();

        harness.KillEverything();
        yield return null;

        Assert.That(Time.timeScale, Is.Zero);
        Assert.That(harness.Player.activeSelf, Is.True);
        Assert.That(view.Menu.activeSelf, Is.False);
    }

    [UnityTest]
    public IEnumerator BossDefeat_AfterOneRealtimeSecondShowsOpaqueMenu()
    {
        StageClearScreenView view = BuildScreen();
        yield return EnterAndReachBossRoom();

        harness.KillEverything();
        yield return new WaitForSecondsRealtime(1.05f);

        Assert.That(view.FadeOverlay.color.a, Is.EqualTo(1f).Within(0.01f));
        Assert.That(view.Menu.activeSelf, Is.True);
    }

    [UnityTest]
    public IEnumerator TitleButton_RestoresTimeAndLoadsTitleScene()
    {
        var loader = new RecordingSceneLoader();
        StageClearScreenView view = BuildScreen(loader);
        yield return EnterAndReachBossRoom();

        harness.KillEverything();
        yield return null;

        view.TitleButton.onClick.Invoke();
        yield return null;

        Assert.That(Time.timeScale, Is.EqualTo(1f));
        Assert.That(loader.LoadedScenePath, Is.EqualTo(GameScenes.Title));
    }

    private sealed class RecordingSceneLoader : ISceneLoader
    {
        public string ActiveScenePath => GameScenes.Dungeon;
        public string LoadedScenePath { get; private set; }

        public void Load(string scenePath)
        {
            LoadedScenePath = scenePath;
        }
    }
}
```

- [ ] **Step 3: 컴파일 실패(빨강)로 실패하는지 확인한다**

Run:
```
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Users\myong\NaManMoo\Namanmoo_client' -runTests -testPlatform PlayMode -testFilter 'StageClearScreenPlayModeTests' -testResults 'C:\Users\myong\NaManMoo\Namanmoo_client\Artifacts\stageclearscreen-red.xml' -logFile 'C:\Users\myong\NaManMoo\Namanmoo_client\Artifacts\stageclearscreen-red.log'
```
Expected: 실행 실패, `StageClearScreen`을 찾을 수 없다는 컴파일 오류.

- [ ] **Step 4: 공용 씬 로더/페이드/전환 가드를 만든다**

`Assets/Scripts/UI/ISceneLoader.cs`:

```csharp
using UnityEngine.SceneManagement;

public interface ISceneLoader
{
    string ActiveScenePath { get; }
    void Load(string scenePath);
}

public sealed class UnitySceneLoader : ISceneLoader
{
    public string ActiveScenePath => SceneManager.GetActiveScene().path;

    public void Load(string scenePath)
    {
        SceneManager.LoadScene(scenePath);
    }
}
```

`Assets/Scripts/UI/IFadeOverlay.cs`:

```csharp
public interface IFadeOverlay
{
    void SetFadeAlpha(float alpha);
    void ShowMenu();
}
```

`Assets/Scripts/UI/ScreenFade.cs`:

```csharp
using System.Collections;
using UnityEngine;

/// <summary>
/// 실시간 기준으로 오버레이를 검게 페이드한다. Time.timeScale이 0이어도
/// Time.unscaledDeltaTime을 쓰므로 계속 진행된다.
/// </summary>
public static class ScreenFade
{
    public static IEnumerator Run(IFadeOverlay view, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            view.SetFadeAlpha(elapsed / duration);
            yield return null;
        }

        view.SetFadeAlpha(1f);
        view.ShowMenu();
    }
}
```

`Assets/Scripts/UI/SceneTransitionGuard.cs`:

```csharp
using UnityEngine;

/// <summary>
/// 씬 전환 버튼을 연속 클릭해도 한 번만 전환하고, 전환 직전에 시간을 복원한다.
/// </summary>
public sealed class SceneTransitionGuard
{
    private bool loading;

    public void Load(ISceneLoader sceneLoader, string scenePath)
    {
        if (loading || string.IsNullOrEmpty(scenePath))
        {
            return;
        }

        loading = true;
        Time.timeScale = 1f;
        sceneLoader.Load(scenePath);
    }
}
```

- [ ] **Step 5: `PlayerDeathScreen`/`PlayerDeathScreenView`가 공용 코드를 쓰도록 고친다**

`Assets/Scripts/UI/PlayerDeathScreen.cs` 전체를 다음으로 교체한다 (공개 API `Initialize`/`ReturnToTitle`/`RestartCurrentScene`/`IsTransitioning`은 그대로다):

```csharp
using System;
using System.Collections;
using UnityEngine;

public sealed class PlayerDeathScreen : MonoBehaviour
{
    public const float FadeDuration = 1f;

    private GameObject player;
    private PlayerHealth health;
    private PlayerDeathScreenView view;
    private ISceneLoader sceneLoader;
    private readonly SceneTransitionGuard sceneTransition = new SceneTransitionGuard();
    private bool deathStarted;

    public bool IsTransitioning { get; private set; }

    public void Initialize(
        GameObject newPlayer,
        PlayerHealth newHealth,
        PlayerDeathScreenView newView)
    {
        Initialize(newPlayer, newHealth, newView, new UnitySceneLoader());
    }

    public void Initialize(
        GameObject newPlayer,
        PlayerHealth newHealth,
        PlayerDeathScreenView newView,
        ISceneLoader newSceneLoader)
    {
        if (newPlayer == null)
        {
            throw new ArgumentNullException(nameof(newPlayer));
        }

        if (newHealth == null)
        {
            throw new ArgumentNullException(nameof(newHealth));
        }

        if (newView == null)
        {
            throw new ArgumentNullException(nameof(newView));
        }

        if (newSceneLoader == null)
        {
            throw new ArgumentNullException(nameof(newSceneLoader));
        }

        if (health != null)
        {
            health.Died -= HandleDeath;
        }

        player = newPlayer;
        health = newHealth;
        view = newView;
        sceneLoader = newSceneLoader;
        health.Died += HandleDeath;
        view.TitleButton.onClick.AddListener(ReturnToTitle);
        view.RestartButton.onClick.AddListener(RestartCurrentScene);
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.Died -= HandleDeath;
        }

        if (view != null)
        {
            view.TitleButton.onClick.RemoveListener(ReturnToTitle);
            view.RestartButton.onClick.RemoveListener(RestartCurrentScene);
        }

        if (deathStarted)
        {
            Time.timeScale = 1f;
        }
    }

    private void HandleDeath()
    {
        if (deathStarted)
        {
            return;
        }

        deathStarted = true;
        IsTransitioning = true;
        player.SetActive(false);
        Time.timeScale = 0f;
        StartCoroutine(FadeToBlack());
    }

    public void ReturnToTitle()
    {
        sceneTransition.Load(sceneLoader, GameScenes.Title);
    }

    public void RestartCurrentScene()
    {
        sceneTransition.Load(sceneLoader, sceneLoader.ActiveScenePath);
    }

    private IEnumerator FadeToBlack()
    {
        yield return ScreenFade.Run(view, FadeDuration);
        IsTransitioning = false;
    }
}
```

`Assets/Scripts/UI/PlayerDeathScreenView.cs`에서 클래스 선언 한 줄만 고친다 (`IFadeOverlay` 구현 추가, 나머지는 그대로):

```csharp
public sealed class PlayerDeathScreenView : MonoBehaviour, IFadeOverlay
```

- [ ] **Step 6: 리팩터링 후에도 `PlayerDeathScreenPlayModeTests`가 통과하는지 확인한다 (회귀 없음)**

Run:
```
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Users\myong\NaManMoo\Namanmoo_client' -runTests -testPlatform PlayMode -testFilter 'PlayerDeathScreenPlayModeTests' -testResults 'C:\Users\myong\NaManMoo\Namanmoo_client\Artifacts\deathscreen-after.xml' -logFile 'C:\Users\myong\NaManMoo\Namanmoo_client\Artifacts\deathscreen-after.log'
```
Expected: PASS

- [ ] **Step 7: `StageClearScreenView`가 `IFadeOverlay`를 구현하도록 고친다**

`Assets/Scripts/UI/StageClearScreenView.cs`에서 클래스 선언 한 줄만 고친다:

```csharp
public sealed class StageClearScreenView : MonoBehaviour, IFadeOverlay
```

- [ ] **Step 8: `StageClearScreen`을 구현한다**

`Assets/Scripts/UI/StageClearScreen.cs`:

```csharp
using System;
using System.Collections;
using NaManMoo.Dungeon;
using UnityEngine;

public sealed class StageClearScreen : MonoBehaviour
{
    public const float FadeDuration = 1f;

    [SerializeField] private DungeonRunner runner;
    private StageClearScreenView view;
    private ISceneLoader sceneLoader;
    private readonly SceneTransitionGuard sceneTransition = new SceneTransitionGuard();
    private bool clearStarted;

    public bool IsTransitioning { get; private set; }

    public void Configure(DungeonRunner dungeonRunner, StageClearScreenView screenView)
    {
        Configure(dungeonRunner, screenView, new UnitySceneLoader());
    }

    public void Configure(
        DungeonRunner dungeonRunner,
        StageClearScreenView screenView,
        ISceneLoader newSceneLoader)
    {
        if (dungeonRunner == null)
        {
            throw new ArgumentNullException(nameof(dungeonRunner));
        }

        if (screenView == null)
        {
            throw new ArgumentNullException(nameof(screenView));
        }

        if (newSceneLoader == null)
        {
            throw new ArgumentNullException(nameof(newSceneLoader));
        }

        if (runner != null)
        {
            runner.BossDefeated -= HandleBossDefeated;
        }

        runner = dungeonRunner;
        view = screenView;
        sceneLoader = newSceneLoader;
        runner.BossDefeated += HandleBossDefeated;
        view.TitleButton.onClick.AddListener(ReturnToTitle);
    }

    private void OnDestroy()
    {
        if (runner != null)
        {
            runner.BossDefeated -= HandleBossDefeated;
        }

        if (view != null)
        {
            view.TitleButton.onClick.RemoveListener(ReturnToTitle);
        }

        if (clearStarted)
        {
            Time.timeScale = 1f;
        }
    }

    private void HandleBossDefeated()
    {
        if (clearStarted)
        {
            return;
        }

        clearStarted = true;
        IsTransitioning = true;
        Time.timeScale = 0f;
        StartCoroutine(FadeToBlack());
    }

    public void ReturnToTitle()
    {
        sceneTransition.Load(sceneLoader, GameScenes.Title);
    }

    private IEnumerator FadeToBlack()
    {
        yield return ScreenFade.Run(view, FadeDuration);
        IsTransitioning = false;
    }
}
```

- [ ] **Step 9: `StageClearScreenPlayModeTests`가 통과(초록)하는지 확인한다**

Run:
```
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Users\myong\NaManMoo\Namanmoo_client' -runTests -testPlatform PlayMode -testFilter 'StageClearScreenPlayModeTests' -testResults 'C:\Users\myong\NaManMoo\Namanmoo_client\Artifacts\stageclearscreen-green.xml' -logFile 'C:\Users\myong\NaManMoo\Namanmoo_client\Artifacts\stageclearscreen-green.log'
```
Expected: PASS

- [ ] **Step 10: 커밋**

```bash
git add Assets/Scripts/UI/ISceneLoader.cs Assets/Scripts/UI/IFadeOverlay.cs Assets/Scripts/UI/ScreenFade.cs Assets/Scripts/UI/SceneTransitionGuard.cs Assets/Scripts/UI/PlayerDeathScreen.cs Assets/Scripts/UI/PlayerDeathScreenView.cs Assets/Scripts/UI/StageClearScreenView.cs Assets/Scripts/UI/StageClearScreen.cs Assets/Tests/PlayMode/StageClearScreenPlayModeTests.cs
git commit -m "feat: 씬 로더/페이드/전환 가드를 공용화하고 보스 처치 시 동작하는 StageClearScreen 추가"
```

---

## Task 5: 던전 씬에 연결

**Files:**
- Modify: `Assets/Editor/DungeonSceneBuilder.cs`
- Modify: `Assets/Scenes/Dungeon.unity` (씬 빌더 재실행으로 재생성)
- Test: `Assets/Tests/Editor/DungeonSceneBuilderTests.cs`

**Interfaces:**
- Consumes: `StageClearScreen.Configure(DungeonRunner, StageClearScreenView)`(Task 4), `StageClearScreenUIFactory.Create(Transform)`(Task 3).

- [ ] **Step 1: 실패하는 EditMode 테스트를 추가한다**

`Assets/Tests/Editor/DungeonSceneBuilderTests.cs` 끝에 추가한다:

```csharp
    [Test]
    public void Scene_HasStageClearScreenWiredToRunner()
    {
        Scene scene = EditorSceneManager.OpenScene(
            DungeonSceneBuilder.ScenePath,
            OpenSceneMode.Single);
        Assert.That(scene.IsValid, Is.True);

        DungeonRunner runner = Object.FindAnyObjectByType<DungeonRunner>();
        StageClearScreen screen = Object.FindAnyObjectByType<StageClearScreen>();
        Assert.That(runner, Is.Not.Null);
        Assert.That(screen, Is.Not.Null);

        var serialized = new SerializedObject(screen);
        Assert.That(
            serialized.FindProperty("runner").objectReferenceValue,
            Is.SameAs(runner));
    }
```

- [ ] **Step 2: 컴파일은 되지만 어서션이 실패(빨강)하는지 확인한다**

`DungeonSceneBuilder.cs`를 아직 고치지 않았으므로 씬에 `StageClearScreen`이 없어 `screen`이 `null`이어야 한다.

Run:
```
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Users\myong\NaManMoo\Namanmoo_client' -runTests -testPlatform EditMode -testFilter 'DungeonSceneBuilderTests' -testResults 'C:\Users\myong\NaManMoo\Namanmoo_client\Artifacts\dungeonscenebuilder-red.xml' -logFile 'C:\Users\myong\NaManMoo\Namanmoo_client\Artifacts\dungeonscenebuilder-red.log'
```
Expected: FAIL, `screen`이 null이라 `Is.Not.Null` 실패.

- [ ] **Step 3: `DungeonSceneBuilder`가 `StageClearScreen`을 붙이도록 고친다**

`Assets/Editor/DungeonSceneBuilder.cs`의 `Build()`에서 `CreateMinimap(runner);` 앞에 호출을 추가한다:

```csharp
        DungeonRunner runner = CreateRunner(
            player.transform,
            normalEnemyDefinitions,
            sultanBossDefinition);
        CreateStageClearScreen(runner);
        CreateMinimap(runner);
        CreateBgmDirector(bgm, runner);
```

`CreateMinimap` 메서드 앞에 새 private static 메서드를 추가한다:

```csharp
    private static void CreateStageClearScreen(DungeonRunner runner)
    {
        StageClearScreenView view = StageClearScreenUIFactory.Create(null);
        StageClearScreen screen = view.gameObject.AddComponent<StageClearScreen>();
        screen.Configure(runner, view);
    }
```

- [ ] **Step 4: 던전 씬을 다시 만든다**

`DungeonSceneBuilder.Build()`는 씬을 새로 짓고 `Assets/Scenes/Dungeon.unity`에 저장한다. 배치 모드로 재실행한다:

```
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Users\myong\NaManMoo\Namanmoo_client' -executeMethod DungeonSceneBuilder.Build -logFile 'C:\Users\myong\NaManMoo\Namanmoo_client\Artifacts\build-dungeon-scene.log'
```

이후 `git status`/`git diff --stat Assets/Scenes/Dungeon.unity`로 씬 파일이 갱신됐는지 확인한다.

- [ ] **Step 5: 테스트가 통과(초록)하는지 확인한다**

Run:
```
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Users\myong\NaManMoo\Namanmoo_client' -runTests -testPlatform EditMode -testFilter 'DungeonSceneBuilderTests' -testResults 'C:\Users\myong\NaManMoo\Namanmoo_client\Artifacts\dungeonscenebuilder-green.xml' -logFile 'C:\Users\myong\NaManMoo\Namanmoo_client\Artifacts\dungeonscenebuilder-green.log'
```
Expected: PASS (전체 `DungeonSceneBuilderTests`, 기존 테스트 포함)

- [ ] **Step 6: 커밋**

```bash
git add Assets/Editor/DungeonSceneBuilder.cs Assets/Scenes/Dungeon.unity Assets/Tests/Editor/DungeonSceneBuilderTests.cs
git commit -m "feat: 던전 씬 빌더가 StageClearScreen을 던전 러너에 연결하게 한다"
```
