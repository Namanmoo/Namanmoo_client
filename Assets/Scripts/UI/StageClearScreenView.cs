using UnityEngine;
using UnityEngine.UI;

public sealed class StageClearScreenView : MonoBehaviour, IFadeOverlay
{
    // 씬 빌더가 에디터에서 Initialize로 채우고 씬에 저장된다. 자동 프로퍼티로 두면
    // 백킹 필드가 직렬화되지 않아, 저장된 씬을 실행할 때 전부 null이 되고
    // Stage Clear 화면이 조용히 사라진다.
    [SerializeField] private Image fadeOverlay;
    [SerializeField] private GameObject menu;
    [SerializeField] private Button titleButton;

    public Image FadeOverlay => fadeOverlay;
    public GameObject Menu => menu;
    public Button TitleButton => titleButton;

    public void Initialize(
        Image newFadeOverlay,
        GameObject newMenu,
        Button newTitleButton)
    {
        fadeOverlay = newFadeOverlay;
        menu = newMenu;
        titleButton = newTitleButton;

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
