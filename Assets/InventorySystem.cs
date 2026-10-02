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

    // This function attempts to add an item and its quantity to the inventory.
    public bool AddItem(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0)
            return false;

        // First, try to add the item to an existing stack.
        for (int i = 0; i < slots.Count; i++)
        {
            if (!slots[i].IsEmpty() &&
                slots[i].item == item &&
                slots[i].amount < item.maxStack)
            {
                int availableSpace =
                    item.maxStack - slots[i].amount;

                int amountToAdd =
                    Mathf.Min(availableSpace, amount);

                slots[i].amount += amountToAdd;
                amount -= amountToAdd;

                if (amount <= 0)
                    return true;
            }
        }

        // If the existing stacks cannot hold everything,
        // look for an empty slot.
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].IsEmpty())
            {
                int amountToAdd =
                    Mathf.Min(item.maxStack, amount);

                slots[i].item = item;
                slots[i].amount = amountToAdd;

                amount -= amountToAdd;

                if (amount <= 0)
                    return true;
            }
        }

        // The inventory could not hold the entire amount.
        return false;
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