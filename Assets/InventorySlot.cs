using System;

// This class represents one slot inside the player's inventory.
[Serializable]
public class InventorySlot
{
    public ItemData item;
    public int amount;

    // This function checks whether this inventory slot is empty.
    public bool IsEmpty()
    {
        return item == null || amount <= 0;
    }

    // This function completely clears the inventory slot.
    public void Clear()
    {
        item = null;
        amount = 0;
    }
}
