using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// One-off helper: replaces SampleScene's enemy with a copy of the enemy that
// was built and tuned in Scene2 (components, settings, hitbox layout, health
// bars — everything). The copy goes where SampleScene's old enemy stood.
// Scene2 is only read, never changed. Delete this tool once it has been used.
public static class EnemyTransferTool
{
    // The scenes are found by GUID (from their .meta files), so renaming or
    // moving them doesn't break the tool.
    const string SourceSceneGuid = "b409d20b7acadcc4bbc3dc8d70f9985e"; // Scene2
    const string TargetSceneGuid = "8c9cfa26abfee488c85f1582747f6a02"; // SampleScene

    const string DialogTitle = "Enemy Transfer";

    [MenuItem("Tools/Enemy Transfer/Copy Scene2 Enemy Into SampleScene")]
    public static void CopyScene2EnemyIntoSampleScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog(DialogTitle,
                "Stop Play Mode first — changes made during Play Mode are thrown away when it ends.", "OK");
            return;
        }

        if (!TryFindScenes(out string sourcePath, out string targetPath, out string error))
        {
            EditorUtility.DisplayDialog(DialogTitle, error, "OK");
            return;
        }

        string sourceName = System.IO.Path.GetFileNameWithoutExtension(sourcePath);
        string targetName = System.IO.Path.GetFileNameWithoutExtension(targetPath);

        bool confirmed = EditorUtility.DisplayDialog(DialogTitle,
            "Replace the enemy in '" + targetName + "' with a copy of the enemy from '" + sourceName +
            "'?\n\nThe copy is placed where the current enemy stands, and '" + targetName +
            "' is saved. '" + sourceName + "' is not changed.",
            "Replace", "Cancel");

        if (!confirmed)
            return;

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        string message = Transfer();

        EditorUtility.DisplayDialog(DialogTitle, message, "OK");
    }

    // Does the work without the dialogs. Returns a message saying what
    // happened; it starts with "Done" on success.
    public static string Transfer()
    {
        if (!TryFindScenes(out string sourcePath, out string targetPath, out string error))
            return Fail(error);

        Scene target = EditorSceneManager.OpenScene(targetPath, OpenSceneMode.Single);
        Scene source = EditorSceneManager.OpenScene(sourcePath, OpenSceneMode.Additive);

        GameObject sourceEnemy = FindOnlyEnemy(source, out error);
        GameObject oldEnemy = sourceEnemy != null ? FindOnlyEnemy(target, out error) : null;

        if (sourceEnemy == null || oldEnemy == null)
        {
            EditorSceneManager.CloseScene(source, true);
            return Fail(error);
        }

        GameObject copy = Object.Instantiate(sourceEnemy);
        SceneManager.MoveGameObjectToScene(copy, target);
        Undo.RegisterCreatedObjectUndo(copy, "Copy " + source.name + " enemy");

        // Take over the old enemy's name, place in the hierarchy and position.
        copy.name = oldEnemy.name;
        copy.transform.SetParent(oldEnemy.transform.parent, false);
        copy.transform.SetSiblingIndex(oldEnemy.transform.GetSiblingIndex());
        copy.transform.position = oldEnemy.transform.position;

        Undo.DestroyObjectImmediate(oldEnemy);

        string sourceName = source.name;
        EditorSceneManager.CloseScene(source, true);
        SceneManager.SetActiveScene(target);

        EditorSceneManager.MarkSceneDirty(target);

        if (!EditorSceneManager.SaveScene(target))
            return Fail("Copied the enemy, but saving '" + target.name + "' failed — save it manually (Ctrl/Cmd+S).");

        string message = "Done — '" + target.name + "' now has the enemy from '" + sourceName +
                         "' and has been saved. Commit " + target.path + ".";
        Debug.Log("EnemyTransferTool: " + message, copy);
        return message;
    }

    static bool TryFindScenes(out string sourcePath, out string targetPath, out string error)
    {
        sourcePath = AssetDatabase.GUIDToAssetPath(SourceSceneGuid);
        targetPath = AssetDatabase.GUIDToAssetPath(TargetSceneGuid);
        error = null;

        if (string.IsNullOrEmpty(sourcePath) || AssetDatabase.LoadAssetAtPath<SceneAsset>(sourcePath) == null)
            error = "Couldn't find the source scene (Scene2). It may have been deleted, or its .meta file replaced " +
                    "(which gives it a new ID). Restore it from git and try again.";
        else if (string.IsNullOrEmpty(targetPath) || AssetDatabase.LoadAssetAtPath<SceneAsset>(targetPath) == null)
            error = "Couldn't find the target scene (SampleScene). It may have been deleted, or its .meta file replaced " +
                    "(which gives it a new ID). Restore it from git and try again.";

        return error == null;
    }

    // The scene's single enemy. Refuses to guess when there are several.
    static GameObject FindOnlyEnemy(Scene scene, out string error)
    {
        GameObject found = null;
        error = null;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (EnemyController enemy in root.GetComponentsInChildren<EnemyController>(true))
            {
                if (found != null)
                {
                    error = "'" + scene.name + "' has more than one enemy — remove the extras first so it's clear which one to use.";
                    return null;
                }

                found = enemy.gameObject;
            }
        }

        if (found == null)
            error = "No enemy (EnemyController) found in '" + scene.name + "'.";

        return found;
    }

    static string Fail(string error)
    {
        Debug.LogWarning("EnemyTransferTool: " + error);
        return "Nothing was changed.\n\n" + error;
    }
}
