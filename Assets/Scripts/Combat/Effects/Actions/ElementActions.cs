using UnityEngine;

/// <summary>화염 — 맞은 적을 태워 지속 피해. 파라미터: dotDamagePerSecond, durationSeconds.</summary>
public sealed class FireDotAction : IEffectAction
{
    public string EffectId => "fire_dot";

    public void Execute(EffectContext context, ParamSet parameters)
    {
        EnemyStatus status = EnemyStatus.EnsureOn(context.Target);
        status?.ApplyBurn(
            parameters.Get("dotDamagePerSecond", 1f),
            parameters.Get("durationSeconds", 2f));
    }
}

/// <summary>냉기 — 맞은 적을 느리게. 파라미터: slowPercent, durationSeconds.</summary>
public sealed class ChillSlowAction : IEffectAction
{
    public string EffectId => "chill_slow";

    public void Execute(EffectContext context, ParamSet parameters)
    {
        EnemyStatus status = EnemyStatus.EnsureOn(context.Target);
        status?.ApplyChill(
            parameters.Get("slowPercent", 10f),
            parameters.Get("durationSeconds", 1f));
    }
}

/// <summary>독 — 중첩 도트. 파라미터: damagePerStackPerSecond, maxStacks, durationSeconds.</summary>
public sealed class PoisonStackAction : IEffectAction
{
    public string EffectId => "poison_stack";

    public void Execute(EffectContext context, ParamSet parameters)
    {
        EnemyStatus status = EnemyStatus.EnsureOn(context.Target);
        status?.ApplyPoison(
            parameters.Get("damagePerStackPerSecond", 1f),
            parameters.GetInt("maxStacks", 2),
            parameters.Get("durationSeconds", 3f));
    }
}

/// <summary>전격 — 즉시 추가 피해 + 잠깐 경직. 파라미터: bonusDamage, staggerSeconds.</summary>
public sealed class ShockAction : IEffectAction
{
    public string EffectId => "shock";

    public void Execute(EffectContext context, ParamSet parameters)
    {
        if (context.Target == null)
        {
            return;
        }

        context.Target.TakeDamage(
            EffectHelpers.RoundDamage(parameters.Get("bonusDamage", 2f)));

        // TakeDamage로 죽었으면 GameObject가 파괴 예약된다 — 상태를 붙이지 않는다
        if (context.Target.CurrentHealth > 0)
        {
            EnemyStatus.EnsureOn(context.Target)
                ?.ApplyStagger(parameters.Get("staggerSeconds", 0.2f));
        }
    }
}
