using System;
using UnityEngine;

// One entry in a character's resistance list: scales incoming buildup of a
// single status. Used by the player (later: changed by equipped gear) and
// by enemies (tailored to where they live).
[Serializable]
public class StatusResistance
{
    public StatusType status;

    [Tooltip("Multiplies incoming buildup. 0 = immune, 0.5 = resistant, 1 = normal, 2 = weak.")]
    [Min(0f)] public float buildupMultiplier = 1f;
}
