using System;
using System.Collections.Generic;

/// <summary>
/// 발사체의 거동을 바꾸는 값 묶음. 관통·도탄처럼 "터지는" 게 아니라
/// "무기가 원래 그렇게 생긴" 효과들이 여기에 쌓인다.
/// </summary>
public sealed class ProjectileTuning
{
    /// <summary>맞고도 계속 날아갈 수 있는 횟수. 0이면 첫 명중에 사라진다.</summary>
    public int PierceCount { get; set; }

    /// <summary>벽에 튕길 수 있는 횟수.</summary>
    public int BounceCount { get; set; }
}

/// <summary>
/// 발사체 거동을 바꾸는 효과. 트리거로 터지지 않으므로 <see cref="IEffectAction"/>이 아니다.
/// </summary>
public interface IProjectileModifier
{
    string EffectId { get; }

    void Apply(ProjectileTuning tuning, ParamSet parameters);
}

/// <summary>
/// effect id → 구현 매핑.
///
/// 효과는 두 종류다 — 트리거에 맞춰 터지는 <see cref="IEffectAction"/>과,
/// 발사체가 생길 때 한 번 읽히는 <see cref="IProjectileModifier"/>.
/// 카탈로그 대조는 둘의 합집합으로 한다.
///
/// <see cref="ValidateAgainstCatalog"/>가 이 구조의 핵심이다. 효과를 덜어낼 때
/// 카탈로그·구현·등록 셋 중 하나만 빠뜨려도 테스트가 즉시 잡는다.
/// </summary>
public sealed class EffectRegistry
{
    private readonly Dictionary<string, IEffectAction> actions =
        new Dictionary<string, IEffectAction>(StringComparer.Ordinal);

    private readonly Dictionary<string, IProjectileModifier> modifiers =
        new Dictionary<string, IProjectileModifier>(StringComparer.Ordinal);

    public void Register(IEffectAction action)
    {
        if (action == null || string.IsNullOrEmpty(action.EffectId))
        {
            throw new ArgumentException("EffectId가 비어 있는 효과는 등록할 수 없습니다.");
        }

        actions[action.EffectId] = action;
    }

    public void Register(IProjectileModifier modifier)
    {
        if (modifier == null || string.IsNullOrEmpty(modifier.EffectId))
        {
            throw new ArgumentException("EffectId가 비어 있는 효과는 등록할 수 없습니다.");
        }

        modifiers[modifier.EffectId] = modifier;
    }

    public bool TryResolveAction(string effectId, out IEffectAction action) =>
        actions.TryGetValue(effectId ?? string.Empty, out action);

    public bool TryResolveModifier(string effectId, out IProjectileModifier modifier) =>
        modifiers.TryGetValue(effectId ?? string.Empty, out modifier);

    public IEnumerable<string> RegisteredIds
    {
        get
        {
            foreach (string id in actions.Keys)
            {
                yield return id;
            }

            foreach (string id in modifiers.Keys)
            {
                yield return id;
            }
        }
    }

    /// <summary>
    /// 카탈로그 대비 누락·과잉을 점검한다. 반환이 비어 있으면 완전히 일치한다.
    /// </summary>
    public List<string> ValidateAgainstCatalog(WeaponCatalog catalog)
    {
        var problems = new List<string>();
        if (catalog == null)
        {
            problems.Add("카탈로그를 읽지 못했습니다.");
            return problems;
        }

        foreach (string id in catalog.EffectIds)
        {
            if (!actions.ContainsKey(id) && !modifiers.ContainsKey(id))
            {
                problems.Add($"카탈로그 효과 '{id}'의 구현이 등록되어 있지 않습니다.");
            }
        }

        foreach (string id in RegisteredIds)
        {
            if (catalog.Effect(id) == null)
            {
                problems.Add($"구현 '{id}'가 카탈로그에 없습니다.");
            }
        }

        return problems;
    }

    /// <summary>
    /// 카탈로그에 있는 효과 전부에 대한 기본 구현.
    ///
    /// 효과를 하나 덜어낼 때 지워야 하는 세 곳 중 하나가 여기다
    /// (나머지는 백엔드 카탈로그 JSON과 액션 파일).
    /// </summary>
    public static EffectRegistry CreateDefault()
    {
        var registry = new EffectRegistry();

        // 속성 — 맞은 대상에게 상태이상을 건다
        registry.Register(new FireDotAction());
        registry.Register(new ChillSlowAction());
        registry.Register(new PoisonStackAction());
        registry.Register(new ShockAction());

        // 광역
        registry.Register(new ExplosionAction());
        registry.Register(new ChainAction());

        // 근접 전용
        registry.Register(new ShockwaveAction());
        registry.Register(new LifestealAction());
        registry.Register(new BladeWaveAction());

        // 원거리 전용 — 발사체 거동을 바꾼다
        registry.Register(new PierceModifier());
        registry.Register(new RicochetModifier());

        return registry;
    }

    /// <summary>로드아웃의 효과들을 훑어 발사체 설정을 만든다.</summary>
    public ProjectileTuning BuildTuning(WeaponLoadout loadout)
    {
        var tuning = new ProjectileTuning();
        if (loadout == null)
        {
            return tuning;
        }

        foreach (EffectSpec spec in loadout.Effects)
        {
            if (TryResolveModifier(spec.EffectId, out IProjectileModifier modifier))
            {
                modifier.Apply(tuning, spec.Params);
            }
        }

        return tuning;
    }
}
