using UnityEngine;

public static class WeaponAttackGeometry
{
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
