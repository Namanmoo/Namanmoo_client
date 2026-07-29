using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAxeAttackerTests : InputTestFixture
{
    private GameObject player;
    private PlayerAxeAttacker attacker;
    private Keyboard keyboard;
    private Texture2D texture;
    private Sprite axeSprite;

    public override void Setup()
    {
        base.Setup();
        keyboard = InputSystem.AddDevice<Keyboard>();
        texture = new Texture2D(20, 40);
        axeSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, 20f, 40f),
            new Vector2(0.5f, 0f),
            100f);
        player = new GameObject("Player");
        attacker = player.AddComponent<PlayerAxeAttacker>();
        attacker.AxeSprite = axeSprite;
        attacker.InitializeInventory(CreateAxeInventory());
    }

    public override void TearDown()
    {
        foreach (AxeSwing swing in
                 Object.FindObjectsByType<AxeSwing>(FindObjectsInactive.Include))
        {
            Object.DestroyImmediate(swing.gameObject);
        }

        Object.DestroyImmediate(player);
        Object.DestroyImmediate(axeSprite);
        Object.DestroyImmediate(texture);
        base.TearDown();
    }

    [TestCase(Key.UpArrow, 0f, 1f)]
    [TestCase(Key.DownArrow, 0f, -1f)]
    [TestCase(Key.LeftArrow, -1f, 0f)]
    [TestCase(Key.RightArrow, 1f, 0f)]
    public void CalculateDirection_RecognizesEveryArrow(
        Key key,
        float expectedX,
        float expectedY)
    {
        Press(keyboard[key]);

        Assert.That(
            PlayerAxeAttacker.CalculateDirection(keyboard),
            Is.EqualTo(new Vector2(expectedX, expectedY)));
    }

    [Test]
    public void HeldArrow_AttacksImmediatelyThenOncePerSecond()
    {
        Press(keyboard.rightArrowKey);

        attacker.ProcessInput(keyboard, 0f);
        Assert.That(CountSwings(), Is.EqualTo(1));

        attacker.ProcessInput(keyboard, 0.99f);
        Assert.That(CountSwings(), Is.EqualTo(1));

        attacker.ProcessInput(keyboard, 1f);
        Assert.That(CountSwings(), Is.EqualTo(2));
    }

    [Test]
    public void ReleaseAndRepress_StillRespectsOneSecondCooldown()
    {
        Press(keyboard.upArrowKey);
        attacker.ProcessInput(keyboard, 0f);
        Release(keyboard.upArrowKey);
        attacker.ProcessInput(keyboard, 0.2f);
        Press(keyboard.leftArrowKey);

        attacker.ProcessInput(keyboard, 0.3f);

        Assert.That(CountSwings(), Is.EqualTo(1));
    }

    [Test]
    public void NonAxeSelection_DoesNotAttack()
    {
        PlayerInventory inventory = CreateAxeInventory();
        inventory.SelectSlot(0);
        attacker.InitializeInventory(inventory);
        Press(keyboard.rightArrowKey);

        attacker.ProcessInput(keyboard, 0f);

        Assert.That(CountSwings(), Is.Zero);
    }

    [Test]
    public void SpawnedSwing_UsesBottomPivotAtPlayerAndConfiguredVisualLength()
    {
        player.transform.position = new Vector3(3f, -2f, 0f);
        Press(keyboard.upArrowKey);

        attacker.ProcessInput(keyboard, 0f);

        AxeSwing swing = Object.FindAnyObjectByType<AxeSwing>();
        SpriteRenderer renderer = swing.GetComponentInChildren<SpriteRenderer>();
        BoxCollider2D blade = swing.GetComponentInChildren<BoxCollider2D>();
        Assert.That(swing.transform.parent, Is.EqualTo(player.transform));
        Assert.That(swing.transform.localPosition, Is.EqualTo(Vector3.zero));
        Assert.That(renderer.sprite, Is.SameAs(axeSprite));
        Assert.That(renderer.transform.localPosition, Is.EqualTo(Vector3.zero));
        Assert.That(renderer.bounds.size.y, Is.EqualTo(3f).Within(0.001f));
        Assert.That(blade.isTrigger, Is.True);
        Assert.That(blade.transform.localPosition.y, Is.GreaterThan(0f));
    }

    private static PlayerInventory CreateAxeInventory()
    {
        var inventory = new PlayerInventory();
        inventory.EnsureUniqueItemInSlot(
            0,
            new ItemData("sword", "Sword", ItemKind.Weapon));
        inventory.EnsureUniqueItemInSlot(
            1,
            new ItemData("axe", "Axe", ItemKind.Weapon));
        inventory.SelectSlot(1);
        return inventory;
    }

    private static int CountSwings()
    {
        return Object.FindObjectsByType<AxeSwing>(FindObjectsInactive.Include).Length;
    }
}
