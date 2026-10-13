using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EquipmentUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EquipmentManager equipmentManager;
    [SerializeField] private InventorySystem inventory;
    [SerializeField] private BonfireUI bonfireUI;

    [Header("Equipment Rows")]
    [SerializeField] private Transform shieldRow;
    [SerializeField] private Transform quickItemRow;
    [SerializeField] private Transform talismanRow;

    [Header("Equipment Overview")]
    [SerializeField] private GameObject equipmentContainer;

    [Header("Inventory Display")]
    [SerializeField] private Transform inventoryContent;
    [SerializeField] private EquipmentUIItemButton itemButtonPrefab;
    [SerializeField] private TMP_Text selectedItemText;

    [Header("Action Buttons")]
    [SerializeField] private Button equipButton;
    [SerializeField] private Button unequipButton;
    [SerializeField] private Button discardButton;

    [Header("Inventory Selection Panel")]
    [SerializeField] private GameObject inventorySelectionPanel;

    private EquipmentSlotType selectedSlotType;
    private int selectedSlotIndex = -1;
    private ItemData selectedItem;

    [SerializeField] private GameObject equipmentBackButton;

    private void OnEnable()
    {
        // This function opens the equipment overview and hides inventory selection.
        if (equipmentContainer != null)
            equipmentContainer.SetActive(true);

        if (equipmentBackButton != null)
            equipmentBackButton.SetActive(true);

        if (inventorySelectionPanel != null)
            inventorySelectionPanel.SetActive(false);

        selectedSlotIndex = -1;
        selectedItem = null;

        if (inventory != null)
            inventory.OnInventoryChanged += RefreshUI;

        if (equipmentManager != null)
            equipmentManager.OnEquipmentChanged += RefreshUI;

        RefreshUI();
        RefreshActionButtons();
        UpdateSelectedItemText();
    }

    private void OnDisable()
    {
        // This function unsubscribes from events when the equipment screen is disabled.
        if (inventory != null)
            inventory.OnInventoryChanged -= RefreshUI;

        if (equipmentManager != null)
            equipmentManager.OnEquipmentChanged -= RefreshUI;
    }

    public void RefreshUI()
    {
        // This function rebuilds all equipment rows and refreshes compatible inventory items.
        if (equipmentManager == null || inventory == null)
            return;

        BuildEquipmentRow(
            shieldRow,
            equipmentManager.ShieldSlots,
            EquipmentSlotType.Shield,
            "Shield"
        );

        BuildEquipmentRow(
            quickItemRow,
            equipmentManager.QuickSlots,
            EquipmentSlotType.Throwable,
            "Quick"
        );

        BuildEquipmentRow(
            talismanRow,
            equipmentManager.TalismanSlots,
            EquipmentSlotType.Talisman,
            "Talisman"
        );

        RefreshInventoryItems();
        RefreshActionButtons();
    }

    private void BuildEquipmentRow(
        Transform row,
        IReadOnlyList<ItemData> slots,
        EquipmentSlotType slotType,
        string rowLabel)
    {
        // This function creates one button for each equipment slot in the specified row.
        if (row == null)
            return;

        ClearChildren(row);

        for (int i = 0; i < slots.Count; i++)
        {
            int slotIndex = i;
            ItemData equippedItem = slots[i];

            EquipmentUIItemButton slotButton =
                Instantiate(itemButtonPrefab, row);

            string label = rowLabel + " " + (i + 1);

            if (equippedItem != null)
            {
                label += "\n" + equippedItem.itemName;
            }
            else
            {
                label += "\nEmpty";
            }

            slotButton.Setup(
                equippedItem != null ? equippedItem.inventoryIcon : null,
                label,
                () => SelectEquipmentSlot(slotType, slotIndex)
            );
        }
    }

    // This function switches from the equipment overview to inventory selection.
    private void SelectEquipmentSlot(
        EquipmentSlotType slotType,
        int slotIndex)
    {
        selectedSlotType = slotType;
        selectedSlotIndex = slotIndex;
        selectedItem = null;

        if (equipmentContainer != null)
            equipmentContainer.SetActive(false);

        if (equipmentBackButton != null)
            equipmentBackButton.SetActive(false);

        if (inventorySelectionPanel != null)
            inventorySelectionPanel.SetActive(true);

        RefreshInventoryItems();
        RefreshActionButtons();
        UpdateSelectedItemText();
    }

    private void RefreshInventoryItems()
    {
        // This function lists owned items compatible with the currently selected equipment slot.
        if (inventoryContent == null)
            return;

        ClearChildren(inventoryContent);

        if (inventory == null ||
            selectedSlotIndex < 0 ||
            itemButtonPrefab == null)
        {
            UpdateSelectedItemText();
            return;
        }

        HashSet<ItemData> displayedItems = new HashSet<ItemData>();

        foreach (InventorySlot inventorySlot in inventory.Slots)
        {
            if (inventorySlot == null ||
                inventorySlot.IsEmpty() ||
                inventorySlot.item == null)
            {
                continue;
            }

            ItemData item = inventorySlot.item;

            if (!IsCompatible(item.equipmentType, selectedSlotType))
                continue;

            // Display each item definition once, even when multiple copies are owned.
            if (!displayedItems.Add(item))
                continue;

            ItemData itemToSelect = item;
            int amount = inventory.GetItemAmount(item);

            EquipmentUIItemButton itemButton =
                Instantiate(itemButtonPrefab, inventoryContent);

            itemButton.Setup(
                item.inventoryIcon,
                item.itemName + " x" + amount,
                () => SelectInventoryItem(itemToSelect)
            );
        }

        // This section fills the inventory grid to 24 visible positions.
        const int visibleInventorySlots = 24;

        // This sets the minimum number of visible inventory grid positions to 24.
        const int totalInventorySlots = 24;

        // This calculates how many empty placeholders are needed to fill the grid.
        int emptySlotsNeeded = Mathf.Max(0, totalInventorySlots - displayedItems.Count);

        for (int i = 0; i < emptySlotsNeeded; i++)
        {
            // This creates one disabled placeholder for an unused grid position.
            EquipmentUIItemButton emptyButton =
                Instantiate(itemButtonPrefab, inventoryContent);

            emptyButton.Setup(
                null,
                "Empty",
                null,
                false
            );
        }

        while (inventoryContent.childCount < totalInventorySlots)
        {
            EquipmentUIItemButton emptyButton =
                Instantiate(itemButtonPrefab, inventoryContent);

            emptyButton.Setup(
                null,
                "Empty",
                null,
                false
            );
        }

        UpdateSelectedItemText();
    }

    private void SelectInventoryItem(ItemData item)
    {
        // This function selects an inventory item to equip or discard.
        selectedItem = item;

        RefreshActionButtons();
        UpdateSelectedItemText();
    }

    public void EquipSelectedItem()
    {
        // This function equips the selected inventory item into the selected equipment slot.
        if (equipmentManager == null ||
            selectedItem == null ||
            selectedSlotIndex < 0)
        {
            return;
        }

        if (equipmentManager.TryEquip(
            selectedItem,
            selectedSlotType,
            selectedSlotIndex))
        {
            selectedItem = null;
            RefreshUI();
        }
    }

    public void UnequipSelectedSlot()
    {
        // This function unequips the item from the currently selected equipment slot.
        if (equipmentManager == null || selectedSlotIndex < 0)
            return;

        if (equipmentManager.Unequip(
            selectedSlotType,
            selectedSlotIndex))
        {
            selectedItem = null;
            RefreshUI();
        }
    }

    public void DiscardSelectedItem()
    {
        // This function discards one unit of the selected inventory item.
        if (equipmentManager == null || selectedItem == null)
            return;

        if (equipmentManager.DiscardItem(selectedItem))
        {
            selectedItem = null;
            RefreshUI();
        }
    }

    private bool IsCompatible(
        EquipmentSlotType itemType,
        EquipmentSlotType slotType)
    {
        // This function permits throwables and usables in the shared quick-item row.
        if (slotType == EquipmentSlotType.Throwable ||
            slotType == EquipmentSlotType.Usable)
        {
            return itemType == EquipmentSlotType.Throwable ||
                   itemType == EquipmentSlotType.Usable;
        }

        return itemType == slotType;
    }

    private void RefreshActionButtons()
    {
        // This function enables actions only when their required selections are available.
        bool hasSlot = selectedSlotIndex >= 0;
        bool hasItem = selectedItem != null;

        if (equipButton != null)
        {
            equipButton.interactable =
                hasSlot &&
                hasItem &&
                inventory != null &&
                inventory.HasItem(selectedItem);
        }

        if (unequipButton != null)
        {
            unequipButton.interactable =
                hasSlot &&
                GetSelectedEquippedItem() != null;
        }

        if (discardButton != null)
        {
            discardButton.interactable =
                hasItem &&
                inventory != null &&
                inventory.HasItem(selectedItem);
        }
    }

    private ItemData GetSelectedEquippedItem()
    {
        // This function returns the item currently occupying the selected slot.
        if (equipmentManager == null || selectedSlotIndex < 0)
            return null;

        switch (selectedSlotType)
        {
            case EquipmentSlotType.Shield:
                if (selectedSlotIndex < equipmentManager.ShieldSlots.Count)
                    return equipmentManager.ShieldSlots[selectedSlotIndex];
                break;

            case EquipmentSlotType.Throwable:
            case EquipmentSlotType.Usable:
                if (selectedSlotIndex < equipmentManager.QuickSlots.Count)
                    return equipmentManager.QuickSlots[selectedSlotIndex];
                break;

            case EquipmentSlotType.Talisman:
                if (selectedSlotIndex < equipmentManager.TalismanSlots.Count)
                    return equipmentManager.TalismanSlots[selectedSlotIndex];
                break;
        }

        return null;
    }

    private void UpdateSelectedItemText()
    {
        // This function displays which inventory item or equipment slot is currently selected.
        if (selectedItemText == null)
            return;

        if (selectedItem != null)
        {
            selectedItemText.text =
                selectedItem.itemName +
                "\n" + selectedItem.description;
        }
        else if (selectedSlotIndex >= 0)
        {
            ItemData equippedItem = GetSelectedEquippedItem();

            selectedItemText.text = equippedItem != null
                ? equippedItem.itemName
                : "Empty slot selected";
        }
        else
        {
            selectedItemText.text = "Select an equipment slot";
        }
    }

    private void ClearChildren(Transform parent)
    {
        // This function destroys generated UI buttons before rebuilding a row or inventory list.
        if (parent == null)
            return;

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Destroy(parent.GetChild(i).gameObject);
        }
    }

    // This function returns to the equipment overview and restores its BackButton.
    public void CloseInventorySelection()
    {
        if (inventorySelectionPanel != null)
            inventorySelectionPanel.SetActive(false);

        if (equipmentContainer != null)
            equipmentContainer.SetActive(true);

        if (equipmentBackButton != null)
            equipmentBackButton.SetActive(true);

        selectedSlotIndex = -1;
        selectedItem = null;

        RefreshActionButtons();
        UpdateSelectedItemText();
    }
}


