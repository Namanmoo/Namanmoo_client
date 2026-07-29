using UnityEngine;

public sealed class WeaponProjectile : MonoBehaviour
{
    private Vector2 direction;
    private WeaponDefinition definition;
    private GameObject owner;
    private float remainingLifetime;
    private bool consumed;

    public void Initialize(WeaponDefinition weapon, Vector2 newDirection, GameObject newOwner)
    {
        definition = weapon;
        direction = newDirection.normalized;
        owner = newOwner;
        remainingLifetime = weapon == null ? 0f : weapon.ProjectileLifetime;

        CircleCollider2D collider = GetComponent<CircleCollider2D>();
        if (collider == null)
        {
            collider = gameObject.AddComponent<CircleCollider2D>();
        }
        collider.isTrigger = true;
        collider.radius = weapon == null ? 0f : weapon.CollisionRadius;

        Rigidbody2D body = GetComponent<Rigidbody2D>();
        if (body == null)
        {
            body = gameObject.AddComponent<Rigidbody2D>();
        }
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;

        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            renderer = gameObject.AddComponent<SpriteRenderer>();
        }
        renderer.sprite = weapon == null ? null : weapon.WorldSprite;
        renderer.color = weapon == null ? Color.white : weapon.DisplayColor;
        renderer.sortingOrder = 5;
    }

    private void Update()
    {
        Advance(Time.deltaTime);
    }

    public void Advance(float deltaTime)
    {
        if (consumed || definition == null || deltaTime <= 0f)
        {
            return;
        }

        transform.position += (Vector3)(direction * definition.ProjectileSpeed * deltaTime);
        remainingLifetime -= deltaTime;
        if (remainingLifetime <= 0f)
        {
            consumed = true;
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryHit(other);
    }

    public bool TryHit(Collider2D other)
    {
        if (consumed || definition == null || other == null ||
            (owner != null && other.transform.IsChildOf(owner.transform)))
        {
            return false;
        }

        EnemyHealth health = other.GetComponentInParent<EnemyHealth>();
        if (health == null)
        {
            return false;
        }

        consumed = true;
        health.TakeDamage(definition.Damage);
        Destroy(gameObject);
        return true;
    }
}
