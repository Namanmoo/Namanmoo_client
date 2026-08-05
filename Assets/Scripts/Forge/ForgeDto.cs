using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 백엔드 POST /forge 응답. JsonUtility로 그대로 파싱되도록 필드 이름을 맞춰 둔다.
/// (JsonUtility는 모르는 필드를 조용히 무시하므로 서버가 budget 같은 진단 정보를
/// 더 내려보내도 문제없다.)
/// </summary>
[Serializable]
public sealed class ForgeResponseDto
{
    public string name;
    public string flavor;

    /// <summary>분류·스탯·궤도·효과가 전부 여기 들어 있다.</summary>
    public ForgeWeaponDto weapon;

    /// <summary>요청한 AI 개입 단계를 그대로 돌려준 값 (0/1/2)</summary>
    public int stage;

    /// <summary>base64 PNG. 0단계이거나 생성이 실패하면 빈 문자열 — 원본 그림을 쓴다.</summary>
    public string image;

    /// <summary>생성에 실패해 원본으로 대체해야 하는가 (0단계는 실패가 아니다)</summary>
    public bool imageFailed;

    /// <summary>"mock" 또는 "stats=...,image=..."</summary>
    public string source;

    /// <summary>무기 생성에 실패해 기본 무기가 지급됐는가</summary>
    public bool fallback;
}

/// <summary>
/// 파라미터 한 개.
///
/// 서버가 사전이 아니라 key/value 배열로 보내는 이유가 둘이다 —
/// JsonUtility는 Dictionary를 파싱하지 못하고, Gemini 구조화 출력은 키가 정해지지 않은
/// 객체를 표현하지 못한다. 스탯도 같은 형태라 파서가 하나로 끝난다.
/// </summary>
[Serializable]
public sealed class ParamPairDto
{
    public string key;
    public float value;
}

[Serializable]
public sealed class ForgeDeliveryDto
{
    public string deliveryId;
    public ParamPairDto[] @params;
}

[Serializable]
public sealed class ForgeEffectDto
{
    public string effectId;
    public string triggerId;
    public ParamPairDto[] @params;
}

[Serializable]
public sealed class ForgeWeaponDto
{
    /// <summary>"melee" 또는 "ranged" — 카탈로그 분류 id</summary>
    public string category;

    /// <summary>"Sword" / "Axe" / "Spear" / "Projectile" / "Gun"</summary>
    public string weaponType;

    /// <summary>키는 분류마다 다르다 (근접 reach·attackArc, 원거리 projectileSpeed·lifetime)</summary>
    public ParamPairDto[] stats;

    public ForgeDeliveryDto delivery;
    public ForgeEffectDto[] effects;
}

/// <summary>
/// 서버 응답을 게임이 쓰는 <see cref="WeaponLoadout"/>으로 옮긴다.
///
/// 서버가 이미 카탈로그와 예산으로 깎아서 보내지만 여기서 한 번 더 조인다
/// — 클라이언트는 서버 응답을 그대로 믿지 않는다. 카탈로그에 없는 id는 버리고,
/// 파라미터는 범위·격자로 다시 스냅한다.
/// </summary>
public static class ForgeWeaponAssembler
{
    /// <summary>서버가 스탯을 빠뜨렸을 때 놓는 자리 (범위의 40% 지점) — 백엔드와 같은 값.</summary>
    public const float MissingStatRatio = 0.4f;

    /// <summary>발사체 충돌 반경. 카탈로그에 없는 연출값이라 여기서 정한다.</summary>
    public const float ProjectileRadius = 0.25f;

    /// <summary>기본 무기 — 응답이 없거나 카탈로그를 못 읽을 때 쥐여 준다.</summary>
    public static WeaponLoadout Fallback(Sprite sprite, string displayName)
    {
        WeaponDefinition definition = WeaponFactory.CreateWeapon(
            ForgedWeapon.ItemId,
            string.IsNullOrWhiteSpace(displayName) ? "만든 무기" : displayName,
            WeaponCategory.Ranged,
            WeaponType.Projectile,
            damage: 5,
            interval: 1f / 3f,
            reach: 1f,
            radius: ProjectileRadius,
            arc: 90f,
            speed: 8f,
            lifetime: 4f,
            sprite,
            Color.white);

        return WeaponLoadout.Plain(definition);
    }

    public static WeaponLoadout FromDto(
        ForgeWeaponDto dto,
        Sprite sprite,
        string displayName,
        WeaponCatalog catalog = null)
    {
        catalog ??= WeaponCatalog.Load();
        CatalogCategory category = catalog?.Category(dto?.category);
        if (category == null)
        {
            return Fallback(sprite, displayName);
        }

        WeaponCategory gameCategory = category.ToWeaponCategory();
        WeaponType weaponType = ResolveWeaponType(dto.weaponType, gameCategory);
        ParamSet stats = ClampStats(category, dto.stats);

        WeaponDefinition definition = WeaponFactory.CreateWeapon(
            ForgedWeapon.ItemId,
            string.IsNullOrWhiteSpace(displayName) ? "만든 무기" : displayName,
            gameCategory,
            weaponType,
            damage: Mathf.RoundToInt(stats.Get("damage", 5f)),
            interval: IntervalFrom(stats, gameCategory),
            reach: stats.Get("reach", 1f),
            radius: ProjectileRadius,
            arc: stats.Get("attackArc", 90f),
            speed: stats.Get("projectileSpeed", 8f),
            lifetime: stats.Get("lifetime", 4f),
            sprite,
            Color.white);

        return new WeaponLoadout(
            definition,
            ResolveDelivery(catalog, category.id, gameCategory, dto.delivery),
            ResolveEffects(catalog, category.id, dto.effects));
    }

