using UnityEngine;

// An enemy's (hidden) status buildups. Works like the player's (buildup
// rate and resistances live in StatusEffectsBase), except condition damage
// goes to the PRIMARY health bar — status conditions are how the player
// actually wears enemies down.
[RequireComponent(typeof(EnemyHealth))]
public class EnemyStatusEffects : StatusEffectsBase
{
    private EnemyHealth health;

    protected override void Awake()
    {
        base.Awake();

        health = GetComponent<EnemyHealth>();
    }

    protected override bool CanBeAffected()
    {
        return !health.IsDead;
    }

    // =========================
    // CONDITIONS
    // =========================

    // What each condition does the moment it starts, on top of the
    // per-tick damage. Add a case here for any new special behaviour.
    protected override void ConditionStarted(StatusMeter meter)
    {
        switch (meter.Definition.type)
        {
            case StatusType.Curse:
                OnCursed();
                break;
        }
    }

    // Curse kills the player outright; what it does to enemies isn't decided
    // yet. Until then it behaves like the others (damage per tick only).
    void OnCursed()
    {
    }

    protected override void ConditionTick(StatusMeter meter)
    {
        health.ApplyStatusDamage(meter.Definition.enemyDamagePerTick);
    }
}
