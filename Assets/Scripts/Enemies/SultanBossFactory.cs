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

        var sensor = boss.AddComponent<CircleCollider2D>();
        sensor.isTrigger = true;
        sensor.radius = definition.ContactRadius;

        SpriteRenderer visual = CreateVisual(
            boss.transform, "Sultan Boss Visual", definition.Phase1Sprite, definition.VisualHeight);

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
