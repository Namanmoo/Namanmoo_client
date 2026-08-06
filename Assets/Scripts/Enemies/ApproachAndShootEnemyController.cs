using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class ApproachAndShootEnemyController : MonoBehaviour
{
    private const string ProjectileName = "Enemy Projectile";
    private const string SquirrelId = "squirrel";
    private const string ContactSensorName = "Contact Sensor";
    private const int ContactDamage = 2;
    private const float PlayerInvulnerabilityDuration = 1f;
    private const float DefaultSensorRadius = 0.5f;
    private const float ProjectileRotationSpeed = 720f;

    private EnemyDefinition definition;
    private Transform target;
    private Rigidbody2D body;
    private Collider2D bodyCollider;
    private EnemyVisualController visualController;
    private float nextAttackTime;

    private void Awake()
    {
        CacheComponents();
        EnsureContactSensor();
    }

    private void OnEnable()
    {
        ConfigurePlayerOverlap();
    }

    public void Initialize(EnemyDefinition newDefinition, Transform newTarget)
    {
        definition = newDefinition;
        target = newTarget;
        nextAttackTime = 0f;
        CacheComponents();
        EnsureContactSensor();
        ConfigurePlayerOverlap();
    }

    private void FixedUpdate()
    {
        if (body == null || target == null || definition == null)
        {
            return;
        }

        Vector2 offsetToTarget = (Vector2)target.position - body.position;
        Vector2 velocity = CalculateVelocity(offsetToTarget);
        // 냉기·경직 배율 — 상태이상이 없으면 1이라 원래 속도 그대로다
        Vector2 knockback = EnemyStatus.KnockbackVelocityOf(gameObject);
        body.MovePosition(
            body.position
                + velocity * EnemyStatus.SpeedMultiplierOf(gameObject) * Time.fixedDeltaTime
                + knockback * Time.fixedDeltaTime);
        if (velocity == Vector2.zero)
        {
            TryAttack(Time.time);
        }
    }

    public Vector2 CalculateVelocity(Vector2 offsetToTarget)
    {
        if (definition == null ||
            offsetToTarget.sqrMagnitude <=
            definition.AttackRange * definition.AttackRange)
        {
            return Vector2.zero;
        }

        return offsetToTarget.normalized * definition.MoveSpeed;
    }

    public bool TryAttack(float currentTime)
    {
        if (definition == null || target == null || currentTime < nextAttackTime)
        {
            return false;
        }

        Vector2 origin = body != null ? body.position : (Vector2)transform.position;
        Vector2 offsetToTarget = (Vector2)target.position - origin;
        if (offsetToTarget.sqrMagnitude >
            definition.AttackRange * definition.AttackRange)
        {
            return false;
        }

        visualController?.PlayAttack();

        GameObject projectileObject = new GameObject(ProjectileName);
        projectileObject.transform.position = origin;
        EnemyProjectile projectile =
            projectileObject.AddComponent<EnemyProjectile>();
        projectile.Initialize(
            gameObject,
            definition.ProjectileSprite,
            offsetToTarget,
            definition.AttackDamage,
            definition.ProjectileSpeed,
            definition.ProjectileLifetime,
            definition.ProjectileRadius,
            definition.Id == SquirrelId ? ProjectileRotationSpeed : 0f);

        nextAttackTime = currentTime + definition.AttackInterval;
        return true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDamagePlayer(other, Time.time);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryDamagePlayer(other, Time.time);
    }

    public bool TryDamagePlayer(Collider2D other, float currentTime)
    {
        if (other == null)
        {
            return false;
        }

        PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
        if (health == null ||
            !health.TryTakeDamage(
                ContactDamage,
                currentTime,
                PlayerInvulnerabilityDuration))
        {
            return false;
        }

        visualController?.PlayAttack();
        return true;
    }

    private void CacheComponents()
    {
        if (body == null)
        {
            body = GetComponent<Rigidbody2D>();
        }

        if (bodyCollider == null)
        {
            bodyCollider = GetComponent<Collider2D>();
        }

        if (visualController == null)
        {
            visualController = GetComponentInChildren<EnemyVisualController>();
        }
    }

    private void EnsureContactSensor()
    {
        Transform existing = transform.Find(ContactSensorName);
        CircleCollider2D sensor;
        if (existing == null)
        {
            var sensorObject = new GameObject(ContactSensorName);
            sensorObject.transform.SetParent(transform, false);
            sensor = sensorObject.AddComponent<CircleCollider2D>();
        }
        else
        {
            sensor = existing.GetComponent<CircleCollider2D>();
            if (sensor == null)
            {
                sensor = existing.gameObject.AddComponent<CircleCollider2D>();
            }
        }

        sensor.isTrigger = true;
        CircleCollider2D circleBody = bodyCollider as CircleCollider2D;
        sensor.radius = circleBody != null
            ? circleBody.radius
            : DefaultSensorRadius;
    }

    private void ConfigurePlayerOverlap()
    {
        if (target == null)
        {
            return;
        }

        CacheComponents();
        if (bodyCollider == null)
        {
            return;
        }

        foreach (Collider2D playerCollider in
            target.GetComponentsInChildren<Collider2D>(true))
        {
            if (playerCollider != null)
            {
                Physics2D.IgnoreCollision(bodyCollider, playerCollider, true);
            }
        }
    }
}
