using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class PlayerHealthDamageEventTests
{
    [UnityTest]
    public IEnumerator TryTakeDamage_RaisesDamagedOnlyForAcceptedHit()
    {
        yield return new EnterPlayMode();
        var player = new GameObject(nameof(PlayerHealthDamageEventTests));
        PlayerHealth health = player.AddComponent<PlayerHealth>();
        int eventCount = 0;
        float reportedDuration = -1f;
        health.Damaged += duration =>
        {
            eventCount++;
            reportedDuration = duration;
        };

        Assert.That(health.TryTakeDamage(2, 10f, 1f), Is.True);
        Assert.That(health.TryTakeDamage(2, 10.5f, 1f), Is.False);
        Assert.That(eventCount, Is.EqualTo(1));
        Assert.That(reportedDuration, Is.EqualTo(1f).Within(0.001f));

        Object.Destroy(player);
        yield return new ExitPlayMode();
    }
}
