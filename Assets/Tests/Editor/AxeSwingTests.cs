using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class AxeSwingTests
{
    [UnityTest]
    public IEnumerator Advance_HalfDurationRotatesClockwiseHalfTurn()
    {
        yield return new EnterPlayMode();
        AxeSwing swing = CreateSwing();
        swing.Initialize(null, 10, 0.45f);

        swing.Advance(0.225f);

        Assert.That(
            Mathf.Repeat(swing.transform.eulerAngles.z, 360f),
            Is.EqualTo(180f).Within(0.001f));

        Object.Destroy(swing.gameObject);
        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator Advance_FullDurationDestroysCompletedSwing()
    {
        yield return new EnterPlayMode();
        AxeSwing swing = CreateSwing();
        swing.Initialize(null, 10, 0.45f);

        swing.Advance(0.45f);
        yield return null;

        Assert.That(swing == null, Is.True);
        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator TryHit_SameEnemyTakesTenDamageOnlyOnce()
    {
        yield return new EnterPlayMode();
        AxeSwing swing = CreateSwing();
        swing.Initialize(null, 10, 0.45f);
        var enemy = new GameObject("Enemy");
        Collider2D collider = enemy.AddComponent<BoxCollider2D>();
        EnemyHealth health = enemy.AddComponent<EnemyHealth>();

        Assert.That(swing.TryHit(collider), Is.True);
        Assert.That(health.CurrentHealth, Is.EqualTo(10));
        Assert.That(swing.TryHit(collider), Is.False);
        Assert.That(health.CurrentHealth, Is.EqualTo(10));

        Object.Destroy(swing.gameObject);
        Object.Destroy(enemy);
        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator TryHit_AppliesKnockbackOppositeTheSwingDirection()
    {
        yield return new EnterPlayMode();
        AxeSwing swing = CreateSwing();
        swing.Initialize(null, 10, 0.45f, Vector2.right);
        var enemy = new GameObject("Enemy");
        Collider2D collider = enemy.AddComponent<BoxCollider2D>();
        enemy.AddComponent<EnemyHealth>();
        enemy.AddComponent<ChaseContactEnemyController>();

        Assert.That(swing.TryHit(collider), Is.True);

        EnemyStatus status = enemy.GetComponent<EnemyStatus>();
        Assert.That(status, Is.Not.Null);
        Assert.That(status.IsKnockedBack, Is.True);

        Object.Destroy(swing.gameObject);
        Object.Destroy(enemy);
        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator TryHit_IgnoresOwnerChildrenAndNonEnemies()
    {
        yield return new EnterPlayMode();
        var owner = new GameObject("Player");
        var ownerChild = new GameObject("Player Child");
        ownerChild.transform.SetParent(owner.transform, false);
        Collider2D ownerCollider = ownerChild.AddComponent<BoxCollider2D>();
        var scenery = new GameObject("Scenery");
        Collider2D sceneryCollider = scenery.AddComponent<BoxCollider2D>();
        AxeSwing swing = CreateSwing();
        swing.Initialize(owner, 10, 0.45f);

        Assert.That(swing.TryHit(ownerCollider), Is.False);
        Assert.That(swing.TryHit(sceneryCollider), Is.False);

        Object.Destroy(swing.gameObject);
        Object.Destroy(owner);
        Object.Destroy(scenery);
        yield return new ExitPlayMode();
    }

    private static AxeSwing CreateSwing()
    {
        return new GameObject("Axe Swing").AddComponent<AxeSwing>();
    }
}
