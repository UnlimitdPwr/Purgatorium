using System;
using UnityEngine;

public class PlayerStamina : MonoBehaviour
{
    [Header("Stamina")]
    [SerializeField] private float maxStamina = 100f;

    [Header("Costs")]
    [Tooltip("Stamina spent by a single dash.")]
    [SerializeField] private float dashCost = 15f;
    [Tooltip("Stamina drained per second while sprinting.")]
    [SerializeField] private float sprintDrainPerSecond = 5f;

    [Header("Regeneration")]
    [Tooltip("Stamina regenerated per second while not sprinting or dashing.")]
    [SerializeField] private float regenPerSecond = 10f;

    [Header("Exhaustion")]
    [Tooltip("Seconds the exhaustion penalty lasts once stamina hits 0. Stamina keeps " +
             "regenerating during it; when it ends, dash and sprint unlock again even " +
             "if the bar isn't full.")]
    [SerializeField] private float exhaustionDuration = 5f;

    [Tooltip("Movement speed multiplier applied while exhausted.")]
    [SerializeField] private float exhaustedSpeedMultiplier = 0.5f;

    private float currentStamina;
    private float exhaustionTimer;
    private bool isSprinting;
    private DashScript dash;

    public float CurrentStamina => currentStamina;
    public float MaxStamina => maxStamina;
    public float StaminaPercent => maxStamina > 0f ? currentStamina / maxStamina : 0f;

    // True for exhaustionDuration seconds after stamina hits 0 — dashing and
    // sprinting are both locked out and movement is slowed for the whole stretch.
    public bool IsExhausted => exhaustionTimer > 0f;

    public bool CanDash => !IsExhausted;
    public bool CanSprint => !IsExhausted;

    // 1 normally, exhaustedSpeedMultiplier while exhausted. MovementScript multiplies this in.
    public float SpeedMultiplier => IsExhausted ? exhaustedSpeedMultiplier : 1f;

    public event Action<float, float> OnStaminaChanged;

    private void Awake()
    {
        currentStamina = maxStamina;
        dash = GetComponent<DashScript>();
    }

    private void Update()
    {
        if (exhaustionTimer > 0f)
            exhaustionTimer = Mathf.Max(0f, exhaustionTimer - Time.deltaTime);

        bool dashing = dash != null && dash.IsDashing;

        if (isSprinting)
        {
            Drain(sprintDrainPerSecond * Time.deltaTime);
        }
        else if (!dashing)
        {
            Regenerate(regenPerSecond * Time.deltaTime);
        }
    }

    // =========================
    // SPRINT
    // =========================

    // Called every frame by DashScript with whatever it wants sprint to be right
    // now (grounded + held). Actual drain only happens while stamina allows it.
    public void SetSprinting(bool sprinting)
    {
        isSprinting = sprinting && CanSprint;
    }

    // =========================
    // DASH
    // =========================

    // Called by DashScript before it commits to a burst. Returns false (and spends
    // nothing) while exhausted; otherwise spends dashCost, even if that drains
    // stamina to/below 0 and triggers exhaustion.
    public bool TryConsumeDash()
    {
        if (!CanDash)
            return false;

        Drain(dashCost);
        return true;
    }

    // =========================
    // RESTORE
    // =========================

    // Called by Bonfire when the player rests: full bar and any exhaustion
    // penalty cleared.
    public void RestoreFullStamina()
    {
        exhaustionTimer = 0f;
        SetStamina(maxStamina);
    }

    // =========================
    // INTERNAL
    // =========================

    private void Drain(float amount)
    {
        if (amount <= 0f)
            return;

        SetStamina(currentStamina - amount);

        // Only starts the timer on the hit to 0 — draining further while already
        // exhausted doesn't extend it.
        if (currentStamina <= 0f && !IsExhausted)
            exhaustionTimer = exhaustionDuration;
    }

    private void Regenerate(float amount)
    {
        if (amount <= 0f)
            return;

        SetStamina(currentStamina + amount);
    }

    private void SetStamina(float value)
    {
        float clamped = Mathf.Clamp(value, 0f, maxStamina);

        if (Mathf.Approximately(clamped, currentStamina))
            return;

        currentStamina = clamped;
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
    }
}
