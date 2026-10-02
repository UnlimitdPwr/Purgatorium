using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyHealth enemyHealth;
    [SerializeField] private Image primaryFill;
    [SerializeField] private Image secondaryFill;

    [Header("Movement")]
    [SerializeField] private float fillDuration = 0.15f;

    [Header("Display")]
    [Tooltip("Show the primary bar starting full and draining toward empty, instead " +
             "of starting empty and filling up. Only changes what's drawn — " +
             "EnemyHealth still counts the primary value up to its threshold.")]
    [SerializeField] private bool primaryDrains = true;

    private Coroutine primaryCoroutine;
    private Coroutine secondaryCoroutine;

    private void OnEnable()
    {
        if (enemyHealth == null)
            return;

        enemyHealth.OnPrimaryChanged += UpdatePrimaryBar;
        enemyHealth.OnSecondaryChanged += UpdateSecondaryBar;

        // Snap to the current state immediately (no animation) — the primary bar
        // starts full, and this keeps both right if enabled mid-fight.
        primaryFill.fillAmount = PrimaryFillFor(enemyHealth.PrimaryValue, enemyHealth.PrimaryThreshold);
        secondaryFill.fillAmount = FillFor(enemyHealth.SecondaryValue, enemyHealth.SecondaryThreshold);
    }

    private void OnDisable()
    {
        if (enemyHealth == null)
            return;

        enemyHealth.OnPrimaryChanged -= UpdatePrimaryBar;
        enemyHealth.OnSecondaryChanged -= UpdateSecondaryBar;
    }

    private void UpdatePrimaryBar(float current, float max)
    {
        float targetFill = PrimaryFillFor(current, max);

        if (primaryCoroutine != null)
            StopCoroutine(primaryCoroutine);

        primaryCoroutine = StartCoroutine(MoveFill(primaryFill, targetFill));
    }

    private void UpdateSecondaryBar(float current, float max)
    {
        float targetFill = FillFor(current, max);

        if (secondaryCoroutine != null)
            StopCoroutine(secondaryCoroutine);

        secondaryCoroutine = StartCoroutine(MoveFill(secondaryFill, targetFill));
    }

    private float PrimaryFillFor(float current, float max)
    {
        float fill = FillFor(current, max);
        return primaryDrains ? 1f - fill : fill;
    }

    private static float FillFor(float current, float max)
    {
        return max > 0f ? current / max : 0f;
    }

    private IEnumerator MoveFill(Image fill, float targetFill)
    {
        float startFill = fill.fillAmount;
        float elapsed = 0f;

        while (elapsed < fillDuration)
        {
            elapsed += Time.deltaTime;

            float progress = elapsed / fillDuration;

            fill.fillAmount = Mathf.Lerp(startFill, targetFill, progress);

            yield return null;
        }

        fill.fillAmount = targetFill;
    }
}
