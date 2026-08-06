using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 무기 카탈로그 — 분류·궤도·트리거·효과의 단일 원본.
///
/// 원본은 백엔드(Namanmoo_Backend의 app/forge/weapon-catalog.json)에 있고, 이 프로젝트는
/// tools/sync-catalog.py로 복사해 온 사본(Assets/Resources/weapon-catalog.json)을 읽는다.
/// 서버가 "AI가 고를 수 있는 것"을, 여기가 "게임이 실행할 수 있는 것"을 담당하며
/// 둘이 어긋나면 <see cref="EffectRegistry"/>·<see cref="DeliveryRegistry"/>의
/// 대조 검사가 잡는다.
///
/// 효과를 덜어낼 때는 백엔드 JSON에서 블록을 지우고 동기화 스크립트를 돌린 뒤
/// 대응하는 액션 파일과 등록 한 줄을 지우면 된다.
/// </summary>
public sealed class WeaponCatalog
{
    public const string ResourceName = "weapon-catalog";

    private static WeaponCatalog cached;

    private readonly Dictionary<string, CatalogCategory> categories;
    private readonly Dictionary<string, CatalogEntry> deliveries;
    private readonly Dictionary<string, CatalogEntry> triggers;
    private readonly Dictionary<string, CatalogEntry> effects;

    private WeaponCatalog(CatalogJson raw)
    {
        Version = raw.version;
        Budget = raw.budget ?? new CatalogBudget();

        categories = (raw.categories ?? Array.Empty<CatalogCategory>())
            .ToDictionary(c => c.id, StringComparer.Ordinal);
        deliveries = (raw.deliveries ?? Array.Empty<CatalogEntry>())
            .ToDictionary(d => d.id, StringComparer.Ordinal);
        triggers = (raw.triggers ?? Array.Empty<CatalogEntry>())
            .ToDictionary(t => t.id, StringComparer.Ordinal);
        effects = (raw.effects ?? Array.Empty<CatalogEntry>())
            .ToDictionary(e => e.id, StringComparer.Ordinal);
    }

    public string Version { get; }
    public CatalogBudget Budget { get; }

    public IReadOnlyCollection<string> CategoryIds => categories.Keys;
    public IReadOnlyCollection<string> DeliveryIds => deliveries.Keys;
    public IReadOnlyCollection<string> TriggerIds => triggers.Keys;
    public IReadOnlyCollection<string> EffectIds => effects.Keys;

    /// <summary>Resources에서 읽어 캐시한다. 실패하면 null을 돌려주고 경고만 남긴다.</summary>
    public static WeaponCatalog Load()
    {
        if (cached != null)
        {
            return cached;
        }

        TextAsset asset = Resources.Load<TextAsset>(ResourceName);
        if (asset == null)
        {
            Debug.LogError(
                $"무기 카탈로그를 찾지 못했습니다 (Resources/{ResourceName}.json). " +
                "tools/sync-catalog.py 를 실행하세요.");
            return null;
        }

        cached = Parse(asset.text);
        return cached;
    }

    /// <summary>테스트에서 임의의 JSON을 넣기 위한 진입점.</summary>
    public static WeaponCatalog Parse(string json)
    {
        CatalogJson raw = JsonUtility.FromJson<CatalogJson>(json);
        if (raw == null)
        {
            throw new ArgumentException("무기 카탈로그 JSON을 해석하지 못했습니다.", nameof(json));
        }

        return new WeaponCatalog(raw);
    }

    /// <summary>에디터에서 카탈로그를 갈아 끼웠을 때 캐시를 비운다.</summary>
    public static void ClearCache()
    {
        cached = null;
    }

    public CatalogCategory Category(string id) =>
        id != null && categories.TryGetValue(id, out CatalogCategory value) ? value : null;

    public CatalogEntry Delivery(string id) =>
        id != null && deliveries.TryGetValue(id, out CatalogEntry value) ? value : null;

    public CatalogEntry Trigger(string id) =>
        id != null && triggers.TryGetValue(id, out CatalogEntry value) ? value : null;

