using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    private int currentHealth;

    private PlayerStats playerStats;
    private BlockScript block;

    public int CurrentHealth => currentHealth;

    // Gets the player's maximum health from PlayerStats.
    public int MaxHealth => playerStats.MaxHealth;

    public event Action<int, int> OnHealthChanged;

    private void Awake()
    {
        playerStats = GetComponent<PlayerStats>();
        block = GetComponent<BlockScript>();

        currentHealth = MaxHealth;

        Debug.Log(
            "Player Health initialized: " +
            currentHealth + "/" + MaxHealth
        );
    }

    public void TakeDamage(int damage)
    {
        if (damage <= 0)
            return;

        Debug.Log(
            "TAKE DAMAGE CALLED | Incoming damage: " +
            damage
        );

        int finalDamage = damage;

        if (block != null && block.IsBlocking)
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

    public void Heal(int amount)
    {
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
