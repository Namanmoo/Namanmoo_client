using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// 전체 화면 오버레이 메뉴(사망 화면·Stage Clear 화면 등)가 함께 쓰는
/// uGUI 조립 도우미. 화면이 늘어날 때마다 캔버스·버튼·텍스트 조립 코드를
/// 복사하지 않도록 여기 한곳에 모아 둔다.
/// </summary>
public static class RuntimeMenuUIFactory
{
    public static readonly Color ButtonColor = new Color(0.92f, 0.86f, 0.7f, 1f);
    public static readonly Color Ink = new Color(0.12f, 0.1f, 0.09f, 1f);

    /// <summary>WebGL은 OS 폰트를 못 쓴다 — 한글은 이 폰트가 유일하다.</summary>
    public const string FontResource = "Fonts/Gaegu-Regular";

    /// <summary>
    /// 전체 화면 오버레이용 Canvas를 만들고 <typeparamref name="TView"/>를 붙여 돌려준다.
    /// </summary>
    public static TView CreateOverlayCanvas<TView>(
        Transform parent,
        string name,
        Vector2 referenceResolution,
        int sortingOrder) where TView : Component
    {
        var canvasObject = new GameObject(
            name,
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(TView));
        if (parent != null)
        {
            canvasObject.transform.SetParent(parent, false);
        }

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = referenceResolution;
        scaler.matchWidthOrHeight = 0.5f;

        return canvasObject.GetComponent<TView>();
    }

    public static Button CreateButton(
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

    public static Text CreateText(
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
        Font font = Resources.Load<Font>(FontResource);
        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        text.font = font;
        text.text = content;
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    public static Image CreateImage(
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

    public static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
    }

    public static void SetCenteredRect(
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

    public static void EnsureEventSystem()
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
