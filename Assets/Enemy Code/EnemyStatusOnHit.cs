using UnityEngine;

// The status effects this enemy inflicts — its "element". Every one of its
// attacks carries them: the sword swing, the leap attack, and any future
// attack that deals its hit through an EnemyHitbox. Set them once here
// instead of on each attack.
//
// Each attack can scale this (its statusMultiplier, e.g. a heavy leap at
// 1.5×) and add extra statuses of its own (its statusOnHit list).
public class EnemyStatusOnHit : MonoBehaviour
{
    [Tooltip("Status buildup every hit from this enemy adds to the player " +
             "(before the attack's statusMultiplier and the player's resistances). " +
             "Leave empty for none.")]
    public StatusInfliction[] inflictions = new StatusInfliction[0];
}
