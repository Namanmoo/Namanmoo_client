using System.Collections;
using UnityEngine;

/// <summary>원거리 궤도들이 공유하는 발사체 생성.</summary>
public static class ProjectileSpawner
{
    /// <summary>플레이어 몸에 겹치지 않게 띄우는 거리.</summary>
    public const float SpawnOffset = 0.1f;

    public static WeaponProjectile Spawn(
        DeliveryContext context, Vector2 position, Vector2 direction)
    {
        var projectileObject =
            new GameObject((context.Weapon?.DisplayName ?? "무기") + " Projectile");
        projectileObject.transform.position = position;

        var projectile = projectileObject.AddComponent<WeaponProjectile>();
        projectile.Initialize(
            context.Weapon, direction, context.Owner, context.Tuning, context.Runner);
        return projectile;
    }

    public static Vector2 MuzzleOf(DeliveryContext context)
    {
        float offset = Mathf.Max(
            context.Weapon != null ? context.Weapon.CollisionRadius : 0.1f, SpawnOffset);
        return context.Origin + context.Direction * offset;
    }

    /// <summary>
    /// 성능을 바꾼 일회용 정의. 검기·낙하처럼 "무기 사본이되 위력이 다른" 발사체가 쓴다.
    /// </summary>
    public static WeaponDefinition DerivedWeapon(
        WeaponDefinition source, int damage, float speed, float lifetime)
    {
        return WeaponFactory.CreateWeapon(
            source != null ? source.Id + "-derived" : "derived",
            source != null ? source.DisplayName : "무기",
            WeaponCategory.Ranged,
            WeaponType.Projectile,
            damage,
            interval: 1f,
            reach: 1f,
            radius: source != null ? source.CollisionRadius : 0.25f,
            arc: 90f,
            speed: speed,
            lifetime: lifetime,
            source != null ? source.WorldSprite : null,
            source != null ? source.DisplayColor : Color.white);
    }
}

/// <summary>직선 발사 — 기본 궤도. 파라미터 없음.</summary>
public sealed class StraightDelivery : IDeliveryBehavior
{
    public string DeliveryId => "straight";

    public void Execute(DeliveryContext context, ParamSet parameters)
    {
        ProjectileSpawner.Spawn(context, ProjectileSpawner.MuzzleOf(context), context.Direction);
    }
}

/// <summary>유도 — 가까운 적을 쫓는다. 파라미터: turnRateDegPerSecond, acquireRange.</summary>
public sealed class HomingDelivery : IDeliveryBehavior
{
    public string DeliveryId => "homing";

    public void Execute(DeliveryContext context, ParamSet parameters)
    {
        WeaponProjectile projectile = ProjectileSpawner.Spawn(
            context, ProjectileSpawner.MuzzleOf(context), context.Direction);
        projectile.EnableHoming(
            parameters.Get("turnRateDegPerSecond", 90f),
            parameters.Get("acquireRange", 3f));
    }
}

/// <summary>부메랑 — 나갔다가 돌아오며 두 번 때린다. 파라미터: maxDistance.</summary>
public sealed class BoomerangDelivery : IDeliveryBehavior
{
    public string DeliveryId => "boomerang";

    public void Execute(DeliveryContext context, ParamSet parameters)
    {
        WeaponProjectile projectile = ProjectileSpawner.Spawn(
            context, ProjectileSpawner.MuzzleOf(context), context.Direction);
        projectile.EnableBoomerang(parameters.Get("maxDistance", 3f));
    }
}

/// <summary>
/// 무기 강림 — 적 주변 허공에 무기 사본이 차례로 나타나 적을 향해 쏘아진다.
/// 파라미터: count, spawnRadius, delayBetweenSeconds.
///
/// 표적이 없으면 조준 방향 한 발로 대체한다 — 허공에 소환해 봤자 아무 데도 못 쏜다.
/// </summary>
public sealed class SummonVolleyDelivery : IDeliveryBehavior
{
    /// <summary>표적을 찾는 반경. 화면 안 어디든 잡히는 정도.</summary>
    public const float TargetSearchRadius = 8f;

    public string DeliveryId => "summon_volley";

    public void Execute(DeliveryContext context, ParamSet parameters)
    {
        var nearest = EffectHelpers.NearestEnemies(
            context.Origin + context.Direction * 2f, TargetSearchRadius, 1, context.Owner);

        if (nearest.Count == 0 || context.Host == null)
        {
            ProjectileSpawner.Spawn(
                context, ProjectileSpawner.MuzzleOf(context), context.Direction);
            return;
        }

        context.Host.StartCoroutine(VolleyRoutine(
            context,
            nearest[0],
            parameters.GetInt("count", 2),
            parameters.Get("spawnRadius", 1f),
            parameters.Get("delayBetweenSeconds", 0.1f)));
    }

    private static IEnumerator VolleyRoutine(
        DeliveryContext context, EnemyHealth target, int count, float radius, float delay)
    {
        for (int i = 0; i < count; i++)
        {
            if (target == null)
            {
                yield break; // 표적이 먼저 죽었으면 남은 소환은 접는다
            }

            Vector2 targetPos = target.transform.position;
            Vector2 spawnAt = targetPos + Random.insideUnitCircle.normalized * radius;
            Vector2 towards = (targetPos - spawnAt).normalized;

            ProjectileSpawner.Spawn(context, spawnAt, towards);

            if (delay > 0f && i < count - 1)
            {
                yield return new WaitForSeconds(delay);
            }
        }
    }
}

/// <summary>
/// 낙하 — 조준 지점 위 허공에서 무기가 쏟아진다.
/// 파라미터: count, spreadRadius, intervalSeconds.
/// </summary>
public sealed class RainDelivery : IDeliveryBehavior
{
    /// <summary>조준 지점 — 플레이어 앞 이만큼. 적이 있으면 그쪽으로 당긴다.</summary>
    public const float AimDistance = 4f;

    /// <summary>떨어지기 시작하는 높이.</summary>
    public const float DropHeight = 4f;

    public string DeliveryId => "rain";

    public void Execute(DeliveryContext context, ParamSet parameters)
    {
        Vector2 aim = context.Origin + context.Direction * AimDistance;
        var nearby = EffectHelpers.NearestEnemies(aim, AimDistance, 1, context.Owner);
        if (nearby.Count > 0)
        {
            aim = nearby[0].transform.position;
        }

        if (context.Host == null)
        {
            ProjectileSpawner.Spawn(
                context, ProjectileSpawner.MuzzleOf(context), context.Direction);
            return;
        }

        context.Host.StartCoroutine(RainRoutine(
            context,
            aim,
            parameters.GetInt("count", 3),
            parameters.Get("spreadRadius", 1.5f),
            parameters.Get("intervalSeconds", 0.1f)));
    }

    private static IEnumerator RainRoutine(
        DeliveryContext context, Vector2 aim, int count, float spread, float interval)
    {
        for (int i = 0; i < count; i++)
        {
            Vector2 landing = aim + Random.insideUnitCircle * spread;
            Vector2 spawnAt = landing + Vector2.up * DropHeight;

            ProjectileSpawner.Spawn(context, spawnAt, Vector2.down);

            if (interval > 0f && i < count - 1)
            {
                yield return new WaitForSeconds(interval);
            }
        }
    }
}
