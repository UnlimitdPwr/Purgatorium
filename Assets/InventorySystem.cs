using System.Collections.Generic;
using UnityEngine;

public class InventorySystem : MonoBehaviour
{
    [Header("Inventory")]
    [SerializeField]
    private int inventorySize = 24;

    [SerializeField]
    private List<InventorySlot> slots = new List<InventorySlot>();

    public IReadOnlyList<InventorySlot> Slots => slots;

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
}