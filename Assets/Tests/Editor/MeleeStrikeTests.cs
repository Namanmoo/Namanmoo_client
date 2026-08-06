using NUnit.Framework;
using UnityEngine;

/// <summary>근접 판정(MeleeStrike) — 맞은 적이 공격 방향의 반대로 넉백되는지.</summary>
public sealed class MeleeStrikeTests
{
    private GameObject owner;
    private WeaponDefinition weapon;

    [SetUp]
    public void SetUp()
    {
        owner = new GameObject("player");
        weapon = WeaponFactory.CreateWeapon(
            "test_sword", "시험 검", WeaponCategory.Melee, WeaponType.Sword,
            damage: 10, interval: 1f, reach: 2f, radius: 0.25f, arc: 360f,
            speed: 0f, lifetime: 0f, sprite: null, color: Color.white);
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
        Object.DestroyImmediate(weapon);
    }

    private DeliveryContext ContextFacing(Vector2 direction)
    {
        var loadout = new WeaponLoadout(
            weapon, new DeliverySpec("swing", ParamSet.Empty), WeaponLoadout.NoEffects);
        return new DeliveryContext(null, owner, Vector2.zero, direction, loadout, null, null);
    }

    private static EnemyHealth MakeChaseEnemy(Vector2 position)
    {
        var enemyObject = new GameObject("enemy");
        enemyObject.transform.position = position;
        enemyObject.AddComponent<CircleCollider2D>().radius = 0.3f;
        EnemyHealth health = enemyObject.AddComponent<EnemyHealth>();
        health.Configure(100);
        enemyObject.AddComponent<ChaseContactEnemyController>();

        Physics2D.SyncTransforms();
        return health;
    }

    [Test]
    public void HitEnemyIsKnockedBackOppositeTheAttackDirection()
    {
        EnemyHealth enemy = MakeChaseEnemy(new Vector2(1f, 0f));

        MeleeStrike.Execute(ContextFacing(Vector2.right));

        EnemyStatus status = enemy.GetComponent<EnemyStatus>();
        Assert.That(status, Is.Not.Null);
        Assert.That(status.IsKnockedBack, Is.True);
    }

    [Test]
    public void MissedEnemyOutsideReachIsNotKnockedBack()
    {
        EnemyHealth enemy = MakeChaseEnemy(new Vector2(10f, 0f));

        MeleeStrike.Execute(ContextFacing(Vector2.right));

        Assert.That(enemy.GetComponent<EnemyStatus>(), Is.Null);
    }
}