    public CatalogEntry Effect(string id) =>
        id != null && effects.TryGetValue(id, out CatalogEntry value) ? value : null;

    public IEnumerable<CatalogEntry> DeliveriesFor(string categoryId) =>
        deliveries.Values.Where(d => d.Allows(categoryId));

    /// <summary>무기 종류와 1:1로 짝인 궤도. 검=swing, 도끼=spin, 창=thrust 식이다.</summary>
    public CatalogEntry DeliveryForType(WeaponType type) =>
        deliveries.Values.FirstOrDefault(
            d => string.Equals(d.weaponType, type.ToString(), StringComparison.Ordinal));

    public IEnumerable<CatalogEntry> EffectsFor(string categoryId) =>
        effects.Values.Where(e => e.Allows(categoryId));
}

/// <summary>JSON 최상위. JsonUtility가 읽을 수 있게 필드 이름을 원본과 맞춘다.</summary>
[Serializable]
public sealed class CatalogJson
{
    public string version;
    public CatalogCategory[] categories;
    public CatalogEntry[] deliveries;
    public CatalogEntry[] triggers;
    public CatalogEntry[] effects;
    public CatalogBudget budget;
}

[Serializable]
public sealed class CatalogBudget
{
    public float weaponBase = 100f;
    public float effortBonusMax = 15f;
    public int maxEffects = 3;
}

[Serializable]
public sealed class CatalogCategory
{
    public string id;
    public string displayName_ko;
    public string description_ko;
    public string[] weaponTypes;
    public CatalogStat[] stats;

    /// <summary>이 분류에 해당하는 게임 쪽 열거값. 카탈로그 id와 짝이 맞지 않으면 Ranged로 본다.</summary>
    public WeaponCategory ToWeaponCategory() =>
        string.Equals(id, "melee", StringComparison.Ordinal)
            ? WeaponCategory.Melee
            : WeaponCategory.Ranged;

    public CatalogStat Stat(string key)
    {
        if (stats == null || key == null)
        {
            return null;
        }

        foreach (CatalogStat stat in stats)
        {
            if (string.Equals(stat.key, key, StringComparison.Ordinal))
            {
                return stat;
            }
        }

        return null;
    }
}

[Serializable]
public sealed class CatalogStat
{
    public string key;
    public float min;
    public float max;
    public float weight;

    public float Clamp(float value) => Mathf.Clamp(value, min, max);
}

/// <summary>궤도·트리거·효과가 공유하는 형태 — id + 설명 + 비용 + 파라미터.</summary>
[Serializable]
public sealed class CatalogEntry
{
    public string id;
    public string displayName_ko;
    public string description_ko;
    public float baseCost;
    public string[] categories;
    public CatalogParam[] @params;

    /// <summary>궤도 전용 — 이 궤도와 1:1로 짝인 무기 종류. 비어 있으면 짝이 없어 뽑히지 않는다.</summary>
    public string weaponType;

    /// <summary>categories가 비어 있으면 분류를 가리지 않는다 (트리거가 그렇다).</summary>
    public bool Allows(string categoryId)
    {
        if (categories == null || categories.Length == 0)
        {
            return true;
        }

        foreach (string candidate in categories)
        {
            if (string.Equals(candidate, categoryId, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    public CatalogParam Param(string key)
    {
        if (@params == null || key == null)
        {
            return null;
        }

        foreach (CatalogParam param in @params)
        {
            if (string.Equals(param.key, key, StringComparison.Ordinal))
            {
                return param;
            }
        }

        return null;
    }
}

[Serializable]
public sealed class CatalogParam
{
    public string key;
    public float min;
    public float max;
    public float step;
    public float costPerStep;

    public float Clamp(float value)
    {
        float bounded = Mathf.Clamp(value, min, max);
        if (step <= 0f)
        {
            return bounded;
        }

        float steps = Mathf.Round((bounded - min) / step);
        return min + steps * step;
    }
}
