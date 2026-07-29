using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerHealthDebugInputTests : InputTestFixture
{
    private GameObject player;
    private PlayerHealth health;
    private PlayerHealthDebugInput debugInput;
    private Keyboard keyboard;

    public override void Setup()
    {
        base.Setup();
        keyboard = InputSystem.AddDevice<Keyboard>();
        player = new GameObject("PlayerHealthDebugInputTests");
        health = player.AddComponent<PlayerHealth>();
        debugInput = player.AddComponent<PlayerHealthDebugInput>();
        typeof(PlayerHealth).GetMethod(
            "Awake",
            BindingFlags.Instance | BindingFlags.NonPublic).Invoke(health, null);
        typeof(PlayerHealthDebugInput).GetMethod(
            "Awake",
            BindingFlags.Instance | BindingFlags.NonPublic).Invoke(debugInput, null);
    }

    public override void TearDown()
    {
        Object.DestroyImmediate(player);
        base.TearDown();
    }

    [Test]
    public void PressingH_AppliesOnePointOfDamage()
    {
        Press(keyboard.hKey);

        MethodInfo update = typeof(PlayerHealthDebugInput).GetMethod(
            "Update",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(update, Is.Not.Null);
        update.Invoke(debugInput, null);

        Assert.That(health.CurrentHealth, Is.EqualTo(19));
    }
}
