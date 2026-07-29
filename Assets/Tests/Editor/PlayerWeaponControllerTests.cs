using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class PlayerWeaponControllerTests : InputTestFixture
{
    private Keyboard keyboard;
    private GameObject player;
    private PlayerWeaponController controller;
    private readonly System.Collections.Generic.List<WeaponDefinition> definitions =
        new System.Collections.Generic.List<WeaponDefinition>();

    public override void Setup()
    {
        base.Setup();
        keyboard = InputSystem.AddDevice<Keyboard>();
        player = new GameObject("Player");
        controller = player.AddComponent<PlayerWeaponController>();
    }

    public override void TearDown()
    {
        foreach (WeaponProjectile projectile in Object.FindObjectsByType<WeaponProjectile>(
                     FindObjectsInactive.Include))
        {
            Object.DestroyImmediate(projectile.gameObject);
        }
        foreach (WeaponDefinition definition in definitions)
        {
            Object.DestroyImmediate(definition);
        }
        Object.DestroyImmediate(player);
        base.TearDown();
    }

    [Test]
    public void CalculateCardinalDirection_RejectsDiagonalInput()
    {
        Press(keyboard.upArrowKey);
        Press(keyboard.rightArrowKey);

        Assert.That(
            PlayerWeaponController.CalculateCardinalDirection(keyboard),
            Is.EqualTo(Vector2.zero));
    }

    [Test]
    public void RangedWeapon_HeldInputUsesConfiguredIntervalAndColliderRadius()
    {
        WeaponDefinition projectile = CreateDefinition(
            WeaponCategory.Ranged, WeaponType.Projectile, 0.8f, 0.6f);
        var inventory = new PlayerInventory();
        inventory.TryAcquire(new ItemData(projectile));
        controller.InitializeInventory(inventory);
        Press(keyboard.rightArrowKey);

        controller.ProcessInput(keyboard, 0f);
        controller.ProcessInput(keyboard, 0.79f);
        Assert.That(ProjectileCount(), Is.EqualTo(1));
        Assert.That(
            Object.FindAnyObjectByType<WeaponProjectile>()
                .GetComponent<CircleCollider2D>().radius,
            Is.EqualTo(0.6f));

        controller.ProcessInput(keyboard, 0.8f);
        Assert.That(ProjectileCount(), Is.EqualTo(2));
    }

    [Test]
    public void Gun_CanUseFasterIntervalAndSmallerCollider()
    {
        WeaponDefinition gun = CreateDefinition(
            WeaponCategory.Ranged, WeaponType.Gun, 0.2f, 0.15f);
        var inventory = new PlayerInventory();
        inventory.TryAcquire(new ItemData(gun));
        controller.InitializeInventory(inventory);
        Press(keyboard.upArrowKey);

        controller.ProcessInput(keyboard, 0f);
        controller.ProcessInput(keyboard, 0.2f);

        Assert.That(ProjectileCount(), Is.EqualTo(2));
        foreach (WeaponProjectile projectile in
                 Object.FindObjectsByType<WeaponProjectile>(FindObjectsInactive.Include))
        {
            Assert.That(projectile.GetComponent<CircleCollider2D>().radius, Is.EqualTo(0.15f));
        }
    }

    private WeaponDefinition CreateDefinition(
        WeaponCategory category, WeaponType type, float interval, float radius)
    {
        WeaponDefinition definition = ScriptableObject.CreateInstance<WeaponDefinition>();
        definition.Configure(
            type.ToString().ToLowerInvariant(), type.ToString(), category, type,
            5, interval, 2f, radius, 90f, 8f, 4f, null, null, Color.white);
        definitions.Add(definition);
        return definition;
    }

    private static int ProjectileCount()
    {
        return Object.FindObjectsByType<WeaponProjectile>(
            FindObjectsInactive.Include).Length;
    }
}
