using UnityEngine;

// The player's status buildups. Exposure (hazards now, enemy attacks later)
// calls AddBuildup(); once a status crosses its threshold the condition
// deals unblockable damage every tick until the bar drains back to 0.
// PlayerStatusBars shows the bars on the HUD.
[RequireComponent(typeof(PlayerHealth))]
public class PlayerStatusEffects : StatusEffectsBase
{
    private PlayerHealth health;
    private PlayerDeath death;

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
    // CONDITIONS
    // =========================

    // What each condition does the moment it starts, on top of the
    // per-tick damage. Add a case here for any new special behaviour.
    protected override void ConditionStarted(StatusMeter meter)
    {
        Debug.Log("Player status condition: " + meter.Definition.displayName, this);

        switch (meter.Definition.type)
        {
            case StatusType.Curse:
                // A full curse bar kills outright.
                health.Kill();
                break;
        }
    }

    protected override void ConditionTick(StatusMeter meter)
    {
        health.TakeDamage(meter.Definition.playerDamagePerTick, false);
    }
}
