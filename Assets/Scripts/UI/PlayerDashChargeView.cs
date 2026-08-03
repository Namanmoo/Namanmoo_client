using UnityEngine;
using UnityEngine.UI;

public sealed class PlayerDashChargeView : MonoBehaviour
{
    public static readonly Color AvailableColor =
        new Color(1f, 0.78f, 0.05f, 1f);
    public static readonly Color SpentColor =
        new Color(0.48f, 0.48f, 0.48f, 1f);
    public static readonly Color OutlineColor =
        new Color(0.30f, 0.30f, 0.30f, 1f);

    private const float SlotSize = 22f;
    private const float FillSize = 16f;
    private const float SlotSpacing = 8f;

    private static Sprite circleSprite;

    [SerializeField]
    private PlayerDash dash;

    private Image[] fills = System.Array.Empty<Image>();
    private bool connected;

    public void Initialize(PlayerDash newDash)
    {
        Disconnect();
        dash = newDash;
        Connect();
    }

    private void OnEnable()
    {
        Connect();
    }

    private void OnDisable()
    {
        Disconnect();
    }

    private void Connect()
    {
        if (connected || dash == null)
        {
            return;
        }

        dash.ChargesChanged += Render;
        connected = true;
        dash.NotifyChargesChanged();
    }

    private void Disconnect()
    {
        if (!connected || dash == null)
        {
            return;
        }

        dash.ChargesChanged -= Render;
        connected = false;
    }

    private void Render(int currentCharges, int maximumCharges)
    {
        if (fills.Length != maximumCharges)
        {
            Rebuild(maximumCharges);
        }

        for (int index = 0; index < fills.Length; index++)
        {
            fills[index].color = index < currentCharges
                ? AvailableColor
                : SpentColor;
        }
    }

    private void Rebuild(int count)
    {
        while (transform.childCount > 0)
        {
            Transform child = transform.GetChild(transform.childCount - 1);
            child.SetParent(null, false);
            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }

        fills = new Image[count];
        RectTransform rootRect = GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(
            count * SlotSize + Mathf.Max(0, count - 1) * SlotSpacing,
            SlotSize);

        for (int index = 0; index < count; index++)
        {
            RectTransform slot = CreateRect(transform, nameof(PlayerDash));
            slot.anchorMin = new Vector2(0f, 1f);
            slot.anchorMax = new Vector2(0f, 1f);
            slot.pivot = new Vector2(0f, 1f);
            slot.anchoredPosition = new Vector2(index * (SlotSize + SlotSpacing), 0f);
            slot.sizeDelta = new Vector2(SlotSize, SlotSize);

            Image outline = CreateCircle(slot, OutlineColor, SlotSize);
            fills[index] = CreateCircle(slot, SpentColor, FillSize);
            outline.transform.SetAsFirstSibling();
        }
    }

    private static RectTransform CreateRect(Transform parent, string objectName)
    {
        var child = new GameObject(objectName, typeof(RectTransform));
        child.transform.SetParent(parent, false);
        return child.GetComponent<RectTransform>();
    }

    private static Image CreateCircle(Transform parent, Color color, float size)
    {
        var circle = new GameObject(
            nameof(Image),
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        circle.transform.SetParent(parent, false);
        Image image = circle.GetComponent<Image>();
        image.sprite = GetCircleSprite();
        image.color = color;
        image.raycastTarget = false;

        RectTransform rect = image.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(size, size);
        return image;
    }

    private static Sprite GetCircleSprite()
    {
        if (circleSprite != null)
        {
            return circleSprite;
        }

        const int size = 32;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            name = nameof(PlayerDashChargeView)
        };

        var pixels = new Color[size * size];
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.5f - 1f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.Clamp01(radius + 1f - distance);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        circleSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            size);
        circleSprite.name = nameof(PlayerDashChargeView);
        return circleSprite;
    }
}
