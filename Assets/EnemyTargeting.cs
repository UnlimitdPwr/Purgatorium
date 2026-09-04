using UnityEngine;

public class EnemyTargeting : MonoBehaviour
{
    private Transform currentTarget;

    public void SetTarget(Transform target)
    {
        currentTarget = target;
    }

    public void ClearTarget()
    {
        currentTarget = null;
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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
