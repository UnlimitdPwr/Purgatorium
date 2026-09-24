using System;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Thresholds")]
    [Tooltip("The primary bar kills the enemy once it reaches this value.")]
    [SerializeField] private float primaryThreshold = 100f;

    [Tooltip("The secondary bar kills the enemy once it reaches this value.")]
    [SerializeField] private float secondaryThreshold = 100f;

    [Header("Parry")]
    [Tooltip("Added to the primary bar on a successful parry.")]
    [SerializeField] private float parryPrimaryAmount = 10f;

    [Tooltip("Added to the secondary bar on a successful parry. Should exceed " +
             "parryPrimaryAmount — parries are meant to fill the secondary bar faster.")]
    [SerializeField] private float parrySecondaryAmount = 20f;

    [Header("Status Effects")]
    [Tooltip("Added to the primary bar by a status effect. Nothing calls " +
             "ApplyStatusEffect() yet — this is here for that future system.")]
    [SerializeField] private float statusEffectAmount = 10f;

    // Both bars start empty and fill up, rather than starting full and draining.
    private float primaryValue;
    private float secondaryValue;
    private bool isDead;

    public float PrimaryValue => primaryValue;
    public float SecondaryValue => secondaryValue;
    public float PrimaryThreshold => primaryThreshold;
    public float SecondaryThreshold => secondaryThreshold;
    public bool IsDead => isDead;

    // (current, threshold) for each bar — EnemyHealthBar hooks into these.
    public event Action<float, float> OnPrimaryChanged;
    public event Action<float, float> OnSecondaryChanged;

    // Fired once, the instant either bar fills.
    public event Action OnDeath;

    // =========================
    // PARRY
    // =========================

    // Called by EnemyHitbox on a successful parry against this enemy.
    public void ApplyParry()
    {
        AddPrimary(parryPrimaryAmount);
        AddSecondary(parrySecondaryAmount);
    }

    // =========================
    // STATUS EFFECTS (future)
    // =========================

    // Not called anywhere yet — the hook a future status-effect system uses.
    public void ApplyStatusEffect()
    {
        AddPrimary(statusEffectAmount);
    }

    // =========================
    // INTERNAL
    // =========================

    private void AddPrimary(float amount)
    {
        if (isDead || amount <= 0f)
            return;

        primaryValue = Mathf.Clamp(primaryValue + amount, 0f, primaryThreshold);
        OnPrimaryChanged?.Invoke(primaryValue, primaryThreshold);

        CheckForDeath();
    }

    private void AddSecondary(float amount)
    {
        if (isDead || amount <= 0f)
            return;

        secondaryValue = Mathf.Clamp(secondaryValue + amount, 0f, secondaryThreshold);
        OnSecondaryChanged?.Invoke(secondaryValue, secondaryThreshold);

        CheckForDeath();
    }

    private void CheckForDeath()
    {
        if (isDead)
            return;

        if (primaryValue >= primaryThreshold || secondaryValue >= secondaryThreshold)
        {
            isDead = true;
            OnDeath?.Invoke();
        }
    }
}
