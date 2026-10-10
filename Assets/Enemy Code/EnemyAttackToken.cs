using System.Collections.Generic;
using UnityEngine;

// Keeps a group of enemies from all attacking at once. An enemy needs a
// token to swing or leap; only maxSimultaneousAttackers enemies can hold
// one at a time. The rest hang back at waitDistance until a token frees up
// (an attacker finishes its combo and retreats, loses the player, dies...).
//
// Shared by every enemy in the scene. An enemy without this component
// ignores tokens and attacks whenever it likes.
public class EnemyAttackToken : MonoBehaviour
{
    [Tooltip("Most enemies that can be attacking at the same time. Each enemy checks its own " +
             "value, so a boss can be given a higher one than the grunts around it.")]
    [Min(1)] public int maxSimultaneousAttackers = 2;

    [Tooltip("While waiting for a turn, the enemy stops this far (horizontally) from the player.")]
    public float waitDistance = 2.5f;

    private static readonly List<EnemyAttackToken> holders = new List<EnemyAttackToken>();

    // Domain reload is off when entering Play Mode, so statics survive
    // between runs — clear the list at the start of each one.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetTokens()
    {
        holders.Clear();
    }

    public bool HasToken => holders.Contains(this);

    // True if this enemy holds a token or could take one now.
    public bool CanAcquire
    {
        get
        {
            holders.RemoveAll(holder => holder == null);
            return HasToken || holders.Count < maxSimultaneousAttackers;
        }
    }

    // Takes a token if one is free. True if this enemy now holds one.
    public bool TryAcquire()
    {
        if (!CanAcquire)
            return false;

        if (!HasToken)
            holders.Add(this);

        return true;
    }

    public void Release()
    {
        holders.Remove(this);
    }

    void OnDisable()
    {
        Release();
    }
}
