
using System;
using System.Collections;
using UnityEngine;

public class ParryScript : MonoBehaviour
{
    // =========================
    // CONFIG
    // =========================

    [Header("Parry Window")]
    [Tooltip("Seconds after TryParry() before the parry window opens.")]
    [SerializeField] private float startDelay = 0.05f;

    [Tooltip("How long the parry window stays open, in seconds.")]
    [SerializeField] private float windowDuration = 0.55f;

    [Tooltip("Seconds from one TryParry() to the next.")]
    [SerializeField] private float cooldown = 0.80f;

    [Header("Parry Action")]
    [Tooltip("How long the player remains locked in the parry action. Match this to the parry animation length.")]
    [SerializeField] private float parryActionDuration = 0.42f;

    [Header("Driving Mode")]
    [Tooltip("ON = animation events control the parry window. OFF = the timer controls it.")]
    [SerializeField] private bool useAnimationEvents = false;

    [Header("Rules")]
    [Tooltip("When ON, an attack coming from behind the player cannot be parried.")]
    [SerializeField] private bool requireFacing = true;

    // =========================
    // REFERENCES
    // =========================

    private PlayerAnimation playerAnimation;
    private SpriteRenderer spriteRenderer;
    private PlayerStatusEffects status;

    // =========================
    // STATE
    // =========================

    private bool windowOpen;
    private float nextParryTime;

    private Coroutine windowRoutine;
    private Coroutine parryRoutine;

    public bool IsParryWindowOpen => windowOpen;
    public bool IsOnCooldown => Time.time < nextParryTime;

    // Indicates whether the complete parry action is currently playing.
    public bool IsParrying { get; private set; }

    // Fired when the player begins the parry action.
    public event Action OnParryStarted;

    // Fired when the player finishes the parry action.
    public event Action OnParryEnded;

    // Fired once when an incoming attack is successfully parried.
    public event Action<GameObject> OnParrySuccess;

    // =========================
    // SETUP
    // =========================

    // Gets the components required by the parry system.
    private void Awake()
    {
        playerAnimation = GetComponent<PlayerAnimation>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        status = GetComponent<PlayerStatusEffects>();
    }

    // Resets the parry state if the player is disabled during a parry.
    private void OnDisable()
    {
        windowOpen = false;
        windowRoutine = null;
        parryRoutine = null;

        if (IsParrying)
        {
            IsParrying = false;
            OnParryEnded?.Invoke();
        }
    }

    // =========================
    // INPUT ENTRY POINT
    // =========================

    // Starts the parry action, plays its animation and starts the parry window.
    public void TryParry()
    {
        if (IsOnCooldown || IsParrying)
            return;

        nextParryTime = Time.time + cooldown;

        // Mark the complete parry action as active.
        IsParrying = true;

        // Notify the state machine that parrying has started.
        OnParryStarted?.Invoke();

        if (playerAnimation != null)
            playerAnimation.PlayParryAnimation();

        // Start the complete parry action timer.
        if (parryRoutine != null)
            StopCoroutine(parryRoutine);

        parryRoutine = StartCoroutine(ParryActionRoutine());

        // In animation-event mode, animation events control the parry window.
        if (useAnimationEvents)
            return;

        if (windowRoutine != null)
            StopCoroutine(windowRoutine);

        windowRoutine = StartCoroutine(TimedParryWindow());
    }

    // Keeps the player in the parrying state for the configured animation duration.
    private IEnumerator ParryActionRoutine()
    {
        yield return new WaitForSeconds(parryActionDuration);

        IsParrying = false;
        parryRoutine = null;

        // Notify the state machine that parrying has ended.
        OnParryEnded?.Invoke();
    }

    // Controls when the player can successfully parry an incoming attack.
    private IEnumerator TimedParryWindow()
    {
        windowOpen = false;

        if (startDelay > 0f)
            yield return new WaitForSeconds(startDelay);

        windowOpen = true;

        float windowMultiplier = status != null
            ? status.ParryWindowMultiplier
            : 1f;

        yield return new WaitForSeconds(windowDuration * windowMultiplier);

        windowOpen = false;
        windowRoutine = null;
    }

    // =========================
    // ANIMATION EVENT HOOKS
    // =========================

    // Opens the parry window when called by an animation event.
    public void OpenParryWindow()
    {
        windowOpen = true;
    }

    // Closes the parry window when called by an animation event.
    public void CloseParryWindow()
    {
        windowOpen = false;
    }

    // =========================
    // PARRY RESOLUTION
    // =========================

    // Checks whether an incoming attack can be parried and notifies listeners on success.
    public bool TryParryAttack(Vector2 attackSourcePosition, GameObject attacker)
    {
        if (!windowOpen)
            return false;

        if (requireFacing && !IsFacingSource(attackSourcePosition))
            return false;

        OnParrySuccess?.Invoke(attacker);

        return true;
    }

    // Checks whether the attacker is in front of the player.
    private bool IsFacingSource(Vector2 attackSourcePosition)
    {
        float toSource = attackSourcePosition.x - transform.position.x;

        if (Mathf.Abs(toSource) < 0.01f)
            return true;

        float facingDir = (spriteRenderer != null && spriteRenderer.flipX)
            ? -1f
            : 1f;

        return Mathf.Sign(toSource) == facingDir;
    }
}

