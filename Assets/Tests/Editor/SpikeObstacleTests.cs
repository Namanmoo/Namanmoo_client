using NaManMoo.Dungeon;
using NUnit.Framework;
using UnityEngine;

public sealed class SpikeObstacleTests
{
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
}
