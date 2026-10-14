using System.Collections.Generic;
using UnityEngine;

// The player's status buildups. Exposure (hazards, enemy attacks) calls
// AddBuildup(); once a status crosses its threshold, the effects ticked under
// "On Player" in its settings asset apply until the bar drains back to 0.
// PlayerStatusBars shows the bars on the HUD.
//
// One-off effects (burst damage, instant death) and damage over time are
// applied here. Everything else is read by the system it changes:
//   MovementScript (slow, jump), PlayerStamina (drain, regen), BlockScript
//   (block strength), ParryScript (window), PlayerHealth (healing), and
//   PlayerController1 (dash / sprint / block / parry switched off).
[RequireComponent(typeof(PlayerHealth))]
public class PlayerStatusEffects : StatusEffectsBase
{
    private PlayerHealth health;
    private PlayerDeath death;

    // Damage-over-time timer per active condition.
    private readonly Dictionary<StatusMeter, float> dotTimers = new Dictionary<StatusMeter, float>();

    protected override void Awake()
    {
        base.Awake();

        health = GetComponent<PlayerHealth>();
        death = GetComponent<PlayerDeath>();
    }

    protected override bool CanBeAffected()
    {
        return death == null || !death.IsDead;
    }

    // =========================
    // WHAT CONDITIONS DO
    // =========================

    protected override void ConditionStarted(StatusMeter meter)
    {
        PlayerConditionEffects effects = meter.Definition.onPlayer;

        Debug.Log("Player status condition: " + meter.Definition.displayName, this);

        dotTimers[meter] = effects.damageOverTime.tickInterval;

        if (effects.burstDamageOnStart.enabled)
            health.TakeDamage(effects.burstDamageOnStart.damage, false);

        if (effects.instantDeath.enabled)
            health.Kill();
    }

    // Damage over time — stacks across conditions (each ticks on its own).
    protected override void ConditionUpdate(StatusMeter meter, float deltaTime)
    {
        PlayerDamageOverTimeEffect dot = meter.Definition.onPlayer.damageOverTime;

        if (!dot.enabled)
            return;

        float timer = dotTimers.TryGetValue(meter, out float t) ? t : dot.tickInterval;
        timer -= deltaTime;

        while (timer <= 0f)
        {
            health.TakeDamage(dot.damagePerTick, false);
            timer += dot.tickInterval;
        }

        dotTimers[meter] = timer;
    }

    protected override void ConditionEnded(StatusMeter meter)
    {
        dotTimers.Remove(meter);
    }

    // =========================
    // READ BY OTHER PLAYER SCRIPTS
    // =========================

    public float MoveSpeedMultiplier => Strongest(d => d.onPlayer.slowMovement, true);

    public float JumpHeightMultiplier => Strongest(d => d.onPlayer.weakerJump, true);

    public float StaminaRegenMultiplier => Strongest(d => d.onPlayer.slowerStaminaRegen, true);

    public float HealingMultiplier => Strongest(d => d.onPlayer.reducedHealing, true);

    // Drains stack, like damage over time.
    public float StaminaDrainPerSecond
    {
        get
        {
            float total = 0f;

            foreach (StatusMeter meter in Meters)
            {
                StaminaDrainEffect drain = meter.Definition.onPlayer.staminaDrain;

                if (meter.IsConditionActive && drain.enabled)
                    total += drain.drainPerSecond;
            }

            return total;
        }
    }

    public bool DashDisabled => AnyActiveWhere(e => e.dashSprintDisabled.enabled && e.dashSprintDisabled.disableDash);
    public bool SprintDisabled => AnyActiveWhere(e => e.dashSprintDisabled.enabled && e.dashSprintDisabled.disableSprint);
    public bool BlockDisabled => AnyActiveWhere(e => e.weakerBlock.enabled && e.weakerBlock.disableBlock);
    public bool ParryDisabled => AnyActiveWhere(e => e.weakerParry.enabled && e.weakerParry.disableParry);

    // Strongest (lowest) block / parry weakening among active conditions.
    public float BlockEffectiveness => LowestWhere(e => e.weakerBlock.enabled, e => e.weakerBlock.effectiveness);
    public float ParryWindowMultiplier => LowestWhere(e => e.weakerParry.enabled, e => e.weakerParry.windowMultiplier);

    bool AnyActiveWhere(System.Func<PlayerConditionEffects, bool> test)
    {
        foreach (StatusMeter meter in Meters)
        {
            if (meter.IsConditionActive && test(meter.Definition.onPlayer))
                return true;
        }

        return false;
    }

    float LowestWhere(System.Func<PlayerConditionEffects, bool> enabled, System.Func<PlayerConditionEffects, float> value)
    {
        float result = 1f;

        foreach (StatusMeter meter in Meters)
        {
            if (meter.IsConditionActive && enabled(meter.Definition.onPlayer))
                result = Mathf.Min(result, value(meter.Definition.onPlayer));
        }

        return result;
    }
}
