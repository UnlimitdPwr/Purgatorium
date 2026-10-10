using System.Collections;
using UnityEngine;

// A platform that shakes once the player lands on it, then falls away.
// Optionally comes back after a delay so a level can be retried without
// reloading. Expects its look on a child (the "visual") so the shake and fall
// move the sprite without moving the collider under the player's feet.
[RequireComponent(typeof(Collider2D))]
public class CrumblingPlatform : MonoBehaviour
{
    // =========================
    // CONFIG
    // =========================

    [Header("Crumble")]
    [Tooltip("Seconds between the player landing and the platform falling.")]
    [SerializeField] private float crumbleDelay = 0.5f;

    [Tooltip("How far the visual jitters while about to fall, in world units.")]
    [SerializeField] private float shakeAmount = 0.05f;

    [Tooltip("Seconds the falling visual stays on screen before it disappears.")]
    [SerializeField] private float fallDuration = 0.6f;

    [Header("Respawn")]
    [Tooltip("Bring the platform back after it falls. Turn off for one-time drops.")]
    [SerializeField] private bool respawn = true;

    [Tooltip("Seconds after falling before the platform comes back.")]
    [SerializeField] private float respawnDelay = 3f;

    [Header("Visual")]
    [Tooltip("The child that holds the sprite. Found automatically if left empty.")]
    [SerializeField] private Transform visual;

    // =========================
    // STATE
    // =========================

    private Collider2D platformCollider;
    private SpriteRenderer visualRenderer;
    private Vector3 visualRestPosition;
    private Color visualRestColor;
    private bool crumbling;

    public bool IsCrumbling => crumbling;

    // =========================
    // SETUP
    // =========================

    void Awake()
    {
        platformCollider = GetComponent<Collider2D>();

        if (visual == null && transform.childCount > 0)
            visual = transform.GetChild(0);

        if (visual != null)
        {
            visualRenderer = visual.GetComponent<SpriteRenderer>();
            visualRestPosition = visual.localPosition;
        }

        if (visualRenderer != null)
            visualRestColor = visualRenderer.color;
    }

    // =========================
    // TRIGGER
    // =========================

    void OnCollisionEnter2D(Collision2D collision)
    {
        TryStartCrumble(collision);
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        TryStartCrumble(collision);
    }

    void TryStartCrumble(Collision2D collision)
    {
        if (crumbling)
            return;

        if (collision.collider.GetComponentInParent<PlayerController1>() == null)
            return;

        // Only standing on top counts — bumping the underside or a side doesn't.
        float playerFeet = collision.collider.bounds.min.y;
        float platformTop = platformCollider.bounds.max.y;

        if (playerFeet < platformTop - 0.05f)
            return;

        StartCoroutine(Crumble());
    }

    // =========================
    // CRUMBLE
    // =========================

    IEnumerator Crumble()
    {
        crumbling = true;

        // Shake.
        float timer = 0f;

        while (timer < crumbleDelay)
        {
            timer += Time.deltaTime;
            SetVisualOffset(Random.insideUnitCircle * shakeAmount);
            yield return null;
        }

        // Fall: the player drops straight away, the visual follows them down.
        platformCollider.enabled = false;

        timer = 0f;
        float fallSpeed = 0f;
        float fallDistance = 0f;

        while (timer < fallDuration)
        {
            timer += Time.deltaTime;
            fallSpeed += Mathf.Abs(Physics2D.gravity.y) * Time.deltaTime;
            fallDistance += fallSpeed * Time.deltaTime;

            SetVisualOffset(Vector2.down * fallDistance);
            SetVisualAlpha(1f - timer / fallDuration);
            yield return null;
        }

        SetVisualVisible(false);

        if (!respawn)
            yield break;

        yield return new WaitForSeconds(respawnDelay);

        // Don't pop back into existence inside the player.
        while (PlayerOverlapsPlatform())
            yield return null;

        SetVisualOffset(Vector2.zero);
        SetVisualAlpha(1f);
        SetVisualVisible(true);
        platformCollider.enabled = true;
        crumbling = false;
    }

    bool PlayerOverlapsPlatform()
    {
        Bounds bounds = platformCollider.bounds;
        Collider2D[] hits = Physics2D.OverlapBoxAll(bounds.center, bounds.size, 0f);

        foreach (Collider2D hit in hits)
        {
            if (hit.GetComponentInParent<PlayerController1>() != null)
                return true;
        }

        return false;
    }

    // =========================
    // VISUAL HELPERS
    // =========================

    // Offsets are in world units, so they look the same on any platform size.
    void SetVisualOffset(Vector2 worldOffset)
    {
        if (visual == null)
            return;

        Vector3 scale = transform.lossyScale;
        visual.localPosition = visualRestPosition + new Vector3(
            worldOffset.x / scale.x,
            worldOffset.y / scale.y,
            0f
        );
    }

    void SetVisualAlpha(float alpha)
    {
        if (visualRenderer == null)
            return;

        Color color = visualRestColor;
        color.a = visualRestColor.a * Mathf.Clamp01(alpha);
        visualRenderer.color = color;
    }

    void SetVisualVisible(bool visible)
    {
        if (visualRenderer != null)
            visualRenderer.enabled = visible;
    }
}
