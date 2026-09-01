using UnityEngine;

public class EnemyHitbox : MonoBehaviour
{
    private Collider2D hitbox;

    void Awake()
    {
        hitbox = GetComponent<Collider2D>();
        hitbox.enabled = false;
    }

    public void EnableHitbox()
    {
        hitbox.enabled = true;
        Debug.Log("Enemy hitbox ON");
    }

    public void DisableHitbox()
    {
        hitbox.enabled = false;
        Debug.Log("Enemy hitbox OFF");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Enemy hitbox detected: " + other.name);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
