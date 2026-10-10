using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Editor helper for environmental hazards, so scenes are never hand-edited.
// Safe to re-run:
//   1. Makes the starter hazard types (Acid, Swamp, Lava) in
//      Assets/Level Code/Hazards/Types if they don't exist. Existing assets
//      are never overwritten, so your tuning is kept.
//   2. Adds a HazardReceiver to the player and every enemy in the open scene.
public static class HazardSetup
{
    const string TypesFolder = "Assets/Level Code/Hazards/Types";

    [MenuItem("Tools/Hazards/Set Up Hazards In Open Scene")]
    public static void SetUpOpenScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog(
                "Hazards",
                "Stop Play Mode first — changes made during Play Mode are thrown away when it ends.",
                "OK");
            return;
        }

        foreach (string typeName in StarterTypes.Keys)
            EnsureHazardType(typeName);

        Scene scene = SceneManager.GetActiveScene();
        bool changed = false;

        foreach (PlayerHealth player in Object.FindObjectsByType<PlayerHealth>(FindObjectsInactive.Include))
            changed |= AddReceiver(player.gameObject);

        foreach (EnemyController enemy in Object.FindObjectsByType<EnemyController>(FindObjectsInactive.Include))
            changed |= AddReceiver(enemy.gameObject);

        if (!changed)
        {
            Debug.Log("HazardSetup: " + scene.name + " already set up — no change.");
            return;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("HazardSetup: done — " + scene.name + " saved.");
    }

    static bool AddReceiver(GameObject target)
    {
        if (target.GetComponent<HazardReceiver>() != null)
            return false;

        Undo.AddComponent<HazardReceiver>(target);
        Debug.Log("HazardSetup: added HazardReceiver to '" + target.name + "'", target);
        return true;
    }

    // =========================
    // HAZARD TYPES
    // =========================

    // Starting values only — tune the assets afterwards.
    static readonly Dictionary<string, System.Action<HazardType>> StarterTypes =
        new Dictionary<string, System.Action<HazardType>>
    {
        // The old AcidPool's damage (4 every 0.5s), plus poison buildup.
        { "Acid", t =>
            {
                t.tint = new Color(0.35f, 0.95f, 0.2f, 0.6f);
                t.tickInterval = 0.5f;
                t.playerDamagePerTick = 4;
                t.enemyDamagePerTick = 2f;
                t.buildupPerTick = new[] { new StatusInfliction { status = StatusType.Poison, buildup = 5f } };
            }
        },
        // Harmless but slow.
        { "Swamp", t =>
            {
                t.tint = new Color(0.33f, 0.38f, 0.18f, 0.8f);
                t.tickInterval = 0.5f;
                t.speedMultiplier = 0.5f;
            }
        },
        { "Lava", t =>
            {
                t.tint = new Color(1f, 0.35f, 0.05f, 0.85f);
                t.tickInterval = 0.5f;
                t.playerDamagePerTick = 8;
                t.enemyDamagePerTick = 4f;
                t.speedMultiplier = 0.7f;
                t.buildupPerTick = new[] { new StatusInfliction { status = StatusType.Burn, buildup = 10f } };
            }
        },
    };

    // Loads Types/<typeName>.asset, creating it (with starter values if it's
    // one of the starter types) when it doesn't exist yet.
    public static HazardType EnsureHazardType(string typeName)
    {
        string path = TypesFolder + "/" + typeName + ".asset";
        HazardType type = AssetDatabase.LoadAssetAtPath<HazardType>(path);

        if (type != null)
            return type;

        EnsureFolder(TypesFolder);

        type = ScriptableObject.CreateInstance<HazardType>();

        if (StarterTypes.TryGetValue(typeName, out System.Action<HazardType> setUp))
            setUp(type);

        AssetDatabase.CreateAsset(type, path);
        AssetDatabase.SaveAssets();
        Debug.Log("HazardSetup: created " + path, type);
        return type;
    }

    static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder))
            return;

        string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
    }
}
