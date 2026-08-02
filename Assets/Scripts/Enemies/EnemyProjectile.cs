using UnityEngine;

public sealed class EnemyProjectile : MonoBehaviour
{
    private const float MinimumLifetime = 0.01f;
    private const float MinimumCollisionRadius = 0.01f;
    private const float PlayerInvulnerabilityDuration = 1f;

    private Vector2 direction;
    private int damage;
    private float speed;
    private float remainingLifetime;
    private GameObject owner;
    private bool initialized;
    private bool consumed;

    public int Damage => damage;
    public float Speed => speed;
    public float RemainingLifetime => remainingLifetime;

    public void Initialize(
        GameObject newOwner,
        Vector2 newDirection,
        int newDamage,
        float newSpeed,
        float newLifetime)
    {
        Initialize(newOwner, null, newDirection, newDamage, newSpeed, newLifetime, MinimumCollisionRadius);
    }

    public void Initialize(
        GameObject newOwner,
        Sprite projectileSprite,
        Vector2 newDirection,
        int newDamage,
        float newSpeed,
        float newLifetime,
        float collisionRadius)
    {
        SpriteRenderer spriteRenderer = GetOrAddComponent<SpriteRenderer>();
        Rigidbody2D body = GetOrAddComponent<Rigidbody2D>();
        CircleCollider2D circleCollider = GetOrAddComponent<CircleCollider2D>();

        spriteRenderer.sprite = projectileSprite;
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        circleCollider.isTrigger = true;
        circleCollider.radius = Mathf.Max(MinimumCollisionRadius, collisionRadius);

        owner = newOwner;
        direction = newDirection.sqrMagnitude > 0f ? newDirection.normalized : Vector2.zero;
        damage = Mathf.Max(0, newDamage);
        speed = Mathf.Max(0f, newSpeed);
        remainingLifetime = Mathf.Max(MinimumLifetime, newLifetime);
        initialized = true;
        consumed = false;
    }

    private void Update()
    {
        if (initialized)
        {
            Advance(Time.deltaTime);
        }
    }

    public void Advance(float deltaTime)
    {
        if (consumed)
        {
            return;
        }

        transform.position += (Vector3)(direction * speed * deltaTime);
        remainingLifetime -= deltaTime;

        if (remainingLifetime <= 0f)
        {
            consumed = true;
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDamagePlayer(other, Time.time);
    }

    public bool TryDamagePlayer(Collider2D other, float currentTime)
    {
        if (consumed || other == null || IsOwnerCollider(other))
        {
            return false;
        }

        PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();
        if (playerHealth == null || !playerHealth.TryTakeDamage(damage, currentTime, PlayerInvulnerabilityDuration))
        {
            return false;
        }

        consumed = true;
        Destroy(gameObject);
        return true;
    }

    private bool IsOwnerCollider(Collider2D other)
    {
        return owner != null &&
            (other.transform == owner.transform ||
             other.transform.IsChildOf(owner.transform) ||
             owner.transform.IsChildOf(other.transform));
    }

    private T GetOrAddComponent<T>() where T : Component
    {
        T component = GetComponent<T>();
        return component != null ? component : gameObject.AddComponent<T>();
    }
}
