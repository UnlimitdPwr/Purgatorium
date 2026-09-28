using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TMP_Text amountText;
    [SerializeField] private Image selectionHighlight;

    private int slotIndex;

    // This function sets up the UI slot with its corresponding inventory slot.
    public void Setup(InventorySlot slot, int index)
    {
        slotIndex = index;

        UpdateSlot(slot);
    }

    // This function updates the visual appearance of the slot.
    public void UpdateSlot(InventorySlot slot)
    {
        // This checks whether the required UI references have been assigned.
        if (itemIcon == null)
        {
            Debug.LogError(
                "InventorySlotUI is missing its Item Icon reference.",
                this
            );

            return;
        }

        if (amountText == null)
        {
            Debug.LogError(
                "InventorySlotUI is missing its Amount Text reference.",
                this
            );

            return;
        }

        // This hides the item visuals when the inventory slot is empty.
        if (slot == null || slot.IsEmpty())
        {
            itemIcon.enabled = false;
            amountText.enabled = false;

            return;
        }

        // This displays the item icon and stack amount.
        itemIcon.enabled = true;
        amountText.enabled = true;

        // This displays the inventory-specific sprite for the item.
        itemIcon.sprite = slot.item.inventoryIcon;

        // This displays the number of items in the stack.
        amountText.text = slot.amount.ToString();
    }

    // This function controls whether this slot is visually selected.
    public void SetSelected(bool selected)
    {
        if (selectionHighlight != null)
        {
            selectionHighlight.enabled = selected;
        }
    }
}