using UnityEngine;

/// <summary>
/// 장로 술탄 보스 한 마리를 만든다. GameObject 뼈대(루트/비주얼/체력/체력바)는
/// <see cref="BossFactoryBase"/>를 통해 다른 보스와 공유하고, 술탄 전용 콜라이더·
/// 투사체 풀·컨트롤러 초기화만 여기서 담당한다.
/// </summary>
public sealed class SultanBossFactory : BossFactoryBase
{
    public static EnemyHealth Create(
        Transform worldParent,
        Transform uiParent,
        Transform player,
        SultanBossDefinition definition,
        Vector2 position,
        Rect roomBounds,
        Transform westSpawnPoint = null,
        Transform eastSpawnPoint = null)
    {
        RequireNotNull(player, nameof(player));
        RequireNotNull(definition, nameof(definition));

        var boss = CreateRoot(worldParent, "Sultan Boss", position);

        var body = boss.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.freezeRotation = true;
        // 적들과 같은 보간. 없으면 물리 스텝이 있는 프레임에만 위치가 갱신돼
        // 움직임이 끊겨 보이고, 이동량으로 판정하는 걷기 모션도 깜빡인다.
        body.interpolation = RigidbodyInterpolation2D.Interpolate;

        var sensor = boss.AddComponent<CircleCollider2D>();
        sensor.isTrigger = true;
        sensor.radius = definition.ContactRadius;

        SpriteRenderer visual = CreateVisual(
            boss.transform, "Sultan Boss Visual", definition.Phase1Sprite, definition.VisualHeight);

        // 1페이즈 좌우 모션. 적들이 쓰는 컴포넌트를 그대로 재사용한다 —
        // 위치 변화로 Idle/Move를 가르고 좌우를 잡는 로직이 보스에도 그대로 맞는다.
        if (definition.Phase1AnimatorController != null)
        {
            visual.gameObject.AddComponent<EnemyVisualController>()
                .Configure(definition.Phase1Sprite, definition.Phase1AnimatorController);
        }

        EnemyHealth health = CreateHealth(boss, definition.MaxHealth, uiParent);
        health.SurfaceMaterial = "flesh";

        var projectilePool = new GameObject("Sultan Projectile Pool");
        projectilePool.transform.SetParent(worldParent, false);

        var controller = boss.AddComponent<SultanBossController>();
        controller.Initialize(
            definition,
            player,
            health,
            body,
            visual,
            worldParent,
            projectilePool.transform,
            roomBounds,
            westSpawnPoint,
            eastSpawnPoint);

        return health;
    }
}
