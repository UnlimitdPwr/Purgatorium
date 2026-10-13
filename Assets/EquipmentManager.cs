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

    private void InitializeSlots(List<ItemData> slots, int count)
    {
        // This function creates the requested number of empty equipment slots.
        slots.Clear();

        for (int i = 0; i < count; i++)
        {
            slots.Add(null);
        }
    }

    public bool TryEquip(
        ItemData item,
        EquipmentSlotType slotType,
        int slotIndex)
    {
        // This function equips an owned item into a compatible slot while at the equipment screen.
        if (!CanChangeEquipment() || item == null)
            return false;

        if (inventory == null || !inventory.HasItem(item))
            return false;

        List<ItemData> slots = GetSlotsForType(slotType);

        if (slots == null ||
            slotIndex < 0 ||
            slotIndex >= slots.Count)
        {
            return false;
        }

        if (!IsCompatible(item.equipmentType, slotType))
            return false;

        slots[slotIndex] = item;

        OnEquipmentChanged?.Invoke();
        return true;
    }

    public bool Unequip(
        EquipmentSlotType slotType,
        int slotIndex)
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
        // This function discards one unit of an owned item and clears its equipment references if none remain.
        if (!CanChangeEquipment() ||
            inventory == null ||
            item == null ||
            !inventory.HasItem(item))
        {
            return false;
        }

        if (!inventory.RemoveItem(item, 1))
            return false;

        // Keep equipped references if more copies of the item remain in inventory.
        if (!inventory.HasItem(item))
        {
            ClearItemFromSlots(shieldSlots, item);
            ClearItemFromSlots(quickSlots, item);
            ClearItemFromSlots(talismanSlots, item);
        }

        OnEquipmentChanged?.Invoke();
        return true;
    }

    public bool IncreaseSlotCapacity(
        EquipmentSlotType slotType,
        int additionalSlots)
    {
        // This function permanently adds empty slots for future capacity upgrades.
        if (additionalSlots <= 0)
            return false;

        List<ItemData> slots = GetSlotsForType(slotType);

        if (slots == null ||
            slotType == EquipmentSlotType.Throwable ||
            slotType == EquipmentSlotType.Usable)
        {
            return false;
        }

        for (int i = 0; i < additionalSlots; i++)
        {
            slots.Add(null);
        }

        OnEquipmentChanged?.Invoke();
        return true;
    }

    private bool CanChangeEquipment()
    {
        // This function prevents equipment changes outside the bonfire equipment screen.
        return bonfireUI != null &&
               bonfireUI.IsEquipmentScreenOpen;
    }

    private List<ItemData> GetSlotsForType(EquipmentSlotType slotType)
    {
        // This function returns the slot list belonging to the selected equipment category.
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

    private bool IsCompatible(
        EquipmentSlotType itemType,
        EquipmentSlotType slotType)
    {
        // This function allows throwables and usable items to share the same quick-item slots.
        if (slotType == EquipmentSlotType.Throwable ||
            slotType == EquipmentSlotType.Usable)
        {
            return itemType == EquipmentSlotType.Throwable ||
                   itemType == EquipmentSlotType.Usable;
        }

        return itemType == slotType;
    }

    private void ClearItemFromSlots(
        List<ItemData> slots,
        ItemData item)
    {
        // This function clears every slot referencing an item that is no longer owned.
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] == item)
            {
                slots[i] = null;
            }
        }
    }

    public bool IsQuickItemEquipped(ItemData item)
    {
        // This function checks whether the specified item is assigned to a quick-item slot.
        if (item == null)
            return false;

        for (int i = 0; i < quickSlots.Count; i++)
        {
            if (quickSlots[i] == item)
                return true;
        }

        return false;
    }

    private void OnEnable()
    {
        // This function subscribes to inventory changes so depleted items can be unequipped.
        if (inventory != null)
            inventory.OnInventoryChanged += HandleInventoryChanged;
    }

    private void OnDisable()
    {
        // This function unsubscribes from inventory changes to prevent duplicate callbacks.
        if (inventory != null)
            inventory.OnInventoryChanged -= HandleInventoryChanged;
    }

    private void HandleInventoryChanged()
    {
        // This function clears equipment slots referencing items the player no longer owns.
        bool equipmentChanged = false;

        equipmentChanged |= ClearUnownedItems(shieldSlots);
        equipmentChanged |= ClearUnownedItems(quickSlots);
        equipmentChanged |= ClearUnownedItems(talismanSlots);

        if (equipmentChanged)
            OnEquipmentChanged?.Invoke();
    }

    private bool ClearUnownedItems(List<ItemData> slots)
    {
        // This function removes equipped references when the corresponding inventory quantity reaches zero.
        bool changed = false;

        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] != null && (inventory == null || !inventory.HasItem(slots[i])))
            {
                slots[i] = null;
                changed = true;
            }
        }

        return changed;
    }
}

