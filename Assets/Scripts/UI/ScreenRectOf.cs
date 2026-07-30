using UnityEngine;

/// <summary>
/// UI 요소가 화면에서 차지하는 자리를 <b>0~1로 정규화해</b> 구한다.
///
/// 웹 입력창을 그 자리에 겹치려면 좌표가 필요한데, 픽셀로 넘기면 맞지 않는다.
/// 유니티가 아는 화면 크기와 브라우저의 canvas 요소 CSS 크기가 다르기 때문이다
/// (고해상도 화면의 devicePixelRatio, 그리고 우리 템플릿의 16:9 레터박싱).
/// 0~1로 넘기면 브라우저 쪽에서 자기 실제 크기에 곱하면 되므로 둘 다 저절로 맞는다.
///
/// 순수 계산이라 EditMode 테스트로 덮는다.
/// </summary>
public static class ScreenRectOf
{
    /// <summary>
    /// 화면 픽셀 사각형을 0~1로. <paramref name="screenSize"/>는 유니티가 보는 화면 크기다.
    /// 좌표계는 유니티 그대로(왼쪽 아래가 0,0) 둔다 — 뒤집는 일은 브라우저 쪽에서 한다.
    /// </summary>
    public static Rect Normalize(Rect pixels, Vector2 screenSize)
    {
        if (screenSize.x <= 0f || screenSize.y <= 0f)
        {
            return new Rect(0f, 0f, 0f, 0f);
        }

        return new Rect(
            pixels.x / screenSize.x,
            pixels.y / screenSize.y,
            pixels.width / screenSize.x,
            pixels.height / screenSize.y);
    }

    /// <summary>
    /// RectTransform이 화면에서 차지하는 픽셀 사각형.
    ///
    /// 네 모서리를 다 보고 최소·최대를 취한다. 회전이나 음수 스케일이 걸려 있으면
    /// 모서리 순서가 뒤바뀌어, 두 점만 보면 폭이 음수가 된다.
    /// </summary>
    public static Rect PixelsOf(RectTransform rect, Camera camera)
    {
        if (rect == null)
        {
            return new Rect(0f, 0f, 0f, 0f);
        }

        var corners = new Vector3[4];
        rect.GetWorldCorners(corners);

        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;

        for (int i = 0; i < corners.Length; i++)
        {
            // Overlay 캔버스는 월드 좌표가 이미 화면 픽셀이라 카메라가 없다(null)
            Vector2 point = camera == null
                ? new Vector2(corners[i].x, corners[i].y)
                : RectTransformUtility.WorldToScreenPoint(camera, corners[i]);

            minX = Mathf.Min(minX, point.x);
            maxX = Mathf.Max(maxX, point.x);
            minY = Mathf.Min(minY, point.y);
            maxY = Mathf.Max(maxY, point.y);
        }

        return Rect.MinMaxRect(minX, minY, maxX, maxY);
    }

    /// <summary>캔버스 종류를 보고 알맞은 카메라를 고른다. Overlay면 null이다.</summary>
    public static Camera CameraFor(Canvas canvas)
    {
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }

        return canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
    }
}
