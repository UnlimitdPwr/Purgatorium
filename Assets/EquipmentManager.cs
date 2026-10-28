using System;
using System.Collections.Generic;
using UnityEngine;

public class EquipmentManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InventorySystem inventory;
    [SerializeField] private BonfireUI bonfireUI;

    [Header("Initial Slot Capacity")]
    [SerializeField, Min(1)] private int shieldSlotCount = 2;
    [SerializeField, Min(1)] private int quickSlotCount = 5;
    [SerializeField, Min(1)] private int talismanSlotCount = 2;

    private List<ItemData> shieldSlots = new List<ItemData>();
    private List<ItemData> quickSlots = new List<ItemData>();
    private List<ItemData> talismanSlots = new List<ItemData>();

    public IReadOnlyList<ItemData> ShieldSlots => shieldSlots;
    public IReadOnlyList<ItemData> QuickSlots => quickSlots;
    public IReadOnlyList<ItemData> TalismanSlots => talismanSlots;

    public event Action OnEquipmentChanged;

    private void Awake()
    {
        // This function creates the initial shield, quick-item, and talisman slots.
        InitializeSlots(shieldSlots, shieldSlotCount);
        InitializeSlots(quickSlots, quickSlotCount);
        InitializeSlots(talismanSlots, talismanSlotCount);
    }

    private void OnEnable()
    {
        // This function subscribes to inventory changes so items are unequipped when their quantity reaches zero.
        if (inventory != null)
            inventory.OnInventoryChanged += HandleInventoryChanged;
    }

    private void OnDisable()
    {
        // This function removes the inventory subscription when this component is disabled.
        if (inventory != null)
            inventory.OnInventoryChanged -= HandleInventoryChanged;
    }

    private void InitializeSlots(List<ItemData> slots, int count)
    {
        // This function creates the requested number of empty equipment slots.
        slots.Clear();

        for (int i = 0; i < count; i++)
            slots.Add(null);
    }

    public bool TryEquip(ItemData item, EquipmentSlotType slotType, int slotIndex)
    {
        // This function equips an owned, compatible item only if it is not equipped in another slot.
        if (!CanChangeEquipment() || item == null)
            return false;

        if (inventory == null || !inventory.HasItem(item))
            return false;

        List<ItemData> slots = GetSlotsForType(slotType);

        if (slots == null || slotIndex < 0 || slotIndex >= slots.Count)
            return false;

        if (!IsCompatible(item.equipmentType, slotType))
            return false;

        // Prevent the same item asset from being assigned to more than one equipment slot.
        if (IsEquippedInAnotherSlot(item, slotType, slotIndex))
        {
            Debug.Log("Cannot equip " + item.itemName + ": it is already equipped in another slot.");
            return false;
        }

        // Assign the item to the selected slot.
        slots[slotIndex] = item;

        OnEquipmentChanged?.Invoke();
        return true;
    }

    public bool Unequip(EquipmentSlotType slotType, int slotIndex)
    {
        // This function clears an equipment slot without removing the item from inventory.
        if (!CanChangeEquipment())
            return false;

        List<ItemData> slots = GetSlotsForType(slotType);

        if (slots == null ||
            slotIndex < 0 ||
            slotIndex >= slots.Count ||
            slots[slotIndex] == null)
        {
            return false;
        }

        slots[slotIndex] = null;

        OnEquipmentChanged?.Invoke();
        return true;
    }

    public bool DiscardItem(ItemData item)
    {
        // This function discards one copy of an owned item and clears its slots if none remain.
        if (!CanChangeEquipment() ||
            inventory == null ||
            item == null ||
            !inventory.HasItem(item))
        {
            return false;
        }

        if (!inventory.RemoveItem(item, 1))
            return false;

        // Keep equipment references while at least one copy remains in inventory.
        if (!inventory.HasItem(item))
        {
            ClearItemFromSlots(shieldSlots, item);
            ClearItemFromSlots(quickSlots, item);
            ClearItemFromSlots(talismanSlots, item);
        }

        OnEquipmentChanged?.Invoke();
        return true;
    }

    public bool IncreaseSlotCapacity(EquipmentSlotType slotType, int additionalSlots)
    {
        // This function adds empty equipment slots for future capacity upgrades.
        if (additionalSlots <= 0)
            return false;

        List<ItemData> slots = GetSlotsForType(slotType);

        // Quick-item capacity is not upgraded through this method.
        if (slots == null ||
            slotType == EquipmentSlotType.Throwable ||
            slotType == EquipmentSlotType.Usable)
        {
            return false;
        }

        for (int i = 0; i < additionalSlots; i++)
            slots.Add(null);

        OnEquipmentChanged?.Invoke();
        return true;
    }

    private bool CanChangeEquipment()
    {
        // This function restricts equipment changes to the open bonfire equipment screen.
        return bonfireUI != null && bonfireUI.IsEquipmentScreenOpen;
    }

    private List<ItemData> GetSlotsForType(EquipmentSlotType slotType)
    {
        // This function returns the slot list belonging to the specified equipment category.
        switch (slotType)
        {
            case EquipmentSlotType.Shield:
                return shieldSlots;

            case EquipmentSlotType.Throwable:
            case EquipmentSlotType.Usable:
                return quickSlots;

            case EquipmentSlotType.Talisman:
                return talismanSlots;

            default:
                return null;
        }
    }

    private bool IsCompatible(EquipmentSlotType itemType, EquipmentSlotType slotType)
    {
        // This function allows throwable and usable items to share quick-item slots.
        if (slotType == EquipmentSlotType.Throwable ||
            slotType == EquipmentSlotType.Usable)
        {
            return itemType == EquipmentSlotType.Throwable ||
                   itemType == EquipmentSlotType.Usable;
        }

        return itemType == slotType;
    }

    public bool IsEquippedInAnotherSlot(
        ItemData item,
        EquipmentSlotType slotType,
        int slotIndex)
    {
        // This function checks every equipment category, excluding the selected slot, for the same item.
        if (item == null)
            return false;

        List<ItemData> selectedSlots = GetSlotsForType(slotType);

        if (selectedSlots == null ||
            slotIndex < 0 ||
            slotIndex >= selectedSlots.Count)
        {
            return false;
        }

        return ContainsItemInAnotherSlot(shieldSlots, item, selectedSlots, slotIndex) ||
               ContainsItemInAnotherSlot(quickSlots, item, selectedSlots, slotIndex) ||
               ContainsItemInAnotherSlot(talismanSlots, item, selectedSlots, slotIndex);
    }

    private bool ContainsItemInAnotherSlot(
        List<ItemData> slots,
        ItemData item,
        List<ItemData> selectedSlots,
        int selectedIndex)
    {
        // This function detects the same item in any slot except the slot currently being edited.
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots == selectedSlots && i == selectedIndex)
                continue;

            if (slots[i] == item)
                return true;
        }

        return false;
    }

    public bool IsQuickItemEquipped(ItemData item)
    {
        // This function checks whether the specified item is assigned to any quick-item slot.
        if (item == null)
            return false;

        for (int i = 0; i < quickSlots.Count; i++)
        {
            if (quickSlots[i] == item)
                return true;
        }

        return false;
    }

    private void HandleInventoryChanged()
    {
        // This function clears equipped references when their items are no longer owned.
        bool equipmentChanged = false;

        equipmentChanged |= ClearUnownedItems(shieldSlots);
        equipmentChanged |= ClearUnownedItems(quickSlots);
        equipmentChanged |= ClearUnownedItems(talismanSlots);

        if (equipmentChanged)
            OnEquipmentChanged?.Invoke();
    }

    private bool ClearUnownedItems(List<ItemData> slots)
    {
        // This function removes equipped references when an item's inventory quantity reaches zero.
        bool changed = false;

        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] != null &&
                (inventory == null || !inventory.HasItem(slots[i])))
            {
                slots[i] = null;
                changed = true;
            }
        }

        return changed;
    }

    private void ClearItemFromSlots(List<ItemData> slots, ItemData item)
    {
        // This function clears every slot that references the specified item.
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] == item)
                slots[i] = null;
        }
    }
}
