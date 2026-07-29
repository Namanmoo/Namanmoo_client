using UnityEngine;

public sealed class Stage1BossEncounter : MonoBehaviour
{
    private static readonly Vector2 BossSpawnPosition = new Vector2(0f, 13f);

    [SerializeField] private Stage1EncounterGate gate;
    [SerializeField] private Transform player;
    [SerializeField] private Sprite bossSprite;
    [SerializeField] private Transform worldParent;
    [SerializeField] private Transform uiParent;
    [SerializeField] private bool started;

    public bool HasStarted => started;

    public void Initialize(
        Stage1EncounterGate newGate,
        Transform newPlayer,
        Sprite newBossSprite,
        Transform newWorldParent,
        Transform newUiParent)
    {
        gate = newGate;
        player = newPlayer;
        bossSprite = newBossSprite;
        worldParent = newWorldParent;
        uiParent = newUiParent;
        started = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryStart(other);
    }

    public bool TryStart(Collider2D other)
    {
        if (started || gate == null || !gate.IsOpen || other == null ||
            other.GetComponentInParent<PlayerHealth>() == null)
        {
            return false;
        }

        started = true;
        gate.Close();
        SpawnBoss();
        return true;
    }

    private void SpawnBoss()
    {
        var boss = new GameObject("Boss Robot");
        boss.transform.SetParent(worldParent, false);
        boss.transform.position =
            new Vector3(BossSpawnPosition.x, BossSpawnPosition.y, -0.2f);

        var visualObject = new GameObject("Boss Robot Visual");
        visualObject.transform.SetParent(boss.transform, false);
        float visualScale = 6f / bossSprite.bounds.size.y;
        visualObject.transform.localScale =
            new Vector3(visualScale, visualScale, 1f);
        SpriteRenderer visual = visualObject.AddComponent<SpriteRenderer>();
        visual.sprite = bossSprite;
        visual.sortingOrder = 6;

        Rigidbody2D body = boss.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;

        CircleCollider2D bodyCollider = boss.AddComponent<CircleCollider2D>();
        bodyCollider.radius = 1f;

        var sensorObject = new GameObject("Boss Contact Sensor");
        sensorObject.transform.SetParent(boss.transform, false);
        CircleCollider2D sensor = sensorObject.AddComponent<CircleCollider2D>();
        sensor.radius = 1.15f;
        sensor.isTrigger = true;

        foreach (Collider2D playerCollider in
                 player.GetComponentsInChildren<Collider2D>(true))
        {
            Physics2D.IgnoreCollision(bodyCollider, playerCollider, true);
        }

        EnemyHealth health = boss.AddComponent<EnemyHealth>();
        health.Configure(100);
        health.Died += OnBossDied;

        var bulletPool = new GameObject("Boss Bullet Pool");
        bulletPool.transform.SetParent(worldParent, false);

        BossRobotController controller = boss.AddComponent<BossRobotController>();
        controller.Initialize(player, health, visual, bulletPool.transform);
        BossHealthBarUIFactory.Create(uiParent, health);
    }

    private void OnBossDied(EnemyHealth health)
    {
        if (health != null)
        {
            health.Died -= OnBossDied;
        }

        gate.Open();
    }
}
