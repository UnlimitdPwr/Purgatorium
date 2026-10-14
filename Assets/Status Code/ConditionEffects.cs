using System;
using UnityEngine;

// The tickable effects a status condition can have. Each status asset holds
// one PlayerConditionEffects and one EnemyConditionEffects; tick a box in the
// Inspector to switch an effect on, and its settings appear underneath.
//
// Several active conditions with the same effect:
//   - Damage over time and stamina drain STACK (add together).
//   - Every multiplier uses only the STRONGEST one; weaker ones are ignored.
//   - On/off effects (can't jump, can't dash...) apply if any condition has them.
//
// Adding a new effect: add a field to PlayerConditionEffects or
// EnemyConditionEffects, then read it in PlayerStatusEffects /
// EnemyStatusEffects (and wherever the game should react to it).

// =========================
// BUILDING BLOCKS
// =========================

// Base for every effect: just the on/off box. ConditionEffectDrawer shows the
// settings only while it's ticked.
[Serializable]
public class ConditionEffect
{
    public bool enabled;
}

// An on/off effect with no settings (e.g. instant death).
[Serializable]
public class ToggleEffect : ConditionEffect
{
}

[Serializable]
public class MultiplierEffect : ConditionEffect
{
    [Tooltip("1 = no change. Below 1 = less, above 1 = more.")]
    [Min(0f)] public float multiplier = 1f;
}

[Serializable]
public class PlayerDamageOverTimeEffect : ConditionEffect
{
    [Tooltip("Unblockable damage dealt each tick.")]
    [Min(0)] public int damagePerTick = 2;

    [Tooltip("Seconds between ticks. The first tick lands one interval after the condition starts.")]
    [Min(0.05f)] public float tickInterval = 1f;
}

[Serializable]
public class EnemyDamageOverTimeEffect : ConditionEffect
{
    [Tooltip("Added to the enemy's primary health bar each tick.")]
    [Min(0f)] public float damagePerTick = 5f;

    [Tooltip("Seconds between ticks. The first tick lands one interval after the condition starts.")]
    [Min(0.05f)] public float tickInterval = 1f;
}

[Serializable]
public class PlayerBurstDamageEffect : ConditionEffect
{
    [Tooltip("Unblockable damage dealt once, the moment the condition starts.")]
    [Min(0)] public int damage = 15;
}

[Serializable]
public class EnemyBurstDamageEffect : ConditionEffect
{
    [Tooltip("Added to the primary health bar once, the moment the condition starts.")]
    [Min(0f)] public float damage = 15f;
}

[Serializable]
public class StaminaDrainEffect : ConditionEffect
{
    [Tooltip("Stamina lost per second while the condition lasts. Hitting 0 exhausts the player as usual.")]
    [Min(0f)] public float drainPerSecond = 5f;
}

[Serializable]
public class DashSprintEffect : ConditionEffect
{
    public bool disableDash = true;
    public bool disableSprint = true;
}

[Serializable]
public class BlockEffect : ConditionEffect
{
    [Tooltip("Multiplies how much blocking reduces damage and status buildup. " +
             "0.5 = blocks stop half as much as normal.")]
    [Range(0f, 1f)] public float effectiveness = 0.5f;

    [Tooltip("Can't block at all while the condition lasts.")]
    public bool disableBlock;
}

[Serializable]
public class ParryEffect : ConditionEffect
{
    [Tooltip("Multiplies how long the parry window stays open. " +
             "(Only works while ParryScript uses its timer, not animation events.)")]
    [Range(0.05f, 1f)] public float windowMultiplier = 0.5f;

    [Tooltip("Can't parry at all while the condition lasts.")]
    public bool disableParry;
}

[Serializable]
public class StunEffect : ConditionEffect
{
    [Tooltip("Seconds the enemy is stunned (no AI) when the condition starts.")]
    [Min(0f)] public float duration = 1f;
}

// =========================
// WHAT A CONDITION DOES
// =========================

[Serializable]
public class PlayerConditionEffects
{
    public PlayerDamageOverTimeEffect damageOverTime = new PlayerDamageOverTimeEffect { enabled = true };

    public PlayerBurstDamageEffect burstDamageOnStart = new PlayerBurstDamageEffect();

    [Tooltip("The player dies the moment the condition starts.")]
    public ToggleEffect instantDeath = new ToggleEffect();

    [Tooltip("Walking speed multiplier (0.7 = 70% speed).")]
    public MultiplierEffect slowMovement = new MultiplierEffect { multiplier = 0.7f };

    [Tooltip("Jump height multiplier (0.6 = 60% as high).")]
    public MultiplierEffect weakerJump = new MultiplierEffect { multiplier = 0.6f };

    public StaminaDrainEffect staminaDrain = new StaminaDrainEffect();

    [Tooltip("Stamina regeneration multiplier (0.5 = regenerates half as fast).")]
    public MultiplierEffect slowerStaminaRegen = new MultiplierEffect { multiplier = 0.5f };

    public DashSprintEffect dashSprintDisabled = new DashSprintEffect();

    public BlockEffect weakerBlock = new BlockEffect();

    public ParryEffect weakerParry = new ParryEffect();

    [Tooltip("Healing multiplier (0.5 = heals half as much). Bonfire rest isn't affected — it clears conditions.")]
    public MultiplierEffect reducedHealing = new MultiplierEffect { multiplier = 0.5f };
}

[Serializable]
public class EnemyConditionEffects
{
    public EnemyDamageOverTimeEffect damageOverTime = new EnemyDamageOverTimeEffect { enabled = true };

    public EnemyBurstDamageEffect burstDamageOnStart = new EnemyBurstDamageEffect();

    [Tooltip("Walking speed multiplier (0.7 = 70% speed).")]
    public MultiplierEffect slowMovement = new MultiplierEffect { multiplier = 0.7f };

    [Tooltip("Multiplies the pauses between swings and combos (1.5 = 50% longer).")]
    public MultiplierEffect slowerAttacks = new MultiplierEffect { multiplier = 1.5f };

    [Tooltip("Attack damage multiplier, sword, leap and body contact (0.75 = 75% damage).")]
    public MultiplierEffect weakerAttacks = new MultiplierEffect { multiplier = 0.75f };

    [Tooltip("Multiplies how much a parry fills the secondary bar (1.5 = 50% more).")]
    public MultiplierEffect parryVulnerability = new MultiplierEffect { multiplier = 1.5f };

    public StunEffect stunOnStart = new StunEffect();

    [Tooltip("Sight range multiplier (0.5 = sees half as far). Hearing isn't affected.")]
    public MultiplierEffect blinded = new MultiplierEffect { multiplier = 0.5f };

    [Tooltip("No jumps, leaps or retreat hops while the condition lasts.")]
    public ToggleEffect cannotJump = new ToggleEffect();
}
