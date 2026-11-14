using System;
using System.Collections.Generic;
using UnityEngine;

// Turns the active ShieldData into the numbers ParryScript and BlockScript use,
// and tracks each shield's Soul Durability (its health bar).
//
// ParryScript / BlockScript keep the player's BASE values and pass them in;
// this applies the shield's multipliers and stat scaling on top. With no
// shield equipped, or a broken one, the base value comes back unchanged.
//
// Lives on the Player next to EquipmentManager and PlayerStats. ParryScript
// and BlockScript add it automatically if it's missing.
public class PlayerShield : MonoBehaviour
{
    private EquipmentManager equipment;
    private PlayerStats stats;

    // Wear = durability lost so far, per shield asset (each shield is a single
    // item, so the asset identifies it). Stored as wear rather than "current"
    // so levelling Soul Durability raises current durability along with the max.
    private readonly Dictionary<ShieldData, float> wear = new Dictionary<ShieldData, float>();

    // Fired whenever a shield's durability changes (for a future durability bar).
    public event Action<ShieldData> OnDurabilityChanged;

    // Fired once when a shield's durability reaches 0.
    public event Action<ShieldData> OnShieldBroken;

    public static PlayerShield GetOrAdd(GameObject player)
    {
        PlayerShield shield = player.GetComponent<PlayerShield>();
        return shield != null ? shield : player.AddComponent<PlayerShield>();
    }

    // References are fetched lazily, so this works whatever order components
    // were added / woke up in.
    private EquipmentManager Equipment => equipment != null ? equipment : (equipment = GetComponent<EquipmentManager>());
    private PlayerStats Stats => stats != null ? stats : (stats = GetComponent<PlayerStats>());

    // =========================
    // WHICH SHIELD
    // =========================

    // The equipped active shield, broken or not (null if none / not a ShieldData).
    public ShieldData Equipped => Equipment != null ? Equipment.ActiveShield as ShieldData : null;

    // The shield that's actually doing anything: equipped and not broken.
    public ShieldData Working
    {
        get
        {
            ShieldData shield = Equipped;
            return shield != null && !IsBroken(shield) ? shield : null;
        }
    }

    // =========================
    // STAT SCALING
    // =========================

    private int StrengthLevel => Stats != null ? Stats.StrengthLevel : 1;
    private int DexterityLevel => Stats != null ? Stats.DexterityLevel : 1;
    private int SoulDurabilityLevel => Stats != null ? Stats.SoulDurabilityLevel : 1;

    public float StrengthBonus(ShieldData shield) =>
        StatScaling.GetBonus(shield.strengthScaling, StrengthLevel);

    public float DexterityBonus(ShieldData shield) =>
        StatScaling.GetBonus(shield.dexterityScaling, DexterityLevel);

    public float SoulDurabilityBonus(ShieldData shield) =>
        StatScaling.GetBonus(shield.soulDurabilityScaling, SoulDurabilityLevel);

    // =========================
    // PARRY VALUES
    // =========================

    public float GetParryWindow(float baseValue)
    {
        ShieldData shield = Working;
        return shield == null ? baseValue
            : baseValue * shield.parryWindowMultiplier * (1f + DexterityBonus(shield));
    }

    public float GetParryStartDelay(float baseValue)
    {
        ShieldData shield = Working;
        return shield == null ? baseValue
            : baseValue * shield.parryStartDelayMultiplier / (1f + DexterityBonus(shield));
    }

    public float GetParryCooldown(float baseValue)
    {
        ShieldData shield = Working;
        return shield == null ? baseValue
            : baseValue * shield.parryCooldownMultiplier / (1f + DexterityBonus(shield));
    }

    public bool GetParryRequiresFacing(bool baseValue)
    {
        ShieldData shield = Working;
        return baseValue && (shield == null || !shield.canParryFromBehind);
    }

    // =========================
    // BLOCK VALUES
    // =========================

