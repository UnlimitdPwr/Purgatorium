using System;
using System.Collections;
using UnityEngine;

public class ParryScript : MonoBehaviour
{
    // =========================
    // CONFIG
    // =========================

    [Header("Parry Window")]
    [Tooltip("Seconds after TryParry() before the parry window opens. " +
             "Tuned for the ~0.42s HeroKnight_Block clip.")]
    [SerializeField] private float startDelay = 0.05f;

    [Tooltip("How long the parry window stays open, in seconds.")]
    [SerializeField] private float windowDuration = 0.30f;

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

    private bool windowOpen;
    private float nextParryTime;
    private Coroutine windowRoutine;

    public bool IsParryWindowOpen => windowOpen;
    public bool IsOnCooldown => Time.time < nextParryTime;

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

        nextParryTime = Time.time + cooldown;

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

        if (startDelay > 0f)
            yield return new WaitForSeconds(startDelay);

        windowOpen = true;

        yield return new WaitForSeconds(windowDuration);

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

        if (requireFacing && !IsFacingSource(attackSourcePosition))
            return false;

        OnParrySuccess?.Invoke(attacker);

        return true;
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
