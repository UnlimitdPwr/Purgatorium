using UnityEngine.InputSystem;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EquipmentHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EquipmentManager equipmentManager;
    [SerializeField] private InventorySystem inventory;
    [SerializeField] private BonfireUI bonfireUI;

    [Header("Quick Item Slot")]
    [SerializeField] private Image quickItemIcon;
    [SerializeField] private TMP_Text quickItemQuantity;

    [Header("Shield Slot")]
    [SerializeField] private Image shieldIcon;

    // The quick slot currently selected during gameplay. (The active shield lives in
    // EquipmentManager, because parry and block need to read it too.)
    private int selectedQuickSlot = -1;

    // This function subscribes to inventory and equipment changes,
    // selects initial equipment, and refreshes the HUD.
    private void OnEnable()
    {
        if (equipmentManager != null)
            equipmentManager.OnEquipmentChanged += RefreshHUD;

        if (inventory != null)
            inventory.OnInventoryChanged += RefreshHUD;

        SelectFirstAvailableSlots();
        RefreshHUD();
    }

    // This function removes event subscriptions when the HUD is disabled.
    private void OnDisable()
    {
        if (equipmentManager != null)
            equipmentManager.OnEquipmentChanged -= RefreshHUD;

        if (inventory != null)
            inventory.OnInventoryChanged -= RefreshHUD;
    }


    // This function detects C and V presses using Unity's New Input System
    // and prevents equipment cycling while the Bonfire equipment screen is open.
    private void Update()
    {
        if (equipmentManager == null || Keyboard.current == null)
            return;

        // Don't cycle equipment while the Bonfire equipment screen is open.
        if (bonfireUI != null && bonfireUI.IsEquipmentScreenOpen)
            return;

        // C cycles through equipped quick items.
        if (Keyboard.current.cKey.wasPressedThisFrame)
            CycleQuickItem();

        // V cycles through equipped shields.
        if (Keyboard.current.vKey.wasPressedThisFrame)
            CycleShield();
    }


    // This function selects the first available quick item and shield on startup.
    private void SelectFirstAvailableSlots()
    {
        selectedQuickSlot = FindNextQuickItem(-1);
    }

    // This function refreshes the selected items and their displayed icons.
    public void RefreshHUD()
    {
        if (equipmentManager == null)
        {
            ClearHUD();
            return;
        }

        // If a selected item was removed, select another occupied slot.
        if (!IsQuickSlotUsable(selectedQuickSlot))
            selectedQuickSlot = FindNextQuickItem(selectedQuickSlot);

        RefreshQuickItem();
        RefreshShield();
    }

    // This function cycles to the next owned quick item when C is pressed.
    private void CycleQuickItem()
    {
        int nextSlot = FindNextQuickItem(selectedQuickSlot);

        if (nextSlot < 0 || nextSlot == selectedQuickSlot)
            return;

        selectedQuickSlot = nextSlot;
        RefreshQuickItem();
    }

    // This function cycles to the next equipped shield when V is pressed.
    private void CycleShield()
    {
        if (equipmentManager.CycleActiveShield())
            RefreshShield();
    }

    // This function finds the next quick-item slot containing an owned item,
    // wrapping around the list and skipping empty or depleted slots.
    private int FindNextQuickItem(int currentSlot)
    {
        if (equipmentManager == null)
            return -1;

        var slots = equipmentManager.QuickSlots;

        for (int step = 1; step <= slots.Count; step++)
        {
            int index = (currentSlot + step) % slots.Count;

            if (IsQuickSlotUsable(index))
                return index;
        }

        return -1;
    }

    // This function checks that a quick-item slot contains an item still owned.
    private bool IsQuickSlotUsable(int index)
    {
        if (equipmentManager == null ||
            inventory == null ||
            index < 0 ||
            index >= equipmentManager.QuickSlots.Count)
        {
            return false;
        }

        ItemData item = equipmentManager.QuickSlots[index];

        return item != null && inventory.HasItem(item);
    }

    // This function displays the selected quick item and its total inventory quantity.
    private void RefreshQuickItem()
    {
        ItemData item = IsQuickSlotUsable(selectedQuickSlot)
            ? equipmentManager.QuickSlots[selectedQuickSlot]
            : null;

        if (quickItemIcon != null)
        {
            quickItemIcon.sprite = item != null ? item.inventoryIcon : null;
            quickItemIcon.enabled = item != null;
        }

        if (quickItemQuantity != null)
        {
            quickItemQuantity.text = item != null
                ? inventory.GetItemAmount(item).ToString()
                : "";
        }
    }

    // This function displays the shield selected for gameplay.
    private void RefreshShield()
    {
        ItemData item = equipmentManager.ActiveShield;

        if (shieldIcon != null)
        {
            shieldIcon.sprite = item != null ? item.inventoryIcon : null;
            shieldIcon.enabled = item != null;
        }
    }

    // This function clears both icons and the quantity if the manager is unavailable.
    private void ClearHUD()
    {
        selectedQuickSlot = -1;

        if (quickItemIcon != null)
        {
            quickItemIcon.sprite = null;
            quickItemIcon.enabled = false;
        }

        if (quickItemQuantity != null)
            quickItemQuantity.text = "";

        if (shieldIcon != null)
        {
            shieldIcon.sprite = null;
            shieldIcon.enabled = false;
        }
    }
}

