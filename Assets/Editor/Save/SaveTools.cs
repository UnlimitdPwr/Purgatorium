using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Tools > Save menu + keeps Resources/ItemDatabase in sync with the project.
public static class SaveTools
{
    private const string DatabasePath = "Assets/Resources/" + ItemDatabase.ResourcePath + ".asset";
    private const string BonfirePrefabPath = "Assets/Prefabs/Bonfire.prefab";
    private const string IntendedScene = "LevelBlockout_01";

    // =========================
    // ITEM DATABASE
    // =========================

    [MenuItem("Tools/Save/Rebuild Item Database")]
    public static void RebuildItemDatabaseMenu() => RebuildItemDatabase(true);

    public static void RebuildItemDatabase(bool log)
    {
        ItemDatabase database = AssetDatabase.LoadAssetAtPath<ItemDatabase>(DatabasePath);

        if (database == null)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");

            database = ScriptableObject.CreateInstance<ItemDatabase>();
            AssetDatabase.CreateAsset(database, DatabasePath);
        }

        var entries = new List<ItemDatabase.Entry>();

        foreach (string guid in AssetDatabase.FindAssets("t:ItemData"))
        {
            ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(AssetDatabase.GUIDToAssetPath(guid));

            if (item != null)
                entries.Add(new ItemDatabase.Entry { id = guid, item = item });
        }

        entries.Sort((a, b) => string.CompareOrdinal(a.id, b.id));

        if (SameEntries(database.entries, entries))
            return;

        database.entries = entries;
        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();

        if (log)
            Debug.Log("Item database rebuilt: " + entries.Count + " items.");
    }

    private static bool SameEntries(List<ItemDatabase.Entry> a, List<ItemDatabase.Entry> b)
    {
        if (a == null || a.Count != b.Count)
            return false;

        for (int i = 0; i < a.Count; i++)
        {
            if (a[i].id != b[i].id || a[i].item != b[i].item)
                return false;
        }

        return true;
    }

    // Rebuild automatically whenever .asset files are added, deleted or moved.
    private class ItemDatabasePostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            if (!TouchesItemAssets(imported) && !TouchesItemAssets(deleted) && !TouchesItemAssets(moved))
                return;

            // Wait until the import finishes before touching assets.
            EditorApplication.delayCall += () => RebuildItemDatabase(false);
        }

        private static bool TouchesItemAssets(string[] paths)
        {
            foreach (string path in paths)
            {
                if (path.EndsWith(".asset") && path != DatabasePath)
                    return true;
            }

            return false;
        }
    }

    // =========================
    // SCENE SETUP
    // =========================

    [MenuItem("Tools/Save/Add Bonfire Saving To Open Scene")]
    public static void AddBonfireSavingToOpenScene()
    {
        var scene = EditorSceneManager.GetActiveScene();

        if (Object.FindAnyObjectByType<BonfireSaveManager>() != null)
        {
            EditorUtility.DisplayDialog("Bonfire Saving", "This scene already has a BonfireSaveManager.", "OK");
            return;
        }

        if (scene.name != IntendedScene &&
            !EditorUtility.DisplayDialog("Bonfire Saving",
                "Saving is only meant for " + IntendedScene + " for now, but the open scene is '" +
                scene.name + "'. Add it anyway?", "Add anyway", "Cancel"))
        {
            return;
        }

        RebuildItemDatabase(true);

        var go = new GameObject("BonfireSaveManager");
        var manager = go.AddComponent<BonfireSaveManager>();

        var serialized = new SerializedObject(manager);
        serialized.FindProperty("bonfirePrefab").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<GameObject>(BonfirePrefabPath);
        serialized.FindProperty("player").objectReferenceValue = GameObject.FindWithTag("Player");
        serialized.ApplyModifiedPropertiesWithoutUndo();

        Undo.RegisterCreatedObjectUndo(go, "Add Bonfire Saving");
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = go;

        Debug.Log("Added BonfireSaveManager to " + scene.name + ". Save the scene to keep it.");
    }

    [MenuItem("Tools/Save/Delete Save Files")]
    public static void DeleteSaveFiles()
    {
        string[] files = Directory.GetFiles(Application.persistentDataPath, "save_*.json");

        foreach (string file in files)
            File.Delete(file);

        Debug.Log("Deleted " + files.Length + " save file(s) from " + Application.persistentDataPath);
    }

    [MenuItem("Tools/Save/Open Save Folder")]
    public static void OpenSaveFolder() => EditorUtility.RevealInFinder(Application.persistentDataPath);
}
