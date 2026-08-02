using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class ApproachAndShootEnemyController : MonoBehaviour
{
    private const string ProjectileName = "Enemy Projectile";

    private EnemyDefinition definition;
    private Transform target;
    private Rigidbody2D body;
    private EnemyVisualController visualController;
    private float nextAttackTime;

    private void Awake()
    {
        CacheComponents();
    }

    public void Initialize(EnemyDefinition newDefinition, Transform newTarget)
    {
        definition = newDefinition;
        target = newTarget;
        nextAttackTime = 0f;
        CacheComponents();
    }

    private void FixedUpdate()
    {
        if (body == null || target == null || definition == null)
        {
            return;
        }

        Vector2 offsetToTarget = (Vector2)target.position - body.position;
        Vector2 velocity = CalculateVelocity(offsetToTarget);
        body.MovePosition(body.position + velocity * Time.fixedDeltaTime);
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
            definition.ProjectileRadius);

        nextAttackTime = currentTime + definition.AttackInterval;
        return true;
    }

    private void CacheComponents()
    {
        if (body == null)
        {
            body = GetComponent<Rigidbody2D>();
        }

        if (visualController == null)
        {
            visualController = GetComponentInChildren<EnemyVisualController>();
        }
    }
}
