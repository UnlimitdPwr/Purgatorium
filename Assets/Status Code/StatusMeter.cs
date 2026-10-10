using UnityEngine;

// The buildup meter for one status on one character. Plain class, not a
// component — PlayerStatusEffects and EnemyStatusEffects each hold one per
// status and call Tick() every frame. All the rules live here so the player
// and enemies can't drift apart.
public class StatusMeter
{
    public StatusEffectDefinition Definition { get; }

    public float Buildup { get; private set; }
    public bool IsConditionActive { get; private set; }

    // 0..1 of the threshold — what a bar should show.
    public float Fill => Buildup / Definition.threshold;

    private float timeSinceExposure;
    private float tickTimer;

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
        tickTimer = Definition.tickInterval;
        return true;
    }

    // =========================
    // PER FRAME
    // =========================

    // Advances the meter. Returns how many damage ticks landed this frame
    // (almost always 0 or 1). conditionEnded is set when it wore off.
    public int Tick(float deltaTime, out bool conditionEnded)
    {
        conditionEnded = false;

        if (IsConditionActive)
            return TickCondition(deltaTime, out conditionEnded);

        if (Buildup <= 0f)
            return 0;

        timeSinceExposure += deltaTime;

        if (timeSinceExposure >= Definition.recedeDelay)
            Buildup = Mathf.Max(0f, Buildup - Definition.recedeRate * deltaTime);

        return 0;
    }

    int TickCondition(float deltaTime, out bool conditionEnded)
    {
        int ticks = 0;

        tickTimer -= deltaTime;

        while (tickTimer <= 0f)
        {
            ticks++;
            tickTimer += Definition.tickInterval;
        }

        float drainPerSecond = Definition.threshold / Definition.conditionDuration;
        Buildup = Mathf.Max(0f, Buildup - drainPerSecond * deltaTime);

        conditionEnded = Buildup <= 0f;

        if (conditionEnded)
            IsConditionActive = false;

        return ticks;
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
        tickTimer = 0f;

        return wasActive;
    }
}
