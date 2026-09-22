using UnityEngine;

public class BlockScript : MonoBehaviour
{
    [Header("Blocking")]
    [SerializeField, Range(0f, 1f)]
    private float blockDamageReduction = 0.6f;

    private bool isBlocking;
    private PlayerAnimation playerAnimation;

    public bool IsBlocking => isBlocking;
    

    private void Awake()
    {
        playerAnimation = GetComponent<PlayerAnimation>();
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

    public int GetBlockedDamage(int incomingDamage)
    {
        if (incomingDamage <= 0)
            return 0;

        float damageMultiplier = 1f - blockDamageReduction;

        int blockedDamage = Mathf.FloorToInt(
            incomingDamage * damageMultiplier
        );

        return Mathf.Max(0, blockedDamage);
    }
}
