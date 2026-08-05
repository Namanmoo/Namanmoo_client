using System;
using UnityEngine;

public static class SlimeBossFactory
{
    public static EnemyHealth Create(
        Transform worldParent,
        Transform uiParent,
        Transform player,
        SlimeBossDefinition definition,
        Vector2 position)
    {
        if (player == null) throw new ArgumentNullException(nameof(player));
        if (definition == null) throw new ArgumentNullException(nameof(definition));

        var boss = new GameObject("Slime Boss");
        boss.transform.SetParent(worldParent, false);
        boss.transform.position = new Vector3(position.x, position.y, -0.2f);

        var body = boss.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.freezeRotation = true;

        var sensor = boss.AddComponent<CircleCollider2D>();
        sensor.isTrigger = true;
        sensor.radius = definition.ContactRadius;

        var visualObject = new GameObject("Slime Boss Visual");
        visualObject.transform.SetParent(boss.transform, false);
        var visual = visualObject.AddComponent<SpriteRenderer>();
        visual.sprite = definition.BodySprite;
        visual.sortingOrder = 6;
        float scale = definition.VisualHeight / definition.BodySprite.bounds.size.y;
        visualObject.transform.localScale = Vector3.one * scale;

        EnemyHealth health = boss.AddComponent<EnemyHealth>();
        health.Configure(definition.MaxHealth);

        var projectilePool = new GameObject("Slime Projectile Pool");
        projectilePool.transform.SetParent(worldParent, false);
        var controller = boss.AddComponent<SlimeBossController>();
        controller.Initialize(definition, player, health, body, visual, projectilePool.transform);

        if (uiParent != null) BossHealthBarUIFactory.Create(uiParent, health);
        return health;
    }
}
