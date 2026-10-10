using UnityEngine;

// Everything that makes one kind of environmental hazard what it is — acid,
// lava, swamp... One asset per kind lives in Assets/Level Code/Hazards/Types.
// Every hazard area in the game is the same EnvironmentalHazard component
// pointing at one of these, so changing a value here changes every pool of
// that kind, and making a new kind is just a new asset:
//     right-click > Create > Hazards > Hazard Type
[CreateAssetMenu(fileName = "NewHazardType", menuName = "Hazards/Hazard Type")]
public class HazardType : ScriptableObject
{
    [Header("Visuals")]
    [Tooltip("Texture drawn over the hazard area. Leave empty to keep the plain coloured box.")]
    public Sprite sprite;

    [Tooltip("Colour multiplied over the sprite (alpha = see-through).")]
    public Color tint = Color.white;

    [Tooltip("On: the sprite repeats across the area. Off: one copy stretched to fit.")]
    public bool tileSprite = true;

    [Header("Damage Over Time")]
    [Tooltip("Seconds between ticks. The first tick lands the moment something enters.")]
    [Min(0.05f)] public float tickInterval = 0.5f;

    [Tooltip("Unblockable damage dealt to the player each tick.")]
    [Min(0)] public int playerDamagePerTick = 0;

    [Tooltip("Added to an enemy's primary health bar each tick.")]
    [Min(0f)] public float enemyDamagePerTick = 0f;

    [Header("Status Buildup")]
    [Tooltip("Status buildup added each tick (before enemy resistances). Leave empty for none.")]
    public StatusInfliction[] buildupPerTick = new StatusInfliction[0];

    [Header("Movement")]
    [Tooltip("Walking speed multiplier while inside. 1 = normal, 0.5 = half speed. " +
             "Standing in several hazards at once uses the slowest one.")]
    [Range(0.05f, 2f)] public float speedMultiplier = 1f;

    [Header("Who It Affects")]
    public bool affectsPlayer = true;
    public bool affectsEnemies = true;
}
