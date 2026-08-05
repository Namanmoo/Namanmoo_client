using UnityEngine;
using UnityEngine.UI;

public static class PlayerDeathScreenRuntimeBinder
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BindSceneDeathScreen()
    {
        PlayerHealth health =
            Object.FindAnyObjectByType<PlayerHealth>(FindObjectsInactive.Include);
        PlayerDeathScreen screen =
            Object.FindAnyObjectByType<PlayerDeathScreen>(FindObjectsInactive.Include);
        TryBind(health, screen);
    }

    public static bool TryBind(PlayerHealth health, PlayerDeathScreen screen)
    {
        if (health == null || !health.CompareTag("Player") || screen == null)
        {
            return false;
        }

        PlayerDeathScreenView view =
            screen.GetComponent<PlayerDeathScreenView>();
        if (view == null)
        {
            return false;
        }

        Transform overlay = view.transform.Find("Fade Overlay");
        Transform menu = view.transform.Find("Death Menu");
        Transform titleButton =
            menu == null ? null : menu.Find("Return To Title Button");
        Transform restartButton =
            menu == null ? null : menu.Find("Restart Button");
        if (overlay == null || menu == null ||
            titleButton == null || restartButton == null)
        {
            return false;
        }

        view.Initialize(
            overlay.GetComponent<Image>(),
            menu.gameObject,
            titleButton.GetComponent<Button>(),
            restartButton.GetComponent<Button>());
        screen.Initialize(health.gameObject, health, view);
        return true;
    }
}