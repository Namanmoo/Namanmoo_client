/// <summary>관통 — 투사체가 적을 뚫고 계속 날아간다. 파라미터: maxPierceCount.</summary>
public sealed class PierceModifier : IProjectileModifier
{
    public string EffectId => "pierce";

    public void Apply(ProjectileTuning tuning, ParamSet parameters)
    {
        tuning.PierceCount += parameters.GetInt("maxPierceCount", 1);
    }
}

/// <summary>도탄 — 투사체가 벽에 튕긴다. 파라미터: maxBounces.</summary>
public sealed class RicochetModifier : IProjectileModifier
{
    public string EffectId => "ricochet";

    public void Apply(ProjectileTuning tuning, ParamSet parameters)
    {
        tuning.BounceCount += parameters.GetInt("maxBounces", 1);
    }
}
