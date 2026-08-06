using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 무기 그림에서 뽑은 색 테마 — 잔상·검기·트레일 같은 물리 계열 이펙트가
/// 무기와 같은 색을 입는다. 원소 효과(화염·냉기·독)는 색이 곧 게임플레이
/// 정보라 테마를 쓰지 않는다.
///
/// 색은 자산에 박혀 있지 않고 스프라이트 픽셀에서 런타임에 뽑는다.
/// 그린 그림이든 생성 이미지든 스프라이트만 있으면 되므로 서버가 없어도
/// 항상 그림과 일치하는 색이 나온다. 서버가 테마를 내려주게 되면
/// 그 값으로 덮어쓰면 된다.
/// </summary>
public sealed class WeaponTheme
{
    /// <summary>지배색 — 잔상 등 이펙트의 본색.</summary>
    public Color Primary { get; }

    /// <summary>보조색 — 그라데이션·하이라이트용. 뽑을 색이 없으면 지배색을 밝힌 것.</summary>
    public Color Accent { get; }

    /// <summary>불투명 픽셀로 치는 알파 하한.</summary>
    public const byte MinAlpha = 128;

    /// <summary>이보다 밝으면 종이 배경 잔재로 보고 버린다 (0~1).</summary>
    public const float NearWhite = 0.92f;

    /// <summary>보조색이 지배색과 이만큼은 떨어져야 따로 친다 (RGB 거리).</summary>
    public const float AccentMinDistance = 0.25f;

    /// <summary>큰 텍스처는 이 수까지만 표본을 뽑는다.</summary>
    public const int MaxSamples = 10000;

    // 채널당 8단계 양자화 — 연필 선의 미묘한 명암을 한 덩이로 묶는 굵기.
    private const int BucketBits = 3;

    private static readonly Dictionary<Sprite, WeaponTheme> cache =
        new Dictionary<Sprite, WeaponTheme>();

    private WeaponTheme(Color primary, Color accent)
    {
        Primary = primary;
        Accent = accent;
    }

    /// <summary>
    /// 스프라이트의 테마. 무기 몇 자루 분량이라 스프라이트별로 캐시한다.
    /// 픽셀을 읽을 수 없는 스프라이트(에셋 임포트 설정)면 fallback 색으로 만든다.
    /// </summary>
    public static WeaponTheme Of(Sprite sprite, Color fallback)
    {
        if (sprite == null)
        {
            return Solid(fallback);
        }

        if (cache.TryGetValue(sprite, out WeaponTheme cached))
        {
            return cached;
        }

        WeaponTheme theme = sprite.texture != null && sprite.texture.isReadable
            ? FromPixels(sprite.texture.GetPixels32(), fallback)
            : Solid(fallback);

        cache[sprite] = theme;
        return theme;
    }

    /// <summary>테스트·에디터에서 캐시를 비운다 — 파괴된 스프라이트 키가 남지 않게.</summary>
    public static void ClearCache()
    {
        cache.Clear();
    }

    /// <summary>
    /// 픽셀에서 테마를 뽑는다. 씬 없이 계산만 하므로 EditMode 테스트로 덮는다.
    ///
    /// 색을 양자화해 세되 채도로 가중치를 준다 — 연필(회색) 선이 아무리 많아도
    /// 칠해 둔 유채색이 있으면 그쪽이 무기의 "색"이다. 전부 무채색인 그림이면
    /// 자연스럽게 그 회색이 이긴다.
    /// </summary>
    public static WeaponTheme FromPixels(IReadOnlyList<Color32> pixels, Color fallback)
    {
        if (pixels == null || pixels.Count == 0)
        {
            return Solid(fallback);
        }

        var buckets = new Dictionary<int, Bucket>();
        int stride = Mathf.Max(1, pixels.Count / MaxSamples);

        for (int i = 0; i < pixels.Count; i += stride)
        {
            Color32 pixel = pixels[i];
            if (pixel.a < MinAlpha)
            {
                continue;
            }

            Color color = pixel;
            if (Mathf.Min(color.r, Mathf.Min(color.g, color.b)) > NearWhite)
            {
                continue; // 종이 배경 잔재
            }

            int key = (pixel.r >> (8 - BucketBits) << (BucketBits * 2))
                | (pixel.g >> (8 - BucketBits) << BucketBits)
                | (pixel.b >> (8 - BucketBits));

            if (!buckets.TryGetValue(key, out Bucket bucket))
            {
                bucket = new Bucket();
                buckets[key] = bucket;
            }

            bucket.Add(color);
        }

        if (buckets.Count == 0)
        {
            return Solid(fallback);
        }

        Bucket primaryBucket = null;
        foreach (Bucket bucket in buckets.Values)
        {
            if (primaryBucket == null || bucket.Score > primaryBucket.Score)
            {
                primaryBucket = bucket;
            }
        }

        Color primary = primaryBucket.Average;

        Bucket accentBucket = null;
        foreach (Bucket bucket in buckets.Values)
        {
            if (Distance(bucket.Average, primary) < AccentMinDistance)
            {
                continue;
            }

            if (accentBucket == null || bucket.Score > accentBucket.Score)
            {
                accentBucket = bucket;
            }
        }

        Color accent = accentBucket != null
            ? accentBucket.Average
            : Color.Lerp(primary, Color.white, 0.5f);

        return new WeaponTheme(Opaque(primary), Opaque(accent));
    }

    private static WeaponTheme Solid(Color color)
    {
        return new WeaponTheme(Opaque(color), Color.Lerp(Opaque(color), Color.white, 0.5f));
    }

    private static Color Opaque(Color color)
    {
        return new Color(color.r, color.g, color.b, 1f);
    }

    private static float Distance(Color a, Color b)
    {
        return Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b);
    }

    /// <summary>양자화 칸 하나 — 개수와 색 합을 들고 있다가 평균과 점수를 내준다.</summary>
    private sealed class Bucket
    {
        private float r;
        private float g;
        private float b;
        private int count;

        public void Add(Color color)
        {
            r += color.r;
            g += color.g;
            b += color.b;
            count++;
        }

        public Color Average => count == 0
            ? Color.white
            : new Color(r / count, g / count, b / count, 1f);

        /// <summary>개수 × (0.3 + 채도) — 유채색을 우대하되 무채색만 있으면 그쪽이 이긴다.</summary>
        public float Score
        {
            get
            {
                Color average = Average;
                float max = Mathf.Max(average.r, Mathf.Max(average.g, average.b));
                float min = Mathf.Min(average.r, Mathf.Min(average.g, average.b));
                float saturation = max <= 0f ? 0f : (max - min) / max;
                return count * (0.3f + saturation);
            }
        }
    }
}
