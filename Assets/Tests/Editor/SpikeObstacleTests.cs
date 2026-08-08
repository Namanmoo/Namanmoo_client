using System.Collections;
using NaManMoo.Dungeon;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class SpikeObstacleTests
{
    [UnityTest]
    public IEnumerator Awake_ForcesColliderToBeTrigger()
    {
        yield return new EnterPlayMode();
        var spikeObject = new GameObject("Spike");
        BoxCollider2D collider = spikeObject.AddComponent<BoxCollider2D>();
        collider.isTrigger = false;

        spikeObject.AddComponent<SpikeObstacle>();

        Assert.That(collider.isTrigger, Is.True);

        Object.Destroy(spikeObject);
        yield return new ExitPlayMode();
    }

    [Test]
    public void TryDamagePlayer_ColliderWithoutPlayerHealthDoesNothing()
    {
        var monster = new GameObject("Monster");
        Collider2D monsterCollider = monster.AddComponent<CircleCollider2D>();
        var spikeObject = new GameObject("Spike");
        spikeObject.AddComponent<BoxCollider2D>();
        SpikeObstacle spike = spikeObject.AddComponent<SpikeObstacle>();

        try
        {
            Assert.That(spike.TryDamagePlayer(monsterCollider, 0f), Is.False);
        }
        finally
        {
            Object.DestroyImmediate(monster);
            Object.DestroyImmediate(spikeObject);
        }
    }

    [UnityTest]
    public IEnumerator TryDamagePlayer_DealsConfiguredDamageAndRespectsInvulnerability()
    {
        yield return new EnterPlayMode();
        GameObject player = new GameObject("Player");
        Collider2D playerCollider = player.AddComponent<CircleCollider2D>();
        PlayerHealth health = player.AddComponent<PlayerHealth>();
        GameObject spikeObject = new GameObject("Spike");
        spikeObject.AddComponent<BoxCollider2D>();
        SpikeObstacle spike = spikeObject.AddComponent<SpikeObstacle>();

        Assert.That(spike.TryDamagePlayer(playerCollider, 0f), Is.True);
        Assert.That(health.CurrentHealth, Is.EqualTo(18));
        Assert.That(spike.TryDamagePlayer(playerCollider, 0.5f), Is.False);
        Assert.That(health.CurrentHealth, Is.EqualTo(18));
        Assert.That(spike.TryDamagePlayer(playerCollider, 1f), Is.True);
        Assert.That(health.CurrentHealth, Is.EqualTo(16));

        Object.Destroy(player);
        Object.Destroy(spikeObject);
        yield return new ExitPlayMode();
    }
}
