using System.Collections.Generic;
using UnityEngine;
using System;

public class InventorySystem : MonoBehaviour
{
    [Header("Inventory")]
    [SerializeField]
    private int inventorySize = 24;

    [SerializeField]
    private List<InventorySlot> slots = new List<InventorySlot>();

    public IReadOnlyList<InventorySlot> Slots => slots;

    public event Action OnInventoryChanged;

    private void Awake()
    {
        // This function makes sure the inventory contains the correct number of slots.
        InitializeInventory();
    }

    // This function creates empty inventory slots until the inventory reaches its configured size.
    private void InitializeInventory()
    {
        while (slots.Count < inventorySize)
        {
            slots.Add(new InventorySlot());
        }

        if (slots.Count > inventorySize)
        {
            slots.RemoveRange(
                inventorySize,
                slots.Count - inventorySize
            );
        }
    }

    public bool AddItem(ItemData item, int amount = 1)
    {
        // This function adds items to existing stacks or empty inventory slots
        // and notifies the UI when the inventory changes.
        if (item == null || amount <= 0 || item.maxStack <= 0)
            return false;

        int originalAmount = amount;

        // First, add items to existing stacks of the same item.
        for (int i = 0; i < slots.Count; i++)
        {
            if (!slots[i].IsEmpty() &&
                slots[i].item == item &&
                slots[i].amount < item.maxStack)
            {
                int availableSpace = item.maxStack - slots[i].amount;
                int amountToAdd = Mathf.Min(availableSpace, amount);

                slots[i].amount += amountToAdd;
                amount -= amountToAdd;

                if (amount <= 0)
                {
                    // Notify the UI after successfully adding the full amount.
                    OnInventoryChanged?.Invoke();
                    return true;
                }
            }
        }

        // Next, add remaining items to empty slots.
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].IsEmpty())
            {
                int amountToAdd = Mathf.Min(item.maxStack, amount);

                slots[i].item = item;
                slots[i].amount = amountToAdd;
                amount -= amountToAdd;

                if (amount <= 0)
                {
                    // Notify the UI after successfully adding the full amount.
                    OnInventoryChanged?.Invoke();
                    return true;
                }
            }
        }

        // Roll back partial additions if the inventory cannot fit the full amount.
        int amountAdded = originalAmount - amount;

        if (amountAdded > 0)
        {
            RemoveItemWithoutNotification(item, amountAdded);
        }

        return false;
    }

    // This function removes items without firing an inventory event during rollback.
    private void RemoveItemWithoutNotification(ItemData item, int amount)
    {
        // This function reverses a partial addition when the inventory lacks sufficient capacity.
        int remaining = amount;

        for (int i = slots.Count - 1; i >= 0 && remaining > 0; i--)
        {
            if (slots[i].IsEmpty() || slots[i].item != item)
                continue;

            int amountToRemove = Mathf.Min(slots[i].amount, remaining);

            slots[i].amount -= amountToRemove;
            remaining -= amountToRemove;

            if (slots[i].amount <= 0)
                slots[i].Clear();
        }
    }

    // This function returns the inventory slot at the requested index.
    public InventorySlot GetSlot(int index)
    {
        if (index < 0 || index >= slots.Count)
            return null;

        return slots[index];
    }

    // This function checks how many of a specific item the player currently has.
    public int GetItemAmount(ItemData item)
    {
        if (item == null)
            return 0;

        int totalAmount = 0;

        for (int i = 0; i < slots.Count; i++)
        {
            if (!slots[i].IsEmpty() &&
                slots[i].item == item)
            {
                totalAmount += slots[i].amount;
            }
        }

        return totalAmount;
    }

    // This function checks whether the player has at least one of the specified item.
    public bool HasItem(ItemData item)
    {
        return GetItemAmount(item) > 0;
    }

    // This function removes a specified amount of an item from the inventory.
    public bool RemoveItem(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0)
            return false;

        if (GetItemAmount(item) < amount)
            return false;

        int remaining = amount;

        // Remove items starting from the first matching slot.
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].IsEmpty() ||
                slots[i].item != item)
            {
                continue;
            }

            int amountToRemove =
                Mathf.Min(slots[i].amount, remaining);

            slots[i].amount -= amountToRemove;
            remaining -= amountToRemove;

            if (slots[i].amount <= 0)
            {
                slots[i].Clear();
            }

            if (remaining <= 0)
                break;
        }

        // Tell the inventory UI that the inventory contents changed.
        OnInventoryChanged?.Invoke();

        return true;
    }
}