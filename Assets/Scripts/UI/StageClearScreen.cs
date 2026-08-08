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
