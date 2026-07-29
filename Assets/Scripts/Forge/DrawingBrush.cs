using UnityEngine;

public enum BrushKind
{
    /// <summary>연필 — 가늘고 또렷한 선</summary>
    Pen,

    /// <summary>크레용 — 굵고 거친 선. 알갱이가 비어 종이 질감이 남는다</summary>
    Crayon,

    /// <summary>지우개 — 알파를 지운다</summary>
    Eraser
}

public struct BrushSettings
{
    public BrushKind Kind;
    public int Radius;
    public Color32 Color;

    public BrushSettings(BrushKind kind, int radius, Color32 color)
    {
        Kind = kind;
        Radius = Mathf.Max(1, radius);
        Color = color;
    }
}

/// <summary>
/// 픽셀 배열에 직접 찍는 브러시. UnityEngine 타입만 쓰고 씬·게임오브젝트에
/// 의존하지 않아서 EditMode 테스트로 전부 덮을 수 있다.
///
/// 좌표계는 Texture2D.GetPixels32와 같다 — (0,0)이 좌하단, 행 우선.
/// </summary>
public static class DrawingBrush
{
    /// <summary>크레용이 비우는 알갱이 비율(0~1). 높을수록 거칠다.</summary>
    private const float CrayonGrain = 0.28f;

    public static void Fill(Color32[] pixels, Color32 color)
    {
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = color;
        }
    }

    /// <summary>한 점을 찍는다.</summary>
    public static void Stamp(
        Color32[] pixels,
        int width,
        int height,
        int centerX,
        int centerY,
        in BrushSettings brush)
    {
        int radius = Mathf.Max(1, brush.Radius);
        int radiusSquared = radius * radius;

        int minX = Mathf.Max(0, centerX - radius);
        int maxX = Mathf.Min(width - 1, centerX + radius);
        int minY = Mathf.Max(0, centerY - radius);
        int maxY = Mathf.Min(height - 1, centerY + radius);

        for (int y = minY; y <= maxY; y++)
        {
            int dy = y - centerY;
            for (int x = minX; x <= maxX; x++)
            {
                int dx = x - centerX;
                if (dx * dx + dy * dy > radiusSquared)
                {
                    continue;
                }

                if (brush.Kind == BrushKind.Crayon && IsGrainHole(x, y))
                {
                    continue;
                }

                int index = y * width + x;
                pixels[index] = brush.Kind == BrushKind.Eraser
                    ? Erase(pixels[index])
                    : Blend(pixels[index], brush.Color);
            }
        }
    }

    /// <summary>
    /// 두 점 사이를 이어 찍는다. 포인터 이벤트는 프레임마다 띄엄띄엄 들어오므로
    /// 이걸 안 하면 빠르게 그을 때 선이 점선이 된다.
    /// </summary>
    public static void StampLine(
        Color32[] pixels,
        int width,
        int height,
        int fromX,
        int fromY,
        int toX,
        int toY,
        in BrushSettings brush)
    {
        int dx = toX - fromX;
        int dy = toY - fromY;
        int steps = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy));

        if (steps == 0)
        {
            Stamp(pixels, width, height, toX, toY, brush);
            return;
        }

        for (int i = 0; i <= steps; i++)
        {
            float t = (float)i / steps;
            int x = Mathf.RoundToInt(Mathf.Lerp(fromX, toX, t));
            int y = Mathf.RoundToInt(Mathf.Lerp(fromY, toY, t));
            Stamp(pixels, width, height, x, y, brush);
        }
    }

    /// <summary>
    /// 크레용 알갱이 — 좌표만으로 정해지는 결정적 패턴이라 같은 자리를 다시 칠하면
    /// 같은 구멍이 남는다(난수를 쓰면 덧칠할수록 메워져 크레용 느낌이 사라진다).
    /// </summary>
    private static bool IsGrainHole(int x, int y)
    {
        // 좌표를 섞어 0~1 값을 만든다 (해시 — 시각적 규칙성만 없으면 충분하다)
        int hash = x * 73856093 ^ y * 19349663;
        hash = hash & 0x7fffffff;
        return (hash % 1000) / 1000f < CrayonGrain;
    }

    private static Color32 Blend(Color32 destination, Color32 source)
    {
        if (source.a == 255)
        {
            return source;
        }

        float sa = source.a / 255f;
        float da = destination.a / 255f;
        float outA = sa + da * (1f - sa);
        if (outA <= 0f)
        {
            return new Color32(0, 0, 0, 0);
        }

        byte Mix(byte s, byte d) =>
            (byte)Mathf.Clamp(
                Mathf.RoundToInt((s * sa + d * da * (1f - sa)) / outA),
                0,
                255);

        return new Color32(
            Mix(source.r, destination.r),
            Mix(source.g, destination.g),
            Mix(source.b, destination.b),
            (byte)Mathf.Clamp(Mathf.RoundToInt(outA * 255f), 0, 255));
    }

    private static Color32 Erase(Color32 destination)
    {
        return new Color32(destination.r, destination.g, destination.b, 0);
    }
}
