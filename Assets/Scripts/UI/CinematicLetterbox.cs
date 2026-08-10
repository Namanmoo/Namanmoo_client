using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 화면 위아래에 검은 띠를 깔아 연출 구간임을 알린다. 보스 페이즈 전환처럼
/// 조작이 멈추는 순간에 쓴다.
///
/// 자기 캔버스를 직접 만든다 — 씬마다 UI 부모를 찾아 넘겨받게 하면 부르는 쪽이
/// 그 사정을 다 알아야 한다.
/// </summary>
public sealed class CinematicLetterbox : MonoBehaviour
{
    /// <summary>띠 하나가 차지하는 화면 세로 비율.</summary>
    public const float DefaultHeightRatio = 0.12f;

    /// <summary>다른 UI 위에 와야 연출로 읽힌다.</summary>
    public const int SortingOrder = 200;

    public static CinematicLetterbox Create(float heightRatio = DefaultHeightRatio)
    {
        var root = new GameObject("Cinematic Letterbox", typeof(RectTransform), typeof(Canvas));
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortingOrder;

        float ratio = Mathf.Clamp(heightRatio, 0.01f, 0.49f);
        CreateBar(root.transform, "Top Bar", new Vector2(0f, 1f - ratio), Vector2.one);
        CreateBar(root.transform, "Bottom Bar", Vector2.zero, new Vector2(1f, ratio));

        return root.AddComponent<CinematicLetterbox>();
    }

    public void Dispose()
    {
        if (this != null)
        {
            Destroy(gameObject);
        }
    }

    private static void CreateBar(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
    {
        var barObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        barObject.transform.SetParent(parent, false);

        RectTransform rect = barObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = barObject.GetComponent<Image>();
        image.color = Color.black;
        // 띠가 클릭을 먹으면 연출이 끝난 뒤에도 조작이 막힌 것처럼 느껴진다.
        image.raycastTarget = false;
    }
}
