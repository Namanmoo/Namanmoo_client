using NUnit.Framework;
using UnityEngine;

public sealed class DrawingFillTests
{
    private const int Size = 32;

    private static readonly Color32 Transparent = new Color32(0, 0, 0, 0);
    private static readonly Color32 Ink = new Color32(20, 20, 20, 255);
    private static readonly Color32 Red = new Color32(220, 40, 40, 255);
    private static readonly Color32 Blue = new Color32(40, 90, 220, 255);

    private static Color32[] Canvas()
    {
        return new Color32[Size * Size];
    }

    private static void Set(Color32[] pixels, int x, int y, Color32 color)
    {
        pixels[y * Size + x] = color;
    }

    private static Color32 At(Color32[] pixels, int x, int y)
    {
        return pixels[y * Size + x];
    }

    /// <summary>가운데에 잉크로 닫힌 사각 테두리를 그린다.</summary>
    private static void DrawBox(Color32[] pixels, int min, int max)
    {
        for (int i = min; i <= max; i++)
        {
            Set(pixels, i, min, Ink);
            Set(pixels, i, max, Ink);
            Set(pixels, min, i, Ink);
            Set(pixels, max, i, Ink);
        }
    }

    [Test]
    public void FillStaysInsideAClosedOutline()
    {
        Color32[] pixels = Canvas();
        DrawBox(pixels, 8, 20);

        bool changed = DrawingFill.Fill(pixels, Size, Size, 14, 14, Red);

        Assert.That(changed, Is.True);
        Assert.That(At(pixels, 14, 14), Is.EqualTo(Red), "안쪽은 칠해진다");
        Assert.That(At(pixels, 8, 14), Is.EqualTo(Ink), "테두리는 그대로다");
        Assert.That(At(pixels, 2, 2), Is.EqualTo(Transparent), "바깥은 손대지 않는다");
    }

    [Test]
    public void FillLeaksThroughAGapInTheOutline()
    {
        // 선이 끊겨 있으면 새어 나가는 게 버킷의 정상 동작이다 — 기대를 고정해 둔다
        Color32[] pixels = Canvas();
        DrawBox(pixels, 8, 20);
        Set(pixels, 14, 8, Transparent);  // 위쪽 테두리에 구멍

        DrawingFill.Fill(pixels, Size, Size, 14, 14, Red);

        Assert.That(At(pixels, 2, 2), Is.EqualTo(Red));
    }

    [Test]
    public void FillingEmptyCanvasCoversEverything()
    {
        Color32[] pixels = Canvas();

        DrawingFill.Fill(pixels, Size, Size, 0, 0, Blue);

        foreach (Color32 pixel in pixels)
        {
            Assert.That(pixel, Is.EqualTo(Blue));
        }
    }

    [Test]
    public void FillingWithTheSameColourChangesNothing()
    {
        // 이 검사가 없으면 조건이 계속 참이라 같은 줄을 무한히 다시 넣는다
        Color32[] pixels = Canvas();
        DrawingFill.Fill(pixels, Size, Size, 5, 5, Red);

        bool changed = DrawingFill.Fill(pixels, Size, Size, 5, 5, Red);

        Assert.That(changed, Is.False);
    }

    [Test]
    public void FillReplacesOnlyTheRegionUnderTheCursor()
    {
        Color32[] pixels = Canvas();
        DrawBox(pixels, 4, 12);
        DrawBox(pixels, 18, 28);
        DrawingFill.Fill(pixels, Size, Size, 8, 8, Red);

        DrawingFill.Fill(pixels, Size, Size, 23, 23, Blue);

        Assert.That(At(pixels, 8, 8), Is.EqualTo(Red), "먼저 칠한 영역은 유지된다");
        Assert.That(At(pixels, 23, 23), Is.EqualTo(Blue));
    }

    [Test]
    public void ToleranceCoversAntiAliasedEdges()
    {
        // 손그림 외곽선은 반투명 픽셀이 섞여 있어 오차가 0이면 얇은 띠가 남는다
        Color32[] pixels = Canvas();
        var almostRed = new Color32(228, 48, 34, 255);
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                Set(pixels, x, y, ((x + y) % 2 == 0) ? Red : almostRed);
            }
        }

        DrawingFill.Fill(pixels, Size, Size, 0, 0, Blue);

        Assert.That(At(pixels, 15, 15), Is.EqualTo(Blue));
        Assert.That(At(pixels, 16, 15), Is.EqualTo(Blue));
    }

    [Test]
    public void ZeroToleranceStopsAtSlightlyDifferentColours()
    {
        Color32[] pixels = Canvas();
        var almostRed = new Color32(228, 48, 34, 255);
        DrawingFill.Fill(pixels, Size, Size, 0, 0, Red);
        Set(pixels, 10, 10, almostRed);

        DrawingFill.Fill(pixels, Size, Size, 0, 0, Blue, tolerance: 0);

        Assert.That(At(pixels, 10, 10), Is.EqualTo(almostRed), "오차 0이면 다른 색은 남는다");
    }

    [Test]
    public void OutOfRangeStartIsIgnored()
    {
        Color32[] pixels = Canvas();

        Assert.That(DrawingFill.Fill(pixels, Size, Size, -1, 5, Red), Is.False);
        Assert.That(DrawingFill.Fill(pixels, Size, Size, Size, 5, Red), Is.False);
        Assert.That(DrawingFill.Fill(pixels, Size, Size, 5, Size, Red), Is.False);
    }

    [Test]
    public void MismatchedDimensionsAreRejected()
    {
        Assert.Throws<System.ArgumentException>(
            () => DrawingFill.Fill(new Color32[10], 4, 4, 0, 0, Red));
        Assert.Throws<System.ArgumentNullException>(
            () => DrawingFill.Fill(null, 4, 4, 0, 0, Red));
    }

    [Test]
    public void FullyTransparentPixelsMatchRegardlessOfRgb()
    {
        // 지우개는 RGB를 남기고 알파만 0으로 만든다. 그 자리들이 서로 다른 색으로
        // 보이면 지운 영역을 한 번에 다시 칠할 수 없다.
        var erasedRed = new Color32(220, 40, 40, 0);
        var erasedBlue = new Color32(40, 90, 220, 0);

        Assert.That(DrawingFill.Matches(erasedRed, erasedBlue, 0), Is.True);
    }

    [Test]
    public void FillHandlesAConcaveRegionWithoutMissingCorners()
    {
        // U자 모양 — 스캔라인이 아래로만 훑으면 한쪽 팔을 놓친다
        Color32[] pixels = Canvas();
        for (int y = 6; y <= 24; y++)
        {
            Set(pixels, 15, y, Ink);
        }

        for (int x = 10; x <= 20; x++)
        {
            Set(pixels, x, 24, Ink);
        }

        // 왼쪽 팔 안쪽에서 시작 — 아래를 돌아 오른쪽 팔까지 이어진다
        DrawingFill.Fill(pixels, Size, Size, 12, 10, Red);

        Assert.That(At(pixels, 12, 10), Is.EqualTo(Red));
        Assert.That(At(pixels, 18, 10), Is.EqualTo(Red), "벽을 돌아 반대쪽도 칠해진다");
        Assert.That(At(pixels, 15, 15), Is.EqualTo(Ink), "벽은 그대로다");
    }
}
