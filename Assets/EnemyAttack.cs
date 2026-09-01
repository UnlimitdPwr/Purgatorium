using UnityEngine;

public class EnemyAttack : MonoBehaviour
{

    public int damage = 10;
    public float attackCooldown = 1f;

    private float attackTimer;

    private EnemyAnimation animation;
    private EnemyHitbox hitbox;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    void Awake()
    {
        animation = GetComponent<EnemyAnimation>();
        hitbox = GetComponentInChildren<EnemyHitbox>();
    }

    // Update is called once per frame
    void Update()
    {
        if (attackTimer > 0f)
        {
            attackTimer -= Time.deltaTime;
        }
    }

    public void Attack()
    {
        if (attackTimer > 0f)
            return;

        Debug.Log("Enemy attacks for " + damage + " damage!");

        animation.PlayAttack();

        attackTimer = attackCooldown;
    }

    public void EnableHitbox()
    {
        hitbox.EnableHitbox();
    }

    public void DisableHitbox()
    {
        hitbox.DisableHitbox();
    }
}
