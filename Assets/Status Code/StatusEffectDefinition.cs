using UnityEngine;

// The tuning for one status effect, shared by the player and every enemy.
// One asset per status lives in Assets/Status Code/Definitions — change a
// value there and it changes everywhere.
//
// How a status works (same for player and enemies):
//   Exposure adds buildup. Stop being exposed and, after recedeDelay, the
//   buildup drains away at recedeRate. If it reaches the threshold instead,
//   the condition starts and its effects (below — tick the ones you want)
//   last while the buildup drains from the threshold back to 0 over
//   conditionDuration. No new buildup of that status lands until it's back at 0.
[CreateAssetMenu(fileName = "NewStatusEffect", menuName = "Status Effects/Status Effect")]
public class StatusEffectDefinition : ScriptableObject
{
    [Header("Identity")]
    public StatusType type;

    [Tooltip("Name shown next to the player's buildup bar.")]
    public string displayName = "Status";

    [Tooltip("Colour of the player's buildup bar.")]
    public Color barColor = Color.white;

    [Header("Buildup")]
    [Tooltip("Buildup needed to trigger the condition.")]
    [Min(1f)] public float threshold = 50f;

    [Tooltip("Seconds after the last exposure before buildup starts receding.")]
    [Min(0f)] public float recedeDelay = 2f;

    [Tooltip("Buildup lost per second while receding (before the threshold is reached).")]
    [Min(0f)] public float recedeRate = 10f;

    [Header("Condition")]
    [Tooltip("Seconds the condition lasts — the time the bar takes to drain from the threshold to 0.")]
    [Min(0.1f)] public float conditionDuration = 10f;

    [Header("Condition Effects")]
    [Tooltip("What the condition does to the player while it lasts.")]
    public PlayerConditionEffects onPlayer = new PlayerConditionEffects();

    [Tooltip("What the condition does to an enemy while it lasts.")]
    public EnemyConditionEffects onEnemy = new EnemyConditionEffects();
}
