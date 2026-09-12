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
    [Tooltip("Movement speed multiplier applied while stamina is at or below 0.")]
    [SerializeField] private float exhaustedSpeedMultiplier = 0.5f;

    private float currentStamina;
    private bool isExhausted;
    private bool isSprinting;
    private DashScript dash;

    public float CurrentStamina => currentStamina;
    public float MaxStamina => maxStamina;
    public float StaminaPercent => maxStamina > 0f ? currentStamina / maxStamina : 0f;

    // True from the moment stamina hits 0 until it fully regenerates back to max —
    // dashing and sprinting are both locked out for the whole stretch.
    public bool IsExhausted => isExhausted;

    public bool CanDash => !isExhausted;
    public bool CanSprint => !isExhausted;

    // 1 normally, exhaustedSpeedMultiplier while exhausted. MovementScript multiplies this in.
    public float SpeedMultiplier => isExhausted ? exhaustedSpeedMultiplier : 1f;

    public event Action<float, float> OnStaminaChanged;

    private void Awake()
    {
        currentStamina = maxStamina;
        dash = GetComponent<DashScript>();
    }

    private void Update()
    {
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
    // INTERNAL
    // =========================

    private void Drain(float amount)
    {
        if (amount <= 0f)
            return;

        SetStamina(currentStamina - amount);

        if (currentStamina <= 0f)
            isExhausted = true;
    }

    private void Regenerate(float amount)
    {
        if (amount <= 0f)
            return;

        SetStamina(currentStamina + amount);

        if (isExhausted && currentStamina >= maxStamina)
            isExhausted = false;
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
