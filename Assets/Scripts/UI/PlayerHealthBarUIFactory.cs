using System;
using UnityEngine;
using UnityEngine.UI;

public static class PlayerHealthBarUIFactory
{
    public static readonly Vector2 Size = new Vector2(260f, 54f);
    public static readonly Vector2 TopLeftInset = new Vector2(24f, -24f);
    public static readonly Color Cream = new Color(1f, 0.965f, 0.84f, 1f);
    public static readonly Color Ink = new Color(0.13f, 0.11f, 0.1f, 1f);
    public static readonly Color HealthRed = new Color(0.88f, 0.12f, 0.16f, 1f);

    public static PlayerHealthBarView Create(
        Transform parent,
        PlayerHealth health,
        Sprite heartSprite)
    {
        if (health == null)
        {
            throw new ArgumentNullException(nameof(health));
        }

        if (heartSprite == null)
        {
            throw new ArgumentNullException(nameof(heartSprite));
        }

        var root = new GameObject(
            "Player Health Bar",
            typeof(RectTransform),
            typeof(PlayerHealthBarView));
        root.transform.SetParent(parent, false);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0f, 1f);
        rootRect.anchorMax = new Vector2(0f, 1f);
        rootRect.pivot = new Vector2(0f, 1f);
        rootRect.anchoredPosition = TopLeftInset;
        rootRect.sizeDelta = Size;

        Image heart = CreateImage(root.transform, "Heart", Color.white);
        heart.sprite = heartSprite;
        heart.preserveAspect = true;
        SetRect(heart.rectTransform, new Vector2(0f, -3f), new Vector2(48f, 48f));

        Text label = CreateText(root.transform);
        SetRect(label.rectTransform, new Vector2(52f, -10f), new Vector2(66f, 34f));

        Image track = CreateImage(root.transform, "Bar Track", Cream);
        SetRect(track.rectTransform, new Vector2(120f, -14f), new Vector2(140f, 27f));

        Image fill = CreateImage(track.transform, "Fill", HealthRed);
        RectTransform fillRect = fill.rectTransform;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(4f, 4f);
        fillRect.offsetMax = new Vector2(-4f, -4f);
        fill.type = Image.Type.Simple;

        CreateBorder(track.transform);

        PlayerHealthBarView view = root.GetComponent<PlayerHealthBarView>();
        view.Initialize(health, label, fill);
        return view;
    }

    private static Text CreateText(Transform parent)
    {
        var textObject = new GameObject(
            "Health Text",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Text));
        textObject.transform.SetParent(parent, false);
        Text text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 22;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Ink;
        text.raycastTarget = false;
        return text;
    }

    private static Image CreateImage(Transform parent, string name, Color color)
    {
        var imageObject = new GameObject(
            name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void CreateBorder(Transform parent)
    {
        CreateBorderEdge(parent, "Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 3f));
        CreateBorderEdge(parent, "Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 3f));
        CreateBorderEdge(parent, "Left", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(3f, 0f));
        CreateBorderEdge(parent, "Right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(3f, 0f));
    }

    private static void CreateBorderEdge(
        Transform parent,
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 size)
    {
        Image edge = CreateImage(parent, name, Ink);
        RectTransform rect = edge.rectTransform;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;
    }
}
