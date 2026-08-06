using UnityEngine;
using UnityEngine.UI;

public sealed class ItemHotbarView : MonoBehaviour
{
    public const int SlotCount = 6;
    public const float SourceWidth = 2170f;
    public const float SourceHeight = 725f;
    public const float BackgroundWidth = 432f;
    public const float BackgroundHeight = 144.3318f;
    public const float BorderThickness = 1f;
    public const float SelectionInset = 2f;
    public const float IconInset = 4f;

    public static readonly Color SelectionBlue = new Color(0.08f, 0.4f, 0.9f, 1f);
    public static readonly Rect[] SlotOverlayRects =
    {
        Rect.MinMaxRect(50f / SourceWidth, (SourceHeight - 535f) / SourceHeight, 410f / SourceWidth, (SourceHeight - 275f) / SourceHeight),
        Rect.MinMaxRect(450f / SourceWidth, (SourceHeight - 535f) / SourceHeight, 785f / SourceWidth, (SourceHeight - 275f) / SourceHeight),
        Rect.MinMaxRect(820f / SourceWidth, (SourceHeight - 535f) / SourceHeight, 1135f / SourceWidth, (SourceHeight - 275f) / SourceHeight),
        Rect.MinMaxRect(1185f / SourceWidth, (SourceHeight - 535f) / SourceHeight, 1455f / SourceWidth, (SourceHeight - 275f) / SourceHeight),
        Rect.MinMaxRect(1500f / SourceWidth, (SourceHeight - 535f) / SourceHeight, 1770f / SourceWidth, (SourceHeight - 275f) / SourceHeight),
        Rect.MinMaxRect(1815f / SourceWidth, (SourceHeight - 535f) / SourceHeight, 2070f / SourceWidth, (SourceHeight - 275f) / SourceHeight)
    };

    [SerializeField] private ItemHotbarController controller;
    [SerializeField] private Image[] icons;
    [SerializeField] private GameObject[] selectionOutlines;

    private PlayerInventory inventory;
    private bool isSubscribed;
    private WeaponTooltipView tooltip;

    public void Initialize(PlayerInventory newInventory)
    {
        controller = null;
        SetInventory(newInventory);
        Refresh();
    }

    public void Initialize(
        PlayerInventory newInventory,
        Image[] newIcons,
        GameObject[] newSelectionOutlines)
    {
        icons = newIcons;
        selectionOutlines = newSelectionOutlines;
        Initialize(newInventory);
    }

    public void Initialize(
        ItemHotbarController newController,
        Image[] newIcons,
        GameObject[] newSelectionOutlines)
    {
        controller = newController;
        icons = newIcons;
        selectionOutlines = newSelectionOutlines;
        ConnectToController();
        Refresh();
    }

    private void Awake()
    {
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

    private void OnDestroy()
    {
        Disconnect();
    }

    public void Connect()
    {
        ConnectToController();
        Subscribe();
        Refresh();
    }

    /// <summary>
    /// 슬롯 호버 툴팁을 붙인다. 팩토리로 만든 핫바든 씬에 저장된 핫바든
    /// 여기서 소급 적용된다 — 씬 쪽만 빠뜨리는 사고를 막는다.
    /// </summary>
    private void EnsureTooltip()
    {
        if (tooltip != null || icons == null || icons.Length < SlotCount)
        {
            return;
        }

        // 캔버스에 레이캐스터가 없으면 호버 이벤트가 아예 안 닿는다
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.GetComponent<GraphicRaycaster>() == null)
        {
            canvas.gameObject.AddComponent<GraphicRaycaster>();
        }

        tooltip = WeaponTooltipView.Create(transform);

        for (int index = 0; index < SlotCount; index++)
        {
            if (icons[index] == null)
            {
                continue;
            }

            Transform slot = icons[index].transform.parent;
            if (slot == null)
            {
                continue;
            }

            var slotImage = slot.GetComponent<Image>();
            if (slotImage != null)
            {
                slotImage.raycastTarget = true; // 투명해도 호버 판정은 받는다
            }

            var hover = slot.GetComponent<HotbarSlotHover>();
            if (hover == null)
            {
                hover = slot.gameObject.AddComponent<HotbarSlotHover>();
            }

            hover.Configure(this, tooltip, index);
        }
    }

    public void Disconnect()
    {
        Unsubscribe();
    }

    /// <summary>슬롯의 아이템 — 비었거나 범위 밖이면 null. 툴팁이 쓴다.</summary>
    public ItemData ItemAt(int index)
    {
        return inventory != null && index >= 0 && index < inventory.Slots.Count
            ? inventory.Slots[index]
            : null;
    }

    private void Refresh()
    {
        if (icons == null ||
            selectionOutlines == null ||
            icons.Length < SlotCount ||
            selectionOutlines.Length < SlotCount)
        {
            return;
        }

        // 팩토리 경로는 Awake 때 아이콘이 없어 여기서야 붙일 수 있다 — 멱등이라 비용 없음
        EnsureTooltip();

        int selectedIndex = inventory != null && inventory.SelectedSlotIndex >= 0 && inventory.SelectedSlotIndex < SlotCount
            ? inventory.SelectedSlotIndex
            : 0;

        for (int index = 0; index < SlotCount; index++)
        {
            ItemData item = inventory != null && index < inventory.Slots.Count ? inventory.Slots[index] : null;
            Image icon = icons[index];
            GameObject selectionOutline = selectionOutlines[index];

            if (icon == null || selectionOutline == null)
            {
                continue;
            }

            icon.sprite = item == null ? null : item.Icon;
            icon.preserveAspect = true;
            icon.enabled = icon.sprite != null;

            selectionOutline.SetActive(index == selectedIndex);
        }
    }

    private void ConnectToController()
    {
        if (controller != null)
        {
            SetInventory(controller.Inventory);
        }
    }

    private void SetInventory(PlayerInventory newInventory)
    {
        if (ReferenceEquals(inventory, newInventory))
        {
            Subscribe();
            return;
        }

        Unsubscribe();
        inventory = newInventory;
        Subscribe();
    }

    private void Subscribe()
    {
        if (inventory == null || isSubscribed)
        {
            return;
        }

        inventory.StateChanged += Refresh;
        isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (inventory != null && isSubscribed)
        {
            inventory.StateChanged -= Refresh;
            isSubscribed = false;
        }
    }
}
