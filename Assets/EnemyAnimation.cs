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

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
    {
        
    }

    // Update is called once per frame
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
            float direction = movement.GetMoveDirection();

            if (direction > 0)
            {
                spriteRenderer.flipX = false;
            }
            else if (direction < 0)
            {
                spriteRenderer.flipX = true;
            }
        }

    public void PlayAttack()
    {
        animator.SetTrigger("Attack");
    }
}
