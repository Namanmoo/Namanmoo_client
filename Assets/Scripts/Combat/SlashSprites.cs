using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 참격 실루엣 — 코드로 굽는 흰 모양. 흰색이라 SpriteRenderer 색이
/// 곧 이펙트 색이 된다(테마색을 곱하는 게 아니라 통째로 입는다).
///
/// 모양 공식: 5×5 그리드에서 맨 윗줄과 맨 아랫줄에 점을 하나씩 랜덤하게 찍고,
/// 그 사이에 2~4개 점을 랜덤으로 찍은 뒤, 위→아래로 이어 닫은 다각형 안을
/// 채운다. 잇는 방식은 모양마다 전부 직선이거나 전부 곡선 — 섞지 않는다.
/// 지그재그로 찍히면 교차 영역이 생겨 더 제멋대로인 꼴이 나온다(의도).
///
/// 전부 시드에서 나온다 — 같은 시드는 같은 모양, 무기마다 자기 꼴.
/// </summary>
public static class SlashSprites
{
    public const int Size = 96;

    /// <summary>화면에서 약 2.4 유닛 — 캐릭터보다 큰 참격.</summary>
    public const float PixelsPerUnit = 40f;

    /// <summary>그라데이션 — 진행 방향(위)은 진하고 플레이어 쪽(아래)은 연하다.</summary>
    public const float LeadingAlpha = 1f;
    public const float TrailingAlpha = 0.35f;

    /// <summary>이보다 얇으면 다시 뽑는다 — 한 줄에 몰린 실 같은 꼴 방지.</summary>
    public const float MinArea = 0.6f;

    /// <summary>얇은 꼴 재추첨 상한.</summary>
    public const int MaxRolls = 12;

    /// <summary>점을 찍는 그리드 한 변 칸 수.</summary>
    public const int GridSize = 5;

    /// <summary>그리드 가장자리가 텍스처 가장자리에 닿지 않게 두는 여백 (0~1).</summary>
    public const float GridExtent = 0.92f;

    /// <summary>중간 줄에 찍는 점 수 범위.</summary>
    public const int MinMiddlePoints = 2;
    public const int MaxMiddlePoints = 4;

    /// <summary>곡선 모드에서 점 사이를 쪼개는 조각 수 — 클수록 매끈하다.</summary>
    public const int CurveSegments = 12;

    /// <summary>시드별 스프라이트 캐시 상한 — 넘으면 비우고 다시 굽는다.</summary>
    public const int CacheLimit = 64;

    private static readonly Dictionary<int, Sprite> cache = new Dictionary<int, Sprite>();

    /// <summary>참격 한 종류 — 그리드 점들과 잇는 방식.</summary>
    public struct Shape
    {
        /// <summary>위→아래 순서의 꼭짓점 (정규화 -1~1). 마지막이 처음으로 닫힌다.</summary>
        public Vector2[] points;

        /// <summary>true면 전부 곡선(스플라인), false면 전부 직선으로 잇는다.</summary>
        public bool curved;
    }

    /// <summary>공식대로 모양을 하나 뽑는다. 같은 rng 상태면 같은 모양이다.</summary>
    public static Shape RandomShape(System.Random rng)
    {
        Vector2[] points = Roll(rng);
        for (int roll = 0; roll < MaxRolls && Area(points) < MinArea; roll++)
        {
            points = Roll(rng); // 한 줄에 몰린 실 같은 꼴 — 다시 뽑는다
        }

        return new Shape
        {
            points = points,
            curved = rng.Next(2) == 0,
        };
    }

    private static Vector2[] Roll(System.Random rng)
    {
        float Cell(int index) => (index - (GridSize - 1) * 0.5f)
            / ((GridSize - 1) * 0.5f) * GridExtent;

        var points = new List<Vector2>
        {
            new Vector2(Cell(rng.Next(GridSize)), GridExtent), // 맨 윗줄 — 무조건 하나
        };

        // 사이 점들이 한 줄에 몰리지 않게 줄(1~3)부터 나눠 갖는다
        int middleCount = rng.Next(MinMiddlePoints, MaxMiddlePoints + 1);
        var rows = new List<int> { 1, 2, 3 };
        for (int i = rows.Count - 1; i > 0; i--)
        {
            int swap = rng.Next(i + 1);
            (rows[i], rows[swap]) = (rows[swap], rows[i]);
        }

        var middles = new List<Vector2>(middleCount);
        for (int i = 0; i < middleCount; i++)
        {
            int row = i < rows.Count ? rows[i] : rng.Next(1, GridSize - 1);
            middles.Add(new Vector2(Cell(rng.Next(GridSize)), Cell(row)));
        }

        // 위→아래로 이어지게 — 같은 줄이면 찍은 순서를 지킨다
        points.AddRange(middles.OrderByDescending(p => p.y));
        points.Add(new Vector2(Cell(rng.Next(GridSize)), -GridExtent)); // 맨 아랫줄
        return points.ToArray();
    }

