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
