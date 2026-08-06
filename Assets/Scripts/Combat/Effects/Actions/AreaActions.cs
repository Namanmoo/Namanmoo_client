using System.Collections.Generic;
using UnityEngine;

/// <summary>폭발 — 터진 지점 주위를 한 번에. 파라미터: explosionDamage, explosionRadius.</summary>
public sealed class ExplosionAction : IEffectAction
{
    public string EffectId => "explosion";

    public void Execute(EffectContext context, ParamSet parameters)
    {
        int damage = EffectHelpers.RoundDamage(parameters.Get("explosionDamage", 3f));
        float radius = parameters.Get("explosionRadius", 1f);

        // 명중한 대상은 무기 피해를 이미 받았다 — 폭발은 주변에만 준다
        foreach (EnemyHealth enemy in EffectHelpers.EnemiesInRadius(
            context.Origin, radius, context.Owner, context.Target))
        {
            enemy.TakeDamage(damage);
        }
    }
}

/// <summary>
/// 연쇄 — 맞은 적에서 주변 적으로 피해가 옮겨붙는다.
/// 파라미터: maxChains, chainDamagePercent, chainRange.
/// </summary>
public sealed class ChainAction : IEffectAction
{
    public string EffectId => "chain";

    public void Execute(EffectContext context, ParamSet parameters)
    {
        int maxChains = parameters.GetInt("maxChains", 1);
        float percent = parameters.Get("chainDamagePercent", 30f);
        float range = parameters.Get("chainRange", 2f);
        int damage = EffectHelpers.RoundDamage(context.WeaponDamage * percent / 100f);

        if (maxChains <= 0 || damage <= 0)
        {
            return;
        }

        // 이미 맞은 적으로 되돌아가지 않게 방문한 적을 기억한다
        var visited = new HashSet<EnemyHealth>();
        if (context.Target != null)
        {
            visited.Add(context.Target);
        }

        Vector2 from = context.Target != null
            ? (Vector2)context.Target.transform.position
            : context.Origin;

        for (int i = 0; i < maxChains; i++)
        {
            EnemyHealth next = Nearest(from, range, context.Owner, visited);
            if (next == null)
            {
                break;
            }

            visited.Add(next);
            from = next.transform.position;
            next.TakeDamage(damage);
        }
    }

    private static EnemyHealth Nearest(
        Vector2 from, float range, GameObject owner, HashSet<EnemyHealth> visited)
    {
        foreach (EnemyHealth candidate in EffectHelpers.NearestEnemies(from, range, -1, owner))
        {
            if (!visited.Contains(candidate))
            {
                return candidate;
            }
        }

        return null;
    }
}
