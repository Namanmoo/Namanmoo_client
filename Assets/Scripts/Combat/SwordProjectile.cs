using UnityEngine;

public sealed class SwordProjectile : MonoBehaviour
{
    private Vector2 direction;
    private int damage;
    private float speed;
    private float spinSpeed;
    private float remainingLifetime;
    private GameObject owner;
    private bool initialized;
    private bool consumed;

    public void Initialize(
        Vector2 direction,
        int damage,
        float speed,
        float spinSpeed,
        float lifetime,
        GameObject owner)
    {
        this.direction = direction.normalized;
        this.damage = damage;
        this.speed = speed;
        this.spinSpeed = spinSpeed;
        remainingLifetime = lifetime;
        this.owner = owner;
        initialized = true;
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
        transform.Rotate(0f, 0f, spinSpeed * deltaTime);
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
        if (consumed || other == null || IsOwnerCollider(other))
        {
            return false;
        }

        EnemyHealth enemyHealth = other.GetComponent<EnemyHealth>();
        if (enemyHealth == null)
        {
            return false;
        }

        consumed = true;
        // 검기에는 무기 정보가 없다 — 무기 축을 any로 두고 대상 재질만 살린다
        NaManMoo.Audio.SfxPlayer.Instance?.Play(
            NaManMoo.Audio.SfxNames.ImpactCandidates(
                "any", "any", enemyHealth.SurfaceMaterial));
        enemyHealth.TakeDamage(damage);
        EnemyKnockback.Apply(enemyHealth, direction);
        Destroy(gameObject);
        return true;
    }

    private bool IsOwnerCollider(Collider2D other)
    {
        return owner != null && other.transform.IsChildOf(owner.transform);
    }
}
