using UnityEngine;

// A solid wall that shatters when the player dashes into it. Stays broken
// until the scene reloads. Swap the check in IsBreakingHit() once the player
// has an attack, if attacks should break it too.
[RequireComponent(typeof(Collider2D))]
public class BreakableWall : MonoBehaviour
{
    private bool broken;

    public bool IsBroken => broken;

    // =========================
    // HIT DETECTION
    // =========================

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (IsBreakingHit(collision))
            Break();
    }

    // Stay as well as Enter: dashing while already pressed against the wall counts.
    void OnCollisionStay2D(Collision2D collision)
    {
        if (IsBreakingHit(collision))
            Break();
    }

    bool IsBreakingHit(Collision2D collision)
    {
        if (broken)
            return false;

        DashScript dash = collision.collider.GetComponentInParent<DashScript>();
        return dash != null && dash.IsDashing;
    }

    // =========================
    // BREAK
    // =========================

    public void Break()
    {
        if (broken)
            return;

        broken = true;
        gameObject.SetActive(false);
    }
}
