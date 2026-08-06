using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 핫바 슬롯 하나의 마우스 호버 — 들어오면 그 칸의 무기 툴팁을 띄우고
/// 나가면 감춘다. 이벤트가 닿으려면 슬롯 이미지가 raycastTarget이어야
/// 하고 캔버스에 GraphicRaycaster가 있어야 한다 (팩토리가 챙긴다).
/// </summary>
public sealed class HotbarSlotHover : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler
{
    private ItemHotbarView view;
    private WeaponTooltipView tooltip;
    private int slotIndex;

    public void Configure(
        ItemHotbarView hotbarView, WeaponTooltipView tooltipView, int index)
    {
        view = hotbarView;
        tooltip = tooltipView;
        slotIndex = index;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (view == null || tooltip == null)
        {
            return;
        }

        tooltip.Show(view.ItemAt(slotIndex), SlotCenterX(slotIndex));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (tooltip != null)
        {
            tooltip.Hide();
        }
    }

    /// <summary>슬롯 중심의 가로 앵커(0~1) — 툴팁이 그 칸 바로 위에 뜨게.</summary>
    public static float SlotCenterX(int index)
    {
        if (index < 0 || index >= ItemHotbarView.SlotOverlayRects.Length)
        {
            return 0.5f;
        }

        return ItemHotbarView.SlotOverlayRects[index].center.x;
    }
}
