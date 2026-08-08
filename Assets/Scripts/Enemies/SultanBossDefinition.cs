using UnityEngine;

/// <summary>
/// 장로 술탄 보스의 능력치와 패턴에 필요한 데이터. Phase 1/2 스프라이트와 공통 6대
/// 능력치는 <see cref="SlimeBossDefinition"/>과 같은 모양을 따르고, 소환형 패턴은
/// 던전에 이미 있는 <see cref="EnemyDefinition"/> 에셋(크랩/스쿼럴/우드타워)을 그대로
/// 참조해 재사용한다.
/// </summary>
[CreateAssetMenu(menuName = "NaManMoo/Sultan Boss Definition")]
public sealed class SultanBossDefinition : ScriptableObject
{
    [Header("Sprites")]
    [SerializeField] private Sprite phase1Sprite;
    [SerializeField] private Sprite phase2Sprite;
    [SerializeField] private Sprite fallMarkerSprite;
    [SerializeField] private Sprite arcProjectileSprite;

    [Header("Boss")]
    [SerializeField, Min(1)] private int maxHealth = 100;
    [SerializeField, Min(0f)] private float moveSpeed = 5f;
    [SerializeField, Min(0.01f)] private float patternInterval = 1.5f;
    [SerializeField, Min(0)] private int contactDamage = 4;
    [SerializeField, Min(0.01f)] private float visualHeight = 6f;
    [SerializeField, Min(0.01f)] private float contactRadius = 1.2f;
    [SerializeField, Range(0.01f, 1f)] private float phaseTwoHealthRatio = 0.5f;

    /// <summary>2페이즈로 변신하는 연출 길이. 이 동안 보스는 무적이고 움직이지 않는다.</summary>
    [SerializeField, Min(0f)] private float phaseTransitionSeconds = 2f;

    /// <summary>
    /// 1페이즈 좌우 모션 컨트롤러(Idle_Left/Right, Move_Left/Right).
    /// 비워 두면 <see cref="phase1Sprite"/> 한 장이 그대로 쓰인다 — 예전 동작.
    /// </summary>
    [SerializeField] private RuntimeAnimatorController phase1AnimatorController;

    /// <summary>
    /// 2페이즈 좌우 모션 컨트롤러. 비워 두면 변신 뒤 <see cref="phase2Sprite"/> 한 장으로
    /// 굳는다 — 모션을 아직 안 그린 보스도 그대로 돌아간다.
    /// </summary>
    [SerializeField] private RuntimeAnimatorController phase2AnimatorController;

    [Header("Summon References (기존 던전 스폰 시스템 재사용)")]
    // krab -> mushroom으로 이름을 바꿨다. 이미 저장된 보스 에셋이 참조를 잃지 않게 옛 이름을 남긴다.
    [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("krabDefinition")]
    private EnemyDefinition mushroomDefinition;
    [SerializeField] private EnemyDefinition squirrelDefinition;
    [SerializeField] private EnemyDefinition woodTowerDefinition;
    [SerializeField, Min(1)] private int maxSummonedMonsters = 5;
    [SerializeField, Min(1)] private int maxSummonedWoodTowers = 2;
    [SerializeField, Min(0.01f)] private float summonOffsetDistance = 4f;
    [SerializeField, Min(0f)] private float roomEdgeInset = 5f;
    [SerializeField, Min(0f)] private float summonWindup = 1f;

    [Header("Fall & Arc Pattern (BossSlime 재사용)")]
    [SerializeField, Min(0f)] private float hiddenDuration = 2f;
    [SerializeField, Min(0f)] private float markerSpeed = 2.5f;
    [SerializeField, Min(0.01f)] private float markerVisualHeight = 2f;
    [SerializeField, Min(0)] private int arcProjectileDamage = 3;
    [SerializeField, Min(0.01f)] private float arcProjectileVisualHeight = 0.8f;
    [SerializeField, Min(0.01f)] private float arcProjectileRadius = 0.4f;
    [SerializeField, Min(0.01f)] private float arcDistance = 6f;
    [SerializeField, Min(0.01f)] private float arcDuration = 1f;
    [SerializeField, Min(0f)] private float arcHeight = 1.5f;

    [Header("Charge Pattern")]
    [SerializeField, Min(0f)] private float chargeWindup = 0.5f;
    [SerializeField, Min(0.01f)] private float chargeDuration = 0.7f;
    [SerializeField, Min(1f)] private float chargeSpeedMultiplier = 2f;

    [Header("Landing Camera Shake")]
    [SerializeField, Min(0f)] private float landingShakeIntensity = 0.2f;
    [SerializeField, Min(0f)] private float landingShakeDuration = 0.25f;

    public Sprite Phase1Sprite => phase1Sprite;
    public Sprite Phase2Sprite => phase2Sprite;
    public Sprite FallMarkerSprite => fallMarkerSprite;
    public Sprite ArcProjectileSprite => arcProjectileSprite;

    public int MaxHealth => maxHealth;
    public float MoveSpeed => moveSpeed;
    public float PatternInterval => patternInterval;
    public int ContactDamage => contactDamage;
    public float VisualHeight => visualHeight;
    public float ContactRadius => contactRadius;
    public float PhaseTwoHealthRatio => phaseTwoHealthRatio;
    public float PhaseTransitionSeconds => phaseTransitionSeconds;
    public RuntimeAnimatorController Phase1AnimatorController => phase1AnimatorController;
    public RuntimeAnimatorController Phase2AnimatorController => phase2AnimatorController;

    public EnemyDefinition MushroomDefinition => mushroomDefinition;
    public EnemyDefinition SquirrelDefinition => squirrelDefinition;
    public EnemyDefinition WoodTowerDefinition => woodTowerDefinition;
    public int MaxSummonedMonsters => maxSummonedMonsters;
    public int MaxSummonedWoodTowers => maxSummonedWoodTowers;
    public float SummonOffsetDistance => summonOffsetDistance;
    public float RoomEdgeInset => roomEdgeInset;
    public float SummonWindup => summonWindup;

    public float HiddenDuration => hiddenDuration;
    public float MarkerSpeed => markerSpeed;
    public float MarkerVisualHeight => markerVisualHeight;
    public int ArcProjectileDamage => arcProjectileDamage;
    public float ArcProjectileVisualHeight => arcProjectileVisualHeight;
    public float ArcProjectileRadius => arcProjectileRadius;
    public float ArcDistance => arcDistance;
    public float ArcDuration => arcDuration;
    public float ArcHeight => arcHeight;

    public float ChargeWindup => chargeWindup;
    public float ChargeDuration => chargeDuration;
    public float ChargeSpeedMultiplier => chargeSpeedMultiplier;

    public float LandingShakeIntensity => landingShakeIntensity;
    public float LandingShakeDuration => landingShakeDuration;

    public void ConfigureSprites(
        Sprite phase1, Sprite phase2, Sprite fallMarker, Sprite arcProjectile)
    {
        phase1Sprite = phase1;
        phase2Sprite = phase2;
        fallMarkerSprite = fallMarker;
        arcProjectileSprite = arcProjectile;
    }

    public void ConfigureSummonReferences(
        EnemyDefinition mushroom, EnemyDefinition squirrel, EnemyDefinition woodTower)
    {
        mushroomDefinition = mushroom;
        squirrelDefinition = squirrel;
        woodTowerDefinition = woodTower;
    }
}
