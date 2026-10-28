
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.Events;

public class InventoryUI : MonoBehaviour
{
    // This enum defines the four inventory categories.
    private enum InventoryCategory
    {
        Shields,
        Talismans,
        UsablesThrowables,
        Other
    }

    [Header("Player")]
    [SerializeField] private PlayerInput playerInput;

    [Header("Game State")]
    [SerializeField] private BonfireUI bonfireUI;

    [Tooltip("Menus that prevent the inventory from opening.")]
    [SerializeField] private GameObject[] otherBlockingMenus;

    [Header("Inventory Data")]
    [SerializeField] private InventorySystem inventory;

    [Header("Inventory UI")]
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private Transform content;
    [SerializeField] private InventorySlotUI slotPrefab;

    [Header("Category Tabs")]
    [Tooltip("Order: Shields, Talismans, Usables/Throwables, Other.")]
    [SerializeField] private Button[] categoryButtons = new Button[4];

    [Tooltip("Assign each category button's Image in the same order.")]
    [SerializeField] private Image[] categoryTabImages = new Image[4];

    [SerializeField] private Color normalTabColor = Color.white;
    [SerializeField] private Color selectedTabColor = Color.yellow;

    [Header("Selected Item")]
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text itemDescriptionText;

    // This array stores the UI object for each inventory slot.
    private InventorySlotUI[] slotUI;

    // This list stores the original inventory indices visible in the active category.
    private readonly List<int> visibleSlotIndices = new List<int>();

    // This stores the currently displayed category.
    private InventoryCategory currentCategory = InventoryCategory.Shields;

    // This tracks whether keyboard navigation is focused on category tabs.
    private bool isOnCategoryTabs;

    // This stores which category tab currently has keyboard focus.
    private int focusedCategoryIndex;

    // This stores the currently selected inventory slot index.
    private int selectedIndex = -1;

    // This stores the number of columns in the inventory grid.
    private int columnCount = 1;

    // This stores the exact button callbacks so they can be removed safely.
    private UnityAction[] categoryButtonCallbacks;

    private void Start()
    {
        // This function creates the inventory slots, connects category buttons, and hides the inventory.
        CreateInventoryUI();
        SetupCategoryButtons();

        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        // This function removes the category button callbacks registered by this script.
        if (categoryButtons == null || categoryButtonCallbacks == null)
            return;

        for (int i = 0; i < categoryButtons.Length; i++)
        {
            if (categoryButtons[i] != null &&
                i < categoryButtonCallbacks.Length &&
                categoryButtonCallbacks[i] != null)
            {
                categoryButtons[i].onClick.RemoveListener(categoryButtonCallbacks[i]);
            }
        }
    }

    private void Update()
    {
        // This function closes the inventory whenever another menu blocks it.
        if (IsInventoryBlocked())
        {
            if (inventoryPanel != null && inventoryPanel.activeSelf)
                inventoryPanel.SetActive(false);

            return;
        }

        // This function toggles the inventory using the existing player input.
        if (playerInput != null && playerInput.InventoryPressed)
            ToggleInventory();

        // This function processes arrow-key navigation while the inventory is open.
        if (inventoryPanel != null && inventoryPanel.activeSelf)
            HandleKeyboardNavigation();
    }

    private bool IsInventoryBlocked()
    {
        // This function checks whether the bonfire or another menu blocks inventory access.
        if (bonfireUI != null && bonfireUI.IsOpen)
            return true;

        if (otherBlockingMenus != null)
        {
            foreach (GameObject menu in otherBlockingMenus)
            {
                if (menu != null && menu.activeInHierarchy)
                    return true;
            }
        }

        return false;
    }

