using UnityEngine;

public class EnemyDetection : MonoBehaviour
{
    public float detectionRange = 5f;

    private Transform player;

    void Awake()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            player = playerObject.transform;
        }
    }

    public bool CanDetectPlayer()
    {
        if (player == null)
            return false;

        float distanceToPlayer = Vector2.Distance(
            transform.position,
            player.position
        );

        return distanceToPlayer <= detectionRange;
    }

    public Transform GetDetectedPlayer()
    {
        if (CanDetectPlayer())
            return player;

        return null;
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
