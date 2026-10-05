using System;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("Stat Levels")]
    [SerializeField] private int healthLevel = 1;
    [SerializeField] private int staminaLevel = 1;
    [SerializeField] private int strengthLevel = 1;
    [SerializeField] private int dexterityLevel = 1;
    [SerializeField] private int soulDurabilityLevel = 1;

    [Header("Base Values")]
    [SerializeField] private int baseHealth = 100;
    [SerializeField] private float baseStamina = 100f;

    [Header("Stat Growth")]
    [SerializeField] private int healthPerLevel = 10;
    [SerializeField] private float staminaPerLevel = 5f;

    [Header("Upgrade Costs")]
    [Tooltip("The Essence cost to upgrade a stat from Level 1 to Level 2.")]
    [SerializeField] private int startingUpgradeCost = 100;

    [Tooltip("Multiplier applied to the previous upgrade cost for each new level.")]
    [SerializeField] private float upgradeCostMultiplier = 1.25f;


    // Called whenever the player's permanent stats have been changed.
    public event Action OnStatsChanged;


    // Returns the player's current Health level.
    public int HealthLevel => healthLevel;

    // Returns the player's current Stamina level.
    public int StaminaLevel => staminaLevel;

    // Returns the player's current Strength level.
    public int StrengthLevel => strengthLevel;

    // Returns the player's current Dexterity level.
    public int DexterityLevel => dexterityLevel;

    // Returns the player's current Soul Durability level.
    public int SoulDurabilityLevel => soulDurabilityLevel;


    // Calculates the player's maximum Health based on their Health level.
    public int MaxHealth =>
        baseHealth +
        ((healthLevel - 1) * healthPerLevel);


    // Calculates the player's maximum Stamina based on their Stamina level.
    public float MaxStamina =>
        baseStamina +
        ((staminaLevel - 1) * staminaPerLevel);


    public int GetUpgradeCost(int currentLevel)
    {
        // This function calculates the Essence cost required to upgrade from the supplied level to the next level.

        if (currentLevel < 1)
            currentLevel = 1;

        return Mathf.RoundToInt(
            startingUpgradeCost *
            Mathf.Pow(
                upgradeCostMultiplier,
                currentLevel - 1
            )
        );
    }


    public int GetTotalUpgradeCost(
        int originalLevel,
        int pendingLevel)
    {
        // This function calculates the total Essence cost of all upgrades between the original and pending levels.

        if (pendingLevel <= originalLevel)
            return 0;

        int totalCost = 0;

        for (
            int level = originalLevel;
            level < pendingLevel;
            level++
        )
        {
            totalCost += GetUpgradeCost(level);
        }

        return totalCost;
    }


    public void ApplyConfirmedLevels(
        int newHealthLevel,
        int newStaminaLevel,
        int newStrengthLevel,
        int newDexterityLevel,
        int newSoulDurabilityLevel)
    {
        // This function permanently applies all stat levels selected by the player after pressing Confirm.

        healthLevel = Mathf.Max(1, newHealthLevel);

        staminaLevel = Mathf.Max(1, newStaminaLevel);

        strengthLevel = Mathf.Max(1, newStrengthLevel);

        dexterityLevel = Mathf.Max(1, newDexterityLevel);

        soulDurabilityLevel =
            Mathf.Max(1, newSoulDurabilityLevel);


        // Tell PlayerHealth and PlayerStamina that the permanent stats have changed.
        OnStatsChanged?.Invoke();
    }
}