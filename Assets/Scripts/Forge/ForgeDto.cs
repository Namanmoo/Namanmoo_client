using UnityEngine;

/// <summary>
/// 백엔드 POST /forge 응답. JsonUtility로 그대로 파싱되도록 필드 이름을 맞춰 둔다.
/// (JsonUtility는 모르는 필드를 조용히 무시하므로 서버가 clamp 같은 진단 정보를
/// 더 내려보내도 문제없다.)
/// </summary>
[System.Serializable]
public sealed class ForgeResponseDto
{
    public string name;
    public string flavor;
    public ForgeStatsDto stats;

    /// <summary>요청한 AI 개입 단계를 그대로 돌려준 값 (0/1/2)</summary>
    public int stage;

    /// <summary>base64 PNG. 0단계이거나 생성이 실패하면 빈 문자열 — 원본 그림을 쓴다.</summary>
    public string image;

    /// <summary>생성에 실패해 원본으로 대체해야 하는가 (0단계는 실패가 아니다)</summary>
    public bool imageFailed;

    /// <summary>"gemini" 또는 "mock"</summary>
    public string source;

    /// <summary>스탯 생성에 실패해 기본 무기가 지급됐는가</summary>
    public bool fallback;
}

[System.Serializable]
public sealed class ForgeStatsDto
{
    public float damage;
    public float shotsPerSecond;
    public float projectileSpeed;
    public float lifetime;
}

/// <summary>
/// 게임에서 쓰는 무기 성능. 서버가 이미 깎아서 보내지만 여기서 한 번 더 조인다
/// — 클라이언트는 서버 응답을 그대로 믿지 않는다.
/// </summary>
public sealed class WeaponStats
{
    // 백엔드 app/forge/schema.py의 STAT_RANGES와 같은 값을 쓴다.
    // 한쪽만 바꾸면 두 검증이 어긋나므로 함께 고쳐야 한다.
    public const int MinDamage = 1;
    public const int MaxDamage = 30;
    public const float MinShotsPerSecond = 0.5f;
    public const float MaxShotsPerSecond = 8f;
    public const float MinProjectileSpeed = 3f;
    public const float MaxProjectileSpeed = 20f;
    public const float MinLifetime = 0.5f;
    public const float MaxLifetime = 8f;

    public WeaponStats(int damage, float shotsPerSecond, float projectileSpeed, float lifetime)
    {
        Damage = Mathf.Clamp(damage, MinDamage, MaxDamage);
        ShotsPerSecond = Mathf.Clamp(shotsPerSecond, MinShotsPerSecond, MaxShotsPerSecond);
        ProjectileSpeed = Mathf.Clamp(projectileSpeed, MinProjectileSpeed, MaxProjectileSpeed);
        Lifetime = Mathf.Clamp(lifetime, MinLifetime, MaxLifetime);
    }

    public int Damage { get; }
    public float ShotsPerSecond { get; }
    public float ProjectileSpeed { get; }
    public float Lifetime { get; }

    /// <summary>기존 검의 성능 — 만든 무기가 없을 때 쓰는 기준값</summary>
    public static WeaponStats Default => new WeaponStats(5, 3f, 8f, 4f);

    public static WeaponStats FromDto(ForgeStatsDto dto)
    {
        if (dto == null)
        {
            return Default;
        }

        return new WeaponStats(
            Mathf.RoundToInt(dto.damage),
            dto.shotsPerSecond,
            dto.projectileSpeed,
            dto.lifetime);
    }
}
