using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 핫바 슬롯에 마우스를 올리면 뜨는 무기 정보 툴팁.
/// 이름 + 결과 화면과 같은 설명(<see cref="WeaponSummary.Describe"/>)을 쓴다 —
/// 화면마다 무기를 다르게 설명하면 어느 쪽이 진실인지 알 수 없게 된다.
/// </summary>
public sealed class WeaponTooltipView : MonoBehaviour
{
    public const float Width = 250f;
    public const float Padding = 10f;

    /// <summary>핫바 위로 띄우는 간격.</summary>
    public const float GapAboveHotbar = 8f;

    /// <summary>WebGL은 OS 폰트를 못 쓴다 — 한글은 이 폰트가 유일하다.</summary>
    public const string FontResource = "Fonts/Gaegu-Regular";

    private static readonly Color PanelColor = new Color(0.08f, 0.08f, 0.1f, 0.88f);
    private static readonly Color BodyColor = new Color(0.85f, 0.85f, 0.85f, 1f);

    private RectTransform rect;
    private Text nameText;
    private Text bodyText;

    public bool IsVisible => gameObject.activeSelf;

    public Text NameText => nameText;

    public Text BodyText => bodyText;

    /// <summary>슬롯 위에 아이템 정보를 띄운다. 빈 슬롯이면 아무것도 안 띄운다.</summary>
    public void Show(ItemData item, float slotCenterX)
    {
        if (!ShouldShow(item))
        {
            Hide();
            return;
        }

        nameText.text = item.DisplayName ?? string.Empty;
        bodyText.text = BodyFor(item);
        bodyText.gameObject.SetActive(!string.IsNullOrEmpty(bodyText.text));

        rect.anchorMin = new Vector2(slotCenterX, 1f);
        rect.anchorMax = new Vector2(slotCenterX, 1f);
        rect.anchoredPosition = new Vector2(0f, GapAboveHotbar);

        gameObject.SetActive(true);
        transform.SetAsLastSibling(); // 아이콘·테두리 위에 그려져야 한다
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    /// <summary>빈 슬롯·이름 없는 아이템은 툴팁을 띄우지 않는다.</summary>
    public static bool ShouldShow(ItemData item)
    {
        return item != null && !string.IsNullOrWhiteSpace(item.DisplayName);
    }

    /// <summary>
    /// 본문 — 무기면 결과 화면과 같은 여러 줄 설명, 그 외 아이템은 비워 둔다.
    /// </summary>
    public static string BodyFor(ItemData item)
    {
        if (item == null || item.Kind != ItemKind.Weapon || item.Loadout == null)
        {
            return string.Empty;
        }

        return WeaponSummary.Describe(item.Loadout);
    }

    /// <summary>핫바 루트 밑에 숨겨진 툴팁을 만들어 둔다.</summary>
    public static WeaponTooltipView Create(Transform hotbarRoot)
    {
        var tooltipObject = new GameObject(
            "Weapon Tooltip",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(VerticalLayoutGroup),
            typeof(ContentSizeFitter));
        tooltipObject.transform.SetParent(hotbarRoot, false);

        var tooltip = tooltipObject.AddComponent<WeaponTooltipView>();
        tooltip.rect = tooltipObject.GetComponent<RectTransform>();
        tooltip.rect.pivot = new Vector2(0.5f, 0f);
        tooltip.rect.sizeDelta = new Vector2(Width, 0f);

        Image panel = tooltipObject.GetComponent<Image>();
        panel.color = PanelColor;
        panel.raycastTarget = false; // 툴팁이 호버를 가로채면 깜빡인다

        VerticalLayoutGroup layout = tooltipObject.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(
            (int)Padding, (int)Padding, (int)Padding, (int)Padding);
        layout.spacing = 4f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = tooltipObject.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Font font = Resources.Load<Font>(FontResource);
        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        tooltip.nameText = CreateText(tooltipObject.transform, "Name", font, 20, Color.white);
        tooltip.nameText.fontStyle = FontStyle.Bold;
        tooltip.bodyText = CreateText(tooltipObject.transform, "Body", font, 16, BodyColor);

        tooltip.Hide();
        return tooltip;
    }

    private static Text CreateText(
        Transform parent, string name, Font font, int size, Color color)
    {
        var textObject = new GameObject(
            name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObject.transform.SetParent(parent, false);

        Text text = textObject.GetComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.color = color;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }
}
