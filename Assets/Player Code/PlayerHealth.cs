using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 100;

    private int currentHealth;
    private BlockScript block;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    public event Action<int, int> OnHealthChanged;

    private void Awake()
    {
        currentHealth = maxHealth;

        block = GetComponent<BlockScript>();

        Debug.Log("Player Health initialized: " +
                  currentHealth + "/" + maxHealth);
    }

    public void TakeDamage(int damage)
    {
        if (damage <= 0)
            return;

        Debug.Log("TAKE DAMAGE CALLED | Incoming damage: " + damage);

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
            currentHealth + "/" + maxHealth
        );

        currentHealth -= finalDamage;

        if (currentHealth < 0)
            currentHealth = 0;

        Debug.Log(
            "HEALTH AFTER DAMAGE: " +
            currentHealth + "/" + maxHealth
        );

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void Heal(int amount)
    {
        Debug.Log("!!! HEAL CALLED: " + amount);

        if (amount <= 0)
            return;

        currentHealth += amount;

        if (currentHealth > maxHealth)
            currentHealth = maxHealth;

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void RestoreFullHealth()
    {
        Debug.Log("!!! RESTORE FULL HEALTH CALLED");

        currentHealth = maxHealth;

        Debug.Log(
            "PLAYER HEALTH RESTORED: " +
            currentHealth + "/" + maxHealth
        );

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

}
