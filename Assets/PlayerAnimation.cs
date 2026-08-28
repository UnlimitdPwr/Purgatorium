using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    private Animator animator;
    private MovementScript movement;
    private SpriteRenderer spriteRenderer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        animator = GetComponent<Animator>();
        movement = GetComponent<MovementScript>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateMovementAnimation();
        UpdateJumpAnimation();
        UpdateFacingDirection();
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
}
