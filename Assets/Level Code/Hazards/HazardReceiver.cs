using System.Collections.Generic;
using UnityEngine;

// Lets a character (player or enemy) be affected by environmental hazards.
// Tracks which hazards it's standing in, and applies a hazard's tick to
// whichever health / status scripts the character has.
// MovementScript and EnemyMovement read SpeedMultiplier.
public class HazardReceiver : MonoBehaviour
{
    private PlayerHealth playerHealth;
    private PlayerDeath playerDeath;
    private EnemyHealth enemyHealth;
    private IStatusReceiver status;

    private readonly List<EnvironmentalHazard> hazardsInside = new List<EnvironmentalHazard>();

    public bool IsPlayer => playerHealth != null;

    public bool IsInHazard => hazardsInside.Count > 0;

    // Slowest multiplier of every hazard the character is in; 1 outside.
    public float SpeedMultiplier
    {
        get
        {
            float multiplier = 1f;
            bool any = false;

            foreach (EnvironmentalHazard hazard in hazardsInside)
            {
                if (hazard == null || hazard.Type == null)
                    continue;

                multiplier = any ? Mathf.Min(multiplier, hazard.Type.speedMultiplier) : hazard.Type.speedMultiplier;
                any = true;
            }

            return multiplier;
        }
    }

    void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        playerDeath = GetComponent<PlayerDeath>();
        enemyHealth = GetComponent<EnemyHealth>();
        status = GetComponent<IStatusReceiver>();
    }

    // =========================
    // ENTER / EXIT (called by EnvironmentalHazard)
    // =========================

    public void EnterHazard(EnvironmentalHazard hazard)
    {
        if (!hazardsInside.Contains(hazard))
            hazardsInside.Add(hazard);
    }

    public void ExitHazard(EnvironmentalHazard hazard)
    {
        hazardsInside.Remove(hazard);
    }

    // =========================
    // TICK
    // =========================

    public void ApplyHazardTick(HazardType type)
    {
        if (IsDead())
            return;

        if (playerHealth != null)
            playerHealth.TakeDamage(type.playerDamagePerTick, false);
        else if (enemyHealth != null)
            enemyHealth.ApplyHazardDamage(type.enemyDamagePerTick);

        if (status == null)
            return;

        foreach (StatusInfliction infliction in type.buildupPerTick)
            status.AddBuildup(infliction.status, infliction.buildup);
    }

    bool IsDead()
    {
        if (playerDeath != null && playerDeath.IsDead)
            return true;

        return enemyHealth != null && enemyHealth.IsDead;
    }
}
