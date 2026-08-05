using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class StationaryFourWayShooterController : MonoBehaviour
{
    private const string ProjectileName = "Wood Tower Projectile";

    private static readonly Vector2[] Directions =
    {
        Vector2.right,
        Vector2.down,
        Vector2.left,
        Vector2.up
    };

    private static readonly float[] VisualAngles =
    {
        0f,
        270f,
        180f,
        90f
    };

    private EnemyDefinition definition;
    private Rigidbody2D body;
    private EnemyVisualController visualController;
    private float nextAttackTime;

    private void Awake()
    {
        CacheComponents();
    }

    public void Initialize(
        EnemyDefinition newDefinition,
        Transform newTarget,
        float spawnTime)
    {
        definition = newDefinition;
        nextAttackTime = spawnTime + definition.AttackInterval;
        CacheComponents();
        if (body != null)
        {
            body.constraints = RigidbodyConstraints2D.FreezeAll;
        }
    }

    private void Update()
    {
        TryAttack(Time.time);
    }

    public bool TryAttack(float currentTime)
    {
        if (definition == null || currentTime < nextAttackTime)
        {
            return false;
        }

        visualController?.PlayAttack();
        Vector2 origin =
            body != null ? body.position : (Vector2)transform.position;

        for (int i = 0; i < Directions.Length; i++)
        {
            var projectileObject = new GameObject(ProjectileName);
            projectileObject.transform.position = origin;
            EnemyProjectile projectile =
                projectileObject.AddComponent<EnemyProjectile>();
            projectile.Initialize(
                gameObject,
                definition.ProjectileSprite,
                Directions[i],
                definition.AttackDamage,
                definition.ProjectileSpeed,
                definition.ProjectileLifetime,
                definition.ProjectileRadius,
                0f,
                VisualAngles[i]);
        }

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
