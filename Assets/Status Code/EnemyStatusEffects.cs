using System;
using System.Collections.Generic;
using UnityEngine;

// An enemy's (hidden) status buildups. Works like the player's, except:
//   - Resistances scale incoming buildup per status, so enemies can be
//     tailored to where they live (0 = immune, 0.5 = resistant, 2 = weak).
//   - Condition damage goes to the PRIMARY health bar — status conditions
//     are how the player actually wears enemies down.
[RequireComponent(typeof(EnemyHealth))]
public class EnemyStatusEffects : StatusEffectsBase
{
    [Serializable]
    public class StatusResistance
    {
        public StatusType status;

        [Tooltip("Multiplies incoming buildup. 0 = immune, 0.5 = resistant, 1 = normal, 2 = weak.")]
        [Min(0f)] public float buildupMultiplier = 1f;
    }

    [Header("Resistances")]
    [Tooltip("Statuses not listed take normal buildup (x1).")]
    [SerializeField] private List<StatusResistance> resistances = new List<StatusResistance>();

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
    // RESISTANCES
    // =========================

    protected override float ScaleBuildup(StatusType type, float amount)
    {
        return amount * GetBuildupMultiplier(type);
    }

    public float GetBuildupMultiplier(StatusType type)
    {
        foreach (StatusResistance resistance in resistances)
        {
            if (resistance.status == type)
                return resistance.buildupMultiplier;
        }

        return 1f;
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