    public float GetBlockDamageReduction(float baseValue)
    {
        ShieldData shield = Working;
        return shield == null ? baseValue
            : Mathf.Clamp01(baseValue * shield.blockDamageMultiplier * (1f + StrengthBonus(shield)));
    }

    public float GetBlockStatusReduction(float baseValue)
    {
        ShieldData shield = Working;
        return shield == null ? baseValue
            : Mathf.Clamp01(baseValue * shield.blockStatusMultiplier * (1f + StrengthBonus(shield)));
    }

    // =========================
    // STATUS ON PARRY
    // =========================

    // Soul Durability scaling boosts it; wear weakens it (x statusMultiplierWhenWorn near 0%).
    public float GetStatusOnParryMultiplier(ShieldData shield)
    {
        if (shield == null || IsBroken(shield))
            return 0f;

        float wornFactor = Mathf.Lerp(shield.statusMultiplierWhenWorn, 1f, GetDurabilityFraction(shield));
        return (1f + SoulDurabilityBonus(shield)) * wornFactor;
    }

    // Called by ParryScript on a successful parry.
    public void ApplyStatusOnParry(GameObject attacker)
    {
        ShieldData shield = Working;

        if (shield == null || shield.statusOnParry == null || attacker == null)
            return;

        IStatusReceiver receiver = attacker.GetComponentInParent<IStatusReceiver>();

        if (receiver == null)
            return;

        float multiplier = GetStatusOnParryMultiplier(shield);

        foreach (StatusInfliction infliction in shield.statusOnParry)
        {
            if (infliction == null)
                continue;

            float buildup = infliction.buildup * multiplier;

            if (buildup > 0f)
                receiver.AddBuildup(infliction.status, buildup);
        }
    }

    // =========================
    // SOUL DURABILITY
    // =========================

    public float GetMaxDurability(ShieldData shield) =>
        shield.baseDurability * (1f + SoulDurabilityBonus(shield));

    public float GetDurability(ShieldData shield)
    {
        wear.TryGetValue(shield, out float worn);
        return Mathf.Max(0f, GetMaxDurability(shield) - worn);
    }

    public float GetDurabilityFraction(ShieldData shield)
    {
        float max = GetMaxDurability(shield);
        return max > 0f ? GetDurability(shield) / max : 0f;
    }

    public bool IsBroken(ShieldData shield) => GetDurability(shield) <= 0f;

    // Called by BlockScript / ParryScript when the working shield is used.
    public void WearFromBlock() => Wear(Working, Working != null ? Working.durabilityLossPerBlock : 0f);
    public void WearFromParry() => Wear(Working, Working != null ? Working.durabilityLossPerParry : 0f);

    private void Wear(ShieldData shield, float amount)
    {
        if (shield == null || amount <= 0f)
            return;

        amount *= PlayerTalismans.GetOrAdd(gameObject).GetMultiplier(TalismanEffect.ShieldDurabilityLoss);

        if (amount <= 0f)
            return;

        wear.TryGetValue(shield, out float worn);
        wear[shield] = worn + amount;

        OnDurabilityChanged?.Invoke(shield);

        if (IsBroken(shield))
        {
            Debug.Log("Shield broken: " + shield.itemName);
            OnShieldBroken?.Invoke(shield);
        }
    }

    // For saving: every shield that has taken wear, and how much.
    public IReadOnlyDictionary<ShieldData, float> WearByShield => wear;

    // For loading: replaces all wear with saved values.
    public void LoadWear(IDictionary<ShieldData, float> saved)
    {
        wear.Clear();

        foreach (KeyValuePair<ShieldData, float> pair in saved)
        {
            if (pair.Key != null && pair.Value > 0f)
                wear[pair.Key] = pair.Value;
        }

        OnDurabilityChanged?.Invoke(Equipped);
    }

    // Hook for the future repair feature: restores a shield to full durability.
    public void Repair(ShieldData shield)
    {
        if (shield == null || !wear.Remove(shield))
            return;

        OnDurabilityChanged?.Invoke(shield);
    }
}
