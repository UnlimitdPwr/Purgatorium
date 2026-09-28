using UnityEngine;

public class EnemyAnimation : MonoBehaviour
{
    private Animator animator;
    private EnemyMovement movement;
    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        animator = GetComponent<Animator>();
        movement = GetComponent<EnemyMovement>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        UpdateMovementAnimation();
        UpdateFacingDirection();
    }

    void UpdateMovementAnimation()
    {
        float speed = Mathf.Abs(movement.GetMoveDirection());

        animator.SetFloat("Speed", speed);
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
}
