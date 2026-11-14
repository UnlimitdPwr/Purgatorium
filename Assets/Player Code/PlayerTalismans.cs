using System;
using UnityEngine;

// Combines every equipped TalismanData into multipliers the player's systems
// read: PlayerHealth, PlayerStamina, ParryScript, BlockScript, PlayerShield
// and PlayerStatusEffects each ask for the effect they own.
//
// Lives on the Player next to EquipmentManager and PlayerStats; scripts that
// need it add it automatically if it's missing (GetOrAdd).
public class PlayerTalismans : MonoBehaviour
{
    private EquipmentManager equipment;
    private PlayerStats stats;
    private bool subscribed;

    // Fired when equipped talismans or stat levels change, i.e. when any
    // multiplier may have changed (PlayerHealth/PlayerStamina re-clamp on this).
    public event Action OnTalismansChanged;

    public static PlayerTalismans GetOrAdd(GameObject player)
    {
        PlayerTalismans talismans = player.GetComponent<PlayerTalismans>();
        return talismans != null ? talismans : player.AddComponent<PlayerTalismans>();
    }

    // References are fetched lazily: other scripts may ask for multipliers in
    // their own Awake, before this one's has run.
    private EquipmentManager Equipment
    {
        get
        {
            if (equipment == null)
                equipment = GetComponent<EquipmentManager>();
            return equipment;
        }
    }

    private PlayerStats Stats
    {
        get
        {
            if (stats == null)
                stats = GetComponent<PlayerStats>();
            return stats;
        }
    }

    private void OnEnable()
    {
        if (subscribed)
            return;

        if (Equipment != null)
            Equipment.OnEquipmentChanged += RaiseChanged;

        if (Stats != null)
            Stats.OnStatsChanged += RaiseChanged;

        subscribed = true;
    }

    private void OnDisable()
    {
        if (!subscribed)
            return;

        if (equipment != null)
            equipment.OnEquipmentChanged -= RaiseChanged;

        if (stats != null)
            stats.OnStatsChanged -= RaiseChanged;

        subscribed = false;
    }

    private void RaiseChanged() => OnTalismansChanged?.Invoke();

    // =========================
    // QUERIES
    // =========================

    // Product of every equipped talisman's multiplier for this effect (1 = none).
    public float GetMultiplier(TalismanEffect effect)
    {
        float result = 1f;

        if (Equipment == null)
            return result;

        foreach (ItemData item in Equipment.TalismanSlots)
        {
            if (!(item is TalismanData talisman) || talisman.multipliers == null)
                continue;

            float bonus = GetScalingBonus(talisman);

            foreach (TalismanModifier modifier in talisman.multipliers)
            {
                if (modifier != null && modifier.effect == effect)
                    result *= Strengthen(modifier.multiplier, bonus);
            }
        }

        return result;
    }

    // Product of every equipped talisman's resistance for this status (1 = none).
    public float GetResistance(StatusType status)
    {
        float result = 1f;

        if (Equipment == null)
            return result;

        foreach (ItemData item in Equipment.TalismanSlots)
        {
            if (!(item is TalismanData talisman) || talisman.resistances == null)
                continue;

            float bonus = GetScalingBonus(talisman);

            foreach (StatusResistance resistance in talisman.resistances)
            {
                if (resistance != null && resistance.status == status)
                    result *= Strengthen(resistance.buildupMultiplier, bonus);
            }
        }

        return result;
    }

    public float GetScalingBonus(TalismanData talisman)
    {
        int level = Stats != null ? Stats.GetLevel(talisman.scalingStat) : 1;
        return StatScaling.GetBonus(talisman.scalingRank, level);
    }

    // Pushes a multiplier further from 1 by the scaling bonus (never below 0).
    public static float Strengthen(float multiplier, float bonus) =>
        Mathf.Max(0f, 1f + (multiplier - 1f) * (1f + bonus));
}
