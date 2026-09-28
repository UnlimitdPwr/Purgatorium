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
}
