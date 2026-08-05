using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 카탈로그 파라미터 묶음 — 서버가 이미 범위·격자로 깎아 보낸 값이다.
/// 없는 키를 물으면 카탈로그의 최소값(또는 지정한 기본값)을 돌려준다.
/// </summary>
public sealed class ParamSet
{
    public static readonly ParamSet Empty = new ParamSet(null);

    private readonly Dictionary<string, float> values;

    public ParamSet(IDictionary<string, float> source)
    {
        values = source == null
            ? new Dictionary<string, float>(StringComparer.Ordinal)
            : new Dictionary<string, float>(source, StringComparer.Ordinal);
    }

    public float Get(string key, float fallback = 0f) =>
        key != null && values.TryGetValue(key, out float value) ? value : fallback;

    public int GetInt(string key, int fallback = 0) =>
        Mathf.RoundToInt(Get(key, fallback));

    public bool Has(string key) => key != null && values.ContainsKey(key);

    public IReadOnlyDictionary<string, float> Values => values;
}

/// <summary>궤도 — 공격이 어떤 모양으로 나가는가. 무기 하나당 정확히 하나.</summary>
public sealed class DeliverySpec
{
    public DeliverySpec(string deliveryId, ParamSet parameters)
    {
        DeliveryId = deliveryId;
        Params = parameters ?? ParamSet.Empty;
    }

    public string DeliveryId { get; }
    public ParamSet Params { get; }
}

/// <summary>효과 블록 하나 = 트리거 + 효과 + 파라미터.</summary>
public sealed class EffectSpec
{
    public EffectSpec(string effectId, string triggerId, ParamSet parameters)
    {
        EffectId = effectId;
        TriggerId = triggerId;
        Params = parameters ?? ParamSet.Empty;
    }

    public string EffectId { get; }
    public string TriggerId { get; }
    public ParamSet Params { get; }
}

/// <summary>
/// 만든 무기 한 자루 = 정의(스탯·분류) + 궤도 + 효과 목록.
///
/// <see cref="WeaponDefinition"/>만으로는 궤도·효과를 담을 수 없어 함께 묶는다.
/// 기존 샘플 무기(검·도끼)는 로드아웃 없이 정의만 갖고, 그 경우 궤도는
/// 분류에 맞는 기본값(straight/swing)으로 동작한다 — 지금 거동 그대로다.
/// </summary>
public sealed class WeaponLoadout
{
    public static readonly IReadOnlyList<EffectSpec> NoEffects = Array.Empty<EffectSpec>();

    public WeaponLoadout(
        WeaponDefinition definition,
        DeliverySpec delivery,
        IReadOnlyList<EffectSpec> effects)
    {
        Definition = definition;
        Delivery = delivery;
        Effects = effects ?? NoEffects;
    }

    public WeaponDefinition Definition { get; }
    public DeliverySpec Delivery { get; }
    public IReadOnlyList<EffectSpec> Effects { get; }

    /// <summary>궤도가 지정되지 않았을 때 이 분류가 쓰는 기본 궤도 id.</summary>
    public static string DefaultDeliveryFor(WeaponCategory category) =>
        category == WeaponCategory.Melee ? "swing" : "straight";

    /// <summary>정의만 있는 무기(샘플 검·도끼)를 위한 로드아웃.</summary>
    public static WeaponLoadout Plain(WeaponDefinition definition)
    {
        WeaponCategory category = definition != null ? definition.Category : WeaponCategory.Ranged;
        return new WeaponLoadout(
            definition,
            new DeliverySpec(DefaultDeliveryFor(category), ParamSet.Empty),
            NoEffects);
    }
}

/// <summary>
/// 무기를 사람이 읽는 문구로. 결과 화면과 무기고 카드가 같은 표현을 쓰도록 한 곳에 둔다.
/// 근접과 원거리는 스탯 축이 달라(사거리 vs 탄속) 분기가 필요하다.
/// </summary>
public static class WeaponSummary
{
    /// <summary>"근접 · 검기" 처럼 분류와 궤도를 한 줄로.</summary>
    public static string Headline(WeaponLoadout loadout, WeaponCatalog catalog = null)
    {
        if (loadout?.Definition == null)
        {
            return string.Empty;
        }

        string category = loadout.Definition.Category == WeaponCategory.Melee ? "근접" : "원거리";
        catalog ??= WeaponCatalog.Load();
        CatalogEntry delivery = catalog?.Delivery(loadout.Delivery?.DeliveryId);

        return delivery == null ? category : $"{category} · {delivery.displayName_ko}";
    }

