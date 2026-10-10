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
    private const string GroundedBool = "IsGrounded";
    private const string VerticalVelocityFloat = "VerticalVelocity";

    private Animator animator;
    private EnemyMovement movement;
    private SpriteRenderer spriteRenderer;

    // Names of the parameters the Animator actually has.
    private readonly HashSet<string> parameters = new HashSet<string>();

    void Awake()
    {
        animator = GetComponent<Animator>();
        movement = GetComponent<EnemyMovement>();
        spriteRenderer = GetComponent<SpriteRenderer>();

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

    void SetOptionalTrigger(string name)
    {
        if (parameters.Contains(name))
            animator.SetTrigger(name);
    }
}
