using UnityEngine;
using UnityEngine.UI;

public static class ItemHotbarUIFactory
{
    public static ItemHotbarView Create(Transform parent, PlayerInventory inventory, Sprite backgroundSprite)
    {
        if (backgroundSprite == null)
        {
            throw new System.ArgumentNullException(nameof(backgroundSprite));
        }

        return Create(parent, inventory, null, backgroundSprite);
    }

    public static ItemHotbarView Create(Transform parent, ItemHotbarController controller, Sprite backgroundSprite)
    {
        if (backgroundSprite == null)
        {
            throw new System.ArgumentNullException(nameof(backgroundSprite));
        }

        return Create(parent, controller.Inventory, controller, backgroundSprite);
    }

    private static ItemHotbarView Create(
        Transform parent,
        PlayerInventory inventory,
        ItemHotbarController controller,
        Sprite backgroundSprite)
    {
        var hotbarObject = new GameObject("Item Hotbar", typeof(RectTransform), typeof(ItemHotbarView));
        hotbarObject.transform.SetParent(parent, false);

        RectTransform hotbarRect = hotbarObject.GetComponent<RectTransform>();
        hotbarRect.anchorMin = new Vector2(0.5f, 0f);
        hotbarRect.anchorMax = new Vector2(0.5f, 0f);
        hotbarRect.pivot = new Vector2(0.5f, 0f);
        hotbarRect.sizeDelta = new Vector2(ItemHotbarView.BackgroundWidth, ItemHotbarView.BackgroundHeight);
        hotbarRect.anchoredPosition = Vector2.zero;

        CreateBackground(hotbarObject.transform, backgroundSprite);

        var icons = new Image[ItemHotbarView.SlotCount];
        var selectionOutlines = new GameObject[ItemHotbarView.SlotCount];
        for (int index = 0; index < ItemHotbarView.SlotCount; index++)
        {
            CreateSlot(hotbarObject.transform, index, icons, selectionOutlines);
        }

        ItemHotbarView view = hotbarObject.GetComponent<ItemHotbarView>();
        if (controller == null)
        {
            view.Initialize(inventory, icons, selectionOutlines);
        }
        else
        {
            view.Initialize(controller, icons, selectionOutlines);
        }

        return view;
    }

    private static void CreateBackground(Transform parent, Sprite backgroundSprite)
    {
        var backgroundObject = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        backgroundObject.transform.SetParent(parent, false);

        RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;

        Image background = backgroundObject.GetComponent<Image>();
        background.sprite = backgroundSprite;
        background.preserveAspect = true;
        background.raycastTarget = false;
    }

    private static void CreateSlot(Transform parent, int index, Image[] icons, GameObject[] selectionOutlines)
    {
        var slotObject = new GameObject("Slot " + (index + 1), typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        slotObject.transform.SetParent(parent, false);

        RectTransform slotRect = slotObject.GetComponent<RectTransform>();
        Rect slotOverlayRect = ItemHotbarView.SlotOverlayRects[index];
        slotRect.anchorMin = slotOverlayRect.min;
        slotRect.anchorMax = slotOverlayRect.max;
        slotRect.pivot = new Vector2(0.5f, 0.5f);
        slotRect.sizeDelta = Vector2.zero;
        slotRect.anchoredPosition = Vector2.zero;

        Image slotImage = slotObject.GetComponent<Image>();
        slotImage.color = Color.clear;
        slotImage.raycastTarget = false;

        icons[index] = CreateIcon(slotObject.transform);
        selectionOutlines[index] = CreateSelectionOutline(slotObject.transform);
    }

    private static Image CreateIcon(Transform parent)
    {
        var iconObject = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        iconObject.transform.SetParent(parent, false);

        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.offsetMin = new Vector2(ItemHotbarView.IconInset, ItemHotbarView.IconInset);
        iconRect.offsetMax = new Vector2(-ItemHotbarView.IconInset, -ItemHotbarView.IconInset);

        Image icon = iconObject.GetComponent<Image>();
        icon.preserveAspect = true;
        icon.enabled = false;
        icon.raycastTarget = false;
        return icon;
    }

    private static GameObject CreateSelectionOutline(Transform parent)
    {
        var outlineObject = new GameObject("Selection Outline", typeof(RectTransform));
        outlineObject.transform.SetParent(parent, false);

        RectTransform outlineRect = outlineObject.GetComponent<RectTransform>();
        outlineRect.anchorMin = Vector2.zero;
        outlineRect.anchorMax = Vector2.one;
        outlineRect.offsetMin = new Vector2(ItemHotbarView.SelectionInset, ItemHotbarView.SelectionInset);
        outlineRect.offsetMax = new Vector2(-ItemHotbarView.SelectionInset, -ItemHotbarView.SelectionInset);

        CreateBorderEdges(outlineObject.transform, ItemHotbarView.SelectionBlue);
        return outlineObject;
    }

    private static void CreateBorderEdges(Transform parent, Color color)
    {
        CreateHorizontalEdge(parent, "Top", 1f, color);
        CreateHorizontalEdge(parent, "Bottom", 0f, color);
        CreateVerticalEdge(parent, "Left", 0f, color);
        CreateVerticalEdge(parent, "Right", 1f, color);
    }

    private static void CreateHorizontalEdge(Transform parent, string name, float anchorY, Color color)
    {
        Image edge = CreateEdge(parent, name, color);
        RectTransform rect = edge.rectTransform;
        rect.anchorMin = new Vector2(0f, anchorY);
        rect.anchorMax = new Vector2(1f, anchorY);
        rect.pivot = new Vector2(0.5f, anchorY);
        rect.sizeDelta = new Vector2(0f, ItemHotbarView.BorderThickness);
        rect.anchoredPosition = Vector2.zero;
    }

    private static void CreateVerticalEdge(Transform parent, string name, float anchorX, Color color)
    {
        Image edge = CreateEdge(parent, name, color);
        RectTransform rect = edge.rectTransform;
        rect.anchorMin = new Vector2(anchorX, 0f);
        rect.anchorMax = new Vector2(anchorX, 1f);
        rect.pivot = new Vector2(anchorX, 0.5f);
        rect.sizeDelta = new Vector2(ItemHotbarView.BorderThickness, 0f);
        rect.anchoredPosition = Vector2.zero;
    }

    private static Image CreateEdge(Transform parent, string name, Color color)
    {
        var edgeObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        edgeObject.transform.SetParent(parent, false);

        Image edge = edgeObject.GetComponent<Image>();
        edge.color = color;
        edge.raycastTarget = false;
        return edge;
    }
}
