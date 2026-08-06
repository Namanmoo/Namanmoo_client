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

    /// <summary>
    /// 검기 참격 모양 — 서버가 무기 JSON에 저장한 좌표를 조립 때 옮겨 담는다.
    /// 직렬화하지 않는다 (무기고에서 다시 불러올 때 JSON에서 새로 채워진다).
    /// 없는 무기(샘플 검 등)가 검기를 달면 그때 랜덤으로 하나 뽑아 채운다.
    /// </summary>
    public SlashSprites.Shape? Slash { get; set; }

    /// <summary>
    /// 그린 무기의 축 보정 각도. 그림에서 그립→끝 방향이 "위"에서 벗어난 만큼이며,
    /// 손에 들 때 이만큼 되돌려 끝이 늘 바깥을 향하게 한다. 카탈로그 무기는 0.
    /// Slash처럼 직렬화하지 않는다 — 만들 때·무기고에서 꺼낼 때 채워진다.
    /// </summary>
    public float SpriteAxisDegrees { get; set; }

    /// <summary>
    /// 그림 위 그립→끝 방향이 "위"에서 벗어난 각도. 끝이 그립과 같은 자리면
    /// 축을 알 수 없으므로 0(위로 뻗은 그림)으로 친다.
    /// </summary>
    public static float AxisDegrees(Vector2 grip01, Vector2 tip01)
    {
        Vector2 axis = tip01 - grip01;
        if (axis.sqrMagnitude < 0.0001f)
        {
            return 0f;
        }

        // Atan2는 오른쪽이 0도 — 위쪽 기준으로 90도 돌려 맞춘다 (PlayerWeaponVisual.AngleFor와 같은 규약)
        return Mathf.Atan2(axis.y, axis.x) * Mathf.Rad2Deg - 90f;
    }
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
            : type == WeaponType.Projectile || type == WeaponType.Missile
                || type == WeaponType.Boomerang;
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
