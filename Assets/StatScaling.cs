using System;
using System.Collections.Generic;
using UnityEngine;

// Souls-style stat scaling: a letter rank on the item says how strongly it
// scales, and the player's stat level says how far along the curve they are.
//
//     bonus = rank coefficient  x  curve(stat level)
//
// The curve goes from 0 at level 1 to 1 at its last point, with soft caps
// where each further level is worth less (Elden Ring's 20/55/80 idea). So an
// S-rank item at max stat gets the full S coefficient as a bonus; a D-rank
// item at a low stat gets very little. Callers apply it as (1 + bonus).
public enum ScalingRank
{
    None,
    E,
    D,
    C,
    B,
    A,
    S
}

// The player's levelled stats (PlayerStats). Append new entries at the END only.
public enum PlayerStatType
{
    Health,
    Stamina,
    Strength,
    Dexterity,
    SoulDurability
}

[Serializable]
public class ScalingPoint
{
    [Min(1)] public int level = 1;

    [Tooltip("Share of the rank's full bonus reached at this level (0-1).")]
    [Range(0f, 1f)] public float share;

    public ScalingPoint(int level, float share)
    {
        this.level = level;
        this.share = share;
    }
}

public static class StatScaling
{
    public const string ResourcePath = "StatScalingSettings";

    private static StatScalingSettings settings;

    public static StatScalingSettings Settings
    {
        get
        {
            if (settings == null)
                settings = Resources.Load<StatScalingSettings>(ResourcePath);

            if (settings == null)
                settings = ScriptableObject.CreateInstance<StatScalingSettings>(); // defaults

            return settings;
        }
    }

    // E.g. GetBonus(ScalingRank.B, 25) = 0.5 x 0.8 = 0.4 -> apply as x1.4.
    public static float GetBonus(ScalingRank rank, int level) => Settings.GetBonus(rank, level);
}
