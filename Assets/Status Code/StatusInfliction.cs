using System;
using UnityEngine;

// "This hit adds X buildup of status Y." Attacks (and later items/hazards)
// hold a list of these, so one hit can carry several statuses.
[Serializable]
public class StatusInfliction
{
    public StatusType status;

    [Tooltip("Buildup added per hit, before the target's resistances.")]
    [Min(0f)] public float buildup = 10f;
}
