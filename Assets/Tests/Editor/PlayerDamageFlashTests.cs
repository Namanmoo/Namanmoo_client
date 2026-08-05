using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class PlayerDamageFlashTests
{
    [UnityTest]
    public IEnumerator AcceptedDamage_FlashesBodyBlackAndRestoresOriginalColor()
    {
        yield return new EnterPlayMode();
        var player = new GameObject(nameof(PlayerDamageFlashTests));
        PlayerHealth health = player.AddComponent<PlayerHealth>();
        var body = new GameObject("Body");
        body.transform.SetParent(player.transform);
        SpriteRenderer renderer = body.AddComponent<SpriteRenderer>();
        renderer.color = Color.white;
        PlayerDamageFlash flash = player.AddComponent<PlayerDamageFlash>();
        flash.Initialize(health, renderer);

        Assert.That(health.TryTakeDamage(1, Time.time, 0.35f), Is.True);
        Assert.That(renderer.color, Is.EqualTo(Color.black));

        yield return WaitForGameTime(0.11f);
        Assert.That(renderer.color, Is.EqualTo(Color.white));

        yield return WaitForGameTime(0.1f);
        Assert.That(renderer.color, Is.EqualTo(Color.black));

        yield return WaitForGameTime(0.16f);
        Assert.That(renderer.color, Is.EqualTo(Color.white));

        Object.Destroy(player);
        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator RejectedDamage_DoesNotRestartFlash()
    {
        yield return new EnterPlayMode();
        var player = new GameObject(nameof(PlayerDamageFlashTests));
        PlayerHealth health = player.AddComponent<PlayerHealth>();
        var body = new GameObject("Body");
        body.transform.SetParent(player.transform);
        SpriteRenderer renderer = body.AddComponent<SpriteRenderer>();
        renderer.color = Color.white;
        PlayerDamageFlash flash = player.AddComponent<PlayerDamageFlash>();
        flash.Initialize(health, renderer);

        Assert.That(health.TryTakeDamage(1, Time.time, 0.15f), Is.True);
        yield return WaitForGameTime(0.11f);
        Assert.That(renderer.color, Is.EqualTo(Color.white));

        Assert.That(health.TryTakeDamage(1, Time.time, 0.15f), Is.False);
        yield return WaitForGameTime(0.06f);

        Assert.That(renderer.color, Is.EqualTo(Color.white));
        Object.Destroy(player);
        yield return new ExitPlayMode();
    }
    [UnityTest]
    public IEnumerator ReinitializeAfterDestroyedHealth_SubscribesToNewHealth()
    {
        yield return new EnterPlayMode();
        var flashObject = new GameObject(nameof(PlayerDamageFlashTests));
        SpriteRenderer renderer = flashObject.AddComponent<SpriteRenderer>();
        PlayerDamageFlash flash = flashObject.AddComponent<PlayerDamageFlash>();
        var firstHealthObject = new GameObject("First Health");
        PlayerHealth firstHealth = firstHealthObject.AddComponent<PlayerHealth>();
        flash.Initialize(firstHealth, renderer);

        Object.Destroy(firstHealthObject);
        yield return null;

        var secondHealthObject = new GameObject("Second Health");
        PlayerHealth secondHealth = secondHealthObject.AddComponent<PlayerHealth>();
        flash.Initialize(secondHealth, renderer);

        Assert.That(secondHealth.TryTakeDamage(1, Time.time, 0.2f), Is.True);
        Assert.That(renderer.color, Is.EqualTo(Color.black));

        Object.Destroy(flashObject);
        Object.Destroy(secondHealthObject);
        yield return new ExitPlayMode();
    }
    private static IEnumerator WaitForGameTime(float duration)
    {
        float deadline = Time.time + duration;
        while (Time.time < deadline)
        {
            yield return null;
        }
    }
}