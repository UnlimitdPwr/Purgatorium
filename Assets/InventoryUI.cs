
using TMPro;
using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private PlayerInput playerInput;

    [Header("Game State")]
    [SerializeField] private BonfireUI bonfireUI;

    [Tooltip("Assign other menu panels that should prevent the inventory from opening, such as Pause or Dialogue menus.")]
    [SerializeField] private GameObject[] otherBlockingMenus;

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
        // This function creates the inventory slots and hides the inventory at startup.
        CreateInventoryUI();

        inventoryPanel.SetActive(false);
    }

    private void Update()
    {
        // This function prevents the inventory from remaining open while another menu is active.
        if (IsInventoryBlocked())
        {
            if (inventoryPanel.activeSelf)
            {
                inventoryPanel.SetActive(false);
            }

            return;
        }

        // This function allows the player to toggle the inventory during normal gameplay.
        if (playerInput != null && playerInput.InventoryPressed)
        {
            ToggleInventory();
        }
    }

    private bool IsInventoryBlocked()
    {
        // This function checks whether the bonfire or another assigned menu blocks inventory access.
        if (bonfireUI != null && bonfireUI.IsOpen)
        {
            return true;
        }

        if (otherBlockingMenus != null)
        {
            foreach (GameObject menu in otherBlockingMenus)
            {
                if (menu != null && menu.activeInHierarchy)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public void ToggleInventory()
    {
        // This function opens or closes the inventory only when gameplay UI is allowed.
        if (IsInventoryBlocked())
        {
            inventoryPanel.SetActive(false);
            return;
        }

        bool shouldShow = !inventoryPanel.activeSelf;

        inventoryPanel.SetActive(shouldShow);

        if (shouldShow)
        {
            RefreshInventory();
        }
    }

    private void CreateInventoryUI()
    {
        // This function creates a UI slot for every slot in the player's inventory.
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

    public void RefreshInventory()
    {
        // This function refreshes the displayed contents of every inventory slot.
        if (slotUI == null)
            return;

        for (int i = 0; i < slotUI.Length; i++)
        {
            slotUI[i].UpdateSlot(
                inventory.GetSlot(i)
            );
        }

        UpdateSelection();
    }

    private void UpdateSelection()
    {
        // This function updates the visual selection and information for the selected slot.
        if (slotUI == null || slotUI.Length == 0)
            return;

        selectedIndex = Mathf.Clamp(
            selectedIndex,
            0,
            slotUI.Length - 1
        );

        for (int i = 0; i < slotUI.Length; i++)
        {
            slotUI[i].SetSelected(
                i == selectedIndex
            );
        }

        UpdateSelectedItemInformation();
    }

    private void UpdateSelectedItemInformation()
    {
        // This function displays the selected item's name and description.
        if (itemNameText == null || itemDescriptionText == null)
            return;

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