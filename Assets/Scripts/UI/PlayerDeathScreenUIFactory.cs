using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public static class PlayerDeathScreenUIFactory
{
    public static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);
    public const int SortingOrder = 100;

    private static readonly Color ButtonColor =
        new Color(0.92f, 0.86f, 0.7f, 1f);
    private static readonly Color Ink =
        new Color(0.12f, 0.1f, 0.09f, 1f);

    public static PlayerDeathScreenView Create(Transform parent)
    {
        var canvasObject = new GameObject(
            "Player Death Canvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(PlayerDeathScreenView));
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

        Image overlay = CreateImage(
            canvasObject.transform,
            "Fade Overlay",
            new Color(0f, 0f, 0f, 0f));
        Stretch(overlay.rectTransform);
        // 투명한 채로 살아 있는 풀스크린 이미지다 — 레이캐스트를 받으면
        // 게임 내내 모든 마우스 이벤트(핫바 툴팁 등)를 삼킨다.
        overlay.raycastTarget = false;

        var menu = new GameObject("Death Menu", typeof(RectTransform));
        menu.transform.SetParent(canvasObject.transform, false);
        RectTransform menuRect = menu.GetComponent<RectTransform>();
        menuRect.anchorMin = new Vector2(0.5f, 0.5f);
        menuRect.anchorMax = new Vector2(0.5f, 0.5f);
        menuRect.pivot = new Vector2(0.5f, 0.5f);
        menuRect.anchoredPosition = Vector2.zero;
        menuRect.sizeDelta = new Vector2(620f, 360f);

        Text message = CreateText(
            menu.transform,
            "Message",
            "이번에도 틀렸나...",
            48,
            Color.white);
        SetCenteredRect(message.rectTransform, new Vector2(0f, 100f), new Vector2(620f, 80f));

        Button titleButton = CreateButton(
            menu.transform,
            "Return To Title Button",
            "타이틀화면으로 돌아가기",
            new Vector2(0f, -10f));
        Button restartButton = CreateButton(
            menu.transform,
            "Restart Button",
            "처음부터 다시하기",
            new Vector2(0f, -105f));

        PlayerDeathScreenView view =
            canvasObject.GetComponent<PlayerDeathScreenView>();
        view.Initialize(overlay, menu, titleButton, restartButton);
        menu.SetActive(false);

        EnsureEventSystem();
        return view;
    }

    private static Button CreateButton(
        Transform parent,
        string name,
        string label,
        Vector2 position)
    {
        var buttonObject = new GameObject(
            name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.GetComponent<Image>();
        image.color = ButtonColor;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        SetCenteredRect(rect, position, new Vector2(420f, 72f));

        Text text = CreateText(buttonObject.transform, "Text", label, 30, Ink);
        Stretch(text.rectTransform);
        return button;
    }

    private static Text CreateText(
        Transform parent,
        string name,
        string content,
        int fontSize,
        Color color)
    {
        var textObject = new GameObject(
            name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Text));
        textObject.transform.SetParent(parent, false);
        Text text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = content;
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    private static Image CreateImage(
        Transform parent,
        string name,
        Color color)
    {
        var imageObject = new GameObject(
            name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
    }

    private static void SetCenteredRect(
        RectTransform rect,
        Vector2 position,
        Vector2 size)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() != null)
        {
            return;
        }

        new GameObject(
            "EventSystem",
            typeof(EventSystem),
            typeof(InputSystemUIInputModule));
    }
}
