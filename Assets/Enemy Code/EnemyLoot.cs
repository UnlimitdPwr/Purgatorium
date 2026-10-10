using UnityEngine;

// What the player gets for killing this enemy. Right now that's essence,
// paid straight into the player's PlayerEssence the moment the enemy dies.
// Set essenceReward per enemy, so tougher enemies can be worth more.
[RequireComponent(typeof(EnemyHealth))]
public class EnemyLoot : MonoBehaviour
{
    [Header("Essence")]
    [Tooltip("Essence given to the player when this enemy is killed. " +
             "(For scale: the first stat upgrade costs 100 — see PlayerStats.)")]
    [Min(0)] public int essenceReward = 25;

    private EnemyHealth health;

    void Awake()
    {
        health = GetComponent<EnemyHealth>();
    }

    void OnEnable()
    {
        health.OnDeath += GiveLoot;
    }

    void OnDisable()
    {
        health.OnDeath -= GiveLoot;
    }

    void GiveLoot()
    {
        if (essenceReward <= 0)
            return;

        PlayerEssence essence = FindAnyObjectByType<PlayerEssence>();

        if (essence == null)
        {
            Debug.LogWarning(name + ": killed, but there's no PlayerEssence in the scene to give " +
                             essenceReward + " essence to.", this);
            return;
        }

        essence.AddEssence(essenceReward);
    }
}
