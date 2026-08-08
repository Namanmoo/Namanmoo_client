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
