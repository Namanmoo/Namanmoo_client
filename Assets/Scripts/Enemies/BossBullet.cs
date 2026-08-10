using UnityEngine;
using NaManMoo.Dungeon;

public sealed class BossBullet : MonoBehaviour
{
    private const int Damage = 4;
    private const float InvulnerabilityDuration = 1f;
    private const float Lifetime = 6f;

    private Vector2 direction;
    private float speed;
    private float remainingLifetime;

    public void Launch(Vector2 newPosition, Vector2 newDirection, float newSpeed)
    {
        transform.position = newPosition;
        direction = newDirection.normalized;
        speed = Mathf.Max(0f, newSpeed);
        remainingLifetime = Lifetime;
        gameObject.SetActive(true);
    }

    private void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
        remainingLifetime -= Time.deltaTime;
        if (remainingLifetime <= 0f)
        {
            gameObject.SetActive(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (TryDamagePlayer(other, Time.time))
        {
            gameObject.SetActive(false);
            return;
        }

        if (other != null && other.GetComponentInParent<BulletBlockingObstacle>() != null)
        {
            gameObject.SetActive(false);
        }
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
                Damage,
                currentTime,
                InvulnerabilityDuration);
    }
}
