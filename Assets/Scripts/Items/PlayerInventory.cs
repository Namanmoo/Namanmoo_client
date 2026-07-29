using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

public sealed class PlayerInventory
{
    private const int SlotCount = 6;
    private readonly ItemData[] slots = new ItemData[SlotCount];
    private readonly IReadOnlyList<ItemData> readOnlySlots;

    public PlayerInventory()
    {
        readOnlySlots = new ReadOnlyCollection<ItemData>(slots);
        SelectedSlotIndex = 0;
    }

    public IReadOnlyList<ItemData> Slots => readOnlySlots;
    public int SelectedSlotIndex { get; private set; }
    public ItemData EquippedItem { get; private set; }

    public event Action StateChanged;
    public event Action<ItemData> EquippedItemChanged;

    public bool TryAcquire(ItemData item)
    {
        if (item == null || !item.IsValid)
        {
            return false;
        }

        for (int index = 0; index < SlotCount; index++)
        {
            if (slots[index] != null)
            {
                continue;
            }

            slots[index] = item;
            if (index == 0)
            {
                SelectedSlotIndex = index;
            }

            if (SelectedSlotIndex == index)
            {
                SetEquippedItem(item);
            }

            StateChanged?.Invoke();
            return true;
        }

        return false;
    }

    public bool SelectSlot(int index)
    {
        if (index < 0 || index >= SlotCount)
        {
            return false;
        }

        if (SelectedSlotIndex == index)
        {
            return true;
        }

        SelectedSlotIndex = index;
        SetEquippedItem(slots[index]);
        StateChanged?.Invoke();
        return true;
    }

    public bool EnsureUniqueItemInSlotZero(ItemData requiredItem)
    {
        ItemData previousSlotZero = slots[0];
        bool result = EnsureUniqueItemInSlot(0, requiredItem);
        if (result && !ReferenceEquals(previousSlotZero, slots[0]))
        {
            SelectedSlotIndex = 0;
            SetEquippedItem(slots[0]);
        }

        return result;
    }

    public bool EnsureUniqueItemInSlot(int slotIndex, ItemData requiredItem)
    {
        if (slotIndex < 0 ||
            slotIndex >= SlotCount ||
            requiredItem == null ||
            !requiredItem.IsValid)
        {
            return false;
        }

        int matchingIdCount = 0;
        ItemData exactExistingItem = null;
        for (int index = 0; index < SlotCount; index++)
        {
            ItemData item = slots[index];
            if (item == null || item.Id != requiredItem.Id)
            {
                continue;
            }

            matchingIdCount++;
            if (exactExistingItem == null && HasExactValues(item, requiredItem))
            {
                exactExistingItem = item;
            }
        }

        if (matchingIdCount == 1 &&
            ReferenceEquals(slots[slotIndex], exactExistingItem))
        {
            return true;
        }

        var preservedItems = new List<ItemData>(SlotCount - 1);
        for (int index = 0; index < SlotCount; index++)
        {
            ItemData item = slots[index];
            if (item != null && item.Id != requiredItem.Id)
            {
                preservedItems.Add(item);
            }
        }

        Array.Clear(slots, 0, slots.Length);
        ItemData normalizedItem = exactExistingItem ?? requiredItem;
        slots[slotIndex] = normalizedItem;
        int preservedIndex = 0;
        for (int index = 0; index < SlotCount && preservedIndex < preservedItems.Count; index++)
        {
            if (index == slotIndex)
            {
                continue;
            }

            slots[index] = preservedItems[preservedIndex++];
        }

        if (SelectedSlotIndex == slotIndex)
        {
            SetEquippedItem(normalizedItem);
        }
        else
        {
            SetEquippedItem(slots[SelectedSlotIndex]);
        }

        StateChanged?.Invoke();
        return true;
    }

    private static bool HasExactValues(ItemData item, ItemData requiredItem)
    {
        return item.Id == requiredItem.Id
            && item.DisplayName == requiredItem.DisplayName
            && item.Kind == requiredItem.Kind
            && ReferenceEquals(item.Icon, requiredItem.Icon)
            && ReferenceEquals(item.Weapon, requiredItem.Weapon);
    }

    private void SetEquippedItem(ItemData item)
    {
        if (ReferenceEquals(EquippedItem, item))
        {
            return;
        }

        EquippedItem = item;
        EquippedItemChanged?.Invoke(item);
    }
}
