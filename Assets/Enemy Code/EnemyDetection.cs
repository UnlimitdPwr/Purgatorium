using UnityEngine;

public class EnemyDetection : MonoBehaviour
{
    public float detectionRange = 5f;

    private Transform player;

    void Awake()
    {
        AcquirePlayer();
    }

    // =========================
    // DETECTION
    // =========================

    public bool CanDetectPlayer()
    {
        // The player reference can be lost if the player is (re)spawned after
        // this component woke up — try to re-acquire it before giving up.
        if (player == null)
            AcquirePlayer();

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

    // =========================
    // INTERNAL
    // =========================

    private void AcquirePlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
            player = playerObject.transform;
    }
}
