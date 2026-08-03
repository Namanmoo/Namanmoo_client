using UnityEngine;
using UnityEngine.UI;

public sealed class PlayerDeathScreenView : MonoBehaviour
{
    public Image FadeOverlay { get; private set; }
    public GameObject Menu { get; private set; }
    public Button TitleButton { get; private set; }
    public Button RestartButton { get; private set; }

    public void Initialize(
        Image fadeOverlay,
        GameObject menu,
        Button titleButton,
        Button restartButton)
    {
        FadeOverlay = fadeOverlay;
        Menu = menu;
        TitleButton = titleButton;
        RestartButton = restartButton;
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
