using System;
using UnityEngine;
using UnityEngine.UI;

public static class BossHealthBarUIFactory
{
    public static BossHealthBarView Create(Transform parent, EnemyHealth health)
    {
        if (health == null)
        {
            throw new ArgumentNullException(nameof(health));
        }

        var root = new GameObject(
            "Boss Health Bar",
            typeof(RectTransform),
            typeof(BossHealthBarView));
        root.transform.SetParent(parent, false);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 1f);
        rootRect.anchorMax = new Vector2(0.5f, 1f);
        rootRect.pivot = new Vector2(0.5f, 1f);
        rootRect.anchoredPosition = new Vector2(0f, -24f);
        rootRect.sizeDelta = new Vector2(520f, 64f);

        Image track = CreateImage(
            root.transform,
            "Boss Bar Track",
            new Color(0.13f, 0.11f, 0.1f, 1f));
        SetRect(track.rectTransform, new Vector2(0f, -26f), new Vector2(520f, 38f));

        Image fill = CreateImage(
            track.transform,
            "Fill",
            new Color(0.78f, 0.08f, 0.1f, 1f));
        fill.rectTransform.anchorMin = Vector2.zero;
        fill.rectTransform.anchorMax = Vector2.one;
        fill.rectTransform.offsetMin = new Vector2(4f, 4f);
        fill.rectTransform.offsetMax = new Vector2(-4f, -4f);

        Text label = CreateText(root.transform);
        SetRect(label.rectTransform, new Vector2(0f, 0f), new Vector2(520f, 28f));

        BossHealthBarView view = root.GetComponent<BossHealthBarView>();
        view.Initialize(health, label, fill);
        return view;
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

    private static Text CreateText(Transform parent)
    {
        var textObject = new GameObject(
            "Boss Health Text",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Text));
        textObject.transform.SetParent(parent, false);
        Text text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 24;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(0.13f, 0.11f, 0.1f, 1f);
        text.raycastTarget = false;
        return text;
    }

    private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}
