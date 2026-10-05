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
        playerStats = GetComponent<PlayerStats>();

        currentStamina = MaxStamina;

        dash = GetComponent<DashScript>();
    }

    private void Update()
    {
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
        if (amount <= 0f)
            return;

        SetStamina(currentStamina + amount);
    }

    private void SetStamina(float value)
    {
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
}
