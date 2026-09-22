using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    private Animator animator;
    private MovementScript movement;
    private SpriteRenderer spriteRenderer;
    private BlockScript block;

    private bool isDead;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        animator = GetComponent<Animator>();
        movement = GetComponent<MovementScript>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        block = GetComponent<BlockScript>();
    }

    // Update is called once per frame
    void Update()
    {
        if (isDead)
            return;

        UpdateMovementAnimation();
        UpdateJumpAnimation();
        UpdateFacingDirection();
        UpdateBlockAnimation();
    }

    // =========================
    // MOVEMENT ANIMATION
    // =========================

    void UpdateMovementAnimation()
    {
        float moveInput = movement.GetMoveInput();

        animator.SetFloat("Speed", Mathf.Abs(moveInput));
    }


    // =========================
    // JUMP ANIMATION
    // =========================

    void UpdateJumpAnimation()
    {
        float verticalVelocity = movement.GetVerticalVelocity();
        bool isGrounded = movement.IsGrounded();

        animator.SetFloat("VerticalVelocity", verticalVelocity);
        animator.SetBool("IsGrounded", isGrounded);
    }

    // =========================
    // FACING DIRECTION
    // =========================

    void UpdateFacingDirection()
    {
        float moveInput = movement.GetMoveInput();

        if (moveInput > 0)
            spriteRenderer.flipX = false;

        if (moveInput < 0)
            spriteRenderer.flipX = true;
    }

    // =========================
    // PARRY ANIMATION
    // =========================

    public void PlayParryAnimation()
    {
        animator.SetTrigger("Parry");
    }

    void UpdateBlockAnimation()
    {
        if (block == null)
            return;

        animator.SetBool("IsBlocking", block.IsBlocking);
    }

    // =========================
    // BLOCK ANIMATION
    // =========================

    public void PlayBlockHitAnimation()
    {
        Debug.Log("PLAYING BLOCK HIT ANIMATION");

        animator.Play("HeroKnight_BlockNoEffect", 0, 0f);
    }


    // =========================
    // DASH ANIMATION
    // =========================

    // No-op until a "Dash" trigger exists on the Animator Controller — SetTrigger
    // on an undefined parameter is safely ignored by Unity.
    public void PlayDashAnimation()
    {
        animator.SetTrigger("Dash");
    }

    // =========================
    // DEATH ANIMATION
    // =========================

    public void PlayDeathAnimation()
    {
        isDead = true;
        animator.SetTrigger("Death");
    }

    public void ResetFromDeath()
    {
        Debug.Log("RESETTING ANIMATION FROM DEATH");

        // Stop the death trigger from firing again
        animator.ResetTrigger("Death");

        // Reset the values that control normal animation
        animator.SetFloat("Speed", 0f);
        animator.SetFloat("VerticalVelocity", 0f);
        animator.SetBool("IsGrounded", true);

        // Force Animator to Idle, starting at the first frame
        animator.Play("HeroKnight_Idle", 0, 0f);

        // Make sure normal animation updates resume
        isDead = false;
    }
}
