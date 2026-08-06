using UnityEngine;

/// <summary>
/// 검기 — 공격할 때마다(on_attack) 테마색 참격이 전방으로 날아간다.
/// 파라미터: waveSpeed, waveRange, waveDamagePercent.
///
/// 궤도가 아니라 효과라서 어떤 근접 궤도와도 조합된다 — 회전 베기 + 검기,
/// 찌르기 + 검기처럼. 참격 모양은 무기별 시드에서 굽는다(<see cref="SlashSprites"/>).
///
/// 참격 자체는 효과 발동기를 달지 않는다 — 참격이 또 효과를 부르면
/// 검기가 검기를 낳는 되먹임이 생긴다.
/// </summary>
public sealed class BladeWaveAction : IEffectAction
{
    /// <summary>기본 참격 속도 — 카탈로그 범위(6~16) 안, 눈에 띄게 빠르게.</summary>
    public const float DefaultWaveSpeed = 10f;

    public string EffectId => "blade_wave";

    public void Execute(EffectContext context, ParamSet parameters)
    {
        if (context.Direction == Vector2.zero)
        {
            return; // 방향 없는 트리거(쿨다운 등)로 걸려도 쏠 곳이 없다
        }

        WeaponDefinition wave = WaveWeapon(
            context.Weapon, context.WeaponDamage, parameters);

        var projectileObject = new GameObject(wave.DisplayName + " Wave");
        projectileObject.transform.position =
            context.Origin + context.Direction * 0.5f;

        var projectile = projectileObject.AddComponent<WeaponProjectile>();
        projectile.Initialize(wave, context.Direction, context.Owner);

        // 발사체는 스스로 돌지 않는다 — 참격의 진한 쪽(위)을 진행 방향으로
        projectile.transform.rotation =
            Quaternion.Euler(0f, 0f, PlayerWeaponVisual.AngleFor(context.Direction));

        // 날아가며 잔상을 흘린다 — 혜성 꼬리
        projectileObject.AddComponent<ProjectileTrail>();
    }

    /// <summary>
    /// 참격 한 발의 정의 — 무기에 저장된 모양 + 테마색 + 깎인 위력.
    /// 씬 없이 만들 수 있어 EditMode 테스트로 덮는다.
    /// </summary>
    public static WeaponDefinition WaveWeapon(
        WeaponDefinition source, int weaponDamage, ParamSet parameters)
    {
        float speed = parameters.Get("waveSpeed", DefaultWaveSpeed);
        float range = parameters.Get("waveRange", 5f);
        int damage = EffectHelpers.RoundDamage(
            weaponDamage * parameters.Get("waveDamagePercent", 30f) / 100f);

        Sprite sourceSprite = source != null ? source.WorldSprite : null;
        Color fallback = source != null ? source.DisplayColor : Color.white;
        WeaponTheme theme = WeaponTheme.Of(sourceSprite, fallback);

        return WeaponFactory.CreateWeapon(
            (source != null ? source.Id : "unknown") + "-wave",
            source != null ? source.DisplayName : "참격",
            WeaponCategory.Ranged,
            WeaponType.Projectile,
            damage,
            interval: 1f,
            reach: 1f,
            // 참격 그림이 큼직한 만큼 판정도 — 보이는데 안 맞으면 억울하다
            radius: (source != null ? source.CollisionRadius : 0.25f) * 1.5f,
            arc: 90f,
            speed: speed,
            lifetime: range / Mathf.Max(0.1f, speed),
            SlashSprite(source),
            theme.Primary);
    }

    /// <summary>
    /// 무기의 참격 스프라이트. 서버가 무기 JSON에 저장한 모양을 쓰고,
    /// 모양이 없는 무기(샘플 검에 디버그로 검기를 달았을 때 등)는
    /// 그 자리에서 하나 뽑아 무기에 붙여 둔다 — 그 무기 동안은 고정된다.
    /// </summary>
    public static Sprite SlashSprite(WeaponDefinition source)
    {
        if (source == null)
        {
            return SlashSprites.ForShape(SlashSprites.ShapeFor(0), 0);
        }

        if (source.Slash == null)
        {
            source.Slash = SlashSprites.RandomShape(
                new System.Random(source.GetHashCode()));
        }

        return SlashSprites.ForShape(source.Slash.Value, source.GetHashCode());
    }
}
