using UnityEngine;
using NaManMoo.Dungeon;

public sealed class SlimeBossProjectile : MonoBehaviour
{
    private const float PlayerInvulnerabilityDuration = 1f;
    private Vector2 start;
    private Vector2 direction;
    private float speed;
    private float lifetime;
    private float elapsed;
    private float arcDistance;
    private float arcDuration;
    private float arcHeight;
    private int damage;
    private bool usesArc;
    private Transform visual;

    public void Initialize(Transform visualTransform, int newDamage)
    {
        visual = visualTransform;
        damage = newDamage;
    }

    public void LaunchStraight(Vector2 position, Vector2 newDirection, float newSpeed, float newLifetime)
    {
        Begin(position, newDirection);
        speed = Mathf.Max(0f, newSpeed);
        lifetime = Mathf.Max(0.01f, newLifetime);
        usesArc = false;
    }

    public void LaunchArc(Vector2 position, Vector2 newDirection, float distance, float duration, float height)
    {
        Begin(position, newDirection);
        arcDistance = Mathf.Max(0f, distance);
        arcDuration = Mathf.Max(0.01f, duration);
        arcHeight = Mathf.Max(0f, height);
        lifetime = arcDuration;
        usesArc = true;
    }

    private void Begin(Vector2 position, Vector2 newDirection)
    {
        start = position;
        direction = newDirection == Vector2.zero ? Vector2.right : newDirection.normalized;
        elapsed = 0f;
        transform.position = position;
        if (visual != null) visual.localPosition = Vector3.zero;
    }

    private void Update() => Advance(Time.deltaTime);

    public void Advance(float deltaTime)
    {
        elapsed += Mathf.Max(0f, deltaTime);
        if (usesArc)
        {
            float progress = Mathf.Clamp01(elapsed / arcDuration);
            transform.position = start + direction * (arcDistance * progress);
            if (visual != null)
            {
                visual.localPosition = Vector3.up * (Mathf.Sin(Mathf.PI * progress) * arcHeight);
            }
        }
        else
        {
            transform.position += (Vector3)(direction * speed * deltaTime);
        }

        if (elapsed >= lifetime) Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerHealth health = other == null ? null : other.GetComponentInParent<PlayerHealth>();
        if (health != null && health.TryTakeDamage(damage, Time.time, PlayerInvulnerabilityDuration))
        {
            Destroy(gameObject);
            return;
        }

        if (other != null && other.GetComponentInParent<BulletBlockingObstacle>() != null)
        {
            Destroy(gameObject);
        }
    }
}
