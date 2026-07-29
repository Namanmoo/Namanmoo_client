using System.Collections.Generic;
using NUnit.Framework;

public class PlayerInventoryTests
{
    [Test]
    public void ItemData_WithNonEmptyId_ExposesItsImmutableValues()
    {
        var item = new ItemData("iron-sword", "Iron Sword", ItemKind.Weapon);

        Assert.That(item.Id, Is.EqualTo("iron-sword"));
        Assert.That(item.DisplayName, Is.EqualTo("Iron Sword"));
        Assert.That(item.Kind, Is.EqualTo(ItemKind.Weapon));
        Assert.That(item.Icon, Is.Null);
    }

    [Test]
    public void ItemData_WithEmptyId_IsInvalid()
    {
        var item = new ItemData("", "Broken Item", ItemKind.Item);

        Assert.That(item.IsValid, Is.False);
    }

    [Test]
    public void NewInventory_HasSixEmptySlotsWithFirstSlotSelectedAndNothingEquipped()
    {
        var inventory = new PlayerInventory();

        Assert.That(inventory.Slots, Is.AssignableTo<IReadOnlyList<ItemData>>());
        Assert.That(inventory.Slots, Has.Count.EqualTo(6));
        Assert.That(inventory.Slots, Is.All.Null);
        Assert.That(inventory.SelectedSlotIndex, Is.EqualTo(0));
        Assert.That(inventory.EquippedItem, Is.Null);
    }

    [Test]
    public void TryAcquire_FirstValidItem_PlacesSelectsAndEquipsIt()
    {
        var inventory = new PlayerInventory();
        ItemData sword = MakeItem("sword");

        bool acquired = inventory.TryAcquire(sword);

        Assert.That(acquired, Is.True);
        Assert.That(inventory.Slots[0], Is.SameAs(sword));
        Assert.That(inventory.SelectedSlotIndex, Is.EqualTo(0));
        Assert.That(inventory.EquippedItem, Is.SameAs(sword));
    }

    [Test]
    public void TryAcquire_FirstValidItem_AfterEmptySlotPreselection_StillSelectsAndEquipsSlotZero()
    {
        var inventory = new PlayerInventory();
        ItemData sword = MakeItem("sword");
        inventory.SelectSlot(2);

        bool acquired = inventory.TryAcquire(sword);

        Assert.That(acquired, Is.True);
        Assert.That(inventory.Slots[0], Is.SameAs(sword));
        Assert.That(inventory.SelectedSlotIndex, Is.EqualTo(0));
        Assert.That(inventory.EquippedItem, Is.SameAs(sword));
    }

    [Test]
    public void TryAcquire_LaterItem_UsesFirstEmptySlotAndPreservesSelection()
    {
        var inventory = new PlayerInventory();
        ItemData sword = MakeItem("sword");
        ItemData potion = MakeItem("potion");
        inventory.TryAcquire(sword);

        bool acquired = inventory.TryAcquire(potion);

        Assert.That(acquired, Is.True);
        Assert.That(inventory.Slots[1], Is.SameAs(potion));
        Assert.That(inventory.SelectedSlotIndex, Is.EqualTo(0));
        Assert.That(inventory.EquippedItem, Is.SameAs(sword));
    }

    [Test]
    public void TryAcquire_IntoSelectedEmptySlot_EquipsAcquiredItemAndRaisesEquipmentEvent()
    {
        var inventory = new PlayerInventory();
        ItemData sword = MakeItem("sword");
        ItemData potion = MakeItem("potion");
        inventory.TryAcquire(sword);
        inventory.SelectSlot(1);

        int equippedChangeCount = 0;
        ItemData changedEquippedItem = null;
        inventory.EquippedItemChanged += item =>
        {
            equippedChangeCount++;
            changedEquippedItem = item;
        };

        bool acquired = inventory.TryAcquire(potion);

        Assert.That(acquired, Is.True);
        Assert.That(inventory.Slots[0], Is.SameAs(sword));
        Assert.That(inventory.Slots[1], Is.SameAs(potion));
        Assert.That(inventory.SelectedSlotIndex, Is.EqualTo(1));
        Assert.That(inventory.EquippedItem, Is.SameAs(potion));
        Assert.That(equippedChangeCount, Is.EqualTo(1));
        Assert.That(changedEquippedItem, Is.SameAs(potion));
    }

