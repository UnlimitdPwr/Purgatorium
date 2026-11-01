using UnityEngine;

// One enemy attack, as an asset: which animation it plays, when its hitbox is
// live, how it's telegraphed, how hard it hits, and whether the player can
// parry or block it. EnemyAttack picks from a list of these each swing, so a
// new move is a new asset — no code.
//
// Create one with right-click > Create > Purgatorium > Enemy Attack. Its
// animation needs a matching state in the enemy's Animator: run
// Tools > Enemy Attack > Add Animator States For Attacks after adding one.
[CreateAssetMenu(fileName = "NewEnemyAttack", menuName = "Purgatorium/Enemy Attack")]
public class EnemyAttackData : ScriptableObject
{
    [Header("Animation")]
    [Tooltip("The swing. Its length is how long the enemy is committed to the attack. " +
             "Played through the Animator state with the same name.")]
    public AnimationClip clip;

    [Header("Hitbox")]
    [Tooltip("Seconds into the swing when the hitbox switches on.")]
    [Min(0f)] public float hitboxStart = 0.33f;

    [Tooltip("Seconds into the swing when the hitbox switches off again.")]
    [Min(0f)] public float hitboxEnd = 0.5f;

    [Header("Telegraph")]
    [Tooltip("Seconds the enemy stands and flashes before the swing starts, so the player " +
             "can read what's coming. 0 = swing straight away.")]
    [Min(0f)] public float windUp = 0f;

    [Tooltip("Colour the enemy flashes during the wind-up. Use a different colour for " +
             "attacks that can't be parried, so the player learns to tell them apart.")]
    public Color windUpTint = new Color(1f, 0.85f, 0.3f);

    [Header("Damage")]
    [Tooltip("Times the enemy's own damage (EnemyAttack.damage). 1 = as set on the enemy.")]
    [Min(0f)] public float damageMultiplier = 1f;

    [Tooltip("Times the strength of the statuses the enemy applies with this attack.")]
    [Min(0f)] public float statusMultiplier = 1f;

    [Tooltip("Extra status buildup only this attack adds.")]
    public StatusInfliction[] extraStatusOnHit = new StatusInfliction[0];

    [Header("Defence")]
    [Tooltip("Off = the player's parry doesn't work against it (block or dodge instead).")]
    public bool canBeParried = true;

    [Tooltip("Off = blocking doesn't reduce it (parry or dodge instead).")]
    public bool canBeBlocked = true;

    [Header("Choice")]
    [Tooltip("How often this attack is picked compared with the enemy's other attacks.")]
    [Min(0f)] public float weight = 1f;

    [Tooltip("When this attack follows another in a combo: an extra pause, picked at random " +
             "between these (seconds), before it starts — a delayed swing to catch an " +
             "early parry. (0, 0) = no extra pause.")]
    public Vector2 comboHesitation = Vector2.zero;

    // Seconds the swing animation lasts (0.6 if there's no clip).
    public float SwingDuration => clip != null && clip.length > 0f ? clip.length : 0.6f;
}
