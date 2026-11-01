using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class EnemyAnimation : MonoBehaviour
{
    // =========================
    // OPTIONAL ANIMATOR PARAMETERS
    // =========================
    // Hooks for animations that haven't been made yet. Each one does nothing
    // until a parameter with that exact name (and type) is added to the
    // enemy's Animator, then starts working with no code change.

    private const string JumpTrigger = "Jump";
    private const string RetreatTrigger = "Retreat";
    private const string JumpAttackWindUpTrigger = "JumpAttackWindUp";
    private const string JumpAttackTrigger = "JumpAttack";
    private const string NoticedTrigger = "Noticed";
    private const string ParriedTrigger = "Parried";

    // Animator state played when the enemy's attack is parried, if the
    // Animator has it (Tools > Enemy Attack > Add Animator States For Attacks
    // adds it). Without it, the optional "Parried" trigger fires instead.
    public const string ParriedStateName = "TakeHit";

    [Header("Noticed")]
    [Tooltip("Show a \"!\" over the enemy's head while it's in its noticed pause " +
             "(just spotted the player). Works with or without a noticed animation.")]
    public bool showNoticedMark = true;

    public Color noticedMarkColor = new Color(1f, 0.85f, 0.2f);

    [Tooltip("Height of the \"!\" text.")]
    public float noticedMarkSize = 6f;

    [Header("Jump Attack Wind-up")]
    [Tooltip("Pulse the sprite this colour while winding up a leap attack, so the player " +
             "can see it coming. Works with or without a wind-up animation.")]
    public bool flashDuringWindUp = true;

    public Color windUpTint = new Color(1f, 0.45f, 0.45f);

    [Tooltip("Pulses per second of the wind-up flash.")]
    public float windUpFlashRate = 8f;
    private const string GroundedBool = "IsGrounded";
    private const string VerticalVelocityFloat = "VerticalVelocity";

    private Animator animator;
    private EnemyMovement movement;
    private SpriteRenderer spriteRenderer;
    private EnemyController controller;

    private TextMeshPro noticedMark;

    private Color baseColor = Color.white;
    private float flashTimer;
    private Color flashTint;

    // Names of the parameters the Animator actually has.
    private readonly HashSet<string> parameters = new HashSet<string>();

    void Awake()
    {
        animator = GetComponent<Animator>();
        movement = GetComponent<EnemyMovement>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        controller = GetComponent<EnemyController>();

        if (spriteRenderer != null)
            baseColor = spriteRenderer.color;

        if (animator != null)
        {
            foreach (AnimatorControllerParameter parameter in animator.parameters)
                parameters.Add(parameter.name);
        }
    }

    void Update()
    {
        UpdateMovementAnimation();
        UpdateAirborneAnimation();
        UpdateFacingDirection();
        UpdateWindUpFlash();
        UpdateNoticedMark();
    }

    void OnEnable()
    {
        if (controller != null)
            controller.OnStateChanged += HandleStateChanged;
    }

    void OnDisable()
    {
        if (controller != null)
            controller.OnStateChanged -= HandleStateChanged;
    }

    void HandleStateChanged(EnemyState previous, EnemyState next)
    {
        // Just spotted the player — fires "Noticed" once that exists.
        if (next == EnemyState.Noticed)
            SetOptionalTrigger(NoticedTrigger);

        if (next == EnemyState.Parried)
            PlayParried();
    }

    void UpdateMovementAnimation()
    {
        float speed = Mathf.Abs(movement.GetMoveDirection());

        animator.SetFloat("Speed", speed);
    }

    // Feeds a jump/fall/land setup once the devs add these parameters
    // (same names as on the player's Animator).
    void UpdateAirborneAnimation()
    {
        if (parameters.Contains(GroundedBool))
            animator.SetBool(GroundedBool, movement.IsGrounded() && !movement.IsLaunched);

        if (parameters.Contains(VerticalVelocityFloat))
            animator.SetFloat(VerticalVelocityFloat, movement.GetVerticalVelocity());
    }

    void UpdateFacingDirection()
    {
        // The sprite art faces right; flip it when the enemy faces left.
        spriteRenderer.flipX = movement.GetFacingDirection() < 0f;
    }

    public void PlayAttack()
    {
        animator.SetTrigger("Attack");
    }

    // Plays an attack's own swing: straight into the Animator state named
    // after the clip, or the plain "Attack" trigger if there's no such state.
    public void PlayAttack(AnimationClip clip)
    {
        if (!TryPlayState(clip != null ? clip.name : null))
            PlayAttack();
    }

    // Telegraphs an attack: pulses the sprite `tint` for `duration` seconds.
    public void PlayAttackWindUp(float duration, Color tint)
    {
        flashTint = tint;
        flashTimer = duration;
    }

    // The enemy's attack was just parried.
    public void PlayParried()
    {
        StopWindUpFlash();

        if (!TryPlayState(ParriedStateName))
            SetOptionalTrigger(ParriedTrigger);
    }

    // No jump animation yet — fires the "Jump" trigger once one exists.
    public void PlayJump()
    {
        SetOptionalTrigger(JumpTrigger);
    }

    // Hop backwards after a combo — fires "Retreat" once that exists.
    public void PlayRetreat()
    {
        SetOptionalTrigger(RetreatTrigger);
    }

    // Leap attack wind-up: fires "JumpAttackWindUp" once that exists, and
    // flashes the sprite for `duration` seconds either way.
    public void PlayJumpAttackWindUp(float duration)
    {
        SetOptionalTrigger(JumpAttackWindUpTrigger);

        if (flashDuringWindUp)
            PlayAttackWindUp(duration, windUpTint);
    }

    // The leap itself — fires "JumpAttack" once that exists.
    public void PlayJumpAttack()
    {
        StopWindUpFlash();
        SetOptionalTrigger(JumpAttackTrigger);
    }

    public void StopWindUpFlash()
    {
        flashTimer = 0f;

        if (spriteRenderer != null)
            spriteRenderer.color = baseColor;
    }

    // Length in seconds of a clip on this enemy's Animator, or 0 if missing.
    public float GetClipLength(string clipName)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
            return 0f;

        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == clipName)
                return clip.length;
        }

        return 0f;
    }

    // =========================
    // INTERNAL
    // =========================

    // The "!" follows the enemy's head while it's in its noticed pause.
    void UpdateNoticedMark()
    {
        bool show = showNoticedMark && controller != null && controller.State == EnemyState.Noticed;

        if (show && noticedMark == null)
            noticedMark = CreateNoticedMark();

        if (noticedMark == null)
            return;

        noticedMark.gameObject.SetActive(show);

        if (!show)
            return;

        Collider2D body = movement != null ? movement.GetBody() : null;
        Vector3 top = body != null
            ? new Vector3(body.bounds.center.x, body.bounds.max.y + 0.4f, 0f)
            : transform.position + Vector3.up;

        noticedMark.transform.position = top;
    }

    TextMeshPro CreateNoticedMark()
    {
        TMP_FontAsset font = TMP_Settings.defaultFontAsset;

        // No TextMesh Pro font in the project — just skip the mark.
        if (font == null)
        {
            showNoticedMark = false;
            return null;
        }

        GameObject go = new GameObject("NoticedMark");
        go.transform.SetParent(transform, false);

        // Undo the enemy's scale so the mark is the same size on any enemy.
        Vector3 scale = transform.lossyScale;
        go.transform.localScale = new Vector3(
            1f / Mathf.Max(0.0001f, Mathf.Abs(scale.x)),
            1f / Mathf.Max(0.0001f, Mathf.Abs(scale.y)),
            1f);

        TextMeshPro mark = go.AddComponent<TextMeshPro>();
        mark.font = font;
        mark.text = "!";
        mark.fontSize = noticedMarkSize;
        mark.fontStyle = FontStyles.Bold;
        mark.color = noticedMarkColor;
        mark.alignment = TextAlignmentOptions.Center;
        mark.sortingOrder = spriteRenderer != null ? spriteRenderer.sortingOrder + 10 : 100;

        return mark;
    }

    void UpdateWindUpFlash()
    {
        if (flashTimer <= 0f || spriteRenderer == null)
            return;

        flashTimer -= Time.deltaTime;

        if (flashTimer <= 0f)
        {
            StopWindUpFlash();
            return;
        }

        float pulse = Mathf.PingPong(Time.time * windUpFlashRate * 2f, 1f);
        spriteRenderer.color = Color.Lerp(baseColor, flashTint, pulse);
    }

    // Jumps straight to a base-layer state, if the Animator has one by that name.
    bool TryPlayState(string stateName)
    {
        if (animator == null || string.IsNullOrEmpty(stateName))
            return false;

        int hash = Animator.StringToHash(stateName);

        if (!animator.HasState(0, hash))
            return false;

        animator.Play(hash, 0, 0f);
        return true;
    }

    void SetOptionalTrigger(string name)
    {
        if (parameters.Contains(name))
            animator.SetTrigger(name);
    }
}
