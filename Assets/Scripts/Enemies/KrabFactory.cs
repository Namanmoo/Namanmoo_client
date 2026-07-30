using UnityEngine;

/// <summary>
/// 크랩 한 마리를 만든다. Stage1과 던전 방이 같은 코드를 쓰게 하려고
/// <see cref="Stage1KrabEncounterSetup"/>에서 떼어냈다 — 두 곳에 복사해 두면
/// 체력이나 콜라이더 크기를 한쪽만 고치는 일이 반드시 생긴다.
/// </summary>
public static class KrabFactory
{
    public const int Hitpoints = 5;

    private const float VisualHeight = 2f;

    public static EnemyHealth Create(
        Transform parent,
        Transform player,
        Sprite sprite,
        Vector2 position,
        string name)
    {
        if (player == null)
        {
            throw new System.ArgumentNullException(nameof(player));
        }

        if (sprite == null)
        {
            throw new System.ArgumentNullException(nameof(sprite));
        }

        var krab = new GameObject(name);
        krab.transform.SetParent(parent, false);
        krab.transform.position = new Vector3(position.x, position.y, -0.2f);

        var visual = new GameObject("Krab Visual");
        visual.transform.SetParent(krab.transform, false);
        float visualScale = VisualHeight / sprite.bounds.size.y;
        visual.transform.localScale = new Vector3(visualScale, visualScale, 1f);

        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 4;

        Rigidbody2D body = krab.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;

        CircleCollider2D collider = krab.AddComponent<CircleCollider2D>();
        collider.radius = 0.7f;
        collider.isTrigger = false;

        var sensorObject = new GameObject("Krab Contact Sensor");
        sensorObject.transform.SetParent(krab.transform, false);
        CircleCollider2D sensor = sensorObject.AddComponent<CircleCollider2D>();
        sensor.radius = 0.75f;
        sensor.isTrigger = true;

        EnemyHealth health = krab.AddComponent<EnemyHealth>();
        health.Configure(Hitpoints);
        KrabEnemy enemy = krab.AddComponent<KrabEnemy>();
        enemy.Initialize(player);
        return health;
    }
}
