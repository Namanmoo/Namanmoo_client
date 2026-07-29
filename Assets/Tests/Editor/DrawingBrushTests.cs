using NUnit.Framework;
using UnityEngine;

public sealed class DrawingBrushTests
{
    private const int Size = 32;

    private static Color32[] EmptyCanvas()
    {
        return new Color32[Size * Size];
    }

    private static Color32 At(Color32[] pixels, int x, int y)
    {
        return pixels[y * Size + x];
    }

    private static int OpaqueCount(Color32[] pixels)
    {
        int count = 0;
        foreach (Color32 pixel in pixels)
        {
            if (pixel.a > 0)
            {
                count++;
            }
        }

        return count;
    }

    [Test]
    public void PenStampPaintsARoundDot()
    {
        Color32[] pixels = EmptyCanvas();
        var brush = new BrushSettings(BrushKind.Pen, 3, new Color32(200, 0, 0, 255));

        DrawingBrush.Stamp(pixels, Size, Size, 16, 16, brush);

        Assert.That(At(pixels, 16, 16).r, Is.EqualTo(200));
        Assert.That(At(pixels, 16, 19).a, Is.EqualTo(255), "반지름 안은 칠해진다");
        Assert.That(At(pixels, 16, 21).a, Is.Zero, "반지름 밖은 그대로다");
    }

    [Test]
    public void StampNearTheEdgeDoesNotWrapOrCrash()
    {
        Color32[] pixels = EmptyCanvas();
        var brush = new BrushSettings(BrushKind.Pen, 5, new Color32(0, 0, 200, 255));

        DrawingBrush.Stamp(pixels, Size, Size, 0, 0, brush);

        Assert.That(At(pixels, 0, 0).a, Is.EqualTo(255));
        // 왼쪽 끝에서 찍었는데 오른쪽 끝이 칠해지면 인덱스가 감긴 것이다
        Assert.That(At(pixels, Size - 1, 0).a, Is.Zero);
    }

    [Test]
    public void StampLineConnectsDistantPoints()
    {
        Color32[] pixels = EmptyCanvas();
        var brush = new BrushSettings(BrushKind.Pen, 1, new Color32(0, 0, 0, 255));

        DrawingBrush.StampLine(pixels, Size, Size, 2, 2, 29, 2, brush);

        // 포인터가 띄엄띄엄 들어와도 선이 끊기면 안 된다
        for (int x = 3; x <= 28; x++)
        {
            Assert.That(At(pixels, x, 2).a, Is.EqualTo(255), $"x={x}에서 선이 끊겼다");
        }
    }

    [Test]
    public void EraserClearsAlphaWithoutTouchingNeighbours()
    {
        Color32[] pixels = EmptyCanvas();
        var pen = new BrushSettings(BrushKind.Pen, 6, new Color32(10, 200, 10, 255));
        DrawingBrush.Stamp(pixels, Size, Size, 16, 16, pen);

        var eraser = new BrushSettings(BrushKind.Eraser, 2, default);
        DrawingBrush.Stamp(pixels, Size, Size, 16, 16, eraser);

        Assert.That(At(pixels, 16, 16).a, Is.Zero);
        Assert.That(At(pixels, 16, 21).a, Is.EqualTo(255));
    }

    [Test]
    public void CrayonLeavesGrainHolesUnlikeThePen()
    {
        var penPixels = EmptyCanvas();
        var crayonPixels = EmptyCanvas();
        var color = new Color32(30, 30, 30, 255);

        DrawingBrush.Stamp(penPixels, Size, Size, 16, 16, new BrushSettings(BrushKind.Pen, 10, color));
        DrawingBrush.Stamp(
            crayonPixels, Size, Size, 16, 16, new BrushSettings(BrushKind.Crayon, 10, color));

        Assert.That(OpaqueCount(crayonPixels), Is.LessThan(OpaqueCount(penPixels)));
        Assert.That(OpaqueCount(crayonPixels), Is.GreaterThan(0));
    }

    [Test]
    public void CrayonGrainIsStable_SoRepaintingDoesNotFillItIn()
    {
        var once = EmptyCanvas();
        var twice = EmptyCanvas();
        var brush = new BrushSettings(BrushKind.Crayon, 8, new Color32(0, 0, 0, 255));

        DrawingBrush.Stamp(once, Size, Size, 16, 16, brush);
        DrawingBrush.Stamp(twice, Size, Size, 16, 16, brush);
        DrawingBrush.Stamp(twice, Size, Size, 16, 16, brush);

        Assert.That(OpaqueCount(twice), Is.EqualTo(OpaqueCount(once)));
    }

    [Test]
    public void FillReplacesEveryPixel()
    {
        Color32[] pixels = EmptyCanvas();

        DrawingBrush.Fill(pixels, new Color32(1, 2, 3, 255));

        Assert.That(OpaqueCount(pixels), Is.EqualTo(Size * Size));
        Assert.That(At(pixels, 0, 0).g, Is.EqualTo(2));
    }

    [Test]
    public void ZeroLengthLineStillPaintsASingleDot()
    {
        Color32[] pixels = EmptyCanvas();
        var brush = new BrushSettings(BrushKind.Pen, 2, new Color32(0, 0, 0, 255));

        DrawingBrush.StampLine(pixels, Size, Size, 10, 10, 10, 10, brush);

        Assert.That(At(pixels, 10, 10).a, Is.EqualTo(255));
    }
}
