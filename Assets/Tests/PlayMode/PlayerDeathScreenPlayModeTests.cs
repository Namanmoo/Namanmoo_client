using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

public sealed class PlayerDeathScreenPlayModeTests
{
    private GameObject root;

    [TearDown]
    public void TearDown()
    {
        Time.timeScale = 1f;

        PlayerDeathScreen deathScreen =
            Object.FindAnyObjectByType<PlayerDeathScreen>(FindObjectsInactive.Include);
        if (deathScreen != null &&
            (root == null || !deathScreen.transform.IsChildOf(root.transform)))
        {
            Object.DestroyImmediate(deathScreen.gameObject);
        }

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

    [UnityTest]
    public IEnumerator Death_ImmediatelyHidesPlayerPausesGameAndKeepsMenuHidden()
    {
        BuildScreen(
            out GameObject player,
            out PlayerHealth health,
            out PlayerDeathScreenView view,
            out PlayerDeathScreen screen);

        health.TryTakeDamage(20, 0f, 0f);
        yield return null;

        Assert.That(player.activeSelf, Is.False);
        Assert.That(Time.timeScale, Is.Zero);
        Assert.That(view.Menu.activeSelf, Is.False);
        Assert.That(screen.IsTransitioning, Is.True);
    }

    [UnityTest]
    public IEnumerator Death_AfterOneRealtimeSecondShowsOpaqueMenu()
    {
        BuildScreen(
            out _,
            out PlayerHealth health,
            out PlayerDeathScreenView view,
            out PlayerDeathScreen screen);

        health.TryTakeDamage(20, 0f, 0f);
        yield return new WaitForSecondsRealtime(1.05f);

        Assert.That(view.FadeOverlay.color.a, Is.EqualTo(1f).Within(0.01f));
        Assert.That(view.Menu.activeSelf, Is.True);
        Assert.That(screen.IsTransitioning, Is.False);
    }

    [UnityTest]
    public IEnumerator TitleButton_RestoresTimeAndLoadsTitleScene()
    {
        var loader = new RecordingSceneLoader(GameScenes.Stage1);
        BuildScreen(
            loader,
            out _,
            out _,
            out PlayerDeathScreenView view,
            out _);
        Time.timeScale = 0f;

        view.TitleButton.onClick.Invoke();
        yield return null;

        Assert.That(Time.timeScale, Is.EqualTo(1f));
        Assert.That(loader.LoadedScenePath, Is.EqualTo(GameScenes.Title));
    }

    [UnityTest]
    public IEnumerator RestartButton_RestoresTimeAndReloadsActiveScene()
    {
        var loader = new RecordingSceneLoader(GameScenes.Stage1);
        BuildScreen(
            loader,
            out _,
            out _,
            out PlayerDeathScreenView view,
            out _);
        Time.timeScale = 0f;

        view.RestartButton.onClick.Invoke();
        yield return null;

        Assert.That(Time.timeScale, Is.EqualTo(1f));
        Assert.That(loader.LoadedScenePath, Is.EqualTo(GameScenes.Stage1));
    }

    [UnityTest]
    public IEnumerator TaggedPlayerWithoutConfiguredScreen_CreatesOneAtRuntime()
    {
        root = new GameObject(nameof(PlayerDeathScreenPlayModeTests));
        var player = new GameObject(nameof(PlayerHealth)) { tag = "Player" };
        player.transform.SetParent(root.transform);
        player.AddComponent<PlayerHealth>();

        yield return null;

        PlayerDeathScreen screen =
            Object.FindAnyObjectByType<PlayerDeathScreen>(FindObjectsInactive.Include);
        Assert.That(screen, Is.Not.Null);
    }

    private void BuildScreen(
        out GameObject player,
        out PlayerHealth health,
        out PlayerDeathScreenView view,
        out PlayerDeathScreen screen)
    {
        root = new GameObject(nameof(PlayerDeathScreenPlayModeTests));
        player = new GameObject("Player");
        player.transform.SetParent(root.transform);
        health = player.AddComponent<PlayerHealth>();
        view = PlayerDeathScreenUIFactory.Create(root.transform);
        screen = view.gameObject.AddComponent<PlayerDeathScreen>();
        screen.Initialize(player, health, view);
    }

    private void BuildScreen(
        ISceneLoader sceneLoader,
        out GameObject player,
        out PlayerHealth health,
        out PlayerDeathScreenView view,
        out PlayerDeathScreen screen)
    {
        root = new GameObject(nameof(PlayerDeathScreenPlayModeTests));
        player = new GameObject(nameof(PlayerHealth));
        player.transform.SetParent(root.transform);
        health = player.AddComponent<PlayerHealth>();
        view = PlayerDeathScreenUIFactory.Create(root.transform);
        screen = view.gameObject.AddComponent<PlayerDeathScreen>();
        screen.Initialize(player, health, view, sceneLoader);
    }

    private sealed class RecordingSceneLoader : ISceneLoader
    {
        public RecordingSceneLoader(string activeScenePath)
        {
            ActiveScenePath = activeScenePath;
        }

        public string ActiveScenePath { get; }
        public string LoadedScenePath { get; private set; }

        public void Load(string scenePath)
        {
            LoadedScenePath = scenePath;
        }
    }
}
