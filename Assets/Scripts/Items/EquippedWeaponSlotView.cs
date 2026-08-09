using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 왼쪽 아래에 장착 중인 무기 하나만 보여주는 슬롯. 6칸 핫바를 대체한다.
/// 인벤토리 모델은 그대로 두고 화면만 한 칸으로 줄였다 —
/// 숫자키 전환과 대장간 무기 슬롯은 예전처럼 동작하고, 바뀐 결과만 여기 보인다.
/// </summary>
public sealed class EquippedWeaponSlotView : MonoBehaviour
{
    public const float SlotSize = 96f;
    public const float ScreenInset = 24f;
    public const float IconInset = 8f;

    public static readonly Color BackgroundColor = new Color(0f, 0f, 0f, 0.45f);
    public static readonly Color BorderColor = new Color(1f, 1f, 1f, 0.6f);

    // 씬에 구워졌다 다시 열려도 인벤토리를 되찾을 수 있게 컨트롤러를 직렬화한다 —
    // PlayerInventory 자체는 직렬화되지 않아 참조가 끊긴다 (ItemHotbarView와 같은 이유).
    [SerializeField] private ItemHotbarController controller;
    [SerializeField] private Image icon;

    private PlayerInventory inventory;
    private bool isSubscribed;

    public static EquippedWeaponSlotView Create(Transform parent, ItemHotbarController controller)
    {
        var slotObject = new GameObject(
            "Equipped Weapon Slot",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(EquippedWeaponSlotView));
        slotObject.transform.SetParent(parent, false);

        RectTransform rect = slotObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero; // 화면 왼쪽 아래
        rect.anchorMax = Vector2.zero;
        rect.pivot = Vector2.zero;
        rect.sizeDelta = new Vector2(SlotSize, SlotSize);
        rect.anchoredPosition = new Vector2(ScreenInset, ScreenInset);

        Image background = slotObject.GetComponent<Image>();
        background.color = BackgroundColor;
        background.raycastTarget = false;

        Outline border = slotObject.AddComponent<Outline>();
        border.effectColor = BorderColor;
        border.effectDistance = new Vector2(2f, 2f);

        var iconObject = new GameObject(
            "Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        iconObject.transform.SetParent(slotObject.transform, false);

        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.offsetMin = new Vector2(IconInset, IconInset);
        iconRect.offsetMax = new Vector2(-IconInset, -IconInset);

        Image iconImage = iconObject.GetComponent<Image>();
        iconImage.preserveAspect = true;
        iconImage.enabled = false;
        iconImage.raycastTarget = false;

        EquippedWeaponSlotView view = slotObject.GetComponent<EquippedWeaponSlotView>();
        view.controller = controller;
        view.icon = iconImage;
        view.Connect();
        return view;
    }

    /// <summary>컨트롤러에서 인벤토리를 되찾아 구독한다. 씬 로드 뒤에도 여기로 복구된다.</summary>
    public void Connect()
    {
        if (controller != null)
        {
            SetInventory(controller.Inventory);
        }

        Subscribe();
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
        Unsubscribe();
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    private void Refresh()
    {
        if (icon == null)
        {
            return;
        }

        ItemData item = inventory?.EquippedItem;
        icon.sprite = item?.Icon;
        icon.preserveAspect = true;
        icon.enabled = icon.sprite != null;
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
