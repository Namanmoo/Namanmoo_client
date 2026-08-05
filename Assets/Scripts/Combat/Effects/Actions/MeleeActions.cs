using UnityEngine;

/// <summary>
/// 충격파 — 주위를 때리며 밀쳐낸다. 근접 전용.
/// 파라미터: waveDamage, waveRadius, knockbackForce.
/// </summary>
public sealed class ShockwaveAction : IEffectAction
{
    public string EffectId => "shockwave";

    public void Execute(EffectContext context, ParamSet parameters)
    {
        int damage = EffectHelpers.RoundDamage(parameters.Get("waveDamage", 2f));
        float radius = parameters.Get("waveRadius", 1.5f);
        float force = parameters.Get("knockbackForce", 1f);

        foreach (EnemyHealth enemy in EffectHelpers.EnemiesInRadius(
            context.Origin, radius, context.Owner))
        {
            Vector2 away = (Vector2)enemy.transform.position - context.Origin;
            if (away == Vector2.zero)
            {
                away = context.Direction == Vector2.zero ? Vector2.up : context.Direction;
            }

            enemy.TakeDamage(damage);
            // TakeDamage로 죽었으면 파괴 예약 상태 — 밀어봤자 의미가 없고 안전하지도 않다
            if (enemy.CurrentHealth > 0)
            {
                EffectHelpers.Knockback(enemy, away, force);
            }
        }
    }
}

/// <summary>
/// 흡혈 — 준 피해의 일부만큼 회복. 근접 전용. 파라미터: lifestealPercent.
///
/// 소수 회복은 버린다(내림). 최소 1을 보장하면 3% 흡혈 단검이 초당 여러 번 때리며
/// 사실상 무적이 된다 — 낮은 흡혈이 낮게 굴러가는 쪽이 맞다.
/// </summary>
public sealed class LifestealAction : IEffectAction
{
    public string EffectId => "lifesteal";

    public void Execute(EffectContext context, ParamSet parameters)
    {
        if (context.Owner == null)
        {
            return;
        }

        int heal = Mathf.FloorToInt(
            context.WeaponDamage * parameters.Get("lifestealPercent", 3f) / 100f);
        if (heal <= 0)
        {
            return;
        }

        PlayerHealth health = context.Owner.GetComponentInParent<PlayerHealth>();
        health?.Heal(heal);
    }
}