    public void ToggleInventory()
    {
        // This function opens or closes the inventory when other menus allow it.
        if (inventoryPanel == null)
            return;

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

            // This function starts keyboard navigation on the current category's items.
            isOnCategoryTabs = false;
        }
    }

    private void CreateInventoryUI()
    {
        // This function creates one UI slot for every slot in the player's inventory.
        if (inventory == null || content == null || slotPrefab == null)
        {
            Debug.LogError(
                "InventoryUI is missing its Inventory, Content, or Slot Prefab reference.",
                this
            );
            return;
        }

        int slotCount = inventory.Slots.Count;
        slotUI = new InventorySlotUI[slotCount];

        // This function reads the column count from the existing grid layout.
        GridLayoutGroup grid = content.GetComponent<GridLayoutGroup>();

        if (grid != null &&
            grid.constraint == GridLayoutGroup.Constraint.FixedColumnCount)
        {
            columnCount = Mathf.Max(1, grid.constraintCount);
        }
        else
        {
            Debug.LogWarning(
                "Set Content's Grid Layout Group to Fixed Column Count for correct navigation.",
                content
            );

            columnCount = Mathf.Max(1, slotCount);
        }

        for (int i = 0; i < slotCount; i++)
        {
            InventorySlotUI newSlot = Instantiate(slotPrefab, content);
            slotUI[i] = newSlot;

            newSlot.Setup(inventory.GetSlot(i), i);
            newSlot.SetSelectionCallback(SelectSlot);

            // This hides the slot until its category is filtered.
            newSlot.gameObject.SetActive(false);
        }

        // This function displays Shields as the initial category.
        ApplyCategory(InventoryCategory.Shields);
    }

    private void SetupCategoryButtons()
    {
        // This function connects each button to its corresponding inventory category.
        if (categoryButtons == null)
            return;

        categoryButtonCallbacks = new UnityAction[categoryButtons.Length];

        for (int i = 0; i < categoryButtons.Length && i < 4; i++)
        {
            if (categoryButtons[i] == null)
                continue;

            int capturedIndex = i;

            categoryButtonCallbacks[i] = () => SelectCategory(capturedIndex);
            categoryButtons[i].onClick.AddListener(categoryButtonCallbacks[i]);
        }

        // This function updates the initial category tab appearance.
        UpdateCategoryTabVisuals();
    }

    public void SelectCategory(int categoryIndex)
    {
        // This function switches categories and focuses the corresponding tab.
        if (categoryIndex < 0 || categoryIndex >= 4)
            return;

        focusedCategoryIndex = categoryIndex;
        isOnCategoryTabs = true;

        ApplyCategory((InventoryCategory)categoryIndex);
    }

    private void ApplyCategory(InventoryCategory category)
    {
        // This function filters inventory slots to show only items in the selected category.
        currentCategory = category;
        visibleSlotIndices.Clear();

        if (slotUI == null || inventory == null)
            return;

        for (int i = 0; i < slotUI.Length; i++)
        {
            InventorySlot slot = inventory.GetSlot(i);

            // This checks whether the item belongs to the active category.
            bool matchesCategory = IsItemInCategory(slot, currentCategory);

            slotUI[i].gameObject.SetActive(matchesCategory);

            if (matchesCategory && slot != null && !slot.IsEmpty())
                visibleSlotIndices.Add(i);
        }

        // This selects the first occupied item in the category, if available.
        selectedIndex = visibleSlotIndices.Count > 0
            ? visibleSlotIndices[0]
            : -1;

        focusedCategoryIndex = (int)currentCategory;

        UpdateCategoryTabVisuals();
        UpdateSelection();
    }

    private bool IsItemInCategory(InventorySlot slot, InventoryCategory category)
    {
        // This function checks the item's EquipmentSlotType against the selected category.
        if (slot == null || slot.IsEmpty() || slot.item == null)
            return false;

        EquipmentSlotType type = slot.item.equipmentType;

        switch (category)
        {
            case InventoryCategory.Shields:
                return type == EquipmentSlotType.Shield;

            case InventoryCategory.Talismans:
                return type == EquipmentSlotType.Talisman;

            case InventoryCategory.UsablesThrowables:
                return type == EquipmentSlotType.Usable ||
                       type == EquipmentSlotType.Throwable;

            case InventoryCategory.Other:
                return type == EquipmentSlotType.Other;

            default:
                return false;
        }
    }

    // Highlights the category tab currently being navigated or selected.
    private void UpdateCategoryTabVisuals()
    {
        if (categoryTabImages == null)
            return;

        for (int i = 0; i < categoryTabImages.Length; i++)
        {
            Image tabImage = categoryTabImages[i];

            if (tabImage == null)
                continue;

            // Highlight the keyboard-focused tab while navigating categories.
            // Otherwise, highlight the currently selected inventory category.
            bool isHighlighted = isOnCategoryTabs
                ? i == focusedCategoryIndex
                : i == (int)currentCategory;

            tabImage.color = isHighlighted
                ? selectedTabColor
                : normalTabColor;
        }
    }

    public void RefreshInventory()
    {
        // This function refreshes every slot and reapplies the active category filter.
        if (slotUI == null || inventory == null)
            return;

        for (int i = 0; i < slotUI.Length; i++)
            slotUI[i].UpdateSlot(inventory.GetSlot(i));

        ApplyCategory(currentCategory);
    }

    private void HandleKeyboardNavigation()
    {
        // This function routes arrow keys to category tabs or inventory items.
        if (Keyboard.current == null)
            return;

        bool left = Keyboard.current.leftArrowKey.wasPressedThisFrame;
        bool right = Keyboard.current.rightArrowKey.wasPressedThisFrame;
        bool up = Keyboard.current.upArrowKey.wasPressedThisFrame;
        bool down = Keyboard.current.downArrowKey.wasPressedThisFrame;

        if (isOnCategoryTabs)
        {
            // This function moves horizontally between category buttons.
            if (left)
            {
                SelectCategory((focusedCategoryIndex + 3) % 4);
            }
            else if (right)
            {
                SelectCategory((focusedCategoryIndex + 1) % 4);
            }
            else if (down)
            {
                // This function returns keyboard focus to the inventory item grid.
                isOnCategoryTabs = false;

                // This selects the first visible item if no valid item is selected.
                if (selectedIndex < 0 || !visibleSlotIndices.Contains(selectedIndex))
                    SelectFirstVisibleItem();

                // This restores the slot highlight and updates the category tab visuals.
                UpdateSelection();
                UpdateCategoryTabVisuals();
            }

            return;
        }

        // This function navigates items without switching categories horizontally.
        if (left)
            NavigateHorizontal(-1);
        else if (right)
            NavigateHorizontal(1);
        else if (up)
            NavigateVertical(-1);
        else if (down)
            NavigateVertical(1);
    }

    private void NavigateHorizontal(int direction)
    {
        // This function moves between visible items in the same row.
        if (visibleSlotIndices.Count == 0 || selectedIndex < 0)
            return;

        int position = visibleSlotIndices.IndexOf(selectedIndex);

        if (position < 0)
            return;

        int nextPosition = position + direction;

        // This prevents left/right navigation from wrapping to another row.
        if (nextPosition < 0 || nextPosition >= visibleSlotIndices.Count)
            return;

        if (position / columnCount != nextPosition / columnCount)
            return;

        SelectSlot(visibleSlotIndices[nextPosition]);
    }

    private void NavigateVertical(int direction)
    {
        // This function moves between item rows and returns to the tabs from the top row.
        if (visibleSlotIndices.Count == 0 || selectedIndex < 0)
        {
            if (direction < 0)
                FocusCategoryTabs();

            return;
        }

        int position = visibleSlotIndices.IndexOf(selectedIndex);

        if (position < 0)
            return;

        // This returns focus to the category tabs when Up is pressed on the top row.
        if (direction < 0 && position < columnCount)
        {
            FocusCategoryTabs();
            return;
        }

        int targetPosition = position + columnCount * direction;

        if (targetPosition >= 0 && targetPosition < visibleSlotIndices.Count)
        {
            // This selects the item in the adjacent row.
            SelectSlot(visibleSlotIndices[targetPosition]);
        }
    }

    // This function moves keyboard focus to the active category tab
    // and hides the inventory slot highlight without clearing the selected item.
    private void FocusCategoryTabs()
    {
        isOnCategoryTabs = true;
        focusedCategoryIndex = (int)currentCategory;

        UpdateCategoryTabVisuals();
        UpdateSelection();
    }

    private void SelectFirstVisibleItem()
    {
        // This function selects the first occupied item when entering a category from its tab.
        if (visibleSlotIndices.Count == 0)
        {
            selectedIndex = -1;
            UpdateSelection();
            return;
        }

        SelectSlot(visibleSlotIndices[0]);
    }

    public void SelectSlot(int index)
    {
        // This function selects an occupied item and gives keyboard navigation to the item grid.
        if (!visibleSlotIndices.Contains(index) || !IsOccupied(index))
            return;

        isOnCategoryTabs = false;
        selectedIndex = index;

        UpdateSelection();
        UpdateCategoryTabVisuals();
    }

    private bool IsOccupied(int index)
    {
        // This function checks whether a given inventory slot contains an item.
        if (inventory == null || index < 0 || index >= inventory.Slots.Count)
            return false;

        InventorySlot slot = inventory.GetSlot(index);

        return slot != null && !slot.IsEmpty();
    }

    // This function updates slot highlights and selected item information,
    // showing a slot highlight only while keyboard focus is on the inventory grid.
    private void UpdateSelection()
    {
        if (slotUI == null)
            return;

        for (int i = 0; i < slotUI.Length; i++)
        {
            // This hides every slot highlight while keyboard focus is on category tabs.
            bool isSelected = !isOnCategoryTabs && i == selectedIndex;

            // This uses the existing InventorySlotUI prefab highlight functionality.
            slotUI[i].SetSelected(isSelected);
        }

        UpdateSelectedItemInformation();
    }

    private void UpdateSelectedItemInformation()
    {
        // This function displays the selected item's name and description.
        if (itemNameText == null || itemDescriptionText == null)
            return;

        if (selectedIndex < 0 || !IsOccupied(selectedIndex))
        {
            itemNameText.text = "";
            itemDescriptionText.text = "";
            return;
        }

        InventorySlot slot = inventory.GetSlot(selectedIndex);

        itemNameText.text = slot.item.itemName;
        itemDescriptionText.text = slot.item.description;
    }
}


