
using System;
using UnityEngine;

public class BlockScript : MonoBehaviour
{
    // These are the player's BASE block values. The active shield (PlayerShield)
    // multiplies them and adds Strength scaling; with no shield (or a broken one)
    // they're used as-is.
    [Header("Blocking (player base)")]
    [SerializeField, Range(0f, 1f)]
    private float blockDamageReduction = 0.6f;

    [Tooltip("Share of an attack's status buildup that blocking stops.")]
    [SerializeField, Range(0f, 1f)]
    private float blockStatusReduction = 0.5f;

    private bool isBlocking;
    private PlayerAnimation playerAnimation;
    private PlayerStatusEffects status;
    private PlayerShield shield;
    private PlayerTalismans talismans;

    // Base value -> after the active shield's multipliers and stat scaling.
    // ...then x any talisman multiplier (capped at 100%).
    private float DamageReduction => Mathf.Clamp01(
        shield.GetBlockDamageReduction(blockDamageReduction) *
        talismans.GetMultiplier(TalismanEffect.BlockDamageReduction));
    private float StatusReduction => Mathf.Clamp01(
        shield.GetBlockStatusReduction(blockStatusReduction) *
        talismans.GetMultiplier(TalismanEffect.BlockStatusReduction));

    public bool IsBlocking => isBlocking;

    // These events notify other scripts when blocking starts and ends.
    public event Action OnBlockStarted;
    public event Action OnBlockEnded;

    private void Awake()
    {
        playerAnimation = GetComponent<PlayerAnimation>();
        status = GetComponent<PlayerStatusEffects>();

        shield = PlayerShield.GetOrAdd(gameObject);

        talismans = PlayerTalismans.GetOrAdd(gameObject);
    }

    // =========================
    // BLOCKING
    // =========================

    // Starts blocking and notifies the state machine.
    public void StartBlocking()
    {
        if (isBlocking)
            return;

        isBlocking = true;

        OnBlockStarted?.Invoke();
    }

    // Stops blocking and notifies the state machine.
    public void StopBlocking()
    {
        if (!isBlocking)
            return;

        isBlocking = false;

        OnBlockEnded?.Invoke();
    }

    // =========================
    // BLOCK HIT
    // =========================

    // Checks whether an incoming attack can be blocked.
    public bool TryBlockHit()
    {
        if (!isBlocking)
            return false;

        Debug.Log("Attack blocked!");

        // Blocking wears the shield's Soul Durability.
        shield.WearFromBlock();

        if (playerAnimation != null)
            playerAnimation.PlayBlockHitAnimation();

        return true;
    }

    // =========================
    // DAMAGE REDUCTION
    // =========================

    // Calculates how effective blocking is under current status effects.
    float Effectiveness => status != null ? status.BlockEffectiveness : 1f;

    // Calculates the damage remaining after blocking.
    public int GetBlockedDamage(int incomingDamage)
    {
        if (incomingDamage <= 0)
            return 0;

        float damageMultiplier = 1f - DamageReduction * Effectiveness;

        int blockedDamage = Mathf.FloorToInt(
            incomingDamage * damageMultiplier + 0.0001f
        );

        return Mathf.Max(0, blockedDamage);
    }

    // Calculates how much status buildup remains after blocking.
    public float GetBlockedStatusBuildup(float incomingBuildup)
    {
        if (incomingBuildup <= 0f)
            return 0f;

        return incomingBuildup * (1f - StatusReduction * Effectiveness);
    }
}

