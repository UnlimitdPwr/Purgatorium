using System.Collections.Generic;
using UnityEngine;

// An enemy's (hidden) status buildups. Works like the player's (buildup
// rate, recovery rate and resistances live in StatusEffectsBase), except
// buildup never recedes on its own; once a condition starts,
// the effects ticked under "On Enemy" in its settings asset apply until the
// bar drains back to 0. Damage goes to the PRIMARY health bar — status
// conditions are how the player actually wears enemies down.
//
// One-off effects (burst damage, stun) and damage over time are applied
// here. Everything else is read by the system it changes: EnemyMovement
// (slow), EnemyAttack / EnemyJumpAttack (cooldown, damage), EnemyHealth
// (parry vulnerability), EnemyDetection (sight), EnemyJump / EnemyController
// (no jumping, stun).
[RequireComponent(typeof(EnemyHealth))]
public class EnemyStatusEffects : StatusEffectsBase
{
    private EnemyHealth health;

    // Damage-over-time timer per active condition.
    private readonly Dictionary<StatusMeter, float> dotTimers = new Dictionary<StatusMeter, float>();

    private float stunTimer;

    protected override void Awake()
    {
        base.Awake();

        health = GetComponent<EnemyHealth>();
    }

    protected override bool CanBeAffected()
    {
        return !health.IsDead;
    }

    // Enemy buildup never recedes — it only clears through a full condition,
    // so partial buildup from earlier in a fight still counts.
    protected override bool BuildupRecedes => false;

    protected override void Update()
    {
        if (stunTimer > 0f)
            stunTimer -= Time.deltaTime;

        base.Update();
    }

    // =========================
    // WHAT CONDITIONS DO
    // =========================

    protected override void ConditionStarted(StatusMeter meter)
    {
        EnemyConditionEffects effects = meter.Definition.onEnemy;

        dotTimers[meter] = effects.damageOverTime.tickInterval;

        if (effects.burstDamageOnStart.enabled)
            health.ApplyStatusDamage(effects.burstDamageOnStart.damage);

        if (effects.stunOnStart.enabled)
            stunTimer = Mathf.Max(stunTimer, effects.stunOnStart.duration);
    }

    // Damage over time — stacks across conditions (each ticks on its own).
    protected override void ConditionUpdate(StatusMeter meter, float deltaTime)
    {
        EnemyDamageOverTimeEffect dot = meter.Definition.onEnemy.damageOverTime;

        if (!dot.enabled)
            return;

        float timer = dotTimers.TryGetValue(meter, out float t) ? t : dot.tickInterval;
        timer -= deltaTime;

        while (timer <= 0f)
        {
            health.ApplyStatusDamage(dot.damagePerTick);
            timer += dot.tickInterval;
        }

        dotTimers[meter] = timer;
    }

    protected override void ConditionEnded(StatusMeter meter)
    {
        dotTimers.Remove(meter);
    }

    // =========================
    // READ BY OTHER ENEMY SCRIPTS
    // =========================

    public bool IsStunned => stunTimer > 0f;

    public float MoveSpeedMultiplier => Strongest(d => d.onEnemy.slowMovement, true);
    public float AttackCooldownMultiplier => Strongest(d => d.onEnemy.slowerAttacks, false);
    public float AttackDamageMultiplier => Strongest(d => d.onEnemy.weakerAttacks, true);
    public float ParrySecondaryMultiplier => Strongest(d => d.onEnemy.parryVulnerability, false);
    public float SightRangeMultiplier => Strongest(d => d.onEnemy.blinded, true);
    public bool CannotJump => AnyActive(d => d.onEnemy.cannotJump);
}
