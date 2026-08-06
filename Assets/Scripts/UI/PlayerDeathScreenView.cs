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

        if (FadeOverlay != null)
        {
            // 투명한 채로 항상 살아 있는 풀스크린 이미지다 — 레이캐스트를 받으면
            // 게임 내내 모든 마우스 이벤트(핫바 툴팁 등)를 삼킨다.
            // 씬에 raycastTarget=true로 구워진 옛 사망 화면도 여기서 소급 교정된다.
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
