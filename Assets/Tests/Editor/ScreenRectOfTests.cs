using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 웹 입력창을 칸 자리에 겹치려면 이 계산이 맞아야 한다. 어긋나면 입력창이 엉뚱한
/// 곳에 뜨는데, 브라우저에서만 보이는 종류라 여기서 최대한 덮는다.
/// </summary>
public sealed class ScreenRectOfTests
{
    private static readonly Vector2 Screen1080 = new Vector2(1920f, 1080f);

    [Test]
    public void NormalizeMapsPixelsToZeroOne()
    {
        Rect result = ScreenRectOf.Normalize(
            new Rect(480f, 270f, 960f, 540f), Screen1080);

        Assert.That(result.x, Is.EqualTo(0.25f).Within(0.0001f));
        Assert.That(result.y, Is.EqualTo(0.25f).Within(0.0001f));
        Assert.That(result.width, Is.EqualTo(0.5f).Within(0.0001f));
        Assert.That(result.height, Is.EqualTo(0.5f).Within(0.0001f));
    }

    [Test]
    public void AFullScreenRectBecomesTheWholeRange()
    {
        Rect result = ScreenRectOf.Normalize(new Rect(0f, 0f, 1920f, 1080f), Screen1080);

        Assert.That(result, Is.EqualTo(new Rect(0f, 0f, 1f, 1f)));
    }

    [Test]
    public void TheSameRectNormalisesTheSameAtAnyResolution()
    {
        // 이게 핵심이다 — 화면 크기가 달라도 같은 자리면 같은 값이어야 한다.
        // 픽셀로 넘기면 devicePixelRatio 와 레터박싱 때문에 어긋난다.
        Rect small = ScreenRectOf.Normalize(
            new Rect(240f, 135f, 480f, 270f), new Vector2(960f, 540f));
        Rect large = ScreenRectOf.Normalize(
            new Rect(960f, 540f, 1920f, 1080f), new Vector2(3840f, 2160f));

        Assert.That(small.x, Is.EqualTo(large.x).Within(0.0001f));
        Assert.That(small.y, Is.EqualTo(large.y).Within(0.0001f));
        Assert.That(small.width, Is.EqualTo(large.width).Within(0.0001f));
        Assert.That(small.height, Is.EqualTo(large.height).Within(0.0001f));
    }

    [Test]
    public void AZeroSizedScreenGivesNothingInsteadOfInfinity()
    {
        // 0으로 나누면 무한대가 나오고, 그 값으로 만든 입력창은 화면을 덮어 버린다
        Assert.That(
            ScreenRectOf.Normalize(new Rect(0f, 0f, 100f, 50f), Vector2.zero),
            Is.EqualTo(new Rect(0f, 0f, 0f, 0f)));
        Assert.That(
            ScreenRectOf.Normalize(new Rect(0f, 0f, 100f, 50f), new Vector2(-10f, 10f)),
            Is.EqualTo(new Rect(0f, 0f, 0f, 0f)));
    }

    [Test]
    public void PixelsOfNothingIsEmpty()
    {
        Assert.That(ScreenRectOf.PixelsOf(null, null), Is.EqualTo(new Rect(0f, 0f, 0f, 0f)));
    }

    [Test]
    public void PixelsOfAnOverlayElementMatchesItsPlacement()
    {
        var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
        try
        {
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var canvasRect = (RectTransform)canvasObject.transform;
            canvasRect.sizeDelta = new Vector2(1920f, 1080f);
            canvasRect.position = new Vector3(960f, 540f, 0f);

            var child = new GameObject("Field", typeof(RectTransform));
            child.transform.SetParent(canvasObject.transform, false);
            var childRect = (RectTransform)child.transform;
            childRect.anchorMin = Vector2.zero;
            childRect.anchorMax = Vector2.zero;
            childRect.pivot = Vector2.zero;
            childRect.anchoredPosition = new Vector2(100f, 200f);
            childRect.sizeDelta = new Vector2(400f, 60f);

            Rect pixels = ScreenRectOf.PixelsOf(childRect, ScreenRectOf.CameraFor(canvas));

            Assert.That(pixels.x, Is.EqualTo(100f).Within(0.01f));
            Assert.That(pixels.y, Is.EqualTo(200f).Within(0.01f));
            Assert.That(pixels.width, Is.EqualTo(400f).Within(0.01f));
            Assert.That(pixels.height, Is.EqualTo(60f).Within(0.01f));
        }
        finally
        {
            Object.DestroyImmediate(canvasObject);
        }
    }

    [Test]
    public void WidthStaysPositiveWhenTheElementIsFlipped()
    {
        // 음수 스케일이면 모서리 순서가 뒤집힌다. 두 점만 보면 폭이 음수가 되고,
        // 그 값으로 만든 입력창은 브라우저에서 아예 보이지 않는다.
        var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
        try
        {
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

            var child = new GameObject("Field", typeof(RectTransform));
            child.transform.SetParent(canvasObject.transform, false);
            var childRect = (RectTransform)child.transform;
            childRect.sizeDelta = new Vector2(400f, 60f);
            childRect.localScale = new Vector3(-1f, -1f, 1f);

            Rect pixels = ScreenRectOf.PixelsOf(childRect, null);

            Assert.That(pixels.width, Is.GreaterThan(0f));
            Assert.That(pixels.height, Is.GreaterThan(0f));
        }
        finally
        {
            Object.DestroyImmediate(canvasObject);
        }
    }

    [Test]
    public void AnOverlayCanvasNeedsNoCamera()
    {
        var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
        try
        {
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            Assert.That(ScreenRectOf.CameraFor(canvas), Is.Null);
            Assert.That(ScreenRectOf.CameraFor(null), Is.Null);
        }
        finally
        {
            Object.DestroyImmediate(canvasObject);
        }
    }

    [Test]
    public void TheBridgeReportsTheFieldPlaceWithinTheScreen()
    {
        var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
        try
        {
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            ((RectTransform)canvasObject.transform).sizeDelta =
                new Vector2(Screen.width, Screen.height);

            var fieldObject = new GameObject(
                "Note Input", typeof(RectTransform), typeof(UnityEngine.UI.InputField));
            fieldObject.transform.SetParent(canvasObject.transform, false);
            var fieldRect = (RectTransform)fieldObject.transform;
            fieldRect.anchorMin = new Vector2(0.6f, 0.3f);
            fieldRect.anchorMax = new Vector2(0.9f, 0.4f);
            fieldRect.offsetMin = Vector2.zero;
            fieldRect.offsetMax = Vector2.zero;

            var bridge = fieldObject.AddComponent<WebTextInput>();
            Rect place = bridge.NormalizedPlace();

            Assert.That(place.x, Is.InRange(0f, 1f));
            Assert.That(place.y, Is.InRange(0f, 1f));
            Assert.That(place.xMax, Is.LessThanOrEqualTo(1.001f));
            Assert.That(place.yMax, Is.LessThanOrEqualTo(1.001f));
            Assert.That(place.width, Is.GreaterThan(0f));
            Assert.That(place.height, Is.GreaterThan(0f));
        }
        finally
        {
            Object.DestroyImmediate(canvasObject);
        }
    }

    [Test]
    public void TheBridgeDoesNothingOutsideTheBrowser()
    {
        // 에디터에서는 원래 한글이 잘 되므로 끼어들면 안 된다
        Assert.That(WebTextInput.IsSupported, Is.False);
    }
}
