// Every status effect in the game. This is what shows up in Inspector
// dropdowns (enemy resistances, hazards, attacks...).
//
// Adding a new status:
//   1. Add it to the END of this list with the next number. Unity saves
//      dropdowns as these numbers, so never reorder or renumber existing
//      entries — every resistance already set in a scene would shift.
//   2. Run Tools > Status Effects > Set Up Status Effects In Open Scene.
//      It creates the new status's settings asset and hands it to the
//      player and every enemy.
//   3. If the condition does something besides damage over time, add a
//      case for it in PlayerStatusEffects / EnemyStatusEffects.
public enum StatusType
{
    Poison = 0,
    Burn = 1,
    Bleed = 2,
    Curse = 3
}