    /// <summary>신발끈 공식 넓이 — 얇은 꼴 걸러내기용. EditMode 테스트로 덮는다.</summary>
    public static float Area(Vector2[] points)
    {
        float total = 0f;
        for (int i = 0; i < points.Length; i++)
        {
            Vector2 a = points[i];
            Vector2 b = points[(i + 1) % points.Length];
            total += a.x * b.y - b.x * a.y;
        }

        return Mathf.Abs(total) * 0.5f;
    }

    /// <summary>테스트용 — 시드로 결정적인 모양.</summary>
    public static Shape ShapeFor(int seed)
    {
        return RandomShape(new System.Random(seed));
    }

    /// <summary>모양을 구운 스프라이트 — cacheKey(무기 인스턴스)별로 캐시한다.</summary>
    public static Sprite ForShape(in Shape shape, int cacheKey)
    {
        if (cache.TryGetValue(cacheKey, out Sprite cached) && cached != null)
        {
            return cached;
        }

        if (cache.Count >= CacheLimit)
        {
            cache.Clear(); // 파괴는 하지 않는다 — 날아가는 참격이 아직 쓸 수 있다
        }

        Sprite sprite = Build(shape, $"Slash {cacheKey}");
        cache[cacheKey] = sprite;
        return sprite;
    }

    /// <summary>테스트·에디터용 — 캐시를 비운다.</summary>
    public static void ClearCache()
    {
        cache.Clear();
    }

    /// <summary>
    /// 채움 판정용 외곽선. 직선 모드는 점 그대로, 곡선 모드는 닫힌
    /// Catmull-Rom 스플라인을 잘게 쪼갠 꼭짓점들이다.
    /// </summary>
    public static Vector2[] Outline(in Shape shape)
    {
        if (!shape.curved || shape.points.Length < 3)
        {
            return shape.points;
        }

        var outline = new List<Vector2>(shape.points.Length * CurveSegments);
        int count = shape.points.Length;
        for (int i = 0; i < count; i++)
        {
            Vector2 p0 = shape.points[(i - 1 + count) % count];
            Vector2 p1 = shape.points[i];
            Vector2 p2 = shape.points[(i + 1) % count];
            Vector2 p3 = shape.points[(i + 2) % count];

            for (int step = 0; step < CurveSegments; step++)
            {
                float t = step / (float)CurveSegments;
                outline.Add(CatmullRom(p0, p1, p2, p3, t));
            }
        }

        return outline.ToArray();
    }

    /// <summary>
    /// 좌표(-1~1)가 채워지는가 — 짝홀 규칙이라 지그재그 교차 꼴도 안정적으로
    /// 채워진다. 계산만 하므로 EditMode 테스트로 덮는다.
    /// </summary>
    public static bool Filled(Vector2[] outline, float x, float y)
    {
        bool inside = false;
        for (int i = 0, j = outline.Length - 1; i < outline.Length; j = i++)
        {
            Vector2 a = outline[i];
            Vector2 b = outline[j];
            if ((a.y > y) != (b.y > y)
                && x < (b.x - a.x) * (y - a.y) / (b.y - a.y) + a.x)
            {
                inside = !inside;
            }
        }

        return inside;
    }

    private static Vector2 CatmullRom(
        Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;
        return 0.5f * (
            2f * p1
            + (p2 - p0) * t
            + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2
            + (3f * p1 - p0 - 3f * p2 + p3) * t3);
    }

    private static Sprite Build(in Shape shape, string name)
    {
        var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            name = name,
        };

        Vector2[] outline = Outline(shape);
        var pixels = new Color32[Size * Size];
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                // 2×2 슈퍼샘플 — 픽셀 크기가 커서 계단이 그대로 보인다
                int hit = 0;
                for (int sub = 0; sub < 4; sub++)
                {
                    float sx = (x + 0.25f + 0.5f * (sub % 2)) / Size * 2f - 1f;
                    float sy = (y + 0.25f + 0.5f * (sub / 2)) / Size * 2f - 1f;
                    if (Filled(outline, sx, sy))
                    {
                        hit++;
                    }
                }

                // 진행 방향(위) 끝은 진하고 플레이어 쪽(아래)은 연하게
                float gradient = Mathf.Lerp(
                    TrailingAlpha, LeadingAlpha, (y + 0.5f) / Size);
                pixels[y * Size + x] = new Color32(
                    255, 255, 255, (byte)Mathf.RoundToInt(hit * 255 / 4f * gradient));
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, false); // 읽기 가능하게 둔다 — 테스트가 픽셀을 본다

        return Sprite.Create(
            texture,
            new Rect(0f, 0f, Size, Size),
            new Vector2(0.5f, 0.5f),
            PixelsPerUnit);
    }
}
