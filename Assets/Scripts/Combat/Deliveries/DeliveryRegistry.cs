using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>궤도가 실행되는 순간의 상황.</summary>
public readonly struct DeliveryContext
{
    public DeliveryContext(
        MonoBehaviour host,
        GameObject owner,
        Vector2 origin,
        Vector2 direction,
        WeaponLoadout loadout,
        ProjectileTuning tuning,
        EffectRunner runner)
    {
        Host = host;
        Owner = owner;
        Origin = origin;
        Direction = direction;
        Loadout = loadout;
        Tuning = tuning;
        Runner = runner;
    }

    /// <summary>코루틴을 돌릴 곳 — 시간차 소환(무기 강림·낙하)이 쓴다.</summary>
    public MonoBehaviour Host { get; }

    public GameObject Owner { get; }
    public Vector2 Origin { get; }
    public Vector2 Direction { get; }
    public WeaponLoadout Loadout { get; }

    /// <summary>관통·도탄 등 발사체 거동 설정. 근접 궤도는 무시한다.</summary>
    public ProjectileTuning Tuning { get; }

    /// <summary>효과 발동기. null이면 효과 없는 무기(샘플)다.</summary>
    public EffectRunner Runner { get; }

    public WeaponDefinition Weapon => Loadout?.Definition;
}

/// <summary>
/// 궤도 실행 구현. 카탈로그의 delivery id 하나당 하나가 <see cref="DeliveryRegistry"/>에 등록된다.
/// 궤도를 덜어낼 때도 효과와 같은 3곳 규칙이다 — 카탈로그 블록·구현·등록 한 줄.
/// </summary>
public interface IDeliveryBehavior
{
    string DeliveryId { get; }

    void Execute(DeliveryContext context, ParamSet parameters);
}

/// <summary>delivery id → 구현 매핑. <see cref="EffectRegistry"/>와 같은 대조 규칙을 따른다.</summary>
public sealed class DeliveryRegistry
{
    private readonly Dictionary<string, IDeliveryBehavior> behaviors =
        new Dictionary<string, IDeliveryBehavior>(StringComparer.Ordinal);

    public void Register(IDeliveryBehavior behavior)
    {
        if (behavior == null || string.IsNullOrEmpty(behavior.DeliveryId))
        {
            throw new ArgumentException("DeliveryId가 비어 있는 궤도는 등록할 수 없습니다.");
        }

        behaviors[behavior.DeliveryId] = behavior;
    }

    public bool TryResolve(string deliveryId, out IDeliveryBehavior behavior) =>
        behaviors.TryGetValue(deliveryId ?? string.Empty, out behavior);

    public IEnumerable<string> RegisteredIds => behaviors.Keys;

    public List<string> ValidateAgainstCatalog(WeaponCatalog catalog)
    {
        var problems = new List<string>();
        if (catalog == null)
        {
            problems.Add("카탈로그를 읽지 못했습니다.");
            return problems;
        }

        foreach (string id in catalog.DeliveryIds)
        {
            if (!behaviors.ContainsKey(id))
            {
                problems.Add($"카탈로그 궤도 '{id}'의 구현이 등록되어 있지 않습니다.");
            }
        }

        foreach (string id in behaviors.Keys)
        {
            if (catalog.Delivery(id) == null)
            {
                problems.Add($"구현 '{id}'가 카탈로그에 없습니다.");
            }
        }

        return problems;
    }

    /// <summary>카탈로그의 궤도 전부에 대한 기본 구현.</summary>
    public static DeliveryRegistry CreateDefault()
    {
        var registry = new DeliveryRegistry();

        // 원거리
        registry.Register(new StraightDelivery());
        registry.Register(new HomingDelivery());
        registry.Register(new BoomerangDelivery());
        // SummonVolleyDelivery(무기 강림)·RainDelivery(낙하)는 카탈로그에서 뺐다 —
        // 근접 이펙트 또는 스킬로 되살릴 예정이라 구현은 남겨 둔다. 등록하면
        // 카탈로그 대조 검사가 고아 구현으로 잡는다.

        // 근접 — 검기(blade_wave)는 궤도가 아니라 효과다 (BladeWaveAction)
        registry.Register(new SwingDelivery());
        registry.Register(new ThrustDelivery());
        registry.Register(new SpinDelivery());

        return registry;
    }
}
