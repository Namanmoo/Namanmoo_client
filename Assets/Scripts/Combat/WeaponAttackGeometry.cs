using UnityEngine;

public static class WeaponAttackGeometry
{
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
