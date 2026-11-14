using System;
using System.Collections;
using UnityEngine;

public class ParryScript : MonoBehaviour
{
    // =========================
    // CONFIG
    // =========================

    // Every value in this block is the NO-SHIELD fallback. While a ShieldData is
    // the active shield (EquipmentManager.ActiveShield), its own parry values are
    // used instead, and a successful parry also applies its statusOnParry.

    [Header("Parry Window (no shield equipped)")]
    [Tooltip("Seconds after TryParry() before the parry window opens. " +
             "Tuned for the ~0.42s HeroKnight_Block clip.")]
    [SerializeField] private float startDelay = 0.05f;

    [Tooltip("How long the parry window stays open, in seconds.")]
    [SerializeField] private float windowDuration = 0.55f;

    [Tooltip("Seconds from one TryParry() to the next. Keep this above the " +
             "clip length so parry can't be spammed.")]
    [SerializeField] private float cooldown = 0.80f;

    [Header("Driving Mode")]
    [Tooltip("ON  = the window is opened/closed only by animation events calling " +
             "OpenParryWindow() / CloseParryWindow() on the parry clip.\n" +
             "OFF = the window is driven by the startDelay / windowDuration timer.")]
    [SerializeField] private bool useAnimationEvents = false;

    [Header("Rules")]
    [Tooltip("When ON, an attack coming from behind the player cannot be parried.")]
    [SerializeField] private bool requireFacing = true;

    // =========================
    // STATE
    // =========================

    private PlayerAnimation playerAnimation;
    private SpriteRenderer spriteRenderer;
    private PlayerStatusEffects status;
    private EquipmentManager equipment;

    private bool windowOpen;
    private float nextParryTime;
    private Coroutine windowRoutine;

    public bool IsParryWindowOpen => windowOpen;
    public bool IsOnCooldown => Time.time < nextParryTime;

    // The active shield's blueprint, or null when none (or a plain ItemData) is equipped.
    public ShieldData ActiveShield => equipment != null ? equipment.ActiveShield as ShieldData : null;

    private float StartDelay => ActiveShield != null ? ActiveShield.parryStartDelay : startDelay;
    private float WindowDuration => ActiveShield != null ? ActiveShield.parryWindowDuration : windowDuration;
    private float Cooldown => ActiveShield != null ? ActiveShield.parryCooldown : cooldown;
    private bool RequireFacing => ActiveShield != null ? ActiveShield.parryRequiresFacing : requireFacing;

    // Fired once when an incoming attack is successfully parried. The argument is
    // the attacker's root GameObject (may be null). Hook VFX / audio here without
    // editing this script.
    public event Action<GameObject> OnParrySuccess;

    // =========================
    // SETUP
    // =========================

    void Awake()
    {
        playerAnimation = GetComponent<PlayerAnimation>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        status = GetComponent<PlayerStatusEffects>();
        equipment = GetComponent<EquipmentManager>();
    }

    void OnDisable()
    {
        // Don't leave the window stuck open if the player is disabled mid-parry.
        windowOpen = false;
        windowRoutine = null;
    }

    // =========================
    // INPUT ENTRY POINT
    // =========================

    // Called by PlayerController1 when the parry button is pressed.
    public void TryParry()
    {
        if (IsOnCooldown)
            return;

        nextParryTime = Time.time + Cooldown;

        if (playerAnimation != null)
            playerAnimation.PlayParryAnimation();

        // In animation-event mode the clip's events own the window.
        if (useAnimationEvents)
            return;

        if (windowRoutine != null)
            StopCoroutine(windowRoutine);

        windowRoutine = StartCoroutine(TimedParryWindow());
    }

    private IEnumerator TimedParryWindow()
    {
        windowOpen = false;

        float delay = StartDelay;

        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        windowOpen = true;

        // A status condition can shorten the window.
        float windowMultiplier = status != null ? status.ParryWindowMultiplier : 1f;

        yield return new WaitForSeconds(WindowDuration * windowMultiplier);

        windowOpen = false;
        windowRoutine = null;
    }

    // =========================
    // ANIMATION EVENT HOOKS
    // =========================

    // Wire these to animation events on the parry clip when useAnimationEvents is ON.
    public void OpenParryWindow()
    {
        windowOpen = true;
    }

    public void CloseParryWindow()
    {
        windowOpen = false;
    }

    // =========================
    // PARRY RESOLUTION
    // =========================

    // Called by EnemyHitbox before it runs any damage logic. Returns true when
    // the attack is parried, in which case the caller must cancel its damage.
    public bool TryParryAttack(Vector2 attackSourcePosition, GameObject attacker)
    {
        if (!windowOpen)
            return false;

        if (RequireFacing && !IsFacingSource(attackSourcePosition))
            return false;

        ApplyStatusOnParry(attacker);

        OnParrySuccess?.Invoke(attacker);

        return true;
    }

    // The active shield's "parrying puts X of status Z on the attacker". The
    // attacker's own resistances (and locked meters) apply as for any buildup.
    private void ApplyStatusOnParry(GameObject attacker)
    {
        ShieldData shield = ActiveShield;

        if (shield == null || shield.statusOnParry == null || attacker == null)
            return;

        IStatusReceiver receiver = attacker.GetComponentInParent<IStatusReceiver>();

        if (receiver == null)
            return;

        foreach (StatusInfliction infliction in shield.statusOnParry)
        {
            if (infliction != null && infliction.buildup > 0f)
                receiver.AddBuildup(infliction.status, infliction.buildup);
        }
    }

    private bool IsFacingSource(Vector2 attackSourcePosition)
    {
        float toSource = attackSourcePosition.x - transform.position.x;

        // Attacker is basically on top of the player — allow the parry.
        if (Mathf.Abs(toSource) < 0.01f)
            return true;

        // PlayerAnimation sets flipX = true when the player faces left.
        float facingDir = (spriteRenderer != null && spriteRenderer.flipX) ? -1f : 1f;

        return Mathf.Sign(toSource) == facingDir;
    }
}
