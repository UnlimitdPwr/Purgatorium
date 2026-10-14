using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    private int currentHealth;

    private PlayerStats playerStats;
    private BlockScript block;
    private PlayerStatusEffects status;

    public int CurrentHealth => currentHealth;

    // Used only when the Player has no PlayerStats component.
    private const int FallbackMaxHealth = 100;

    // Gets the player's maximum health from PlayerStats.
    public int MaxHealth =>
        playerStats != null ? playerStats.MaxHealth : FallbackMaxHealth;

    public event Action<int, int> OnHealthChanged;

    private void Awake()
    {
        playerStats = GetComponent<PlayerStats>();
        block = GetComponent<BlockScript>();
        status = GetComponent<PlayerStatusEffects>();

        if (playerStats == null)
        {
            Debug.LogWarning(
                "PlayerHealth: no PlayerStats on '" + name +
                "' — using a fallback max health of " + FallbackMaxHealth +
                ". Add PlayerStats (Tools > Level Setup).",
                this
            );
        }

        currentHealth = MaxHealth;

        Debug.Log(
            "Player Health initialized: " +
            currentHealth + "/" + MaxHealth
        );
    }

    public void TakeDamage(int damage)
    {
        TakeDamage(damage, true);
    }

    // canBeBlocked = false is for damage a shield can't stop (e.g. acid) —
    // it skips the block reduction and doesn't count as a blocked hit.
    public void TakeDamage(int damage, bool canBeBlocked)
    {
        if (damage <= 0)
            return;

        Debug.Log(
            "TAKE DAMAGE CALLED | Incoming damage: " +
            damage
        );

        int finalDamage = damage;

        if (canBeBlocked && block != null && block.IsBlocking)
        {
            finalDamage = block.GetBlockedDamage(damage);

            Debug.Log(
                "BLOCKED HIT | Incoming: " + damage +
                " | Final: " + finalDamage
            );

            block.TryBlockHit();
        }

        Debug.Log(
            "HEALTH BEFORE DAMAGE: " +
            currentHealth + "/" + MaxHealth
        );

        currentHealth -= finalDamage;

        if (currentHealth < 0)
            currentHealth = 0;

        Debug.Log(
            "HEALTH AFTER DAMAGE: " +
            currentHealth + "/" + MaxHealth
        );

        OnHealthChanged?.Invoke(currentHealth, MaxHealth);
    }

    // Drops health straight to 0 (e.g. a full curse bar). PlayerDeath
    // picks it up from OnHealthChanged like any other death.
    public void Kill()
    {
        if (currentHealth <= 0)
            return;

        currentHealth = 0;

        Debug.Log("PLAYER KILLED OUTRIGHT");

        OnHealthChanged?.Invoke(currentHealth, MaxHealth);
    }

    public void Heal(int amount)
    {
        // A status condition can reduce healing.
        if (status != null)
            amount = Mathf.RoundToInt(amount * status.HealingMultiplier);

        if (amount <= 0)
            return;

        currentHealth += amount;

        if (currentHealth > MaxHealth)
            currentHealth = MaxHealth;

        OnHealthChanged?.Invoke(currentHealth, MaxHealth);
    }

    // Restores the player's health to their current maximum health.
    public void RestoreFullHealth()
    {
        currentHealth = MaxHealth;

        Debug.Log(
            "PLAYER HEALTH RESTORED: " +
            currentHealth + "/" + MaxHealth
        );

        OnHealthChanged?.Invoke(currentHealth, MaxHealth);
    }
}
