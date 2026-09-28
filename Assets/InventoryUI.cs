using TMPro;
using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private PlayerInput playerInput;

    [Header("Inventory")]
    [SerializeField] private InventorySystem inventory;

    [Header("UI")]
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private Transform content;
    [SerializeField] private InventorySlotUI slotPrefab;

    [Header("Selected Item")]
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text itemDescriptionText;

    private InventorySlotUI[] slotUI;
    private int selectedIndex = 0;

    private void Start()
    {
        // This function creates the inventory UI and hides it when the game starts.
        CreateInventoryUI();

        inventoryPanel.SetActive(false);
    }

    private void Update()
    {
        // This checks the centralized player input for the inventory button.
        if (playerInput != null &&
            playerInput.InventoryPressed)
        {
            ToggleInventory();
        }
    }

    // This function opens or closes the inventory window.
    public void ToggleInventory()
    {
        bool shouldShow = !inventoryPanel.activeSelf;

        inventoryPanel.SetActive(shouldShow);

        if (shouldShow)
        {
            RefreshInventory();
        }
    }

    // This function creates one UI slot for every inventory slot.
    private void CreateInventoryUI()
    {
        int slotCount = inventory.Slots.Count;

        slotUI = new InventorySlotUI[slotCount];

        for (int i = 0; i < slotCount; i++)
        {
            InventorySlotUI newSlot =
                Instantiate(slotPrefab, content);

            slotUI[i] = newSlot;

            newSlot.Setup(
                inventory.GetSlot(i),
                i
            );
        }

        UpdateSelection();
    }

    // This function refreshes every inventory slot.
    public void RefreshInventory()
    {
        for (int i = 0; i < slotUI.Length; i++)
        {
            slotUI[i].UpdateSlot(
                inventory.GetSlot(i)
            );
        }

        UpdateSelection();
    }

    // This function updates which inventory slot is currently selected.
    private void UpdateSelection()
    {
        if (slotUI == null || slotUI.Length == 0)
            return;

        for (int i = 0; i < slotUI.Length; i++)
        {
            slotUI[i].SetSelected(
                i == selectedIndex
            );
        }

        UpdateSelectedItemInformation();
    }

    // This function displays the name and description of the selected item.
    private void UpdateSelectedItemInformation()
    {
        InventorySlot slot =
            inventory.GetSlot(selectedIndex);

        if (slot == null || slot.IsEmpty())
        {
            itemNameText.text = "";
            itemDescriptionText.text = "";

            return;
        }

        itemNameText.text = slot.item.itemName;
        itemDescriptionText.text = slot.item.description;
    }
}