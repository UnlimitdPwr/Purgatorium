using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BonfireStatsUI : MonoBehaviour
{
    [Header("Player References")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private PlayerEssence playerEssence;

    [Header("Essence")]
    [SerializeField] private TMP_Text spentEssenceText;
    [SerializeField] private TMP_Text remainingEssenceText;

    [Header("Health")]
    [SerializeField] private TMP_Text healthLevelText;
    [SerializeField] private TMP_Text healthCostText;
    [SerializeField] private Button healthMinusButton;
    [SerializeField] private Button healthPlusButton;

    [Header("Stamina")]
    [SerializeField] private TMP_Text staminaLevelText;
    [SerializeField] private TMP_Text staminaCostText;
    [SerializeField] private Button staminaMinusButton;
    [SerializeField] private Button staminaPlusButton;

    [Header("Strength")]
    [SerializeField] private TMP_Text strengthLevelText;
    [SerializeField] private TMP_Text strengthCostText;
    [SerializeField] private Button strengthMinusButton;
    [SerializeField] private Button strengthPlusButton;

    [Header("Dexterity")]
    [SerializeField] private TMP_Text dexterityLevelText;
    [SerializeField] private TMP_Text dexterityCostText;
    [SerializeField] private Button dexterityMinusButton;
    [SerializeField] private Button dexterityPlusButton;

    [Header("Soul Durability")]
    [SerializeField] private TMP_Text soulDurabilityLevelText;
    [SerializeField] private TMP_Text soulDurabilityCostText;
    [SerializeField] private Button soulDurabilityMinusButton;
    [SerializeField] private Button soulDurabilityPlusButton;

    [Header("Confirmation")]
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button backButton;

    private int originalHealthLevel;
    private int originalStaminaLevel;
    private int originalStrengthLevel;
    private int originalDexterityLevel;
    private int originalSoulDurabilityLevel;

    private int pendingHealthLevel;
    private int pendingStaminaLevel;
    private int pendingStrengthLevel;
    private int pendingDexterityLevel;
    private int pendingSoulDurabilityLevel;

    private int startingEssence;

    private void Start()
    {
        // This function connects the Stats UI to Essence changes and initializes the temporary stat selection.
        if (playerEssence != null)
        {
            playerEssence.OnEssenceChanged += OnEssenceChanged;
        }

        BeginStatSelection();
    }

    private void OnDestroy()
    {
        // This function removes the Essence event subscription when the Stats UI is destroyed.
        if (playerEssence != null)
        {
            playerEssence.OnEssenceChanged -= OnEssenceChanged;
        }
    }

    public void BeginStatSelection()
    {
        // This function creates a snapshot of the player's real stats so upgrades can be tested without being permanently applied.

        originalHealthLevel = playerStats.HealthLevel;
        originalStaminaLevel = playerStats.StaminaLevel;
        originalStrengthLevel = playerStats.StrengthLevel;
        originalDexterityLevel = playerStats.DexterityLevel;
        originalSoulDurabilityLevel = playerStats.SoulDurabilityLevel;

        pendingHealthLevel = originalHealthLevel;
        pendingStaminaLevel = originalStaminaLevel;
        pendingStrengthLevel = originalStrengthLevel;
        pendingDexterityLevel = originalDexterityLevel;
        pendingSoulDurabilityLevel = originalSoulDurabilityLevel;

        startingEssence = playerEssence.CurrentEssence;

        RefreshUI();
    }

    private int GetTotalPendingCost()
    {
        // This function calculates the total Essence required for all currently selected upgrades.

        int totalCost = 0;

        totalCost += playerStats.GetTotalUpgradeCost(
            originalHealthLevel,
            pendingHealthLevel
        );

        totalCost += playerStats.GetTotalUpgradeCost(
            originalStaminaLevel,
            pendingStaminaLevel
        );

        totalCost += playerStats.GetTotalUpgradeCost(
            originalStrengthLevel,
            pendingStrengthLevel
        );

        totalCost += playerStats.GetTotalUpgradeCost(
            originalDexterityLevel,
            pendingDexterityLevel
        );

        totalCost += playerStats.GetTotalUpgradeCost(
            originalSoulDurabilityLevel,
            pendingSoulDurabilityLevel
        );

        return totalCost;
    }

    private int GetRemainingEssence()
    {
        // This function calculates how much Essence remains available for additional temporary upgrades.
        return startingEssence - GetTotalPendingCost();
    }

    public void IncreaseHealth()
    {
        // This function temporarily increases the Health level without permanently spending Essence.
        int cost = playerStats.GetUpgradeCost(pendingHealthLevel);

        if (GetRemainingEssence() < cost)
            return;

        pendingHealthLevel++;

        RefreshUI();
    }

    public void DecreaseHealth()
    {
        // This function removes one temporary Health upgrade but never goes below the original level.
        if (pendingHealthLevel <= originalHealthLevel)
            return;

        pendingHealthLevel--;

        RefreshUI();
    }

    public void IncreaseStamina()
    {
        // This function temporarily increases the Stamina level without permanently spending Essence.
        int cost = playerStats.GetUpgradeCost(pendingStaminaLevel);

        if (GetRemainingEssence() < cost)
            return;

        pendingStaminaLevel++;

        RefreshUI();
    }

    public void DecreaseStamina()
    {
        // This function removes one temporary Stamina upgrade but never goes below the original level.
        if (pendingStaminaLevel <= originalStaminaLevel)
            return;

        pendingStaminaLevel--;

        RefreshUI();
    }

    public void IncreaseStrength()
    {
        // This function temporarily increases the Strength level without permanently spending Essence.
        int cost = playerStats.GetUpgradeCost(pendingStrengthLevel);

        if (GetRemainingEssence() < cost)
            return;

        pendingStrengthLevel++;

        RefreshUI();
    }

    public void DecreaseStrength()
    {
        // This function removes one temporary Strength upgrade but never goes below the original level.
        if (pendingStrengthLevel <= originalStrengthLevel)
            return;

        pendingStrengthLevel--;

        RefreshUI();
    }

    public void IncreaseDexterity()
    {
        // This function temporarily increases the Dexterity level without permanently spending Essence.
        int cost = playerStats.GetUpgradeCost(pendingDexterityLevel);

        if (GetRemainingEssence() < cost)
            return;

        pendingDexterityLevel++;

        RefreshUI();
    }

    public void DecreaseDexterity()
    {
        // This function removes one temporary Dexterity upgrade but never goes below the original level.
        if (pendingDexterityLevel <= originalDexterityLevel)
            return;

        pendingDexterityLevel--;

        RefreshUI();
    }

    public void IncreaseSoulDurability()
    {
        // This function temporarily increases the Soul Durability level without permanently spending Essence.
        int cost = playerStats.GetUpgradeCost(pendingSoulDurabilityLevel);

        if (GetRemainingEssence() < cost)
            return;

        pendingSoulDurabilityLevel++;

        RefreshUI();
    }

    public void DecreaseSoulDurability()
    {
        // This function removes one temporary Soul Durability upgrade but never goes below the original level.
        if (pendingSoulDurabilityLevel <= originalSoulDurabilityLevel)
            return;

        pendingSoulDurabilityLevel--;

        RefreshUI();
    }

    public void Confirm()
    {
        // This function permanently applies all pending stat levels and spends the required Essence.

        int totalCost = GetTotalPendingCost();

        if (totalCost <= 0)
            return;

        if (playerEssence.CurrentEssence < totalCost)
            return;

        bool spent = playerEssence.TrySpendEssence(totalCost);

        if (!spent)
            return;

        playerStats.ApplyConfirmedLevels(
            pendingHealthLevel,
            pendingStaminaLevel,
            pendingStrengthLevel,
            pendingDexterityLevel,
            pendingSoulDurabilityLevel
        );

        BeginStatSelection();
    }

    public void Cancel()
    {
        // This function discards every temporary upgrade and restores the screen to the player's real stats.
        BeginStatSelection();
    }

    public void RefreshUI()
    {
        // This function updates all stat levels, costs, Essence values and button states on the Stats screen.

        int totalCost = GetTotalPendingCost();
        int remainingEssence = startingEssence - totalCost;

        spentEssenceText.text = "Spent: " + totalCost;
        remainingEssenceText.text = "Remaining: " + remainingEssence;

        healthLevelText.text = "Lv. " + pendingHealthLevel;
        healthCostText.text = GetNextCostText(pendingHealthLevel);

        healthMinusButton.interactable =
            pendingHealthLevel > originalHealthLevel;

        healthPlusButton.interactable =
            CanIncreaseStat(pendingHealthLevel);


        staminaLevelText.text = "Lv. " + pendingStaminaLevel;
        staminaCostText.text = GetNextCostText(pendingStaminaLevel);

        staminaMinusButton.interactable =
            pendingStaminaLevel > originalStaminaLevel;

        staminaPlusButton.interactable =
            CanIncreaseStat(pendingStaminaLevel);


        strengthLevelText.text = "Lv. " + pendingStrengthLevel;
        strengthCostText.text = GetNextCostText(pendingStrengthLevel);

        strengthMinusButton.interactable =
            pendingStrengthLevel > originalStrengthLevel;

        strengthPlusButton.interactable =
            CanIncreaseStat(pendingStrengthLevel);


        dexterityLevelText.text = "Lv. " + pendingDexterityLevel;
        dexterityCostText.text = GetNextCostText(pendingDexterityLevel);

        dexterityMinusButton.interactable =
            pendingDexterityLevel > originalDexterityLevel;

        dexterityPlusButton.interactable =
            CanIncreaseStat(pendingDexterityLevel);


        soulDurabilityLevelText.text =
            "Lv. " + pendingSoulDurabilityLevel;

        soulDurabilityCostText.text =
            GetNextCostText(pendingSoulDurabilityLevel);

        soulDurabilityMinusButton.interactable =
            pendingSoulDurabilityLevel > originalSoulDurabilityLevel;

        soulDurabilityPlusButton.interactable =
            CanIncreaseStat(pendingSoulDurabilityLevel);


        confirmButton.interactable = totalCost > 0;
    }

    private bool CanIncreaseStat(int currentPendingLevel)
    {
        // This function checks whether the player has enough remaining Essence for another level.
        int nextCost = playerStats.GetUpgradeCost(currentPendingLevel);

        return GetRemainingEssence() >= nextCost;
    }

    private string GetNextCostText(int currentLevel)
    {
        // This function creates the text displaying the cost of the next stat level.
        int nextCost = playerStats.GetUpgradeCost(currentLevel);

        return "Next Level: " + nextCost + " Essence";
    }

    private void OnEssenceChanged(int newAmount)
    {
        // This function refreshes the Stats UI when the player's Essence changes.
        RefreshUI();
    }

    public void BackToBonfire()
    {
        // This function discards unconfirmed upgrades and returns to the main Bonfire menu.
        Cancel();

        BonfireUI bonfireUI = FindFirstObjectByType<BonfireUI>();

        if (bonfireUI != null)
        {
            bonfireUI.CloseStats();
        }
    }
}