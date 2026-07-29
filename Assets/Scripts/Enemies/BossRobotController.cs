using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class BossRobotController : MonoBehaviour
{
    public const float ChaseSpeed = 2f;
    public const float BulletSpeed = 3.75f;
    public const float DashSpeed = 15f;
    public const int RadialWaveCount = 3;
    public const float PatternInterval = 2f;

    private const int ContactDamage = 4;
    private const float InvulnerabilityDuration = 1f;
    private const float WaveInterval = 0.15f;
    private const float DashWindup = 2f;
    private const float DashDuration = 0.6f;
    private const float DashRecovery = 1f;
    private const int PoolSize = 48;

    private enum CombatState
    {
        Chase,
        Stationary,
        Dash
    }

    [SerializeField] private Transform target;
    [SerializeField] private EnemyHealth health;
    [SerializeField] private SpriteRenderer visual;

    private readonly List<BossBullet> bullets = new List<BossBullet>();
    private Rigidbody2D body;
    private CombatState state = CombatState.Stationary;
    private Vector2 dashDirection;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
    }

    public void Initialize(
        Transform newTarget,
        EnemyHealth newHealth,
        SpriteRenderer newVisual,
        Transform bulletParent)
    {
        target = newTarget;
        health = newHealth;
        visual = newVisual;
        body = GetComponent<Rigidbody2D>();
        CreateBulletPool(bulletParent);

        health.HealthChanged += OnHealthChanged;
        OnHealthChanged(health.CurrentHealth, health.MaxHealth);
        StartCoroutine(CombatLoop());
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.HealthChanged -= OnHealthChanged;
        }
    }

    private void FixedUpdate()
    {
        if (body == null || target == null)
        {
            return;
        }

        if (state == CombatState.Chase)
        {
            MoveTowards(target.position, ChaseSpeed);
        }
        else if (state == CombatState.Dash)
        {
            body.MovePosition(
                body.position + dashDirection * DashSpeed * Time.fixedDeltaTime);
        }
    }

    private IEnumerator CombatLoop()
    {
        while (health != null && health.CurrentHealth > 0)
        {
            state = CombatState.Chase;
            yield return new WaitForSeconds(PatternInterval);

            if (ShouldDash(Random.value, health.CurrentHealth, health.MaxHealth))
            {
                yield return DashPattern();
            }
            else
            {
                yield return BulletPattern();
            }
        }
    }

    private IEnumerator BulletPattern()
    {
        state = CombatState.Stationary;
        Vector2[] directions = CreateRadialDirections();
        for (int wave = 0; wave < RadialWaveCount; wave++)
        {
            foreach (Vector2 direction in directions)
            {
                FireBullet(direction);
            }

            if (wave < RadialWaveCount - 1)
            {
                yield return new WaitForSeconds(WaveInterval);
            }
        }
    }

    private IEnumerator DashPattern()
    {
        state = CombatState.Stationary;
        yield return new WaitForSeconds(DashWindup);
        dashDirection = target == null
            ? Vector2.zero
            : ((Vector2)target.position - body.position).normalized;
        state = CombatState.Dash;
        yield return new WaitForSeconds(DashDuration);
        state = CombatState.Stationary;
        yield return new WaitForSeconds(DashRecovery);
    }

    private void MoveTowards(Vector2 position, float speed)
    {
        Vector2 offset = position - body.position;
        if (offset != Vector2.zero)
        {
            body.MovePosition(
                body.position +
                offset.normalized * speed * Time.fixedDeltaTime);
        }
    }

    private void FireBullet(Vector2 direction)
    {
        BossBullet bullet = bullets.Find(candidate => !candidate.gameObject.activeSelf);
        if (bullet != null)
        {
            bullet.Launch(transform.position, direction, BulletSpeed);
        }
    }

    private void CreateBulletPool(Transform parent)
    {
        Sprite bulletSprite = CreateCircleSprite();
        for (int index = 0; index < PoolSize; index++)
        {
            var bulletObject = new GameObject("Boss Bullet " + (index + 1));
            bulletObject.transform.SetParent(parent, false);
            bulletObject.transform.localScale = new Vector3(0.7f, 0.7f, 1f);
            SpriteRenderer renderer = bulletObject.AddComponent<SpriteRenderer>();
            renderer.sprite = bulletSprite;
            renderer.color = new Color(1f, 0.2f, 0.15f, 1f);
            renderer.sortingOrder = 7;
            CircleCollider2D collider = bulletObject.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.45f;
            BossBullet bullet = bulletObject.AddComponent<BossBullet>();
            bullets.Add(bullet);
            bulletObject.SetActive(false);
        }
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
                ContactDamage,
                currentTime,
                InvulnerabilityDuration);
    }

    private void OnHealthChanged(int current, int maximum)
    {
        if (visual != null)
        {
            visual.color = GetHealthTint(current, maximum);
        }
    }

    public static bool ShouldDash(float roll, int current, int maximum)
    {
        float chance = maximum > 0 && current <= maximum * 0.5f ? 0.7f : 0.5f;
        return roll < chance;
    }

    public static Color GetHealthTint(int current, int maximum)
    {
        return maximum > 0 && current <= maximum * 0.5f
            ? new Color(1f, 0.65f, 0.65f, 1f)
            : Color.white;
    }

    public static Vector2[] CreateRadialDirections()
    {
        Vector2 diagonalUpRight = new Vector2(1f, 1f).normalized;
        Vector2 diagonalUpLeft = new Vector2(-1f, 1f).normalized;
        Vector2 diagonalDownLeft = new Vector2(-1f, -1f).normalized;
        Vector2 diagonalDownRight = new Vector2(1f, -1f).normalized;
        return new[]
        {
            Vector2.right,
            diagonalUpRight,
            Vector2.up,
            diagonalUpLeft,
            Vector2.left,
            diagonalDownLeft,
            Vector2.down,
            diagonalDownRight
        };
    }

    private static Sprite CreateCircleSprite()
    {
        const int size = 16;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "Boss Bullet Circle",
            filterMode = FilterMode.Point
        };
        var pixels = new Color[size * size];
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.45f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                pixels[y * size + x] =
                    Vector2.Distance(new Vector2(x, y), center) <= radius
                        ? Color.white
                        : Color.clear;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            size);
    }
}
