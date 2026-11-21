using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

// One possible item drop: which item, how likely, and how many.
[Serializable]
public class EnemyItemDrop
{
    public ItemData item;

    [Tooltip("Chance this item drops when the enemy dies, in percent. 100 = always.")]
    [Range(0f, 100f)] public float dropChance = 50f;

    [Tooltip("How many of the item drop together when the roll succeeds.")]
    [Min(1)] public int amount = 1;
}

// What the player gets for killing this enemy:
//   - Essence, paid straight into the player's PlayerEssence.
//   - Item drops (optional): each entry is rolled on its own, so an enemy can
//     drop several items, one, or none. Drops pop out of the body as normal
//     item pickups (Assets/Prefabs/ItemPickup.prefab).
// Set both per enemy, so tougher enemies can be worth more.
[RequireComponent(typeof(EnemyHealth))]
public class EnemyLoot : MonoBehaviour
{
    [Header("Essence")]
    [Tooltip("Essence given to the player when this enemy is killed. " +
             "(For scale: the first stat upgrade costs 100 — see PlayerStats.)")]
    [Min(0)] public int essenceReward = 25;

    [Header("Item Drops")]
    [Tooltip("Turn item drops on or off for this enemy.")]
    public bool itemDropsEnabled = false;

    [Tooltip("Items this enemy can drop. Each is rolled separately against its own chance.")]
    public List<EnemyItemDrop> itemDrops = new List<EnemyItemDrop>();

    [Tooltip("The pickup spawned for each drop. Filled in automatically with " +
             "Assets/Prefabs/ItemPickup.prefab when drops are turned on in the Inspector.")]
    public WorldItem pickupPrefab;

    [Tooltip("How hard drops pop out of the body: x = max sideways speed, y = upward speed.")]
    public Vector2 dropPop = new Vector2(1.5f, 3f);

    private EnemyHealth health;
    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        health = GetComponent<EnemyHealth>();
        spriteRenderer = GetComponent<SpriteRenderer>();
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
        GiveEssence();

        if (itemDropsEnabled)
            DropItems();
    }

    void GiveEssence()
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

    // Rolls every entry and spawns a pickup for each success. Public so it can
    // be tested from the component's right-click menu.
    [ContextMenu("Debug: Roll Item Drops")]
    public void DropItems()
    {
        if (itemDrops == null || itemDrops.Count == 0)
            return;

        if (pickupPrefab == null)
        {
            Debug.LogWarning(name + ": item drops are on but Pickup Prefab is empty — " +
                             "assign Assets/Prefabs/ItemPickup.prefab.", this);
            return;
        }

        Vector3 origin = spriteRenderer != null ? spriteRenderer.bounds.center : transform.position;

        foreach (EnemyItemDrop drop in itemDrops)
        {
            if (drop == null || drop.item == null)
                continue;

            // 100% always drops; 0% never does.
            bool dropped = drop.dropChance >= 100f || Random.value * 100f < drop.dropChance;

            if (!dropped)
                continue;

            SpawnPickup(drop.item, drop.amount, origin);
        }
    }

    void SpawnPickup(ItemData item, int amount, Vector3 position)
    {
        WorldItem pickup = Instantiate(pickupPrefab, position, Quaternion.identity);
        pickup.name = "Drop_" + item.name;
        pickup.Initialize(item, amount);

        Rigidbody2D body = pickup.GetComponent<Rigidbody2D>();

        if (body != null)
            body.linearVelocity = new Vector2(Random.Range(-dropPop.x, dropPop.x), dropPop.y);
    }
}
