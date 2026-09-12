using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerStaminaBar : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerStamina playerStamina;
    [SerializeField] private Image staminaFill;

    [Header("Movement")]
    [SerializeField] private float fillDuration = 0.15f;

    private Coroutine fillCoroutine;

    private void Start()
    {
        playerStamina.OnStaminaChanged += UpdateStaminaBar;

        float startingFill = playerStamina.StaminaPercent;
        staminaFill.fillAmount = startingFill;
    }

    private void OnDestroy()
    {
        if (playerStamina != null)
        {
            playerStamina.OnStaminaChanged -= UpdateStaminaBar;
        }
    }

    private void UpdateStaminaBar(float currentStamina, float maxStamina)
    {
        float targetFill = maxStamina > 0f ? currentStamina / maxStamina : 0f;

        if (fillCoroutine != null)
        {
            StopCoroutine(fillCoroutine);
        }

        fillCoroutine = StartCoroutine(MoveStaminaFill(targetFill));
    }

    private IEnumerator MoveStaminaFill(float targetFill)
    {
        float startFill = staminaFill.fillAmount;
        float elapsed = 0f;

        while (elapsed < fillDuration)
        {
            elapsed += Time.deltaTime;

            float progress = elapsed / fillDuration;

            staminaFill.fillAmount = Mathf.Lerp(
                startFill,
                targetFill,
                progress
            );

            yield return null;
        }

        staminaFill.fillAmount = targetFill;
    }
}
