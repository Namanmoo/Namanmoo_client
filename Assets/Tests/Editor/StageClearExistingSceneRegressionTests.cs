using System;
using System.Collections;
using System.Reflection;
using NaManMoo.Dungeon;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// 저장된 Dungeon.unity를 실제로 열고 플레이해서 Stage Clear 화면이 살아 있는지 본다.
/// 에디터에서 만든 뒤 씬에 구운 참조가 직렬화를 넘어오지 못하면
/// 보스를 잡아도 아무 일이 없다 — 그 회귀를 여기서 잡는다.
/// </summary>
public sealed class StageClearExistingSceneRegressionTests
{
    [TearDown]
    public void TearDown()
    {
        Time.timeScale = 1f;
    }

    [UnityTest]
    public IEnumerator ExistingScene_BossDefeatPausesGameAndShowsStageClearMenu()
    {
        EditorSceneManager.OpenScene(DungeonSceneBuilder.ScenePath);
        yield return new EnterPlayMode();

        DungeonRunner runner = UnityEngine.Object.FindAnyObjectByType<DungeonRunner>();
        StageClearScreen screen = UnityEngine.Object.FindAnyObjectByType<StageClearScreen>();
        StageClearScreenView view =
            UnityEngine.Object.FindAnyObjectByType<StageClearScreenView>();
        Assert.That(runner, Is.Not.Null, "씬에 DungeonRunner가 없다");
        Assert.That(screen, Is.Not.Null, "씬에 StageClearScreen이 없다");
        Assert.That(view, Is.Not.Null, "씬에 StageClearScreenView가 없다");
        Assert.That(view.Menu, Is.Not.Null, "뷰의 Menu 참조가 직렬화되지 않았다");
        Assert.That(view.TitleButton, Is.Not.Null, "뷰의 TitleButton 참조가 직렬화되지 않았다");

        // 무작위 시드 던전을 실제로 걸어 보스방까지 가는 건 이 테스트에서 비현실적이다.
        // 이벤트를 직접 쏴서 "보스를 잡았다"만 재현한다.
        FieldInfo field = typeof(DungeonRunner).GetField(
            "BossDefeated",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.That(field, Is.Not.Null, "BossDefeated 백킹 필드를 찾지 못했다");

        FieldInfo screenRunnerField = typeof(StageClearScreen).GetField(
            "runner", BindingFlags.NonPublic | BindingFlags.Instance);
        FieldInfo screenViewField = typeof(StageClearScreen).GetField(
            "view", BindingFlags.NonPublic | BindingFlags.Instance);
        var screenRunner = (DungeonRunner)screenRunnerField.GetValue(screen);
        var screenView = (StageClearScreenView)screenViewField.GetValue(screen);
        int runnerCount =
            UnityEngine.Object.FindObjectsByType<DungeonRunner>(
                FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
        int screenCount =
            UnityEngine.Object.FindObjectsByType<StageClearScreen>(
                FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
        string diag =
            $"screenRunner={(screenRunner == null ? "null" : screenRunner.name)} " +
            $"sameRunner={ReferenceEquals(screenRunner, runner)} " +
            $"screenView={(screenView == null ? "null" : screenView.name)} " +
            $"screenBtn={(screenView == null || screenView.TitleButton == null ? "null" : "ok")} " +
            $"runnerCount={runnerCount} screenCount={screenCount} " +
            $"screenActive={screen.isActiveAndEnabled}";
        Debug.Log($"[DIAG] {diag}");

        var handler = (Action)field.GetValue(runner);
        Assert.That(
            handler,
            Is.Not.Null,
            "씬에서 살아난 StageClearScreen이 BossDefeated를 구독하지 않았다 :: " + diag);
        handler.Invoke();
        yield return null;

        Assert.That(Time.timeScale, Is.Zero, "보스 처치 후 게임이 멈추지 않았다");
        Assert.That(view.Menu.activeSelf, Is.False, "페이드 전에 메뉴가 먼저 떴다");

        yield return new WaitForSecondsRealtime(1.05f);

        Assert.That(view.FadeOverlay.color.a, Is.EqualTo(1f).Within(0.01f));
        Assert.That(view.Menu.activeSelf, Is.True, "페이드 후 Stage Clear 메뉴가 뜨지 않았다");

        Time.timeScale = 1f;
        yield return new ExitPlayMode();
    }
}
