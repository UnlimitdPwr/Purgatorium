using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerHealthBar : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Image healthFill;
    [SerializeField] private Image damageFill;

    [Header("Health Movement")]
    [SerializeField] private float healthDuration = 0.4f;

    [Header("Damage Effect")]
    [SerializeField] private float damageDelay = 0.2f;
    [SerializeField] private float damageDuration = 0.7f;

    private Coroutine healthCoroutine;
    private Coroutine damageCoroutine;

    private void Start()
    {
        playerHealth.OnHealthChanged += UpdateHealthBar;

        float startingHealth =
            (float)playerHealth.CurrentHealth / playerHealth.MaxHealth;

        healthFill.fillAmount = startingHealth;
        damageFill.fillAmount = startingHealth;
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= UpdateHealthBar;
        }
    }

    private void UpdateHealthBar(int currentHealth, int maxHealth)
    {
        float healthPercent = (float)currentHealth / maxHealth;

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
        float startFill = healthFill.fillAmount;
        float elapsed = 0f;

        while (elapsed < healthDuration)
        {
            elapsed += Time.deltaTime;

            float progress = elapsed / healthDuration;

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
        yield return new WaitForSeconds(damageDelay);

        float startFill = damageFill.fillAmount;
        float elapsed = 0f;

        while (elapsed < damageDuration)
        {
            elapsed += Time.deltaTime;

            float progress = elapsed / damageDuration;

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

