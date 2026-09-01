using UnityEngine;
using UnityEngine.InputSystem;

public class EnemyController : MonoBehaviour
{
    public Transform player;
    public float detectionRange = 5f;
    public float attackRange = 1.2f;
    public float attackCooldown = 1f;

    private EnemyMovement movement;
    private EnemyAttack attack;

    void Awake()
    {
        movement = GetComponent<EnemyMovement>();
        attack = GetComponent<EnemyAttack>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

// Update is called once per frame
void Update()
    {
        if (Keyboard.current.jKey.wasPressedThisFrame)
        {
            movement.Jump();
        }

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);


        if (distanceToPlayer > detectionRange)
        {
            movement.Stop();
        }
        else if (distanceToPlayer <= attackRange)
        {
           AttackPlayer();
        }
        else
        {
            ChasePlayer();
        }

}

    void ChasePlayer()
    {
        float direction = Mathf.Sign(player.position.x - transform.position.x);

        movement.Move(direction);
    }

    void AttackPlayer()
    {
        movement.Stop();

        attack.Attack();
    }
}