    [Test]
    public void TryAcquire_NullInvalidOrFull_DoesNotMutateInventory()
    {
        var inventory = new PlayerInventory();
        ItemData first = MakeItem("first");
        int stateChangeCount = 0;
        int equippedChangeCount = 0;
        inventory.StateChanged += () => stateChangeCount++;
        inventory.EquippedItemChanged += _ => equippedChangeCount++;
        inventory.TryAcquire(first);

        stateChangeCount = 0;
        equippedChangeCount = 0;

        Assert.That(inventory.TryAcquire(null), Is.False);
        Assert.That(inventory.TryAcquire(new ItemData("", "Broken", ItemKind.Item)), Is.False);
        Assert.That(inventory.Slots[0], Is.SameAs(first));
        Assert.That(inventory.Slots[1], Is.Null);
        Assert.That(stateChangeCount, Is.EqualTo(0));
        Assert.That(equippedChangeCount, Is.EqualTo(0));

        for (int index = 1; index < 6; index++)
        {
            Assert.That(inventory.TryAcquire(MakeItem("item-" + index)), Is.True);
        }

        ItemData finalSlot = inventory.Slots[5];
        stateChangeCount = 0;
        equippedChangeCount = 0;
        Assert.That(inventory.TryAcquire(MakeItem("extra")), Is.False);
        Assert.That(inventory.Slots[5], Is.SameAs(finalSlot));
        Assert.That(stateChangeCount, Is.EqualTo(0));
        Assert.That(equippedChangeCount, Is.EqualTo(0));
    }

    [Test]
    public void SelectSlot_UsesOccupiedAndEmptySlotsToSetEquippedItem()
    {
        var inventory = new PlayerInventory();
        ItemData sword = MakeItem("sword");
        inventory.TryAcquire(sword);

        Assert.That(inventory.SelectSlot(3), Is.True);
        Assert.That(inventory.SelectedSlotIndex, Is.EqualTo(3));
        Assert.That(inventory.EquippedItem, Is.Null);

        Assert.That(inventory.SelectSlot(0), Is.True);
        Assert.That(inventory.SelectedSlotIndex, Is.EqualTo(0));
        Assert.That(inventory.EquippedItem, Is.SameAs(sword));
    }

    [TestCase(-1)]
    [TestCase(6)]
    public void SelectSlot_OutsideSlotRange_ReturnsFalseWithoutMutation(int index)
    {
        var inventory = new PlayerInventory();
        ItemData sword = MakeItem("sword");
        inventory.TryAcquire(sword);

        bool selected = inventory.SelectSlot(index);

        Assert.That(selected, Is.False);
        Assert.That(inventory.SelectedSlotIndex, Is.EqualTo(0));
        Assert.That(inventory.EquippedItem, Is.SameAs(sword));
    }

    [Test]
    public void StateEvents_AreRaisedOnlyForActualStateChanges()
    {
        var inventory = new PlayerInventory();
        ItemData sword = MakeItem("sword");
        int stateChangeCount = 0;
        int equippedChangeCount = 0;
        ItemData changedEquippedItem = sword;
        inventory.StateChanged += () => stateChangeCount++;
        inventory.EquippedItemChanged += item =>
        {
            equippedChangeCount++;
            changedEquippedItem = item;
        };

        inventory.TryAcquire(sword);
        inventory.TryAcquire(null);
        inventory.SelectSlot(0);
        inventory.SelectSlot(2);
        inventory.SelectSlot(2);
        inventory.SelectSlot(7);

        Assert.That(stateChangeCount, Is.EqualTo(2));
        Assert.That(equippedChangeCount, Is.EqualTo(2));
        Assert.That(changedEquippedItem, Is.Null);
    }

    private static ItemData MakeItem(string id)
    {
        return new ItemData(id, id, ItemKind.Item);
    }
}
