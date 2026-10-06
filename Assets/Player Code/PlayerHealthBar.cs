using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerHealthBar : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Image healthFill;
    [SerializeField] private Image damageFill;

    [Header("Bar Size")]
    [Tooltip("The width of the health bar at Health Level 1.")]
    [SerializeField] private float baseBarWidth = 200f;

    [Tooltip("How many pixels the health bar grows for each additional maximum health point.")]
    [SerializeField] private float widthPerHealth = 2f;

    [Header("Health Movement")]
    [SerializeField] private float healthDuration = 0.4f;

    [Header("Damage Effect")]
    [SerializeField] private float damageDelay = 0.2f;
    [SerializeField] private float damageDuration = 0.7f;

    private Coroutine healthCoroutine;
    private Coroutine damageCoroutine;

    private PlayerStats playerStats;
    private RectTransform barTransform;

    private void Start()
    {
        // This function gets the player's stats and the RectTransform of the entire health bar.
        playerStats = playerHealth.GetComponent<PlayerStats>();
        barTransform = GetComponent<RectTransform>();

        playerHealth.OnHealthChanged += UpdateHealthBar;
        playerStats.OnStatsChanged += UpdateBarSize;

        // This function initializes the health bar to the player's current health percentage.
        float startingHealth =
            playerHealth.MaxHealth > 0
                ? (float)playerHealth.CurrentHealth / playerHealth.MaxHealth
                : 0f;

        healthFill.fillAmount = startingHealth;
        damageFill.fillAmount = startingHealth;

        // This function sets the initial physical width of the health bar.
        UpdateBarSize();
    }

    private void OnDestroy()
    {
        // This function removes the health event subscription when the health bar is destroyed.
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= UpdateHealthBar;
        }

        // This function removes the stats event subscription when the health bar is destroyed.
        if (playerStats != null)
        {
            playerStats.OnStatsChanged -= UpdateBarSize;
        }
    }

    private void UpdateBarSize()
    {
        // This function changes the health bar width while keeping its left edge fixed.
        if (barTransform == null || playerStats == null)
            return;

        float healthIncrease =
            playerStats.MaxHealth -
            playerStats.MaxHealthAtLevelOne;

        float newWidth =
            baseBarWidth +
            (healthIncrease * widthPerHealth);

        Vector2 size = barTransform.sizeDelta;
        size.x = newWidth;

        barTransform.sizeDelta = size;

    }

    private void UpdateHealthBar(int currentHealth, int maxHealth)
    {
        // This function updates the filled portion of the health bar based on current health.
        float healthPercent =
            maxHealth > 0
                ? (float)currentHealth / maxHealth
                : 0f;

        if (healthCoroutine != null)
        {
            StopCoroutine(healthCoroutine);
        }

        if (damageCoroutine != null)
        {
            StopCoroutine(damageCoroutine);
        }

        healthCoroutine = StartCoroutine(
            MoveHealthFill(healthPercent)
        );

        damageCoroutine = StartCoroutine(
            MoveDamageFill(healthPercent)
        );
    }

    private IEnumerator MoveHealthFill(float targetFill)
    {
        // This function smoothly moves the main health fill toward the new health percentage.
        float startFill = healthFill.fillAmount;
        float elapsed = 0f;

        while (elapsed < healthDuration)
        {
            elapsed += Time.deltaTime;

            float progress =
                elapsed / healthDuration;

            healthFill.fillAmount = Mathf.Lerp(
                startFill,
                targetFill,
                progress
            );

            yield return null;
        }

        healthFill.fillAmount = targetFill;
    }

    private IEnumerator MoveDamageFill(float targetFill)
    {
        // This function creates the delayed damage-fill effect behind the main health bar.
        yield return new WaitForSeconds(damageDelay);

        float startFill = damageFill.fillAmount;
        float elapsed = 0f;

        while (elapsed < damageDuration)
        {
            elapsed += Time.deltaTime;

            float progress =
                elapsed / damageDuration;

            damageFill.fillAmount = Mathf.Lerp(
                startFill,
                targetFill,
                progress
            );

            yield return null;
        }

        damageFill.fillAmount = targetFill;
    }
}