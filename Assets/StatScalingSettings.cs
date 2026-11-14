using System.Collections.Generic;
using UnityEngine;

// The single shared tuning asset, loaded from Resources/StatScalingSettings.
// If it's missing, the same defaults below are used, so nothing breaks.
[CreateAssetMenu(fileName = "StatScalingSettings", menuName = "Inventory/Stat Scaling Settings")]
public class StatScalingSettings : ScriptableObject
{
    [Header("Rank Coefficients (bonus at the top of the curve)")]
    [Tooltip("0.1 = +10% at max stat.")]
    public float rankE = 0.10f;
    public float rankD = 0.20f;
    public float rankC = 0.35f;
    public float rankB = 0.50f;
    public float rankA = 0.70f;
    public float rankS = 0.90f;

    [Header("Stat Curve (soft caps)")]
    [Tooltip("Level -> share of the full bonus, straight lines between points. Each later " +
             "segment should be flatter than the one before - those bends are the soft caps. " +
             "Points must be in increasing level order.")]
    public List<ScalingPoint> curve = DefaultCurve();

    public static List<ScalingPoint> DefaultCurve() => new List<ScalingPoint>
    {
        new ScalingPoint(1, 0f),
        new ScalingPoint(10, 0.40f),  // soft cap 1: levels 1-10 are the cheapest gains
        new ScalingPoint(25, 0.80f),  // soft cap 2
        new ScalingPoint(40, 0.95f),  // soft cap 3 (expected end-game level)
        new ScalingPoint(60, 1f)      // hard cap: no gain past this
    };

    public float GetCoefficient(ScalingRank rank)
    {
        switch (rank)
        {
            case ScalingRank.E: return rankE;
            case ScalingRank.D: return rankD;
            case ScalingRank.C: return rankC;
            case ScalingRank.B: return rankB;
            case ScalingRank.A: return rankA;
            case ScalingRank.S: return rankS;
            default: return 0f;
        }
    }

    public float GetCurveShare(int level)
    {
        if (curve == null || curve.Count == 0)
            return 0f;

        if (level <= curve[0].level)
            return curve[0].share;

        for (int i = 1; i < curve.Count; i++)
        {
            if (level <= curve[i].level)
            {
                ScalingPoint a = curve[i - 1];
                ScalingPoint b = curve[i];
                float t = Mathf.InverseLerp(a.level, b.level, level);
                return Mathf.Lerp(a.share, b.share, t);
            }
        }

        return curve[curve.Count - 1].share;
    }

    public float GetBonus(ScalingRank rank, int level) =>
        GetCoefficient(rank) * GetCurveShare(level);
}
