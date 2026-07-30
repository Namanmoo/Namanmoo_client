using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 색 채우기(버킷). 누른 지점과 비슷한 색이 이어진 영역을 한 번에 칠한다.
///
/// 스캔라인 방식이다 — 픽셀 하나하나를 스택에 넣는 방식은 512x512에서 스택이
/// 수십만 개까지 불어난다. 가로줄 단위로 처리하면 그 수가 크게 줄어든다.
/// (AIGame의 web/src/drawing/floodFill.ts와 같은 접근)
///
/// UnityEngine 타입만 쓰고 씬에 의존하지 않아 EditMode 테스트로 덮을 수 있다.
/// </summary>
public static class DrawingFill
{
    /// <summary>
    /// 같은 색으로 볼 허용 오차. 손그림은 외곽선이 안티에일리어싱돼 있어
    /// 0으로 두면 선 안쪽에 얇은 띠가 남는다.
    /// </summary>
    public const byte DefaultTolerance = 32;

    /// <summary>
    /// <paramref name="startX"/>, <paramref name="startY"/>에서 시작해 이어진 영역을
    /// <paramref name="color"/>로 칠한다. 실제로 바뀐 픽셀이 있으면 true.
    /// </summary>
    public static bool Fill(
        Color32[] pixels,
        int width,
        int height,
        int startX,
        int startY,
        Color32 color,
        byte tolerance = DefaultTolerance)
    {
        if (pixels == null)
        {
            throw new System.ArgumentNullException(nameof(pixels));
        }

        if (width <= 0 || height <= 0 || pixels.Length != width * height)
        {
            throw new System.ArgumentException(
                $"픽셀 수({pixels.Length})가 {width}x{height}와 맞지 않습니다.",
                nameof(pixels));
        }

        if (startX < 0 || startX >= width || startY < 0 || startY >= height)
        {
            return false;
        }

        Color32 seed = pixels[startY * width + startX];

        // 칠할 색이 이미 그 색이면 할 일이 없다.
        // (이 검사가 없으면 조건이 계속 참이라 같은 줄을 무한히 다시 넣는다)
        if (Matches(seed, color, 0))
        {
            return false;
        }

        var pending = new Stack<int>();
        pending.Push(startY * width + startX);
        bool changed = false;

        while (pending.Count > 0)
        {
            int index = pending.Pop();
            int y = index / width;
            int rowStart = y * width;

            // 이 줄에서 왼쪽 끝까지 뻗는다
            int left = index - rowStart;
            while (left > 0 && Matches(pixels[rowStart + left - 1], seed, tolerance))
            {
                left--;
            }

            // 오른쪽 끝까지
            int right = index - rowStart;
            while (right < width - 1 && Matches(pixels[rowStart + right + 1], seed, tolerance))
            {
                right++;
            }

            // 이미 칠해진 줄이면 건너뛴다 — 아래위에서 같은 줄로 다시 들어올 수 있다
            if (!Matches(pixels[rowStart + left], seed, tolerance))
            {
                continue;
            }

            for (int x = left; x <= right; x++)
            {
                pixels[rowStart + x] = color;
                changed = true;
            }

            // 위아래 줄에서 이어지는 구간의 시작점만 넣는다
            PushSpans(pending, pixels, width, left, right, y - 1, seed, tolerance);
            PushSpans(pending, pixels, width, left, right, y + 1, seed, tolerance);
        }

        return changed;
    }

    /// <summary>두 색이 허용 오차 안에서 같은가. 알파도 함께 본다.</summary>
    public static bool Matches(Color32 a, Color32 b, byte tolerance)
    {
        // 둘 다 완전히 투명하면 RGB가 무엇이든 같은 빈 칸이다
        if (a.a == 0 && b.a == 0)
        {
            return true;
        }

        return Diff(a.r, b.r) <= tolerance
            && Diff(a.g, b.g) <= tolerance
            && Diff(a.b, b.b) <= tolerance
            && Diff(a.a, b.a) <= tolerance;
    }

    private static void PushSpans(
        Stack<int> pending,
        Color32[] pixels,
        int width,
        int left,
        int right,
        int y,
        Color32 seed,
        byte tolerance)
    {
        if (y < 0 || y * width >= pixels.Length)
        {
            return;
        }

        int rowStart = y * width;
        bool inSpan = false;

        for (int x = left; x <= right; x++)
        {
            bool matches = Matches(pixels[rowStart + x], seed, tolerance);
            if (matches && !inSpan)
            {
                // 구간마다 한 점씩만 넣는다 — 이게 스캔라인이 스택을 줄이는 지점이다
                pending.Push(rowStart + x);
                inSpan = true;
            }
            else if (!matches)
            {
                inSpan = false;
            }
        }
    }

    private static int Diff(byte a, byte b)
    {
        return a > b ? a - b : b - a;
    }
}
