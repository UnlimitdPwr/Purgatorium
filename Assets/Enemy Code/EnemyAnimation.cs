using System.Collections.Generic;
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

    private Color baseColor = Color.white;
    private float flashTimer;

    // Names of the parameters the Animator actually has.
    private readonly HashSet<string> parameters = new HashSet<string>();

    void Awake()
    {
        animator = GetComponent<Animator>();
        movement = GetComponent<EnemyMovement>();
        spriteRenderer = GetComponent<SpriteRenderer>();

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
            flashTimer = duration;
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
        spriteRenderer.color = Color.Lerp(baseColor, windUpTint, pulse);
    }

    void SetOptionalTrigger(string name)
    {
        if (parameters.Contains(name))
            animator.SetTrigger(name);
    }
}
