using UnityEngine;

public class BlockScript : MonoBehaviour
{
    // These are the player's BASE block values. The active shield (PlayerShield)
    // multiplies them and adds Strength scaling; with no shield (or a broken one)
    // they're used as-is.
    [Header("Blocking (player base)")]
    [SerializeField, Range(0f, 1f)]
    private float blockDamageReduction = 0.6f;

    [Tooltip("Share of an attack's status buildup that blocking stops. " +
             "0 = blocking doesn't help, 1 = blocked hits add no buildup. (Parries always stop all of it.)")]
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
    

    private void Awake()
    {
        playerAnimation = GetComponent<PlayerAnimation>();
        status = GetComponent<PlayerStatusEffects>();

        shield = GetComponent<PlayerShield>();
        if (shield == null)
            shield = gameObject.AddComponent<PlayerShield>();

        talismans = PlayerTalismans.GetOrAdd(gameObject);
    }

    // =========================
    // BLOCKING
    // =========================

    public void StartBlocking()
    {
        if (isBlocking)
            return;

        isBlocking = true;
    }

    public void StopBlocking()
    {
        if (!isBlocking)
            return;

        isBlocking = false;
    }

    // =========================
    // BLOCK HIT
    // =========================

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

    // A status condition can weaken blocks (1 = full strength).
    float Effectiveness => status != null ? status.BlockEffectiveness : 1f;

    public int GetBlockedDamage(int incomingDamage)
    {
        if (incomingDamage <= 0)
            return 0;

        float damageMultiplier = 1f - DamageReduction * Effectiveness;

        // Round down, but with a tiny tolerance: float maths makes 1 - 0.6
        // come out as 0.39999998, which would floor 10 * 0.4 to 3 instead of 4.
        int blockedDamage = Mathf.FloorToInt(
            incomingDamage * damageMultiplier + 0.0001f
        );

        return Mathf.Max(0, blockedDamage);
    }

    public float GetBlockedStatusBuildup(float incomingBuildup)
    {
        if (incomingBuildup <= 0f)
            return 0f;

        return incomingBuildup * (1f - StatusReduction * Effectiveness);
    }
}
