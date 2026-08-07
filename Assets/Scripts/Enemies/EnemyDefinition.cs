using UnityEngine;

[CreateAssetMenu(menuName = "NaManMoo/Enemy Definition")]
public sealed class EnemyDefinition : ScriptableObject
{
    [SerializeField] private string enemyId;
    [SerializeField] private string displayName;
    [SerializeField] private Sprite bodySprite;
    [SerializeField] private Sprite projectileSprite;
    [SerializeField] private EnemyBehaviorType behaviorType;
    [SerializeField] private RuntimeAnimatorController animatorController;
    [SerializeField, Min(0.01f)] private float visualHeight = 2f;
    [SerializeField, Min(0.01f)] private float bodyCollisionRadius = 0.7f;
    [SerializeField, Min(1)] private int maxHealth = 1;
    [SerializeField, Min(0f)] private float moveSpeed;
    [SerializeField, Min(0)] private int attackDamage;
    [SerializeField, Min(0.01f)] private float attackRange = 0.01f;
    [SerializeField, Min(0.01f)] private float attackInterval = 0.01f;
    [SerializeField, Min(0f)] private float projectileSpeed;
    [SerializeField, Min(0.01f)] private float projectileLifetime = 0.01f;
    [SerializeField, Min(0.01f)] private float projectileRadius = 0.01f;

    /// <summary>
    /// 타격음의 대상 재질 축(Impact/NAMING.md 어휘: shell·flesh·wood·metal…).
    /// 소리에만 쓰이므로 비워 두면 any로 굴러간다.
    /// </summary>
    [SerializeField] private string surfaceMaterial = "any";

    public string Id => enemyId;
    public string DisplayName => displayName;
    public Sprite BodySprite => bodySprite;
    public Sprite ProjectileSprite => projectileSprite;
    public EnemyBehaviorType BehaviorType => behaviorType;
    public RuntimeAnimatorController AnimatorController => animatorController;
    public float VisualHeight => visualHeight;
    public float BodyCollisionRadius => bodyCollisionRadius;
    public int MaxHealth => maxHealth;
    public float MoveSpeed => moveSpeed;
    public int AttackDamage => attackDamage;
    public float AttackRange => attackRange;
    public float AttackInterval => attackInterval;

    public string SurfaceMaterial =>
        string.IsNullOrEmpty(surfaceMaterial) ? "any" : surfaceMaterial;

    /// <summary>재질은 소리 전용이라 본 Configure와 별도로 정한다.</summary>
    public void ConfigureSurfaceMaterial(string newSurfaceMaterial)
    {
        surfaceMaterial = newSurfaceMaterial;
    }
    public float ProjectileSpeed => projectileSpeed;
    public float ProjectileLifetime => projectileLifetime;
    public float ProjectileRadius => projectileRadius;

    public void Configure(
        string id,
        string newDisplayName,
        Sprite newBodySprite,
        Sprite newProjectileSprite,
        EnemyBehaviorType newBehaviorType,
        int newMaxHealth,
        float newMoveSpeed,
        int newAttackDamage,
        float newAttackRange,
        float newAttackInterval,
        float newProjectileSpeed,
        float newProjectileLifetime,
        float newProjectileRadius)
    {
        enemyId = id;
        displayName = newDisplayName;
        bodySprite = newBodySprite;
        projectileSprite = newProjectileSprite;
        behaviorType = newBehaviorType;
        maxHealth = newMaxHealth;
        moveSpeed = newMoveSpeed;
        attackDamage = newAttackDamage;
        attackRange = newAttackRange;
        attackInterval = newAttackInterval;
        projectileSpeed = newProjectileSpeed;
        projectileLifetime = newProjectileLifetime;
        projectileRadius = newProjectileRadius;
        NormalizeValues();
    }

    public void ConfigurePresentation(
        float newVisualHeight,
        float newBodyCollisionRadius)
    {
        visualHeight = newVisualHeight;
        bodyCollisionRadius = newBodyCollisionRadius;
        NormalizeValues();
    }

    private void OnValidate()
    {
        NormalizeValues();
    }

    private void NormalizeValues()
    {
        visualHeight = Mathf.Max(0.01f, visualHeight);
        bodyCollisionRadius = Mathf.Max(0.01f, bodyCollisionRadius);
        maxHealth = Mathf.Max(1, maxHealth);
        moveSpeed = Mathf.Max(0f, moveSpeed);
        attackDamage = Mathf.Max(0, attackDamage);
        attackRange = Mathf.Max(0.01f, attackRange);
        attackInterval = Mathf.Max(0.01f, attackInterval);
        projectileSpeed = Mathf.Max(0f, projectileSpeed);
        projectileLifetime = Mathf.Max(0.01f, projectileLifetime);
        projectileRadius = Mathf.Max(0.01f, projectileRadius);
    }
}
