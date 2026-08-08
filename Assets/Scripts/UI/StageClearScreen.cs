using System;
using System.Collections;
using NaManMoo.Dungeon;
using UnityEngine;

public sealed class StageClearScreen : MonoBehaviour
{
    public const float FadeDuration = 1f;

    // 씬 빌더가 넣고 씬에 저장된다. [SerializeField]가 없으면 저장 뒤 실행할 때
    // null이 되어 보스를 잡아도 아무 일도 일어나지 않는다.
    [SerializeField] private DungeonRunner runner;
    [SerializeField] private StageClearScreenView view;

    // 인터페이스라 직렬화되지 않는다. 씬에서 살아난 경우 구독 시점에 기본 로더를 채운다.
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

        // OnEnable은 AddComponent 직후, 즉 Configure보다 먼저 한 번 돌고 만다.
        // 그때는 필드가 비어 있어 아무것도 못 하므로 여기서 다시 구독한다.
        Subscribe();
    }

    private void OnEnable()
    {
        // 저장된 씬을 실행하는 경로에서는 Configure가 불리지 않는다.
        // 직렬화된 필드가 이미 채워져 있으므로 여기서 구독해야 한다.
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    /// <summary>
    /// 두 번 불려도 안전하다. 이미 걸린 핸들러를 떼고 다시 건다.
    /// </summary>
    private void Subscribe()
    {
        if (runner == null || view == null || view.TitleButton == null)
        {
            return;
        }

        runner.BossDefeated -= HandleBossDefeated;
        runner.BossDefeated += HandleBossDefeated;
        view.TitleButton.onClick.RemoveListener(ReturnToTitle);
        view.TitleButton.onClick.AddListener(ReturnToTitle);
    }

    private void Unsubscribe()
    {
        if (runner != null)
        {
            runner.BossDefeated -= HandleBossDefeated;
        }

        if (view != null && view.TitleButton != null)
        {
            view.TitleButton.onClick.RemoveListener(ReturnToTitle);
        }
    }

    private void OnDestroy()
    {
        Unsubscribe();

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
        // 씬에서 살아난 경로에는 Configure가 없어 로더가 비어 있다.
        sceneLoader ??= new UnitySceneLoader();
        sceneTransition.Load(sceneLoader, GameScenes.Title);
    }

    private IEnumerator FadeToBlack()
    {
        yield return ScreenFade.Run(view, FadeDuration);
        IsTransitioning = false;
    }
}
