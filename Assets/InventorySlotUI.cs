
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InventorySlotUI : MonoBehaviour, IPointerClickHandler
{
    [Header("UI References")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TMP_Text amountText;
    [SerializeField] private Image selectionHighlight;

    private int slotIndex;
    private Action<int> selectionCallback;

    // This function connects the UI slot to its inventory index.
    public void Setup(InventorySlot slot, int index)
    {
        slotIndex = index;
        UpdateSlot(slot);
    }

    // This function connects mouse clicks to the InventoryUI selection system.
    public void SetSelectionCallback(Action<int> callback)
    {
        selectionCallback = callback;
    }

    // This function selects the slot when the player clicks it with the mouse.
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
            selectionCallback?.Invoke(slotIndex);
    }

    // This function updates the displayed item icon and stack amount.
    public void UpdateSlot(InventorySlot slot)
    {
        if (itemIcon == null || amountText == null)
        {
            Debug.LogError("InventorySlotUI is missing its icon or amount reference.", this);
            return;
        }

        if (slot == null || slot.IsEmpty())
        {
            itemIcon.enabled = false;
            amountText.enabled = false;
            return;
        }

        itemIcon.enabled = true;
        amountText.enabled = true;
        itemIcon.sprite = slot.item.inventoryIcon;
        amountText.text = slot.amount.ToString();
    }

    // This function shows or hides the highlight for the selected slot.
    public void SetSelected(bool selected)
    {
        if (selectionHighlight != null)
            selectionHighlight.enabled = selected;
    }
}

