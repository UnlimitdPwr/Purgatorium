using UnityEngine;

// The shield blueprint. Each shield in the game is one of these assets
// (Create > Inventory > Shield). It is still an ItemData, so it works with the
// inventory, the bonfire equipment screen and the HUD exactly like before —
// it just carries the numbers that ParryScript and BlockScript use while it
// is the active shield (EquipmentManager.ActiveShield, cycled with V).
//
// With no shield equipped, ParryScript and BlockScript fall back to their own
// Inspector values. The defaults below match those values, so turning an
// existing shield into a ShieldData doesn't change how it plays.
[CreateAssetMenu(fileName = "NewShield", menuName = "Inventory/Shield")]
public class ShieldData : ItemData
{
    [Header("Parry Window")]
    [Tooltip("Seconds after pressing parry before the window opens.")]
    [Min(0f)] public float parryStartDelay = 0.05f;

    [Tooltip("How long the parry window stays open, in seconds.")]
    [Min(0f)] public float parryWindowDuration = 0.55f;

    [Tooltip("Seconds from one parry press to the next.")]
    [Min(0f)] public float parryCooldown = 0.80f;

    [Tooltip("When ON, attacks from behind the player can't be parried with this shield.")]
    public bool parryRequiresFacing = true;

    [Header("Status On Parry")]
    [Tooltip("Status buildup this shield puts on the attacker when it parries them " +
             "(e.g. 15 Burn). The enemy's own resistances still apply. Leave empty for none.")]
    public StatusInfliction[] statusOnParry = new StatusInfliction[0];

    [Header("Blocking")]
    [Tooltip("Share of a blocked hit's damage this shield stops. 0 = none, 1 = all.")]
    [Range(0f, 1f)] public float blockDamageReduction = 0.6f;

    [Tooltip("Share of a blocked hit's status buildup this shield stops. 0 = none, 1 = all. " +
             "(Parries always stop all of it.)")]
    [Range(0f, 1f)] public float blockStatusReduction = 0.5f;

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