    /// <summary>공격 간격 — 근접은 attacksPerSecond, 원거리는 shotsPerSecond를 쓴다.</summary>
    private static float IntervalFrom(ParamSet stats, WeaponCategory category)
    {
        float perSecond = category == WeaponCategory.Melee
            ? stats.Get("attacksPerSecond", 2f)
            : stats.Get("shotsPerSecond", 3f);

        return 1f / Mathf.Max(0.01f, perSecond);
    }

    private static WeaponType ResolveWeaponType(string raw, WeaponCategory category)
    {
        if (Enum.TryParse(raw, out WeaponType parsed)
            && WeaponDefinition.IsCategoryValid(category, parsed))
        {
            return parsed;
        }

        // 분류와 짝이 맞지 않으면 그 분류의 기본 종류로 — WeaponDefinition이 거부하는 조합을
        // 만들면 IsValid가 false가 되어 무기가 아예 공격하지 않는다.
        return category == WeaponCategory.Melee ? WeaponType.Sword : WeaponType.Projectile;
    }

    private static ParamSet ClampStats(CatalogCategory category, ParamPairDto[] raw)
    {
        var values = new Dictionary<string, float>(StringComparer.Ordinal);
        Dictionary<string, float> sent = ToDictionary(raw);

        foreach (CatalogStat stat in category.stats ?? Array.Empty<CatalogStat>())
        {
            float fallback = Mathf.Lerp(stat.min, stat.max, MissingStatRatio);
            float value = sent.TryGetValue(stat.key, out float found) ? found : fallback;
            values[stat.key] = stat.Clamp(Sanitize(value, fallback));
        }

        return new ParamSet(values);
    }

    private static DeliverySpec ResolveDelivery(
        WeaponCatalog catalog,
        string categoryId,
        WeaponCategory gameCategory,
        ForgeDeliveryDto raw)
    {
        CatalogEntry entry = raw == null ? null : catalog.Delivery(raw.deliveryId);
        if (entry == null || !entry.Allows(categoryId))
        {
            return new DeliverySpec(WeaponLoadout.DefaultDeliveryFor(gameCategory), ParamSet.Empty);
        }

        return new DeliverySpec(entry.id, ClampParams(entry, null, raw.@params));
    }

    private static IReadOnlyList<EffectSpec> ResolveEffects(
        WeaponCatalog catalog,
        string categoryId,
        ForgeEffectDto[] raw)
    {
        if (raw == null || raw.Length == 0)
        {
            return WeaponLoadout.NoEffects;
        }

        var resolved = new List<EffectSpec>(raw.Length);
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (ForgeEffectDto item in raw)
        {
            if (item == null)
            {
                continue;
            }

            CatalogEntry effect = catalog.Effect(item.effectId);
            CatalogEntry trigger = catalog.Trigger(item.triggerId);
            if (effect == null || trigger == null || !effect.Allows(categoryId))
            {
                continue;
            }

            if (!seen.Add(effect.id + "@" + trigger.id))
            {
                continue;
            }

            resolved.Add(new EffectSpec(
                effect.id, trigger.id, ClampParams(effect, trigger, item.@params)));
        }

        return resolved.Count == 0 ? WeaponLoadout.NoEffects : resolved;
    }

    /// <summary>
    /// 트리거 파라미터를 먼저 깔고 효과 파라미터로 덮는다 — 키가 겹치면 효과 쪽이 이긴다.
    /// 백엔드 clamp.py와 같은 순서다.
    /// </summary>
    private static ParamSet ClampParams(
        CatalogEntry effect, CatalogEntry trigger, ParamPairDto[] raw)
    {
        var values = new Dictionary<string, float>(StringComparer.Ordinal);
        Dictionary<string, float> sent = ToDictionary(raw);

        foreach (CatalogEntry source in new[] { trigger, effect })
        {
            foreach (CatalogParam param in source?.@params ?? Array.Empty<CatalogParam>())
            {
                float value = sent.TryGetValue(param.key, out float found) ? found : param.min;
                values[param.key] = param.Clamp(Sanitize(value, param.min));
            }
        }

        return new ParamSet(values);
    }

    private static Dictionary<string, float> ToDictionary(ParamPairDto[] pairs)
    {
        var map = new Dictionary<string, float>(StringComparer.Ordinal);
        foreach (ParamPairDto pair in pairs ?? Array.Empty<ParamPairDto>())
        {
            if (pair != null && !string.IsNullOrEmpty(pair.key))
            {
                map[pair.key] = pair.value;
            }
        }

        return map;
    }

    /// <summary>NaN·무한대를 걸러 낸다 — 그대로 두면 무기가 조용히 고장 난다.</summary>
    private static float Sanitize(float value, float fallback) =>
        float.IsNaN(value) || float.IsInfinity(value) ? fallback : value;
}
