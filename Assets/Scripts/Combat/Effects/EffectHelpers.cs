using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 효과 구현들이 공유하는 조회. 주변 적 찾기가 거의 전부라 한곳에 모아 둔다.
/// </summary>
public static class EffectHelpers
{
    /// <summary>
    /// 반경 안의 적들. 자기 자신(공격자)과 이미 맞은 대상은 걸러 낸다.
    /// 같은 적의 콜라이더가 여럿일 수 있어 <see cref="EnemyHealth"/> 기준으로 중복을 없앤다.
    /// </summary>
    public static List<EnemyHealth> EnemiesInRadius(
        Vector2 center,
        float radius,
        GameObject owner = null,
        EnemyHealth exclude = null)
    {
        var found = new List<EnemyHealth>();
        if (radius <= 0f)
        {
            return found;
        }

        var seen = new HashSet<EnemyHealth>();
        foreach (Collider2D candidate in Physics2D.OverlapCircleAll(center, radius))
        {
            if (candidate == null)
            {
                continue;
            }

            if (owner != null && candidate.transform.IsChildOf(owner.transform))
            {
                continue;
            }

            EnemyHealth health = candidate.GetComponentInParent<EnemyHealth>();
            if (health == null || health == exclude || !seen.Add(health))
            {
                continue;
            }

            found.Add(health);
        }

        return found;
    }

    /// <summary>가까운 순으로 정렬해 최대 몇 마리까지.</summary>
    public static List<EnemyHealth> NearestEnemies(
        Vector2 center,
        float radius,
        int max,
        GameObject owner = null,
        EnemyHealth exclude = null)
    {
        List<EnemyHealth> found = EnemiesInRadius(center, radius, owner, exclude);
        found.Sort((a, b) =>
            ((Vector2)a.transform.position - center).sqrMagnitude
                .CompareTo(((Vector2)b.transform.position - center).sqrMagnitude));

        if (max >= 0 && found.Count > max)
        {
            found.RemoveRange(max, found.Count - max);
        }

        return found;
    }

    /// <summary>소수 피해를 정수로. 0.4처럼 작은 값이 통째로 사라지지 않게 최소 1을 보장한다.</summary>
    public static int RoundDamage(float amount)
    {
        if (amount <= 0f)
        {
            return 0;
        }

        return Mathf.Max(1, Mathf.RoundToInt(amount));
    }

    /// <summary>대상을 살짝 밀어낸다. Rigidbody2D가 없으면 위치를 직접 옮긴다.</summary>
    public static void Knockback(EnemyHealth target, Vector2 direction, float force)
    {
        if (target == null || force <= 0f || direction == Vector2.zero)
        {
            return;
        }

        Vector2 push = direction.normalized * force;
        var body = target.GetComponent<Rigidbody2D>();
        if (body != null && body.bodyType == RigidbodyType2D.Dynamic)
        {
            body.AddForce(push, ForceMode2D.Impulse);
            return;
        }

        target.transform.position += (Vector3)(push * Time.deltaTime);
    }
}
