using UnityEngine;
using System.Collections.Generic;

public class InventorySystem : MonoBehaviour
{
    [SerializeField]
    private int inventorySize = 24;

    [SerializeField]
    private List<InventorySlot> slots = new List<InventorySlot>();

    public IReadOnlyList<InventorySlot> Slots => slots;

    private void Awake()
    {
        InitializeInventory();
    }

    private void Start()
    {
        // Temporary test
        Debug.Log($"Inventory contains {Slots.Count} slots.");
    }

    private void InitializeInventory()
    {
        while (slots.Count < inventorySize)
        {
            slots.Add(new InventorySlot());
        }

        if (slots.Count > inventorySize)
        {
            slots.RemoveRange(inventorySize, slots.Count - inventorySize);
        }
    }

    public bool AddItem(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0)
            return false;

    

        // First try to add to an existing stack.
        for (int i = 0; i<slots.Count; i++)
        {
            if (!slots[i].IsEmpty() &&
                slots[i].item == item &&
                slots[i].amount<item.maxStack)
            {
                int space = item.maxStack - slots[i].amount;
                int amountToAdd = Mathf.Min(space, amount);

                slots[i].amount += amountToAdd;
                amount -= amountToAdd;

                if (amount <= 0)
                    return true;
            }
        }

      // Then look for empty slots.
        for (int i = 0; i < slots.Count; i++)
        {
           if (slots[i].IsEmpty())
           {
               int amountToAdd = Mathf.Min(item.maxStack, amount);

               slots[i].item = item;
               slots[i].amount = amountToAdd;

               amount -= amountToAdd;

               if (amount <= 0)
                   return true;
           }
        }

       return false;
    }

    public InventorySlot GetSlot(int index)
    {
        if (index < 0 || index >= slots.Count)
            return null;

        return slots[index];
    }
}
