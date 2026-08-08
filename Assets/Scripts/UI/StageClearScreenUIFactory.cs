using UnityEngine;
using UnityEngine.UI;

public static class StageClearScreenUIFactory
{
    public static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);
    public const int SortingOrder = 100;

    public static StageClearScreenView Create(Transform parent)
    {
        var canvasObject = new GameObject(
            "Stage Clear Canvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(StageClearScreenView));
        if (parent != null)
        {
            canvasObject.transform.SetParent(parent, false);
        }

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortingOrder;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = ReferenceResolution;
        scaler.matchWidthOrHeight = 0.5f;

        Image overlay = RuntimeMenuUIFactory.CreateImage(
            canvasObject.transform,
            "Fade Overlay",
            new Color(0f, 0f, 0f, 0f));
        RuntimeMenuUIFactory.Stretch(overlay.rectTransform);
        overlay.raycastTarget = false;

        var menu = new GameObject("Stage Clear Menu", typeof(RectTransform));
        menu.transform.SetParent(canvasObject.transform, false);
        RectTransform menuRect = menu.GetComponent<RectTransform>();
        menuRect.anchorMin = new Vector2(0.5f, 0.5f);
        menuRect.anchorMax = new Vector2(0.5f, 0.5f);
        menuRect.pivot = new Vector2(0.5f, 0.5f);
        menuRect.anchoredPosition = Vector2.zero;
        menuRect.sizeDelta = new Vector2(620f, 220f);

        Text message = RuntimeMenuUIFactory.CreateText(
            menu.transform,
            "Message",
            "Stage Clear!",
            48,
            Color.white);
        RuntimeMenuUIFactory.SetCenteredRect(
            message.rectTransform, new Vector2(0f, 55f), new Vector2(620f, 80f));

        Button titleButton = RuntimeMenuUIFactory.CreateButton(
            menu.transform,
            "Return To Title Button",
            "타이틀화면으로 돌아가기",
            new Vector2(0f, -40f));

        StageClearScreenView view =
            canvasObject.GetComponent<StageClearScreenView>();
        view.Initialize(overlay, menu, titleButton);
        menu.SetActive(false);

        RuntimeMenuUIFactory.EnsureEventSystem();
        return view;
    }
}
