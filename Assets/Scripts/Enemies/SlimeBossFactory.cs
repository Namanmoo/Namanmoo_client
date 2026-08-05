using UnityEngine;

public sealed class SlimeBossFactory : BossFactoryBase
{
    public static EnemyHealth Create(
        Transform worldParent,
        Transform uiParent,
        Transform player,
        SlimeBossDefinition definition,
        Vector2 position)
    {
        RequireNotNull(player, nameof(player));
        RequireNotNull(definition, nameof(definition));

        var boss = CreateRoot(worldParent, "Slime Boss", position);

        var body = boss.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.freezeRotation = true;

        var sensor = boss.AddComponent<CircleCollider2D>();
        sensor.isTrigger = true;
        sensor.radius = definition.ContactRadius;

        SpriteRenderer visual = CreateVisual(
            boss.transform, "Slime Boss Visual", definition.BodySprite, definition.VisualHeight);

        EnemyHealth health = CreateHealth(boss, definition.MaxHealth, uiParent);

        var projectilePool = new GameObject("Slime Projectile Pool");
        projectilePool.transform.SetParent(worldParent, false);
        var controller = boss.AddComponent<SlimeBossController>();
        controller.Initialize(definition, player, health, body, visual, projectilePool.transform);

        return health;
    }
}
