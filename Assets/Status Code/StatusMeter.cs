using UnityEngine;

// The buildup meter for one status on one character. Plain class, not a
// component — PlayerStatusEffects and EnemyStatusEffects each hold one per
// status and call Tick() every frame. All the buildup rules live here so the
// player and enemies can't drift apart; what a condition DOES lives in them.
public class StatusMeter
{
    public StatusEffectDefinition Definition { get; }

    public float Buildup { get; private set; }
    public bool IsConditionActive { get; private set; }

    // 0..1 of the threshold — what a bar should show.
    public float Fill => Buildup / Definition.threshold;

    private float timeSinceExposure;

    public StatusMeter(StatusEffectDefinition definition)
    {
        Definition = definition;
    }

    // =========================
    // EXPOSURE
    // =========================

    // Returns true if this exposure pushed the meter over the threshold
    // (the condition just started). Ignored while a condition is active.
    public bool AddBuildup(float amount)
    {
        if (IsConditionActive || amount <= 0f)
            return false;

        Buildup += amount;
        timeSinceExposure = 0f;

        if (Buildup < Definition.threshold)
            return false;

        Buildup = Definition.threshold;
        IsConditionActive = true;
        return true;
    }

    // =========================
    // PER FRAME
    // =========================

    // Advances the meter: recedes buildup, or drains an active condition.
    // Returns true on the frame the condition wears off.
    public bool Tick(float deltaTime)
    {
        if (IsConditionActive)
        {
            float drainPerSecond = Definition.threshold / Definition.conditionDuration;
            Buildup = Mathf.Max(0f, Buildup - drainPerSecond * deltaTime);

            if (Buildup > 0f)
                return false;

            IsConditionActive = false;
            return true;
        }

        if (Buildup <= 0f)
            return false;

        timeSinceExposure += deltaTime;

        if (timeSinceExposure >= Definition.recedeDelay)
            Buildup = Mathf.Max(0f, Buildup - Definition.recedeRate * deltaTime);

        return false;
    }

    // =========================
    // RESET
    // =========================

    // Wipes buildup and any active condition (bonfire rest, respawn).
    // Returns true if a condition was active.
    public bool Clear()
    {
        bool wasActive = IsConditionActive;

        Buildup = 0f;
        IsConditionActive = false;
        timeSinceExposure = 0f;

        return wasActive;
    }
}
