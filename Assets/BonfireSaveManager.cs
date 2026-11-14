using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

// Saves the game when the player rests at a bonfire (and again when they
// leave it, to keep upgrades/equipment changes made while resting), and loads
// it when the scene starts. Only scenes that contain this component save -
// currently just LevelBlockout_01 (Tools > Save > Add Bonfire Saving To Open Scene).
//
// Saved: checkpoint bonfire, every bonfire's position (player-placed ones are
// rebuilt on load), Essence, stat levels, inventory, equipment slots, active
// shield and shield wear. Not saved: enemies (they reset on rest anyway),
// health/stamina/status (resting fills/clears them).
//
// File: <Application.persistentDataPath>/save_<scene name>.json
public class BonfireSaveManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The player. Found by the 'Player' tag if empty.")]
    [SerializeField] private GameObject player;

    [Tooltip("Spawned on load for player-placed bonfires that aren't in the scene.")]
    [SerializeField] private GameObject bonfirePrefab;

    [Header("Options")]
    [Tooltip("Load the save when the scene starts. Turn off to test from a fresh start without deleting the file.")]
    [SerializeField] private bool loadOnStart = true;

    // Bonfires closer than this to a saved position count as the same bonfire.
    private const float SameBonfireDistance = 0.5f;

    public string SavePath => GetSavePath(SceneManager.GetActiveScene().name);

    public static string GetSavePath(string sceneName) =>
        Path.Combine(Application.persistentDataPath, "save_" + sceneName + ".json");

    private void OnEnable()
    {
        Bonfire.OnRested += HandleRested;
        BonfireUI.OnLeft += Save;
    }

    private void OnDisable()
    {
        Bonfire.OnRested -= HandleRested;
        BonfireUI.OnLeft -= Save;
    }

    // Start, not Awake: every other component has set itself up by now.
    private void Start()
    {
        if (player == null)
            player = GameObject.FindWithTag("Player");

        if (loadOnStart && File.Exists(SavePath))
            Load();
    }

    private void HandleRested(Bonfire bonfire, GameObject interactor) => Save();

    // =========================
    // SAVE
    // =========================

    [ContextMenu("Save Now")]
    public void Save()
    {
        if (player == null)
        {
            Debug.LogWarning("BonfireSaveManager: no player, nothing saved.", this);
            return;
        }

        ItemDatabase database = ItemDatabase.Instance;

        if (database == null)
        {
            Debug.LogWarning("BonfireSaveManager: no Resources/ItemDatabase asset, nothing saved. " +
                             "Run Tools > Save > Rebuild Item Database.", this);
            return;
        }

        var data = new SaveData();

        // World
        foreach (Bonfire bonfire in FindObjectsByType<Bonfire>(FindObjectsSortMode.None))
            data.bonfirePositions.Add(bonfire.transform.position);

        if (CheckpointManager.Instance != null && CheckpointManager.Instance.CurrentBonfire != null)
        {
            data.hasCheckpoint = true;
            data.checkpointPosition = CheckpointManager.Instance.CurrentBonfire.transform.position;
        }

        // Progression
        PlayerEssence essence = player.GetComponent<PlayerEssence>();
        if (essence != null)
            data.essence = essence.CurrentEssence;

        PlayerStats stats = player.GetComponent<PlayerStats>();
        if (stats != null)
        {
            data.healthLevel = stats.HealthLevel;
            data.staminaLevel = stats.StaminaLevel;
            data.strengthLevel = stats.StrengthLevel;
            data.dexterityLevel = stats.DexterityLevel;
            data.soulDurabilityLevel = stats.SoulDurabilityLevel;
        }

        // Items
        InventorySystem inventory = player.GetComponent<InventorySystem>();
        if (inventory != null)
        {
            foreach (InventorySlot slot in inventory.Slots)
            {
                data.inventory.Add(new SaveData.SavedStack
                {
                    itemId = slot.IsEmpty() ? "" : database.GetId(slot.item),
                    amount = slot.IsEmpty() ? 0 : slot.amount
                });
            }
        }

        EquipmentManager equipment = player.GetComponent<EquipmentManager>();
        if (equipment != null)
        {
            foreach (ItemData item in equipment.ShieldSlots) data.shieldSlots.Add(database.GetId(item));
            foreach (ItemData item in equipment.QuickSlots) data.quickSlots.Add(database.GetId(item));
            foreach (ItemData item in equipment.TalismanSlots) data.talismanSlots.Add(database.GetId(item));
            data.activeShieldIndex = equipment.ActiveShieldIndex;
        }

        PlayerShield shield = player.GetComponent<PlayerShield>();
        if (shield != null)
        {
            foreach (KeyValuePair<ShieldData, float> pair in shield.WearByShield)
                data.shieldWear.Add(new SaveData.SavedWear { shieldId = database.GetId(pair.Key), wear = pair.Value });
        }

        File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
        Debug.Log("Game saved: " + SavePath);
    }

    // =========================
    // LOAD
    // =========================

    [ContextMenu("Load Now")]
    public void Load()
    {
        if (player == null || !File.Exists(SavePath))
            return;

        ItemDatabase database = ItemDatabase.Instance;

        if (database == null)
        {
            Debug.LogWarning("BonfireSaveManager: no Resources/ItemDatabase asset, can't load items.", this);
            return;
        }

        SaveData data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));

        if (data == null)
        {
            Debug.LogWarning("BonfireSaveManager: save file unreadable: " + SavePath, this);
            return;
        }

        // Stats first: max health/stamina depend on them.
        PlayerStats stats = player.GetComponent<PlayerStats>();
        if (stats != null)
        {
            stats.ApplyConfirmedLevels(data.healthLevel, data.staminaLevel, data.strengthLevel,
                                       data.dexterityLevel, data.soulDurabilityLevel);
        }

        PlayerEssence essence = player.GetComponent<PlayerEssence>();
        if (essence != null)
            essence.SetEssence(data.essence);

        // Inventory before equipment: equipment drops anything not owned.
        InventorySystem inventory = player.GetComponent<InventorySystem>();
        if (inventory != null)
        {
            var items = new List<ItemData>();
            var amounts = new List<int>();

            foreach (SaveData.SavedStack stack in data.inventory)
            {
                items.Add(database.GetItem(stack.itemId));
                amounts.Add(stack.amount);
            }

            inventory.LoadSlots(items, amounts);
        }

        EquipmentManager equipment = player.GetComponent<EquipmentManager>();
        if (equipment != null)
        {
            equipment.LoadState(ToItems(database, data.shieldSlots), ToItems(database, data.quickSlots),
                                ToItems(database, data.talismanSlots), data.activeShieldIndex);
        }

        PlayerShield shield = player.GetComponent<PlayerShield>();
        if (shield != null)
        {
            var wear = new Dictionary<ShieldData, float>();

            foreach (SaveData.SavedWear saved in data.shieldWear)
            {
                if (database.GetItem(saved.shieldId) is ShieldData shieldData)
                    wear[shieldData] = saved.wear;
            }

            shield.LoadWear(wear);
        }

        // World: rebuild player-placed bonfires, then put the player at the checkpoint.
        foreach (Vector3 position in data.bonfirePositions)
        {
            if (FindBonfireNear(position) == null && bonfirePrefab != null)
            {
                GameObject spawned = Instantiate(bonfirePrefab, position, Quaternion.identity);
                spawned.name = "Bonfire_Saved";
            }
        }

        if (data.hasCheckpoint)
        {
            Bonfire checkpoint = FindBonfireNear(data.checkpointPosition);

            if (checkpoint != null)
            {
                if (CheckpointManager.Instance != null)
                    CheckpointManager.Instance.SetCheckpoint(checkpoint);

                Transform respawn = checkpoint.GetRespawnPoint();
                player.transform.position = respawn != null ? respawn.position : checkpoint.transform.position;

                Rigidbody2D body = player.GetComponent<Rigidbody2D>();
                if (body != null)
                    body.linearVelocity = Vector2.zero;
            }
        }

        // Rested state: full health and stamina.
        PlayerHealth health = player.GetComponent<PlayerHealth>();
        if (health != null)
            health.RestoreFullHealth();

        PlayerStamina stamina = player.GetComponent<PlayerStamina>();
        if (stamina != null)
            stamina.RestoreFullStamina();

        Debug.Log("Game loaded: " + SavePath);
    }

    [ContextMenu("Delete Save")]
    public void DeleteSave()
    {
        if (File.Exists(SavePath))
        {
            File.Delete(SavePath);
            Debug.Log("Deleted save: " + SavePath);
        }
    }

    private static List<ItemData> ToItems(ItemDatabase database, List<string> ids)
    {
        var items = new List<ItemData>();

        foreach (string id in ids)
            items.Add(database.GetItem(id));

        return items;
    }

    private static Bonfire FindBonfireNear(Vector3 position)
    {
        foreach (Bonfire bonfire in FindObjectsByType<Bonfire>(FindObjectsSortMode.None))
        {
            if (Vector2.Distance(bonfire.transform.position, position) <= SameBonfireDistance)
                return bonfire;
        }

        return null;
    }
}
