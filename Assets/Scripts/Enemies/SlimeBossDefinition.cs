using UnityEngine;

[CreateAssetMenu(menuName = "NaManMoo/Slime Boss Definition")]
public sealed class SlimeBossDefinition : ScriptableObject
{
    [Header("Sprites")]
    [SerializeField] private Sprite bodySprite;
    [SerializeField] private Sprite projectileSprite;
    [SerializeField] private Sprite fallMarkerSprite;
    [Header("Boss")]
    [SerializeField, Min(1)] private int maxHealth = 100;
    [SerializeField, Min(0f)] private float moveSpeed = 3f;
    [SerializeField, Min(0.01f)] private float patternInterval = 2f;
    [SerializeField, Min(0)] private int contactDamage = 4;
    [SerializeField, Min(0.01f)] private float visualHeight = 6f;
    [SerializeField, Min(0.01f)] private float contactRadius = 1.2f;
    [Header("Projectile")]
    [SerializeField, Min(0)] private int projectileDamage = 3;
    [SerializeField, Min(0f)] private float projectileSpeed = 8f;
    [SerializeField, Min(0.01f)] private float projectileLifetime = 5f;
    [SerializeField, Min(0.01f)] private float projectileVisualHeight = 0.8f;
    [SerializeField, Min(0.01f)] private float projectileRadius = 0.4f;
    [Header("Burrow Pattern")]
    [SerializeField, Min(0f)] private float burrowWindup = 0.75f;
    [SerializeField, Min(0f)] private float hiddenDuration = 2f;
    [SerializeField, Min(0f)] private float markerSpeed = 2.5f;
    [SerializeField, Min(0.01f)] private float markerVisualHeight = 2f;
    [SerializeField, Min(0.01f)] private float arcDistance = 6f;
    [SerializeField, Min(0.01f)] private float arcDuration = 1f;
    [SerializeField, Min(0f)] private float arcHeight = 1.5f;

    public Sprite BodySprite => bodySprite;
    public Sprite ProjectileSprite => projectileSprite;
    public Sprite FallMarkerSprite => fallMarkerSprite;
    public int MaxHealth => maxHealth;
    public float MoveSpeed => moveSpeed;
    public float PatternInterval => patternInterval;
    public int ContactDamage => contactDamage;
    public float VisualHeight => visualHeight;
    public float ContactRadius => contactRadius;
    public int ProjectileDamage => projectileDamage;
    public float ProjectileSpeed => projectileSpeed;
    public float ProjectileLifetime => projectileLifetime;
    public float ProjectileVisualHeight => projectileVisualHeight;
    public float ProjectileRadius => projectileRadius;
    public float BurrowWindup => burrowWindup;
    public float HiddenDuration => hiddenDuration;
    public float MarkerSpeed => markerSpeed;
    public float MarkerVisualHeight => markerVisualHeight;
    public float ArcDistance => arcDistance;
    public float ArcDuration => arcDuration;
    public float ArcHeight => arcHeight;

    public void ConfigureSprites(Sprite body, Sprite projectile, Sprite marker)
    {
        bodySprite = body;
        projectileSprite = projectile;
        fallMarkerSprite = marker;
    }
}
