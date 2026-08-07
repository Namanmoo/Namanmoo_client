using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 보스 로봇 한 마리와 딸린 체력 바를 만든다. Stage1의 보스 조우와 던전 보스방이
/// 같은 코드를 쓰게 <see cref="Stage1BossEncounter"/>에서 떼어냈다 — 체력이나
/// 콜라이더를 한쪽만 고치는 일이 생기지 않게.
/// </summary>
public static class BossFactory
{
    public const int Hitpoints = 100;

    private const float VisualHeight = 6f;
    private const float BodyRadius = 1f;
    private const float SensorRadius = 1.15f;

    /// <summary>
    /// 보스를 세우고 체력을 돌려준다. 죽음을 감지하려면 돌려받은
    /// <see cref="EnemyHealth.Died"/>를 구독한다.
    /// </summary>
    public static EnemyHealth Create(
        Transform worldParent,
        Transform uiParent,
        Transform player,
        Sprite bossSprite,
        Vector2 position)
    {
        if (player == null)
        {
            throw new System.ArgumentNullException(nameof(player));
        }

        if (bossSprite == null)
        {
            throw new System.ArgumentNullException(nameof(bossSprite));
        }

        var boss = new GameObject("Boss Robot");
        boss.transform.SetParent(worldParent, false);
        boss.transform.position = new Vector3(position.x, position.y, -0.2f);

        var visualObject = new GameObject("Boss Robot Visual");
        visualObject.transform.SetParent(boss.transform, false);
        float visualScale = VisualHeight / bossSprite.bounds.size.y;
        visualObject.transform.localScale = new Vector3(visualScale, visualScale, 1f);
        SpriteRenderer visual = visualObject.AddComponent<SpriteRenderer>();
        visual.sprite = bossSprite;
        visual.sortingOrder = 6;

        Rigidbody2D body = boss.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;

        CircleCollider2D bodyCollider = boss.AddComponent<CircleCollider2D>();
        bodyCollider.radius = BodyRadius;

        var sensorObject = new GameObject("Boss Contact Sensor");
        sensorObject.transform.SetParent(boss.transform, false);
        CircleCollider2D sensor = sensorObject.AddComponent<CircleCollider2D>();
        sensor.radius = SensorRadius;
        sensor.isTrigger = true;

        // 몸끼리 밀지 않게 — 접촉 피해는 센서가 따로 본다
        foreach (Collider2D playerCollider in player.GetComponentsInChildren<Collider2D>(true))
        {
            Physics2D.IgnoreCollision(bodyCollider, playerCollider, true);
        }

        EnemyHealth health = boss.AddComponent<EnemyHealth>();
        health.Configure(Hitpoints);
        health.SurfaceMaterial = "metal";

        var bulletPool = new GameObject("Boss Bullet Pool");
        bulletPool.transform.SetParent(worldParent, false);

        BossRobotController controller = boss.AddComponent<BossRobotController>();
        controller.Initialize(player, health, visual, bulletPool.transform);

        if (uiParent != null)
        {
            BossHealthBarUIFactory.Create(uiParent, health);
        }

        return health;
    }

    /// <summary>
    /// 보스 체력 바를 담을 화면 캔버스. 방과 함께 사라지도록 방 아래에 만든다.
    /// </summary>
    public static Transform CreateOverlayCanvas(Transform parent, string name)
    {
        var canvasObject = new GameObject(
            name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(parent, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        return canvasObject.transform;
    }
}
