using System;
using System.Collections.Generic;
using UnityEngine;

// What a talisman multiplier can change. Append new entries at the END only
// (assets store these as numbers).
public enum TalismanEffect
{
    MaxHealth,
    MaxStamina,
    StaminaRegen,
    ParryWindow,
    ParryCooldown,
    BlockDamageReduction,
    BlockStatusReduction,
    StatusBuildupTaken,   // all statuses; per-status ones go in Resistances
    StatusRecovery,
    ShieldDurabilityLoss
}

[Serializable]
public class TalismanModifier
{
    public TalismanEffect effect;

    [Tooltip("1 = no change, 1.2 = +20%, 0.8 = -20%.")]
    [Min(0f)] public float multiplier = 1f;
}

// The talisman blueprint (Create > Inventory > Talisman). Every equipped
// talisman is active at once (no cycling). Its effects:
//   - Multipliers: each scales one player value (see TalismanEffect).
//   - Resistances: per-status buildup multipliers on the player
//     (0 = immune, 0.5 = resistant, 2 = weak).
//   - Stat scaling: one stat + E-S rank. The bonus STRENGTHENS whatever the
//     talisman does, i.e. pushes each value further from 1:
//         effective = 1 + (value - 1) x (1 + bonus)
//     so x1.2 max health with a +50% bonus becomes x1.3, and 0.5 poison
//     buildup becomes 0.25.
// Several talismans (and talisman + base values) multiply together.
// PlayerTalismans does the maths at runtime.
[CreateAssetMenu(fileName = "NewTalisman", menuName = "Inventory/Talisman")]
public class TalismanData : ItemData
{
    [Header("Stat Scaling (E lowest, S highest)")]
    public PlayerStatType scalingStat = PlayerStatType.SoulDurability;
    public ScalingRank scalingRank = ScalingRank.None;

    [Header("Multipliers")]
    public List<TalismanModifier> multipliers = new List<TalismanModifier>();

    [Header("Character Resistances")]
    [Tooltip("Per-status buildup multipliers for the player: 0 = immune, 0.5 = resistant, 1 = normal, 2 = weak.")]
    public List<StatusResistance> resistances = new List<StatusResistance>();

    private void Reset()
    {
        equipmentType = EquipmentSlotType.Talisman;
        maxStack = 1;
    }

    private void OnValidate()
    {
        equipmentType = EquipmentSlotType.Talisman;
    }
}
