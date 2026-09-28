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
    const string SourceSceneName = "Scene2";
    const string TargetSceneName = "SampleScene";

    [MenuItem("Tools/Enemy Transfer/Copy Scene2 Enemy Into SampleScene")]
    public static void CopyScene2EnemyIntoSampleScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog(
                "Enemy Transfer",
                "Stop Play Mode first — changes made during Play Mode are thrown away when it ends.",
                "OK");
            return;
        }

        bool confirmed = EditorUtility.DisplayDialog(
            "Enemy Transfer",
            "Replace the enemy in " + TargetSceneName + " with a copy of the enemy from " + SourceSceneName +
            "?\n\nThe copy is placed where " + TargetSceneName + "'s current enemy stands, and " +
            TargetSceneName + " is saved. " + SourceSceneName + " is not changed.",
            "Replace", "Cancel");

        if (!confirmed)
            return;

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        Transfer();
    }

    // Does the work without the dialogs. Returns true on success.
    public static bool Transfer()
    {
        string sourcePath = FindScenePath(SourceSceneName);
        string targetPath = FindScenePath(TargetSceneName);

        if (sourcePath == null || targetPath == null)
        {
            Debug.LogWarning("EnemyTransferTool: couldn't find " + SourceSceneName + " and " + TargetSceneName + ".");
            return false;
        }

        Scene target = EditorSceneManager.OpenScene(targetPath, OpenSceneMode.Single);
        Scene source = EditorSceneManager.OpenScene(sourcePath, OpenSceneMode.Additive);

        GameObject sourceEnemy = FindOnlyEnemy(source);
        GameObject oldEnemy = FindOnlyEnemy(target);

        if (sourceEnemy == null || oldEnemy == null)
        {
            EditorSceneManager.CloseScene(source, true);
            return false;
        }

        GameObject copy = Object.Instantiate(sourceEnemy);
        SceneManager.MoveGameObjectToScene(copy, target);
        Undo.RegisterCreatedObjectUndo(copy, "Copy " + SourceSceneName + " enemy");

        // Take over the old enemy's name, place in the hierarchy and position.
        copy.name = oldEnemy.name;
        copy.transform.SetParent(oldEnemy.transform.parent, false);
        copy.transform.SetSiblingIndex(oldEnemy.transform.GetSiblingIndex());
        copy.transform.position = oldEnemy.transform.position;

        Undo.DestroyObjectImmediate(oldEnemy);

        EditorSceneManager.CloseScene(source, true);
        SceneManager.SetActiveScene(target);

        EditorSceneManager.MarkSceneDirty(target);
        EditorSceneManager.SaveScene(target);

        Debug.Log("EnemyTransferTool: replaced " + TargetSceneName + "'s enemy with the " +
                  SourceSceneName + " enemy — " + TargetSceneName + " saved.", copy);
        return true;
    }

    // The scene's single root enemy. Refuses to guess when there are several.
    static GameObject FindOnlyEnemy(Scene scene)
    {
        GameObject found = null;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (EnemyController enemy in root.GetComponentsInChildren<EnemyController>(true))
            {
                if (found != null)
                {
                    Debug.LogWarning("EnemyTransferTool: " + scene.name + " has more than one enemy — " +
                                     "remove the extras first so it's clear which one to use.");
                    return null;
                }

                found = enemy.gameObject;
            }
        }

        if (found == null)
            Debug.LogWarning("EnemyTransferTool: no enemy found in " + scene.name + ".");

        return found;
    }

    static string FindScenePath(string sceneName)
    {
        foreach (string guid in AssetDatabase.FindAssets(sceneName + " t:Scene"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            if (System.IO.Path.GetFileNameWithoutExtension(path) == sceneName)
                return path;
        }

        return null;
    }
}
