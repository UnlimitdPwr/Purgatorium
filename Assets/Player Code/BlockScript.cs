using UnityEngine;

public class BlockScript : MonoBehaviour
{
    // Both values below are the NO-SHIELD fallback. While a ShieldData is the
    // active shield (EquipmentManager.ActiveShield), its block values are used instead.
    [Header("Blocking (no shield equipped)")]
    [SerializeField, Range(0f, 1f)]
    private float blockDamageReduction = 0.6f;

    [Tooltip("Share of an attack's status buildup that blocking stops. " +
             "0 = blocking doesn't help, 1 = blocked hits add no buildup. (Parries always stop all of it.)")]
    [SerializeField, Range(0f, 1f)]
    private float blockStatusReduction = 0.5f;

    private bool isBlocking;
    private PlayerAnimation playerAnimation;
    private PlayerStatusEffects status;
    private EquipmentManager equipment;

    private ShieldData ActiveShield => equipment != null ? equipment.ActiveShield as ShieldData : null;
    private float DamageReduction => ActiveShield != null ? ActiveShield.blockDamageReduction : blockDamageReduction;
    private float StatusReduction => ActiveShield != null ? ActiveShield.blockStatusReduction : blockStatusReduction;

    public bool IsBlocking => isBlocking;
    

    private void Awake()
    {
        playerAnimation = GetComponent<PlayerAnimation>();
        status = GetComponent<PlayerStatusEffects>();
        equipment = GetComponent<EquipmentManager>();
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
