using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 100;

    private int currentHealth;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    public event Action<int, int> OnHealthChanged;

    private void Awake()
    {
        currentHealth = maxHealth;

        Debug.Log("Player Health initialized: " +
                  currentHealth + "/" + maxHealth);
    }

    public void TakeDamage(int damage)
    {
        if (damage <= 0)
            return;

        currentHealth -= damage;

        if (currentHealth < 0)
            currentHealth = 0;

        Debug.Log("PLAYER HEALTH: " + currentHealth + "/" + maxHealth);

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        if (amount <= 0)
            return;

        currentHealth += amount;

        if (currentHealth > maxHealth)
            currentHealth = maxHealth;
    }

    private void Die()
    {
        Debug.Log("Player died");
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
