using NUnit.Framework;
using UnityEngine;

public sealed class ForgedWeaponTests
{
    private Sprite sprite;

    [SetUp]
    public void SetUp()
    {
        ForgedWeapon.Clear();
        var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        sprite = Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f));
    }

    [TearDown]
    public void TearDown()
    {
        ForgedWeapon.Clear();
        if (sprite != null)
        {
            Object.DestroyImmediate(sprite.texture);
            Object.DestroyImmediate(sprite);
        }
    }

    [Test]
    public void WithoutForging_NothingIsHandedToTheGame()
    {
        Assert.That(ForgedWeapon.HasWeapon, Is.False);
        Assert.That(ForgedWeapon.ToItemData(), Is.Null);
    }

    [Test]
    public void SetStoresTheWeaponAsAnInventoryItem()
    {
        WeaponLoadout loadout = ForgeWeaponAssembler.Fallback(sprite, "불꽃 검");

        ForgedWeapon.Set(sprite, loadout, null, "불꽃 검", version: 3);

        Assert.That(ForgedWeapon.HasWeapon, Is.True);
        Assert.That(ForgedWeapon.Version, Is.EqualTo(3));

        ItemData item = ForgedWeapon.ToItemData();
        Assert.That(item.Id, Is.EqualTo(ForgedWeapon.ItemId));
        Assert.That(item.DisplayName, Is.EqualTo("불꽃 검"));
        Assert.That(item.Kind, Is.EqualTo(ItemKind.Weapon));
        Assert.That(item.Icon, Is.SameAs(sprite));
        Assert.That(item.Loadout, Is.SameAs(loadout));
    }

    [Test]
    public void BlankNameFallsBackToAReadableLabel()
    {
        ForgedWeapon.Set(sprite, ForgeWeaponAssembler.Fallback(sprite, "x"), null, "   ", version: 1);

        Assert.That(ForgedWeapon.ToItemData().DisplayName, Is.EqualTo("만든 무기"));
    }

    [Test]
    public void MissingLoadoutFallsBackToTheDefaultWeapon()
    {
        ForgedWeapon.Set(sprite, null, null, "이름", version: 1);

        Assert.That(ForgedWeapon.HasWeapon, Is.True);
        Assert.That(ForgedWeapon.Loadout.Definition.Category, Is.EqualTo(WeaponCategory.Ranged));
    }

    [Test]
    public void ForgedWeaponGoesToTheThirdSlot()
    {
        Assert.That(ForgedWeapon.SlotIndex, Is.EqualTo(2));

        ForgedWeapon.Set(sprite, ForgeWeaponAssembler.Fallback(sprite, "이름"), null, "이름", version: 1);
        var inventory = new PlayerInventory();
        inventory.EnsureUniqueItemInSlotZero(new ItemData("sword", "Sword", ItemKind.Weapon));
        inventory.EnsureUniqueItemInSlot(1, new ItemData("axe", "Axe", ItemKind.Weapon));

        Assert.That(
            inventory.EnsureUniqueItemInSlot(ForgedWeapon.SlotIndex, ForgedWeapon.ToItemData()),
            Is.True);

        Assert.That(inventory.Slots[0].Id, Is.EqualTo("sword"));
        Assert.That(inventory.Slots[1].Id, Is.EqualTo("axe"));
        Assert.That(inventory.Slots[2].Id, Is.EqualTo(ForgedWeapon.ItemId));
    }
}
