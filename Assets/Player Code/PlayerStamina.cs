using System;
using UnityEngine;

public class PlayerStamina : MonoBehaviour
{
    [Header("Costs")]
    [Tooltip("Stamina spent by a single dash.")]
    [SerializeField] private float dashCost = 15f;

    [Tooltip("Stamina drained per second while sprinting.")]
    [SerializeField] private float sprintDrainPerSecond = 5f;

    [Header("Regeneration")]
    [Tooltip("Stamina regenerated per second while not sprinting or dashing.")]
    [SerializeField] private float regenPerSecond = 10f;

    [Header("Exhaustion")]
    [Tooltip("Seconds the exhaustion penalty lasts once stamina hits 0.")]
    [SerializeField] private float exhaustionDuration = 5f;

    [Tooltip("Movement speed multiplier applied while exhausted.")]
    [SerializeField] private float exhaustedSpeedMultiplier = 0.5f;

    private float currentStamina;
    private float exhaustionTimer;
    private bool isSprinting;

    private DashScript dash;
    private PlayerStats playerStats;

    public float CurrentStamina => currentStamina;

    // Gets maximum stamina from PlayerStats.
    public float MaxStamina => playerStats.MaxStamina;

    public float StaminaPercent =>
        MaxStamina > 0f ? currentStamina / MaxStamina : 0f;

    public bool IsExhausted => exhaustionTimer > 0f;

    public bool CanDash => !IsExhausted;

    public bool CanSprint => !IsExhausted;

    public float SpeedMultiplier =>
        IsExhausted ? exhaustedSpeedMultiplier : 1f;

    public event Action<float, float> OnStaminaChanged;

    private void Awake()
    {
        // This function initializes stamina and connects the stamina system to PlayerStats.
        playerStats = GetComponent<PlayerStats>();

        currentStamina = MaxStamina;

        dash = GetComponent<DashScript>();

        if (playerStats != null)
        {
            playerStats.OnStatsChanged += OnStatsChanged;
        }
    }

    private void OnDestroy()
    {
        // This function removes the PlayerStats event subscription when the stamina component is destroyed.
        if (playerStats != null)
        {
            playerStats.OnStatsChanged -= OnStatsChanged;
        }
    }

    private void Update()
    {
        // This function handles exhaustion timing, stamina draining and stamina regeneration every frame.

        if (exhaustionTimer > 0f)
        {
            exhaustionTimer = Mathf.Max(
                0f,
                exhaustionTimer - Time.deltaTime
            );
        }

        bool dashing = dash != null && dash.IsDashing;

        if (isSprinting)
        {
            Drain(
                sprintDrainPerSecond *
                Time.deltaTime
            );
        }
        else if (!dashing)
        {
            Regenerate(
                regenPerSecond *
                Time.deltaTime
            );
        }
    }

    // Sets whether the player is currently sprinting.
    public void SetSprinting(bool sprinting)
    {
        isSprinting = sprinting && CanSprint;
    }

    // Attempts to consume stamina for a dash.
    // Returns false if the player is exhausted.
    public bool TryConsumeDash()
    {
        if (!CanDash)
            return false;

        Drain(dashCost);

        return true;
    }

    // Fully restores stamina and removes exhaustion.
    public void RestoreFullStamina()
    {
        exhaustionTimer = 0f;

        SetStamina(MaxStamina);
    }

    private void Drain(float amount)
    {
        // This function removes stamina and starts exhaustion when stamina reaches zero.
        if (amount <= 0f)
            return;

        SetStamina(currentStamina - amount);

        if (currentStamina <= 0f && !IsExhausted)
        {
            exhaustionTimer = exhaustionDuration;
        }
    }

    private void Regenerate(float amount)
    {
        // This function regenerates stamina up to the player's current maximum stamina.
        if (amount <= 0f)
            return;

        SetStamina(currentStamina + amount);
    }

    private void SetStamina(float value)
    {
        // This function sets stamina within the valid 0-to-maximum range and notifies the stamina bar.
        float clamped = Mathf.Clamp(
            value,
            0f,
            MaxStamina
        );

        if (Mathf.Approximately(
            clamped,
            currentStamina))
        {
            return;
        }

        currentStamina = clamped;

        OnStaminaChanged?.Invoke(
            currentStamina,
            MaxStamina
        );
    }

    private void OnStatsChanged()
    {
        // This function immediately fills stamina to the new maximum after the player upgrades Stamina.
        currentStamina = MaxStamina;

        OnStaminaChanged?.Invoke(
            currentStamina,
            MaxStamina
        );
    }
}