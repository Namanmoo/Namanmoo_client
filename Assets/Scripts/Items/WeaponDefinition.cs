using UnityEngine;

[CreateAssetMenu(menuName = "NaManMoo/Weapon Definition")]
public sealed class WeaponDefinition : ScriptableObject
{
    [SerializeField] private string weaponId;
    [SerializeField] private string displayName;
    [SerializeField] private WeaponCategory category;
    [SerializeField] private WeaponType weaponType;
    [SerializeField, Min(0)] private int damage;
    [SerializeField, Min(0.01f)] private float attackInterval = 1f;
    [SerializeField, Min(0f)] private float reach = 1f;
    [SerializeField, Min(0f)] private float collisionRadius = 0.25f;
    [SerializeField, Range(0f, 360f)] private float attackArc = 90f;
    [SerializeField, Min(0f)] private float projectileSpeed;
    [SerializeField, Min(0f)] private float projectileLifetime;
    [SerializeField] private Sprite icon;
    [SerializeField] private Sprite worldSprite;
    [SerializeField] private Color displayColor = Color.white;

    public string Id => weaponId;
    public string DisplayName => displayName;
    public WeaponCategory Category => category;
    public WeaponType Type => weaponType;
    public int Damage => damage;
    public float AttackInterval => attackInterval;
    public float Reach => reach;
    public float CollisionRadius => collisionRadius;
    public float AttackArc => attackArc;
    public float ProjectileSpeed => projectileSpeed;
    public float ProjectileLifetime => projectileLifetime;
    public Sprite Icon => icon;
    public Sprite WorldSprite => worldSprite;
    public Color DisplayColor => displayColor;
    public bool IsValid =>
        !string.IsNullOrEmpty(weaponId) && IsCategoryValid(category, weaponType);

    public void Configure(
        string id,
        string newDisplayName,
        WeaponCategory newCategory,
        WeaponType newType,
        int damage,
        float attackInterval,
        float reach,
        float collisionRadius,
        float attackArc,
        float projectileSpeed,
        float projectileLifetime,
        Sprite icon,
        Sprite worldSprite,
        Color displayColor)
    {
        weaponId = id;
        displayName = newDisplayName;
        category = newCategory;
        weaponType = newType;
        this.damage = damage;
        this.attackInterval = attackInterval;
        this.reach = reach;
        this.collisionRadius = collisionRadius;
        this.attackArc = attackArc;
        this.projectileSpeed = projectileSpeed;
        this.projectileLifetime = projectileLifetime;
        this.icon = icon;
        this.worldSprite = worldSprite;
        this.displayColor = displayColor;
        ClampValues();
    }

    public static bool IsCategoryValid(WeaponCategory category, WeaponType type)
    {
        return category == WeaponCategory.Melee
            ? type == WeaponType.Spear || type == WeaponType.Sword || type == WeaponType.Axe
            : type == WeaponType.Projectile || type == WeaponType.Gun;
    }

    private void OnValidate()
    {
        ClampValues();
    }

    private void ClampValues()
    {
        damage = Mathf.Max(0, damage);
        attackInterval = Mathf.Max(0.01f, attackInterval);
        reach = Mathf.Max(0f, reach);
        collisionRadius = Mathf.Max(0f, collisionRadius);
        attackArc = Mathf.Clamp(attackArc, 0f, 360f);
        projectileSpeed = Mathf.Max(0f, projectileSpeed);
        projectileLifetime = Mathf.Max(0f, projectileLifetime);
    }
}
