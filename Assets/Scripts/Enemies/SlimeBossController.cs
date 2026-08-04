using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class SlimeBossController : MonoBehaviour
{
    private const float PlayerInvulnerabilityDuration = 1f;
    private SlimeBossDefinition definition;
    private Transform player;
    private EnemyHealth health;
    private Rigidbody2D body;
    private SpriteRenderer visual;
    private Transform projectileParent;
    private bool patternRunning;

    public void Initialize(
        SlimeBossDefinition newDefinition,
        Transform newPlayer,
        EnemyHealth newHealth,
        Rigidbody2D newBody,
        SpriteRenderer newVisual,
        Transform newProjectileParent)
    {
        definition = newDefinition;
        player = newPlayer;
        health = newHealth;
        body = newBody;
        visual = newVisual;
        projectileParent = newProjectileParent;
        StartCoroutine(PatternLoop());
    }

    private void FixedUpdate()
    {
        if (patternRunning || player == null || definition == null) return;
        Vector2 next = Vector2.MoveTowards(
            body.position, player.position, definition.MoveSpeed * Time.fixedDeltaTime);
        body.MovePosition(next);
    }

    private IEnumerator PatternLoop()
    {
        while (enabled)
        {
            yield return new WaitForSeconds(definition.PatternInterval);
            if (Random.value < 0.5f) Spit();
            else yield return Burrow();
        }
    }

    public void Spit()
    {
        if (player == null) return;
        Vector2 direction = (player.position - transform.position).normalized;
        SlimeBossProjectile projectile = CreateProjectile();
        projectile.LaunchStraight(
            transform.position, direction, definition.ProjectileSpeed, definition.ProjectileLifetime);
    }

    private IEnumerator Burrow()
    {
        patternRunning = true;
        yield return new WaitForSeconds(definition.BurrowWindup);
        health.SetInvulnerable(true);
        visual.enabled = false;

        SlimeFallMaker marker = CreateMarker(transform.position);
        yield return new WaitForSeconds(definition.HiddenDuration);
        transform.position = marker.transform.position;
        Destroy(marker.gameObject);
        visual.enabled = true;
        health.SetInvulnerable(false);

        FireArc(Vector2.right);
        FireArc(Vector2.down);
        FireArc(Vector2.left);
        FireArc(Vector2.up);
        patternRunning = false;
    }

    private void FireArc(Vector2 direction)
    {
        CreateProjectile().LaunchArc(
            transform.position, direction, definition.ArcDistance,
            definition.ArcDuration, definition.ArcHeight);
    }

    private SlimeBossProjectile CreateProjectile()
    {
        var root = new GameObject("Slime Projectile");
        root.transform.SetParent(projectileParent, false);
        var collider = root.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = definition.ProjectileRadius;
        var rigidbody = root.AddComponent<Rigidbody2D>();
        rigidbody.bodyType = RigidbodyType2D.Kinematic;
        rigidbody.gravityScale = 0f;

        var visualObject = new GameObject("Visual");
        visualObject.transform.SetParent(root.transform, false);
        var renderer = visualObject.AddComponent<SpriteRenderer>();
        renderer.sprite = definition.ProjectileSprite;
        renderer.sortingOrder = 7;
        float scale = definition.ProjectileVisualHeight / definition.ProjectileSprite.bounds.size.y;
        visualObject.transform.localScale = Vector3.one * scale;

        var projectile = root.AddComponent<SlimeBossProjectile>();
        projectile.Initialize(visualObject.transform, definition.ProjectileDamage);
        return projectile;
    }

    private SlimeFallMaker CreateMarker(Vector2 position)
    {
        var markerObject = new GameObject("Slime Fall Marker");
        markerObject.transform.SetParent(transform.parent, false);
        markerObject.transform.position = position;
        var renderer = markerObject.AddComponent<SpriteRenderer>();
        renderer.sprite = definition.FallMarkerSprite;
        renderer.sortingOrder = 6;
        float scale = definition.MarkerVisualHeight / definition.FallMarkerSprite.bounds.size.y;
        markerObject.transform.localScale = Vector3.one * scale;
        var marker = markerObject.AddComponent<SlimeFallMaker>();
        marker.Initialize(player, definition.MarkerSpeed);
        return marker;
    }

    private void OnTriggerEnter2D(Collider2D other) => TryDamagePlayer(other);
    private void OnTriggerStay2D(Collider2D other) => TryDamagePlayer(other);

    private void TryDamagePlayer(Collider2D other)
    {
        if (!patternRunning || visual.enabled)
        {
            PlayerHealth playerHealth = other == null ? null : other.GetComponentInParent<PlayerHealth>();
            playerHealth?.TryTakeDamage(
                definition.ContactDamage, Time.time, PlayerInvulnerabilityDuration);
        }
    }
}
