using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 생성된 무기 이미지의 흰 배경을 투명하게 만든다.
///
/// 이미지 생성 모델은 보통 불투명한 이미지를 낸다. 서버가 "흰 배경"으로 요청하므로
/// 그 흰색을 지워야 스프라이트로 쓸 수 있다.
///
/// 전역으로 흰 픽셀을 다 지우면 무기 안쪽의 하이라이트·흰 장식까지 뚫린다.
/// 그래서 <em>가장자리에서 시작하는 플러드 필</em>로 바깥과 이어진 흰색만 지운다.
/// 순수 함수라 EditMode 테스트로 덮는다.
/// </summary>
public static class WhiteBackgroundKey
{
    /// <summary>흰색으로 볼 최소 밝기. 이 값 이상이면서 색이 옅으면 배경 후보다.</summary>
    public const byte DefaultBrightness = 232;

    /// <summary>R·G·B 간 최대 편차. 이보다 색이 치우쳐 있으면 배경이 아니다.</summary>
    public const byte DefaultChroma = 18;

    /// <summary>
    /// 바깥과 이어진 흰 영역을 투명하게 만든 새 배열을 돌려준다. 원본은 건드리지 않는다.
    /// </summary>
    public static Color32[] RemoveBackground(
        Color32[] pixels,
        int width,
        int height,
        byte brightness = DefaultBrightness,
        byte chroma = DefaultChroma)
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

        var result = new Color32[pixels.Length];
        System.Array.Copy(pixels, result, pixels.Length);

        var visited = new bool[pixels.Length];
        var frontier = new Stack<int>();

        // 네 변의 픽셀을 전부 시작점으로 넣는다 — 무기가 한쪽 변에 붙어 있어도
        // 나머지 변에서 배경을 따라 들어갈 수 있다.
        for (int x = 0; x < width; x++)
        {
            PushIfBackground(frontier, visited, pixels, x, brightness, chroma);
            PushIfBackground(frontier, visited, pixels, (height - 1) * width + x, brightness, chroma);
        }

        for (int y = 0; y < height; y++)
        {
            PushIfBackground(frontier, visited, pixels, y * width, brightness, chroma);
            PushIfBackground(frontier, visited, pixels, y * width + width - 1, brightness, chroma);
        }

        while (frontier.Count > 0)
        {
            int index = frontier.Pop();
            result[index].a = 0;

            int x = index % width;
            int y = index / width;

            if (x > 0) PushIfBackground(frontier, visited, pixels, index - 1, brightness, chroma);
            if (x < width - 1) PushIfBackground(frontier, visited, pixels, index + 1, brightness, chroma);
            if (y > 0) PushIfBackground(frontier, visited, pixels, index - width, brightness, chroma);
            if (y < height - 1) PushIfBackground(frontier, visited, pixels, index + width, brightness, chroma);
        }

        return result;
    }

    /// <summary>흰 배경으로 볼 만한 픽셀인가 — 충분히 밝고 색이 치우치지 않았는가.</summary>
    public static bool IsBackground(Color32 pixel, byte brightness, byte chroma)
    {
        if (pixel.a == 0)
        {
            // 이미 투명한 픽셀도 배경으로 취급해 그 너머까지 이어간다
            return true;
        }

        if (pixel.r < brightness || pixel.g < brightness || pixel.b < brightness)
        {
            return false;
        }

        byte max = System.Math.Max(pixel.r, System.Math.Max(pixel.g, pixel.b));
        byte min = System.Math.Min(pixel.r, System.Math.Min(pixel.g, pixel.b));
        return max - min <= chroma;
    }

    private static void PushIfBackground(
        Stack<int> frontier,
        bool[] visited,
        Color32[] pixels,
        int index,
        byte brightness,
        byte chroma)
    {
        if (visited[index])
        {
            return;
        }

        visited[index] = true;
        if (IsBackground(pixels[index], brightness, chroma))
        {
            frontier.Push(index);
        }
    }
}
