using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Turns the scene-built enemy into a prefab and swaps scene enemies for
// instances of it, so every enemy shares one set of components, children and
// tuning. Change Enemy_Base and every enemy follows; give an enemy type its
// own values with a Prefab Variant (right-click the prefab > Create > Prefab
// Variant).
//
// 1. Create Enemy_Base From Selected Enemy — select a fully set-up enemy in
//    the open scene (LevelBlockout_01's are). A copy of it, with any missing
//    systems added, is saved as Assets/Prefabs/Enemies/Enemy_Base.prefab.
//    The scene is not changed.
// 2. Convert Enemies In Open Scene — every enemy that isn't a prefab instance
//    yet becomes an Enemy_Base instance. Nothing about how it plays changes:
//    whatever differs from the prefab (position, scale, older tuning values,
//    layer) is kept as an override, shown in bold in the Inspector. Systems
//    the enemy was missing come from the prefab. Saves the scene.
public static class EnemyPrefabTool
{
    public const string PrefabPath = "Assets/Prefabs/Enemies/Enemy_Base.prefab";

    const string DialogTitle = "Enemy Prefab";

    [MenuItem("Tools/Enemy Prefab/Create Enemy_Base From Selected Enemy")]
    public static void CreateFromSelected()
    {
        if (!CheckNotPlaying())
            return;

        GameObject selected = Selection.activeGameObject;
        EnemyController enemy = selected != null ? selected.GetComponentInParent<EnemyController>() : null;

        if (enemy == null || !enemy.gameObject.scene.IsValid())
        {
            EditorUtility.DisplayDialog(DialogTitle,
                "Select an enemy in the open scene first (LevelBlockout_01's enemies are fully set up).", "OK");
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null &&
            !EditorUtility.DisplayDialog(DialogTitle,
                "Enemy_Base already exists. Replace it with a copy of '" + enemy.name + "'?\n\n" +
                "Enemies that are already instances keep their overrides.", "Replace", "Cancel"))
            return;

        EditorUtility.DisplayDialog(DialogTitle, CreatePrefab(enemy.gameObject), "OK");
    }

    [MenuItem("Tools/Enemy Prefab/Convert Enemies In Open Scene")]
    public static void ConvertOpenScene()
    {
        if (!CheckNotPlaying())
            return;

        EditorUtility.DisplayDialog(DialogTitle, ConvertScene(SceneManager.GetActiveScene()), "OK");
    }

    // Saves a copy of `source` as Enemy_Base. Returns a message saying what happened.
    public static string CreatePrefab(GameObject source)
    {
        if (PrefabUtility.IsPartOfPrefabInstance(source))
            return "'" + source.name + "' is already a prefab instance — pick an enemy that isn't.";

        Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));

        GameObject copy = Object.Instantiate(source);

        try
        {
            copy.name = Path.GetFileNameWithoutExtension(PrefabPath);
            copy.transform.position = Vector3.zero;
            LevelSystemsSetup.AddMissingEnemySystems(copy);

            if (PrefabUtility.SaveAsPrefabAsset(copy, PrefabPath, out bool saved) == null || !saved)
                return "Couldn't save " + PrefabPath + " — see the Console.";
        }
        finally
        {
            Object.DestroyImmediate(copy);
        }

        Debug.Log("EnemyPrefabTool: saved " + PrefabPath + " from '" + source.name + "'");
        return "Done — saved " + PrefabPath + " from '" + source.name + "'.\n\n" +
               "Next: Tools > Enemy Prefab > Convert Enemies In Open Scene.";
    }

    // Converts every enemy in `scene` that isn't a prefab instance yet, then
    // saves the scene. Returns a message saying what happened.
    public static string ConvertScene(Scene scene)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

        if (prefab == null)
            return "No " + PrefabPath + " yet — run Create Enemy_Base From Selected Enemy first.";

        var settings = new ConvertToPrefabInstanceSettings
        {
            // Keep the enemy exactly as it plays now: differing values,
            // extra components and extra children all stay, as overrides.
            recordPropertyOverridesOfMatches = true,
            componentsNotMatchedBecomesOverride = true,
            gameObjectsNotMatchedBecomesOverride = true,
            changeRootNameToAssetName = false,
            objectMatchMode = ObjectMatchMode.ByHierarchy,
        };

        int converted = 0;
        int skipped = 0;

        foreach (EnemyController enemy in Object.FindObjectsByType<EnemyController>(FindObjectsInactive.Include))
        {
            GameObject go = enemy.gameObject;

            if (go.scene != scene)
                continue;

            if (PrefabUtility.IsPartOfPrefabInstance(go))
            {
                skipped++;
                continue;
            }

            PrefabUtility.ConvertToPrefabInstance(go, prefab, settings, InteractionMode.UserAction);
            Debug.Log("EnemyPrefabTool: '" + go.name + "' is now an Enemy_Base instance", go);
            converted++;
        }

        if (converted == 0)
            return "No change — " + (skipped > 0
                ? "every enemy in " + scene.name + " is already a prefab instance."
                : "there are no enemies in " + scene.name + ".");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        return "Done — converted " + converted + " enem" + (converted == 1 ? "y" : "ies") + " in " + scene.name +
               " and saved it." + (skipped > 0 ? "\n" + skipped + " already were prefab instances." : "") +
               "\n\nValues that differ from Enemy_Base are kept as overrides (bold in the Inspector). " +
               "To make an enemy match the prefab, right-click a component > Revert.";
    }

    static bool CheckNotPlaying()
    {
        if (!EditorApplication.isPlayingOrWillChangePlaymode)
            return true;

        EditorUtility.DisplayDialog(DialogTitle,
            "Stop Play Mode first — changes made during Play Mode are thrown away when it ends.", "OK");
        return false;
    }
}
