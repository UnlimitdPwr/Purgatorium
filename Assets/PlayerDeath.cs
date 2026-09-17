using UnityEngine;

public class PlayerDeath : MonoBehaviour
{
    private PlayerHealth health;
    private PlayerAnimation playerAnimation;
    private PlayerController1 controller;

    private bool isDead;

    public bool IsDead => isDead;

    private void Awake()
    {
        health = GetComponent<PlayerHealth>();
        playerAnimation = GetComponent<PlayerAnimation>();
        controller = GetComponent<PlayerController1>();
    }

    private void OnEnable()
    {
        if (health != null)
            health.OnHealthChanged += CheckForDeath;
    }

    private void OnDisable()
    {
        if (health != null)
            health.OnHealthChanged -= CheckForDeath;
    }

    private void CheckForDeath(int currentHealth, int maxHealth)
    {
        if (isDead)
            return;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;

        Debug.Log("Player died");

        if (controller != null)
            controller.enabled = false;

        if (playerAnimation != null)
            playerAnimation.PlayDeathAnimation();

        DeathScreen deathScreen = FindFirstObjectByType<DeathScreen>(
            FindObjectsInactive.Include
        );

        if (deathScreen != null)
        {
            deathScreen.Show();
        }
    }

    public void Respawn()
    {
        Transform respawnPoint =
        CheckpointManager.Instance.GetRespawnPoint();

        if (respawnPoint == null)
        {
            Debug.LogWarning("No checkpoint available.");
            return;
        }

        transform.position = respawnPoint.position;

        PlayerHealth health = GetComponent<PlayerHealth>();

        if (health != null)
        {
            health.RestoreFullHealth();
        }

        

        if (controller != null)
            controller.enabled = true;

        isDead = false;

        Debug.Log("Player respawned.");

        if (playerAnimation != null)
        {
            playerAnimation.ResetFromDeath();
        }
    }


}
