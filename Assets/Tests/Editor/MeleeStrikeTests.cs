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

    /// <summary>
    /// 회전(360도)은 콜라이더가 탐색 원에 걸리면 명중이다. 중심 거리로 재면
    /// 사거리가 몸통 반지름 합보다 짧은 무기는 붙어 있어도 영영 안 맞는다 —
    /// 대장간 회전 무기가 실제로 그랬다.
    /// </summary>
    [Test]
    public void SpinHitsAdjacentEnemyWhoseCenterIsBeyondReach()
    {
        // 적 중심 1.6, 몸통 반지름 0.3 → 콜라이더 가장자리 1.3.
        // 사거리 1.2 + 무기 두께 0.25 = 탐색 원 1.45가 가장자리에 닿는다.
        EnemyHealth enemy = MakeChaseEnemy(new Vector2(1.6f, 0f));
        Object.DestroyImmediate(weapon);
        weapon = WeaponFactory.CreateWeapon(
            "test_short_spin", "짧은 회전 무기", WeaponCategory.Melee, WeaponType.Sword,
            damage: 10, interval: 1f, reach: 1.2f, radius: 0.25f, arc: 90f,
            speed: 0f, lifetime: 0f, sprite: null, color: Color.white);

        int hits = MeleeStrike.Execute(ContextFacing(Vector2.right), arcOverride: 360f);

        Assert.That(hits, Is.EqualTo(1));
        Assert.That(enemy.CurrentHealth, Is.EqualTo(90));
    }

    /// <summary>근접 범위는 화면에 들린 무기의 칼끝 거리와 같아야 한다.</summary>
    [Test]
    public void VisualReach_UsesHeldSpriteTipAndFallsBackToStatWithoutSprite()
    {
        // 스프라이트 없는 무기(SetUp의 시험 검)는 스탯 사거리 그대로
        Assert.That(WeaponAttackGeometry.VisualReach(weapon), Is.EqualTo(2f));

        // 100픽셀=1유닛, 1x2유닛 그림, pivot은 아래 중앙(그립) → 가장 먼 픽셀은 위 모서리
        var texture = new Texture2D(100, 200);
        Sprite sprite = Sprite.Create(
            texture, new Rect(0f, 0f, 100f, 200f), new Vector2(0.5f, 0f), 100f);
        WeaponDefinition drawn = WeaponFactory.CreateWeapon(
            "test_drawn", "그린 무기", WeaponCategory.Melee, WeaponType.Sword,
            damage: 10, interval: 1f, reach: 99f, radius: 0.25f, arc: 90f,
            speed: 0f, lifetime: 0f, sprite: sprite, color: Color.white);
        try
        {
            float expected = PlayerWeaponVisual.DefaultHandOffset.magnitude
                + Mathf.Sqrt(0.5f * 0.5f + 2f * 2f) * PlayerWeaponVisual.WeaponScale;
            Assert.That(
                WeaponAttackGeometry.VisualReach(drawn),
                Is.EqualTo(expected).Within(0.0001f));
        }
        finally
        {
            Object.DestroyImmediate(drawn);
            Object.DestroyImmediate(sprite);
            Object.DestroyImmediate(texture);
        }
    }
}
