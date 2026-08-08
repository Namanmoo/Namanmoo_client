using System.Collections.Generic;
using UnityEngine;

public static class WeaponAttackGeometry
{
    /// <summary>
    /// 점에서 다각형(무기 외곽선)까지 거리 — 안이면 0, 밖이면 가장 가까운 변까지.
    /// 접촉 판정이 적 몸(원)과 무기 그림의 실제 모양을 맞대 볼 때 쓴다.
    /// 계산만 하므로 EditMode 테스트로 덮는다.
    /// </summary>
    public static float DistanceToPolygon(Vector2 point, IReadOnlyList<Vector2> polygon)
    {
        int count = polygon.Count;
        if (count == 0)
        {
            return float.PositiveInfinity;
        }

        if (count == 1)
        {
            return Vector2.Distance(point, polygon[0]);
        }

        bool inside = false;
        float nearestEdge = float.PositiveInfinity;
        for (int i = 0, j = count - 1; i < count; j = i++)
        {
            Vector2 a = polygon[j];
            Vector2 b = polygon[i];
            nearestEdge = Mathf.Min(nearestEdge, DistanceToSegment(point, a, b));

            // 짝홀 교차 검사 — 점에서 오른쪽 반직선이 변을 몇 번 지나는지
            if ((b.y > point.y) != (a.y > point.y)
                && point.x < (a.x - b.x) * (point.y - b.y) / (a.y - b.y) + b.x)
            {
                inside = !inside;
            }
        }

        return inside ? 0f : nearestEdge;
    }

    /// <summary>
    /// 근접 판정 사거리 = 화면에 들린 무기의 칼끝 거리. 눈에 닿아 보이는데 안 맞거나
    /// 안 닿아 보이는데 맞으면 안 된다 — 근접은 스탯이 아니라 그림이 범위다.
    ///
    /// 칼끝 거리 = 그립 궤도 반지름 + (pivot에서 가장 먼 픽셀까지) × 손 배율.
    /// 스프라이트가 없으면(테스트·아이콘 없는 구성) 스탯 사거리로 돌아간다.
    /// </summary>
    public static float VisualReach(WeaponDefinition weapon)
    {
        if (weapon == null)
        {
            return 0f;
        }

        Sprite sprite = weapon.WorldSprite != null ? weapon.WorldSprite : weapon.Icon;
        if (weapon.Category != WeaponCategory.Melee || sprite == null)
        {
            return weapon.Reach;
        }

        // pivot이 곧 그립이다. 축이 기울어 그려진 무기도 덮도록
        // 바운드 네 모서리 중 가장 먼 곳을 끝으로 본다.
        Bounds bounds = sprite.bounds;
        float x = Mathf.Max(Mathf.Abs(bounds.min.x), Mathf.Abs(bounds.max.x));
        float y = Mathf.Max(Mathf.Abs(bounds.min.y), Mathf.Abs(bounds.max.y));
        float tipExtent = Mathf.Sqrt(x * x + y * y);

        return PlayerWeaponVisual.DefaultHandOffset.magnitude
            + tipExtent * PlayerWeaponVisual.WeaponScale;
    }

    /// <summary>선분 위에서 점에 가장 가까운 자리.</summary>
    public static Vector2 ClosestPointOnSegment(Vector2 point, Vector2 start, Vector2 end)
    {
        Vector2 span = end - start;
        float lengthSquared = span.sqrMagnitude;
        if (lengthSquared <= Mathf.Epsilon)
        {
            return start;
        }

        float t = Mathf.Clamp01(Vector2.Dot(point - start, span) / lengthSquared);
        return start + span * t;
    }

    /// <summary>
    /// 점에서 선분까지 최단 거리 — 접촉 판정이 무기(그립→끝 선분)와 적 몸의
    /// 거리를 잴 때 쓴다. 계산만 하므로 EditMode 테스트로 덮는다.
    /// </summary>
    public static float DistanceToSegment(Vector2 point, Vector2 start, Vector2 end)
    {
        return Vector2.Distance(point, ClosestPointOnSegment(point, start, end));
    }

    /// <summary>
    /// 다각형(무기 외곽선)에서 점에 가장 가까운 자리 — 안이면 점 그대로.
    /// 여기서 나온 자리를 적 콜라이더에 맞대 보면 "그림이 몸에 닿았는가"가 된다.
    /// </summary>
    public static Vector2 ClosestPointOnPolygon(Vector2 point, IReadOnlyList<Vector2> polygon)
    {
        int count = polygon.Count;
        if (count == 0)
        {
            return point;
        }

        if (DistanceToPolygon(point, polygon) <= 0f)
        {
            return point;
        }

        Vector2 nearest = polygon[0];
        float nearestDistance = float.PositiveInfinity;
        for (int i = 0, j = count - 1; i < count; j = i++)
        {
            Vector2 candidate = ClosestPointOnSegment(point, polygon[j], polygon[i]);
            float distance = Vector2.Distance(point, candidate);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = candidate;
            }
        }

        return nearest;
    }

    public static bool IsMeleeHit(
        WeaponType type,
        Vector2 origin,
        Vector2 direction,
        Vector2 target,
        float reach,
        float collisionRadius,
        float attackArc)
    {
        Vector2 offset = target - origin;
        float distance = offset.magnitude;
        if (distance > reach + collisionRadius)
        {
            return false;
        }

        if (type == WeaponType.Axe)
        {
            return distance <= reach;
        }

        if (direction == Vector2.zero || distance <= Mathf.Epsilon)
        {
            return true;
        }

        Vector2 normalizedDirection = direction.normalized;
        if (type == WeaponType.Spear)
        {
            float forward = Vector2.Dot(offset, normalizedDirection);
            float sideways = Mathf.Abs(
                normalizedDirection.x * offset.y - normalizedDirection.y * offset.x);
            return forward >= 0f && forward <= reach && sideways <= collisionRadius;
        }

        if (type == WeaponType.Sword)
        {
            float angle = Vector2.Angle(normalizedDirection, offset);
            return distance <= reach && angle <= attackArc * 0.5f + 0.0001f;
        }

        return false;
    }
}
