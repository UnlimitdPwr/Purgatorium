using System;
using UnityEngine;

public class PlayerEssence : MonoBehaviour
{
    [Header("Essence")]
    [SerializeField] private int currentEssence = 0;

    // Returns the player's current amount of Essence.
    public int CurrentEssence => currentEssence;

    // Called whenever the player's Essence changes.
    public event Action<int> OnEssenceChanged;


    // =========================
    // ADD ESSENCE
    // =========================

    // Adds Essence to the player's current amount.
    public void AddEssence(int amount)
    {
        if (amount <= 0)
            return;

        currentEssence += amount;

        OnEssenceChanged?.Invoke(currentEssence);
    }


    // =========================
    // SPEND ESSENCE
    // =========================

    // Attempts to spend Essence.
    // Returns false if the player does not have enough.
    public bool TrySpendEssence(int amount)
    {
        if (amount <= 0)
            return false;

        if (currentEssence < amount)
            return false;

        currentEssence -= amount;

        OnEssenceChanged?.Invoke(currentEssence);

        return true;
    }
}
