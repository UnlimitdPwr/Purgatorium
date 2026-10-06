using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerStaminaBar : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerStamina playerStamina;
    [SerializeField] private Image staminaFill;

    [Header("Bar Size")]
    [Tooltip("The width of the stamina bar at Stamina Level 1.")]
    [SerializeField] private float baseBarWidth = 200f;

    [Tooltip("How many pixels the stamina bar grows for each additional maximum stamina point.")]
    [SerializeField] private float widthPerStamina = 2f;

    [Header("Movement")]
    [SerializeField] private float fillDuration = 0.15f;

    private Coroutine fillCoroutine;

    private PlayerStats playerStats;
    private RectTransform barTransform;

    private void Start()
    {
        // This function gets the player's stats and the RectTransform of the entire stamina bar.
        playerStats = playerStamina.GetComponent<PlayerStats>();
        barTransform = GetComponent<RectTransform>();

        playerStamina.OnStaminaChanged += UpdateStaminaBar;
        playerStats.OnStatsChanged += UpdateBarSize;

        // This function initializes the stamina fill to the player's current stamina percentage.
        float startingFill = playerStamina.StaminaPercent;

        staminaFill.fillAmount = startingFill;

        // This function sets the initial physical width of the stamina bar.
        UpdateBarSize();
    }

    private void OnDestroy()
    {
        // This function removes the stamina event subscription when the stamina bar is destroyed.
        if (playerStamina != null)
        {
            playerStamina.OnStaminaChanged -= UpdateStaminaBar;
        }

        // This function removes the stats event subscription when the stamina bar is destroyed.
        if (playerStats != null)
        {
            playerStats.OnStatsChanged -= UpdateBarSize;
        }
    }

    private void UpdateBarSize()
    {
        // This function changes the stamina bar width while keeping its left edge fixed.
        if (barTransform == null || playerStats == null)
            return;

        float staminaIncrease =
            playerStats.MaxStamina -
            playerStats.MaxStaminaAtLevelOne;

        float newWidth =
            baseBarWidth +
            (staminaIncrease * widthPerStamina);

        Vector2 size = barTransform.sizeDelta;
        size.x = newWidth;

        barTransform.sizeDelta = size;

    }

    private void UpdateStaminaBar(
        float currentStamina,
        float maxStamina)
    {
        // This function updates the filled portion of the stamina bar based on current stamina.
        float targetFill =
            maxStamina > 0f
                ? currentStamina / maxStamina
                : 0f;

        if (fillCoroutine != null)
        {
            StopCoroutine(fillCoroutine);
        }

        fillCoroutine =
            StartCoroutine(
                MoveStaminaFill(targetFill)
            );
    }

    private IEnumerator MoveStaminaFill(float targetFill)
    {
        // This function smoothly moves the stamina fill toward the new stamina percentage.
        float startFill =
            staminaFill.fillAmount;

        float elapsed = 0f;

        while (elapsed < fillDuration)
        {
            elapsed += Time.deltaTime;

            float progress =
                elapsed / fillDuration;

            staminaFill.fillAmount =
                Mathf.Lerp(
                    startFill,
                    targetFill,
                    progress
                );

            yield return null;
        }

        staminaFill.fillAmount = targetFill;
    }
}