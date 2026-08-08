using UnityEngine;
using UnityEngine.UI;

public static class PlayerDeathScreenUIFactory
{
    public static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);
    public const int SortingOrder = 100;

    public static PlayerDeathScreenView Create(Transform parent)
    {
        PlayerDeathScreenView view =
            RuntimeMenuUIFactory.CreateOverlayCanvas<PlayerDeathScreenView>(
                parent,
                "Player Death Canvas",
                ReferenceResolution,
                SortingOrder);
        GameObject canvasObject = view.gameObject;

        Image overlay = RuntimeMenuUIFactory.CreateImage(
            canvasObject.transform,
            "Fade Overlay",
            new Color(0f, 0f, 0f, 0f));
        RuntimeMenuUIFactory.Stretch(overlay.rectTransform);
        // 투명한 채로 살아 있는 풀스크린 이미지다 — 레이캐스트를 받으면
        // 게임 내내 모든 마우스 이벤트(핫바 툴팁 등)를 삼킨다.
        overlay.raycastTarget = false;

        var menu = new GameObject("Death Menu", typeof(RectTransform));
        menu.transform.SetParent(canvasObject.transform, false);
        RectTransform menuRect = menu.GetComponent<RectTransform>();
        RuntimeMenuUIFactory.SetCenteredRect(menuRect, Vector2.zero, new Vector2(620f, 360f));

        Text message = RuntimeMenuUIFactory.CreateText(
            menu.transform,
            "Message",
            "이번에도 틀렸나...",
            48,
            Color.white);
        RuntimeMenuUIFactory.SetCenteredRect(
            message.rectTransform, new Vector2(0f, 100f), new Vector2(620f, 80f));

        Button titleButton = RuntimeMenuUIFactory.CreateButton(
            menu.transform,
            "Return To Title Button",
            "타이틀화면으로 돌아가기",
            new Vector2(0f, -10f));
        Button restartButton = RuntimeMenuUIFactory.CreateButton(
            menu.transform,
            "Restart Button",
            "처음부터 다시하기",
            new Vector2(0f, -105f));

        view.Initialize(overlay, menu, titleButton, restartButton);
        menu.SetActive(false);

        RuntimeMenuUIFactory.EnsureEventSystem();
        return view;
    }
}
