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

    private Coroutine primaryCoroutine;
    private Coroutine secondaryCoroutine;

    private void OnEnable()
    {
        if (enemyHealth == null)
            return;

        enemyHealth.OnPrimaryChanged += UpdatePrimaryBar;
        enemyHealth.OnSecondaryChanged += UpdateSecondaryBar;

        // Paint the current state immediately — both bars start empty, but this
        // keeps the bar correct if it's ever enabled mid-fight.
        UpdatePrimaryBar(enemyHealth.PrimaryValue, enemyHealth.PrimaryThreshold);
        UpdateSecondaryBar(enemyHealth.SecondaryValue, enemyHealth.SecondaryThreshold);
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
        float targetFill = max > 0f ? current / max : 0f;

        if (primaryCoroutine != null)
            StopCoroutine(primaryCoroutine);

        primaryCoroutine = StartCoroutine(MoveFill(primaryFill, targetFill));
    }

    private void UpdateSecondaryBar(float current, float max)
    {
        float targetFill = max > 0f ? current / max : 0f;

        if (secondaryCoroutine != null)
            StopCoroutine(secondaryCoroutine);

        secondaryCoroutine = StartCoroutine(MoveFill(secondaryFill, targetFill));
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
