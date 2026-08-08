using UnityEngine;

/// <summary>
/// 근접 궤도들이 공유하는 타격 판정. <see cref="WeaponAttackGeometry"/>를 그대로 쓰되
/// 궤도가 사거리·부채꼴을 덮어쓸 수 있게 한다 (찌르기 = 좁고 길게, 회전 = 360도).
/// </summary>
public static class MeleeStrike
{
    /// <summary>범위 안 적을 때리고 맞은 수를 돌려준다. 효과는 맞은 적마다 발동한다.</summary>
    public static int Execute(
        DeliveryContext context, float reachOverride = -1f, float arcOverride = -1f)
    {
        WeaponDefinition weapon = context.Weapon;
        if (weapon == null)
        {
            return 0;
        }

        // 무기가 손에 그려져 있으면 즉발 원 판정 대신 접촉 판정 —
        // 스윙 커브가 움직이는 무기가 적에 닿는 프레임에 때린다.
        // 사거리·부채꼴은 이제 모션이 정하므로 override 값들은 이 길에서 안 쓴다.
        PlayerWeaponVisual visual = context.Owner != null
            ? context.Owner.GetComponent<PlayerWeaponVisual>()
            : null;
        if (visual != null && visual.Renderer != null
            && visual.Renderer.sprite != null && visual.Renderer.enabled)
        {
            WeaponContactSweep.Begin(context, visual);
            return 0;
        }

        if (WeaponContactSweep.DebugLogs)
        {
            Debug.Log($"[sweep] 즉발 경로로 감 — visual={visual != null}"
                + $" renderer={visual?.Renderer != null}"
                + $" sprite={visual?.Renderer?.sprite?.name}"
                + $" enabled={visual?.Renderer?.enabled}");
        }

        // 무기를 손에 그리지 않는 구성(테스트 등)의 대비책 — 예전 즉발 판정.
        // 근접은 스탯이 아니라 화면에 들린 무기 길이가 범위다.
        float reach = reachOverride > 0f
            ? reachOverride
            : WeaponAttackGeometry.VisualReach(weapon);
        float arc = arcOverride > 0f ? arcOverride : weapon.AttackArc;
        int hits = 0;

        foreach (EnemyHealth enemy in EffectHelpers.EnemiesInRadius(
            context.Origin, reach + weapon.CollisionRadius, context.Owner))
        {
            // 회전은 방향이 없으므로 탐색 원(사거리+무기 두께, 콜라이더 겹침)이 찾은
            // 것이 곧 명중이다. 중심 거리로 다시 재면 안 된다 — 플레이어와 적의 몸통
            // 반지름 합(≈1.7)보다 사거리가 짧은 무기는 닿아 보여도 영영 안 맞는다.
            bool hit = arc >= 360f
                || WeaponAttackGeometry.IsMeleeHit(
                    weapon.Type,
                    context.Origin,
                    context.Direction,
                    enemy.transform.position,
                    reach,
                    weapon.CollisionRadius,
                    arc);

            if (!hit)
            {
                continue;
            }

            hits++;
            if (hits == 1)
            {
                // 한 번 휘둘러 여럿을 맞혀도 타격음은 한 번 — 겹치면 소리만 커진다
                NaManMoo.Audio.SfxPlayer.Instance?.Play(
                    NaManMoo.Audio.SfxNames.ImpactCandidates(
                        NaManMoo.Audio.SfxNames.MaterialNameOf(weapon.Material),
                        NaManMoo.Audio.SfxNames.WeightOf(
                            weapon.Damage, weapon.AttackInterval),
                        enemy.SurfaceMaterial));
            }

            enemy.TakeDamage(weapon.Damage);
            EnemyKnockback.Apply(enemy, context.Direction);
            // 죽음 판정은 피해 적용 후 — on_kill이 여기서 갈린다
            context.Runner?.NotifyHit(enemy, enemy.transform.position, context.Direction);
        }

        return hits;
    }
}

/// <summary>휘두르기 — 전방 부채꼴. 기본 근접 동작. 파라미터 없음.</summary>
public sealed class SwingDelivery : IDeliveryBehavior
{
    public string DeliveryId => "swing";

    public void Execute(DeliveryContext context, ParamSet parameters)
    {
        MeleeStrike.Execute(context);
    }
}

/// <summary>찌르기 — 좁고 길게. 파라미터: reachBonus.</summary>
public sealed class ThrustDelivery : IDeliveryBehavior
{
    /// <summary>찌르기의 부채꼴 — 좁을수록 창처럼 나간다.</summary>
    public const float ThrustArc = 20f;

    public string DeliveryId => "thrust";

    public void Execute(DeliveryContext context, ParamSet parameters)
    {
        float reach = (context.Weapon != null
                ? WeaponAttackGeometry.VisualReach(context.Weapon)
                : 1f)
            + parameters.Get("reachBonus", 0.5f);
        MeleeStrike.Execute(context, reachOverride: reach, arcOverride: ThrustArc);
    }
}

/// <summary>회전 베기 — 방향 없이 제자리 한 바퀴. 파라미터 없음.</summary>
public sealed class SpinDelivery : IDeliveryBehavior
{
    public string DeliveryId => "spin";

    public void Execute(DeliveryContext context, ParamSet parameters)
    {
        MeleeStrike.Execute(context, arcOverride: 360f);
    }
}

// 검기(blade_wave)는 궤도가 아니라 효과다 — Effects/Actions/BladeWaveAction.cs.
// 효과라서 어떤 근접 궤도와도 조합된다 (회전 베기 + 검기 등).
