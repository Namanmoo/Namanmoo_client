using NUnit.Framework;
using UnityEngine;

public sealed class WhiteBackgroundKeyTests
{
    private const int Size = 8;

    private static Color32[] WhiteCanvas()
    {
        var pixels = new Color32[Size * Size];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = new Color32(255, 255, 255, 255);
        }

        return pixels;
    }

    private static void Set(Color32[] pixels, int x, int y, Color32 color)
    {
        pixels[y * Size + x] = color;
    }

    private static Color32 Get(Color32[] pixels, int x, int y)
    {
        return pixels[y * Size + x];
    }

    [Test]
    public void OuterWhiteBecomesTransparent()
    {
        Color32[] pixels = WhiteCanvas();
        Set(pixels, 4, 4, new Color32(200, 20, 20, 255));

        Color32[] result = WhiteBackgroundKey.RemoveBackground(pixels, Size, Size);

        Assert.That(Get(result, 0, 0).a, Is.Zero);
        Assert.That(Get(result, 7, 7).a, Is.Zero);
        Assert.That(Get(result, 4, 4).a, Is.EqualTo(255));
    }

    [Test]
    public void WhiteEnclosedByTheSubjectSurvives()
    {
        // 무기 안쪽의 흰 하이라이트는 남아야 한다 — 전역으로 흰색을 지우면 뚫린다
        Color32[] pixels = WhiteCanvas();
        var ink = new Color32(20, 20, 20, 255);
        for (int x = 2; x <= 5; x++)
        {
            for (int y = 2; y <= 5; y++)
            {
                Set(pixels, x, y, ink);
            }
        }

        Set(pixels, 3, 3, new Color32(255, 255, 255, 255));

        Color32[] result = WhiteBackgroundKey.RemoveBackground(pixels, Size, Size);

        Assert.That(Get(result, 3, 3).a, Is.EqualTo(255), "둘러싸인 흰 픽셀은 남아야 한다");
        Assert.That(Get(result, 0, 0).a, Is.Zero);
    }

    [Test]
    public void SubjectTouchingTheEdgeIsNotEatenAway()
    {
        Color32[] pixels = WhiteCanvas();
        var ink = new Color32(10, 90, 200, 255);
        for (int y = 0; y < Size; y++)
        {
            Set(pixels, 0, y, ink);
        }

        Color32[] result = WhiteBackgroundKey.RemoveBackground(pixels, Size, Size);

        for (int y = 0; y < Size; y++)
        {
            Assert.That(Get(result, 0, y).a, Is.EqualTo(255));
        }

        Assert.That(Get(result, 7, 0).a, Is.Zero);
    }

    [Test]
    public void NearWhiteIsTreatedAsBackgroundButTintedPixelsAreNot()
    {
        Assert.That(
            WhiteBackgroundKey.IsBackground(
                new Color32(250, 248, 252, 255),
                WhiteBackgroundKey.DefaultBrightness,
                WhiteBackgroundKey.DefaultChroma),
            Is.True);

        // 밝지만 색이 뚜렷하게 치우친 픽셀(연한 하늘색)은 그림의 일부다
        Assert.That(
            WhiteBackgroundKey.IsBackground(
                new Color32(235, 250, 255, 255),
                WhiteBackgroundKey.DefaultBrightness,
                WhiteBackgroundKey.DefaultChroma),
            Is.False);
    }

    [Test]
    public void AlreadyTransparentImageIsUnchanged()
    {
        var pixels = new Color32[Size * Size];
        Set(pixels, 4, 4, new Color32(200, 20, 20, 255));

        Color32[] result = WhiteBackgroundKey.RemoveBackground(pixels, Size, Size);

        Assert.That(Get(result, 4, 4).a, Is.EqualTo(255));
        Assert.That(Get(result, 0, 0).a, Is.Zero);
    }

    [Test]
    public void SourceArrayIsNotModified()
    {
        Color32[] pixels = WhiteCanvas();

        WhiteBackgroundKey.RemoveBackground(pixels, Size, Size);

        Assert.That(pixels[0].a, Is.EqualTo(255));
    }

    [Test]
    public void MismatchedDimensionsAreRejected()
    {
        Assert.Throws<System.ArgumentException>(
            () => WhiteBackgroundKey.RemoveBackground(new Color32[10], 4, 4));
        Assert.Throws<System.ArgumentNullException>(
            () => WhiteBackgroundKey.RemoveBackground(null, 4, 4));
    }
}