    /// <summary>"공격력 12  연사 2.5/초  사거리 1.8" — 분류에 맞는 스탯만.</summary>
    public static string Stats(WeaponLoadout loadout)
    {
        WeaponDefinition weapon = loadout?.Definition;
        if (weapon == null)
        {
            return string.Empty;
        }

        float perSecond = 1f / Mathf.Max(0.01f, weapon.AttackInterval);
        string tail = weapon.Category == WeaponCategory.Melee
            ? $"사거리 {weapon.Reach:0.##}"
            : $"탄속 {weapon.ProjectileSpeed:0.##}";

        return $"공격력 {weapon.Damage}  연사 {perSecond:0.##}/초  {tail}";
    }

    /// <summary>"화염, 관통" — 붙은 효과 이름. 없으면 빈 문자열.</summary>
    public static string Effects(WeaponLoadout loadout, WeaponCatalog catalog = null)
    {
        if (loadout == null || loadout.Effects.Count == 0)
        {
            return string.Empty;
        }

        catalog ??= WeaponCatalog.Load();
        var names = new List<string>(loadout.Effects.Count);
        foreach (EffectSpec spec in loadout.Effects)
        {
            CatalogEntry entry = catalog?.Effect(spec.EffectId);
            names.Add(entry != null ? entry.displayName_ko : spec.EffectId);
        }

        return string.Join(", ", names);
    }

    /// <summary>궤도가 실제로 어떻게 움직이는지 — 카탈로그 설명. 없으면 빈 문자열.</summary>
    public static string DeliveryDescription(WeaponLoadout loadout, WeaponCatalog catalog = null)
    {
        catalog ??= WeaponCatalog.Load();
        CatalogEntry delivery = catalog?.Delivery(loadout?.Delivery?.DeliveryId);
        return delivery?.description_ko ?? string.Empty;
    }

    /// <summary>
    /// "명중 시 · 관통 — 적을 꿰뚫고 계속 날아간다." 효과 하나당 한 줄.
    /// 카탈로그에 없는 효과는 id만이라도 보여 준다 — 숨기면 붙은 줄도 모른다.
    /// </summary>
    public static List<string> EffectLines(WeaponLoadout loadout, WeaponCatalog catalog = null)
    {
        var lines = new List<string>();
        if (loadout == null || loadout.Effects.Count == 0)
        {
            return lines;
        }

        catalog ??= WeaponCatalog.Load();
        foreach (EffectSpec spec in loadout.Effects)
        {
            CatalogEntry effect = catalog?.Effect(spec.EffectId);
            CatalogEntry trigger = catalog?.Trigger(spec.TriggerId);

            string name = effect != null ? effect.displayName_ko : spec.EffectId;
            string when = trigger != null ? trigger.displayName_ko : spec.TriggerId;
            string head = string.IsNullOrEmpty(when) ? name : $"{when} · {name}";

            lines.Add(string.IsNullOrEmpty(effect?.description_ko)
                ? head
                : $"{head} — {effect.description_ko}");
        }

        return lines;
    }

    /// <summary>결과 화면용 여러 줄 설명 — 궤도·효과가 뭘 하는지까지 풀어 쓴다.</summary>
    public static string Describe(WeaponLoadout loadout, WeaponCatalog catalog = null)
    {
        catalog ??= WeaponCatalog.Load();
        var lines = new List<string>
        {
            Headline(loadout, catalog),
            DeliveryDescription(loadout, catalog),
            Stats(loadout)
        };
        lines.AddRange(EffectLines(loadout, catalog));

        return string.Join("\n", lines.FindAll(line => !string.IsNullOrEmpty(line)));
    }
}
