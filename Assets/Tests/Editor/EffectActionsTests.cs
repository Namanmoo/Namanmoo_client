using NUnit.Framework;
using UnityEngine;

/// <summary>효과 구현 — 대상·주변·플레이어에게 실제로 무슨 일이 일어나는지.</summary>
public sealed class EffectActionsTests
{
    private GameObject owner;

    [SetUp]
    public void SetUp()
    {
        owner = new GameObject("player");
    }

    [TearDown]
    public void TearDown()
    {
        foreach (EnemyHealth leftover in Object.FindObjectsByType<EnemyHealth>(
            FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Object.DestroyImmediate(leftover.gameObject);
        }

        Object.DestroyImmediate(owner);
    }

    private static EnemyHealth MakeEnemy(Vector2 position, int hp = 100)
    {
        var enemyObject = new GameObject("enemy");
        enemyObject.transform.position = position;
        enemyObject.AddComponent<CircleCollider2D>().radius = 0.3f;
        EnemyHealth health = enemyObject.AddComponent<EnemyHealth>();
        health.Configure(hp);

        // EditMode에서는 물리 갱신이 자동으로 돌지 않는다 — OverlapCircle이 새 콜라이더를
        // 보려면 트랜스폼을 물리 세계에 직접 밀어 넣어야 한다.
        Physics2D.SyncTransforms();
        return health;
    }

    private EffectContext ContextWith(EnemyHealth target, int weaponDamage = 10)
    {
        Vector2 origin = target != null ? (Vector2)target.transform.position : Vector2.zero;
        return new EffectContext(owner, origin, Vector2.right, target, weaponDamage);
    }

    private PlayerHealth AddPlayerHealth()
    {
        PlayerHealth health = owner.AddComponent<PlayerHealth>();
        // EditMode에서는 Awake가 자동으로 불리지 않아 CurrentHealth가 0으로 남는다.
        // SendMessage는 EditMode 검증(ShouldRunBehaviour)에 걸려 리플렉션으로 직접 부른다.
        typeof(PlayerHealth)
            .GetMethod(
                "Awake",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            .Invoke(health, null);
        return health;
    }

    private static ParamSet Params(params (string key, float value)[] entries)
    {
        var map = new System.Collections.Generic.Dictionary<string, float>();
        foreach ((string key, float value) in entries)
        {
            map[key] = value;
        }

        return new ParamSet(map);
    }

    [Test]
    public void FireDotAttachesABurn()
    {
        EnemyHealth enemy = MakeEnemy(Vector2.zero);

        new FireDotAction().Execute(
            ContextWith(enemy), Params(("dotDamagePerSecond", 2f), ("durationSeconds", 3f)));

        Assert.That(enemy.GetComponent<EnemyStatus>().IsBurning, Is.True);
    }

    [Test]
    public void ShockDealsBonusDamageAndStaggers()
    {
        EnemyHealth enemy = MakeEnemy(Vector2.zero);

        new ShockAction().Execute(
            ContextWith(enemy), Params(("bonusDamage", 5f), ("staggerSeconds", 0.4f)));

        Assert.That(enemy.CurrentHealth, Is.EqualTo(95));
        Assert.That(enemy.GetComponent<EnemyStatus>().IsStaggered, Is.True);
    }

    [Test]
    public void ExplosionHitsNeighboursButNotTheDirectTarget()
    {
        EnemyHealth target = MakeEnemy(Vector2.zero);
        EnemyHealth near = MakeEnemy(new Vector2(1f, 0f));
        EnemyHealth far = MakeEnemy(new Vector2(9f, 0f));

        new ExplosionAction().Execute(
            ContextWith(target),
            Params(("explosionDamage", 7f), ("explosionRadius", 2f)));

        Assert.That(target.CurrentHealth, Is.EqualTo(100), "직격 대상은 무기 피해만 받는다");
        Assert.That(near.CurrentHealth, Is.EqualTo(93));
        Assert.That(far.CurrentHealth, Is.EqualTo(100));
    }

    [Test]
    public void ChainJumpsToNearbyEnemiesWithoutRevisiting()
    {
        EnemyHealth first = MakeEnemy(Vector2.zero);
        EnemyHealth second = MakeEnemy(new Vector2(1.5f, 0f));
        EnemyHealth third = MakeEnemy(new Vector2(3f, 0f));

        new ChainAction().Execute(
            ContextWith(first, weaponDamage: 10),
            Params(("maxChains", 2f), ("chainDamagePercent", 50f), ("chainRange", 2f)));

        Assert.That(first.CurrentHealth, Is.EqualTo(100), "시작점은 무기 피해만 받는다");
        Assert.That(second.CurrentHealth, Is.EqualTo(95));
        Assert.That(third.CurrentHealth, Is.EqualTo(95));
    }

    [Test]
    public void LifestealHealsTheOwner()
    {
        PlayerHealth health = AddPlayerHealth();
        health.TakeDamage(10);
        int before = health.CurrentHealth;

        new LifestealAction().Execute(
            ContextWith(null, weaponDamage: 20), Params(("lifestealPercent", 15f)));

        Assert.That(health.CurrentHealth, Is.EqualTo(before + 3)); // 20 × 15% = 3
    }

    [Test]
    public void TinyLifestealRoundsDownToNothing()
    {
        PlayerHealth health = AddPlayerHealth();
        health.TakeDamage(10);
        int before = health.CurrentHealth;

        // 5 × 3% = 0.15 → 내림 0. 최소 1을 보장하면 연사 무기가 사실상 무적이 된다
        new LifestealAction().Execute(
            ContextWith(null, weaponDamage: 5), Params(("lifestealPercent", 3f)));

        Assert.That(health.CurrentHealth, Is.EqualTo(before));
    }

    [Test]
    public void ShockwavePushesAndDamagesAround()
    {
        EnemyHealth near = MakeEnemy(new Vector2(1f, 0f));

        new ShockwaveAction().Execute(
            new EffectContext(owner, Vector2.zero, Vector2.right, null, 10),
            Params(("waveDamage", 4f), ("waveRadius", 2f), ("knockbackForce", 0f)));

        Assert.That(near.CurrentHealth, Is.EqualTo(96));
    }

    [Test]
    public void ModifiersAccumulateIntoTuning()
    {
        var registry = EffectRegistry.CreateDefault();
        var loadout = new WeaponLoadout(
            null,
            new DeliverySpec("straight", ParamSet.Empty),
            new[]
            {
                new EffectSpec("pierce", "on_hit", Params(("maxPierceCount", 3f))),
                new EffectSpec("ricochet", "on_hit", Params(("maxBounces", 2f))),
                new EffectSpec("fire_dot", "on_hit", ParamSet.Empty), // 수정자가 아니다 — 무시
            });

        ProjectileTuning tuning = registry.BuildTuning(loadout);

        Assert.That(tuning.PierceCount, Is.EqualTo(3));
        Assert.That(tuning.BounceCount, Is.EqualTo(2));
    }
}
