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
    [SerializeField, Min(1)] private int maxHealth = 1;
    [SerializeField, Min(0f)] private float moveSpeed;
    [SerializeField, Min(0)] private int attackDamage;
    [SerializeField, Min(0.01f)] private float attackRange = 0.01f;
    [SerializeField, Min(0.01f)] private float attackInterval = 0.01f;
    [SerializeField, Min(0f)] private float projectileSpeed;
    [SerializeField, Min(0.01f)] private float projectileLifetime = 0.01f;
    [SerializeField, Min(0.01f)] private float projectileRadius = 0.01f;

    public string Id => enemyId;
    public string DisplayName => displayName;
    public Sprite BodySprite => bodySprite;
    public Sprite ProjectileSprite => projectileSprite;
    public EnemyBehaviorType BehaviorType => behaviorType;
    public RuntimeAnimatorController AnimatorController => animatorController;
    public int MaxHealth => maxHealth;
    public float MoveSpeed => moveSpeed;
    public int AttackDamage => attackDamage;
    public float AttackRange => attackRange;
    public float AttackInterval => attackInterval;
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

    private void OnValidate()
    {
        NormalizeValues();
    }

    private void NormalizeValues()
    {
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
