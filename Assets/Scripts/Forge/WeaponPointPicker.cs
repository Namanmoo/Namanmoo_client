using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 그림 위를 누르거나 끈 자리를 정규화 좌표(0~1, 왼쪽 아래 원점)로 알려 준다.
/// 무기고 수정 화면이 기준점(그립·중심·끝)을 옮기는 데 쓴다 —
/// DrawingCanvas와 달리 칠하지 않고 자리만 읽는다.
/// </summary>
public sealed class WeaponPointPicker : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    /// <summary>누른 자리 (0~1, 왼쪽 아래 원점). 밖으로 끌면 가장자리에 붙인다.</summary>
    public event System.Action<Vector2> Picked;

    public void OnPointerDown(PointerEventData eventData) => Report(eventData);

    public void OnDrag(PointerEventData eventData) => Report(eventData);

    private void Report(PointerEventData eventData)
    {
        var rectTransform = (RectTransform)transform;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 local))
        {
            return;
        }

        Rect rect = rectTransform.rect;
        Picked?.Invoke(new Vector2(
            Mathf.Clamp01((local.x - rect.xMin) / rect.width),
            Mathf.Clamp01((local.y - rect.yMin) / rect.height)));
    }
}
