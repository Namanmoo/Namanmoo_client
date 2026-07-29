using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class AxeInventoryTests
{
    private GameObject player;
    private GameObject uiRoot;
    private Texture2D texture;
    private Sprite swordSprite;
    private Sprite axeSprite;
    private ItemHotbarController controller;

    [SetUp]
    public void SetUp()
    {
        texture = new Texture2D(8, 4);
        swordSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, 4f, 4f),
            new Vector2(0.5f, 0.5f));
        axeSprite = Sprite.Create(
            texture,
            new Rect(4f, 0f, 4f, 4f),
            new Vector2(0.5f, 0.5f));
        player = new GameObject("Player", typeof(PlayerSwordShooter));
        controller = player.AddComponent<ItemHotbarController>();
        uiRoot = new GameObject("UI Root", typeof(RectTransform));
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(uiRoot);
        Object.DestroyImmediate(player);
        Object.DestroyImmediate(swordSprite);
        Object.DestroyImmediate(axeSprite);
        Object.DestroyImmediate(texture);
    }

    [Test]
    public void ConfigureStartingWeapons_PutsExactSwordInSlotOneAndAxeInSlotTwo()
    {
        controller.ConfigureStartingWeapons(swordSprite, axeSprite);

        PlayerInventory inventory = controller.Inventory;
        Assert.That(inventory.Slots[0].Id, Is.EqualTo("sword"));
        Assert.That(inventory.Slots[0].Icon, Is.SameAs(swordSprite));
        Assert.That(inventory.Slots[1].Id, Is.EqualTo("axe"));
        Assert.That(inventory.Slots[1].DisplayName, Is.EqualTo("Axe"));
        Assert.That(inventory.Slots[1].Kind, Is.EqualTo(ItemKind.Weapon));
        Assert.That(inventory.Slots[1].Icon, Is.SameAs(axeSprite));
        Assert.That(inventory.SelectedSlotIndex, Is.Zero);
        Assert.That(inventory.EquippedItem, Is.SameAs(inventory.Slots[0]));
    }

    [Test]
    public void RepeatedConfiguration_KeepsOneSwordAndOneAxe()
    {
        controller.ConfigureStartingWeapons(swordSprite, axeSprite);
        controller.ConfigureStartingWeapons(swordSprite, axeSprite);

        int swordCount = 0;
        int axeCount = 0;
        foreach (ItemData item in controller.Inventory.Slots)
        {
            swordCount += item != null && item.Id == "sword" ? 1 : 0;
            axeCount += item != null && item.Id == "axe" ? 1 : 0;
        }

        Assert.That(swordCount, Is.EqualTo(1));
        Assert.That(axeCount, Is.EqualTo(1));
    }

    [Test]
    public void HotbarView_ShowsExactAxeSpriteInSlotTwo()
    {
        controller.ConfigureStartingWeapons(swordSprite, axeSprite);
        Texture2D backgroundTexture = new Texture2D(8, 4);
        Sprite backgroundSprite = Sprite.Create(
            backgroundTexture,
            new Rect(0f, 0f, 8f, 4f),
            new Vector2(0.5f, 0.5f));

        try
        {
            ItemHotbarView view =
                ItemHotbarUIFactory.Create(uiRoot.transform, controller, backgroundSprite);
            Image icon = view.transform.Find("Slot 2/Icon").GetComponent<Image>();

            Assert.That(icon.enabled, Is.True);
            Assert.That(icon.sprite, Is.SameAs(axeSprite));
        }
        finally
        {
            Object.DestroyImmediate(backgroundSprite);
            Object.DestroyImmediate(backgroundTexture);
        }
    }
}
