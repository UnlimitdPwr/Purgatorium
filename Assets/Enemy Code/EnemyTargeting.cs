using UnityEngine;

public class EnemyTargeting : MonoBehaviour
{
    private Transform currentTarget;
    private Collider2D currentTargetCollider;

    public void SetTarget(Transform target)
    {
        if (target == currentTarget)
            return;

        currentTarget = target;
        currentTargetCollider = target != null ? target.GetComponent<Collider2D>() : null;
    }

    public void ClearTarget()
    {
        currentTarget = null;
        currentTargetCollider = null;
    }

    // Bottom-centre of the target's body. Transform pivots differ between the
    // player and enemy sprites, so compare feet rather than positions.
    public Vector2 GetTargetFeetPosition()
    {
        if (currentTarget == null)
            return Vector2.zero;

        if (currentTargetCollider != null && currentTargetCollider.enabled)
        {
            Bounds bounds = currentTargetCollider.bounds;
            return new Vector2(bounds.center.x, bounds.min.y);
        }

        return currentTarget.position;
    }

    public bool HasTarget()
    {
        return currentTarget != null;
    }

    public Transform GetTarget()
    {
        return currentTarget;
    }

    public float GetDistanceToTarget()
    {
        if (currentTarget == null)
            return Mathf.Infinity;

        return Vector2.Distance(
            transform.position,
            currentTarget.position
        );
    }

    public float GetHorizontalDirectionToTarget()
    {
        if (currentTarget == null)
            return 0f;

        float difference = currentTarget.position.x - transform.position.x;

        // Deadzone prevents tiny ± values from causing direction flipping.
        if (Mathf.Abs(difference) < 0.05f)
            return 0f;

        return Mathf.Sign(difference);
    }
}
