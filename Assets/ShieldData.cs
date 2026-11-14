using UnityEngine;

// The shield blueprint. Each shield in the game is one of these assets
// (Create > Inventory > Shield). It is still an ItemData, so it works with the
// inventory, the bonfire equipment screen and the HUD like any item.
//
// Block and parry numbers here are MULTIPLIERS on the player's own base values
// (the Inspector values on ParryScript / BlockScript): 1 = unchanged,
// 1.2 = 20% more, 0.8 = 20% less. Stat scaling then grows them further as the
// player levels the matching stat:
//     Strength       -> block (damage + status reduction)
//     Dexterity      -> parry (longer window, faster start and cooldown)
//     Soul Durability-> the shield's durability bar and its status on parry
// PlayerShield does all the maths at runtime.
[CreateAssetMenu(fileName = "NewShield", menuName = "Inventory/Shield")]
public class ShieldData : ItemData
{
    [Header("Stat Scaling (E lowest, S highest)")]
    [Tooltip("How much the player's Strength improves this shield's blocking.")]
    public ScalingRank strengthScaling = ScalingRank.None;

    [Tooltip("How much the player's Dexterity improves this shield's parrying.")]
    public ScalingRank dexterityScaling = ScalingRank.None;

    [Tooltip("How much the player's Soul Durability raises this shield's max durability " +
             "and its status-on-parry strength.")]
    public ScalingRank soulDurabilityScaling = ScalingRank.None;

    [Header("Parry (multipliers on the player's base)")]
    [Tooltip("x the player's parry window length. Higher = more forgiving.")]
    [Min(0f)] public float parryWindowMultiplier = 1f;

    [Tooltip("x the player's delay before the window opens. Lower = snappier.")]
    [Min(0f)] public float parryStartDelayMultiplier = 1f;

    [Tooltip("x the player's parry cooldown. Lower = can parry again sooner.")]
    [Min(0f)] public float parryCooldownMultiplier = 1f;

    [Tooltip("When ON, this shield can parry attacks from behind, even though the player normally can't.")]
    public bool canParryFromBehind = false;

    [Header("Block (multipliers on the player's base)")]
    [Tooltip("x the player's block damage reduction (the result is capped at 100%).")]
    [Min(0f)] public float blockDamageMultiplier = 1f;

    [Tooltip("x the player's block status-buildup reduction (the result is capped at 100%).")]
    [Min(0f)] public float blockStatusMultiplier = 1f;

    [Header("Status On Parry (flat buildup)")]
    [Tooltip("Status buildup put on the attacker when this shield parries them (e.g. 15 Burn). " +
             "Scaled by Soul Durability scaling and by how worn the shield is; the enemy's " +
             "own resistances still apply. Leave empty for none.")]
    public StatusInfliction[] statusOnParry = new StatusInfliction[0];

    [Header("Soul Durability (the shield's health bar)")]
    [Tooltip("Durability at Soul Durability level 1. Raised by Soul Durability scaling. " +
             "At 0 the shield is broken and acts as if no shield were equipped.")]
    [Min(1f)] public float baseDurability = 100f;

    [Tooltip("Durability lost each time a hit is blocked with this shield.")]
    [Min(0f)] public float durabilityLossPerBlock = 5f;

    [Tooltip("Durability lost each successful parry with this shield.")]
    [Min(0f)] public float durabilityLossPerParry = 2f;

    [Tooltip("Status-on-parry strength when durability is almost empty (x this at ~0%, " +
             "x1 at 100%, straight line between).")]
    [Range(0f, 1f)] public float statusMultiplierWhenWorn = 0.25f;

    // A shield is always a single shield-slot item.
    private void Reset()
    {
        equipmentType = EquipmentSlotType.Shield;
        maxStack = 1;
    }

    private void OnValidate()
    {
        equipmentType = EquipmentSlotType.Shield;
    }
}
