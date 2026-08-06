using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class KrabEnemy : MonoBehaviour
{
    [SerializeField, Min(0f)]
    private float moveSpeed = 2.5f;

    [SerializeField, Min(0)]
    private int contactDamage = 2;

    [SerializeField, Min(0f)]
    private float playerInvulnerabilityDuration = 1f;

    private Rigidbody2D body;
    private Collider2D bodyCollider;

    [SerializeField]
    private Transform target;

    private void Awake()
    {
        CacheComponents();
    }

    private void OnEnable()
    {
        ConfigurePlayerOverlap();
    }

    public void Initialize(Transform newTarget)
    {
        target = newTarget;
        CacheComponents();
        ConfigurePlayerOverlap();
    }

    private void FixedUpdate()
    {
        if (body == null || target == null)
        {
            return;
        }

        Vector2 velocity = CalculateVelocity(body.position, target.position, moveSpeed);
        // 냉기·경직 배율 — 상태이상이 없으면 1이라 원래 속도 그대로다
        Vector2 knockback = EnemyStatus.KnockbackVelocityOf(gameObject);
        body.MovePosition(
            body.position
                + velocity * EnemyStatus.SpeedMultiplierOf(gameObject) * Time.fixedDeltaTime
                + knockback * Time.fixedDeltaTime);
    }

    public static Vector2 CalculateVelocity(
        Vector2 currentPosition,
        Vector2 targetPosition,
        float speed)
    {
        Vector2 offset = targetPosition - currentPosition;
        return offset == Vector2.zero
            ? Vector2.zero
            : offset.normalized * Mathf.Max(0f, speed);
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

        PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();
        return playerHealth != null &&
            playerHealth.TryTakeDamage(
                contactDamage,
                currentTime,
                playerInvulnerabilityDuration);
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

        Collider2D[] playerColliders = target.GetComponentsInChildren<Collider2D>(true);
        foreach (Collider2D playerCollider in playerColliders)
        {
            if (playerCollider != null)
            {
                Physics2D.IgnoreCollision(bodyCollider, playerCollider, true);
            }
        }
    }
}
