using System.Collections;
using UnityEngine;

// A climbable ladder (a trigger area the player's LadderClimbScript reads).
// It can start pulled up out of reach and drop into place when Drop() is
// called — e.g. by an ArenaEncounter once its boss is beaten.
[RequireComponent(typeof(Collider2D))]
public class Ladder : MonoBehaviour
{
    // =========================
    // CONFIG
    // =========================

    [Tooltip("Start pulled up out of reach. Drop() lowers it into place.")]
    [SerializeField] private bool startRaised;

    [Tooltip("How far above its placed position the ladder hangs while raised.")]
    [SerializeField] private float raisedHeight = 5f;

    [Tooltip("Seconds the drop takes.")]
    [SerializeField] private float dropDuration = 0.4f;

    // =========================
    // STATE
    // =========================

    private Collider2D ladderCollider;
    private Vector3 loweredPosition;
    private bool isLowered;

    // Can't be grabbed while raised or mid-drop.
    public bool IsUsable => isLowered;

    // World-space height of the top of the ladder.
    public float TopY => ladderCollider.bounds.max.y;
    public float CenterX => ladderCollider.bounds.center.x;

    // =========================
    // SETUP
    // =========================

    void Awake()
    {
        ladderCollider = GetComponent<Collider2D>();
        ladderCollider.isTrigger = true;
        loweredPosition = transform.position;

        if (startRaised)
        {
            transform.position = loweredPosition + Vector3.up * raisedHeight;
            isLowered = false;
        }
        else
        {
            isLowered = true;
        }
    }

    // =========================
    // DROP
    // =========================

    [ContextMenu("Drop")]
    public void Drop()
    {
        if (isLowered || !Application.isPlaying)
            return;

        StopAllCoroutines();
        StartCoroutine(DropRoutine());
    }

    IEnumerator DropRoutine()
    {
        Vector3 start = transform.position;
        float timer = 0f;

        while (timer < dropDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / dropDuration);

            // Accelerate like a falling object.
            transform.position = Vector3.Lerp(start, loweredPosition, t * t);
            yield return null;
        }

        transform.position = loweredPosition;
        isLowered = true;
    }
}
