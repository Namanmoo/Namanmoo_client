using System.Collections.Generic;
using UnityEngine;

public static class Stage1MapDefinition
{
    private const float MapScale = 2.5f;

    private static readonly Vector2[] OutlinePoints =
    {
        new Vector2(-9f, -8f),
        new Vector2(-3f, -7.9f),
        new Vector2(1.5f, -7.3f),
        new Vector2(6.5f, -6.6f),
        new Vector2(9f, -6.1f),
        new Vector2(8.5f, -2f),
        new Vector2(8.3f, -1.1f),
        new Vector2(2.1f, -1.1f),
        new Vector2(0.8f, -1.3f),
        new Vector2(0.8f, 1.8f),
        new Vector2(4.8f, 2.2f),
        new Vector2(7.8f, 2.8f),
        new Vector2(8.5f, 5.2f),
        new Vector2(8.6f, 7.8f),
        new Vector2(1.2f, 8f),
        new Vector2(-2.5f, 7.5f),
        new Vector2(-8f, 7.3f),
        new Vector2(-8.8f, 4.8f),
        new Vector2(-8.9f, 1.6f),
        new Vector2(-4.1f, 1.6f),
        new Vector2(-4.4f, -1.2f),
        new Vector2(-7.6f, -1.3f),
        new Vector2(-8.5f, -3.4f)
    };

    private static readonly Vector2[] ScaledOutlinePoints = ScaleOutline(OutlinePoints);
    private static readonly int[] TriangleIndices = Triangulate(ScaledOutlinePoints);

    public static IReadOnlyList<Vector2> Outline => ScaledOutlinePoints;
    public static IReadOnlyList<int> Triangles => TriangleIndices;

    public static bool Contains(Vector2 point)
    {
        bool inside = false;

        for (int current = 0, previous = ScaledOutlinePoints.Length - 1;
             current < ScaledOutlinePoints.Length;
             previous = current++)
        {
            Vector2 a = ScaledOutlinePoints[current];
            Vector2 b = ScaledOutlinePoints[previous];
            bool crossesRay = (a.y > point.y) != (b.y > point.y);

            if (crossesRay &&
                point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x)
            {
                inside = !inside;
            }
        }

        return inside;
    }

    private static Vector2[] ScaleOutline(IReadOnlyList<Vector2> points)
    {
        var scaledPoints = new Vector2[points.Count];

        for (int index = 0; index < points.Count; index++)
        {
            scaledPoints[index] = points[index] * MapScale;
        }

        return scaledPoints;
    }

    private static int[] Triangulate(IReadOnlyList<Vector2> points)
    {
        var remaining = new List<int>(points.Count);
        var triangles = new List<int>((points.Count - 2) * 3);

        for (int index = 0; index < points.Count; index++)
        {
            remaining.Add(index);
        }

        int safety = points.Count * points.Count;
        while (remaining.Count > 3 && safety-- > 0)
        {
            bool clippedEar = false;

            for (int index = 0; index < remaining.Count; index++)
            {
                int previous = remaining[(index - 1 + remaining.Count) % remaining.Count];
                int current = remaining[index];
                int next = remaining[(index + 1) % remaining.Count];

                if (!IsConvex(points[previous], points[current], points[next]) ||
                    ContainsOtherVertex(points, remaining, previous, current, next))
                {
                    continue;
                }

                triangles.Add(previous);
                triangles.Add(current);
                triangles.Add(next);
                remaining.RemoveAt(index);
                clippedEar = true;
                break;
            }

            if (!clippedEar)
            {
                break;
            }
        }

        if (remaining.Count == 3)
        {
            triangles.Add(remaining[0]);
            triangles.Add(remaining[1]);
            triangles.Add(remaining[2]);
        }

        return triangles.ToArray();
    }

    private static bool IsConvex(Vector2 previous, Vector2 current, Vector2 next)
    {
        return Cross(current - previous, next - current) > 0f;
    }

    private static bool ContainsOtherVertex(
        IReadOnlyList<Vector2> points,
        IReadOnlyList<int> remaining,
        int a,
        int b,
        int c)
    {
        foreach (int index in remaining)
        {
            if (index != a && index != b && index != c &&
                PointInTriangle(points[index], points[a], points[b], points[c]))
            {
                return true;
            }
        }

        return false;
    }

    private static bool PointInTriangle(Vector2 point, Vector2 a, Vector2 b, Vector2 c)
    {
        float ab = Cross(b - a, point - a);
        float bc = Cross(c - b, point - b);
        float ca = Cross(a - c, point - c);
        return ab >= 0f && bc >= 0f && ca >= 0f;
    }

    private static float Cross(Vector2 a, Vector2 b)
    {
        return a.x * b.y - a.y * b.x;
    }
}
