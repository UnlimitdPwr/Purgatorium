using UnityEngine;

public class EnemyAttack : MonoBehaviour
{

    public int damage = 10;
    public float attackCooldown = 1f;

    private float attackTimer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
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

        attackTimer = attackCooldown;
    }
}
