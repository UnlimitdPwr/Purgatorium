using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Copies the enemy that's built and tuned in Scene2 (components, settings,
// hitbox layout, health bars — everything) into the scene you have open.
//
// The copy is ADDED — enemies already in the scene are left alone. It lands
// with its feet on the selected object — on top of it if it's a platform, at
// its position if it's a marker/empty — or in the middle of the Scene view if
// nothing is selected. Scene2 is only read, never
// changed. The open scene is not saved for you: Ctrl/Cmd+Z undoes the copy.
public static class EnemyTransferTool
{
    // Found by GUID (from its .meta file), so renaming or moving it doesn't break the tool.
    const string SourceSceneGuid = "b409d20b7acadcc4bbc3dc8d70f9985e"; // Scene2

    const string DialogTitle = "Enemy Transfer";

    [MenuItem("Tools/Enemy Transfer/Copy Scene2 Enemy Into Open Scene")]
    public static void CopyScene2EnemyIntoOpenScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog(DialogTitle,
                "Stop Play Mode first — changes made during Play Mode are thrown away when it ends.", "OK");
            return;
        }

        string message = Transfer(Selection.activeGameObject, GetSceneViewCenter());

        EditorUtility.DisplayDialog(DialogTitle, message, "OK");
    }

    // Does the work without the dialogs. `placeAt` (may be null) is the object
    // whose position the enemy's feet go to; otherwise `fallbackPosition`.
    // Returns a message saying what happened; it starts with "Done" on success.
    public static string Transfer(GameObject placeAt, Vector3 fallbackPosition)
    {
        string sourcePath = AssetDatabase.GUIDToAssetPath(SourceSceneGuid);

        if (string.IsNullOrEmpty(sourcePath) || AssetDatabase.LoadAssetAtPath<SceneAsset>(sourcePath) == null)
            return Fail("Couldn't find Scene2. It may have been deleted, or its .meta file replaced " +
                        "(which gives it a new ID). Restore it from git and try again.");

        Scene target = SceneManager.GetActiveScene();

        if (target.path == sourcePath)
            return Fail("Scene2 is the open scene — open the scene you want the enemy in first.");

        // Only use the selection if it belongs to the scene we're copying into.
        if (placeAt != null && placeAt.scene != target)
            placeAt = null;

        // Use Scene2 as-is if it's already open alongside; otherwise open it
        // additively just long enough to copy from it.
        Scene source = SceneManager.GetSceneByPath(sourcePath);
        bool openedSource = !source.isLoaded;

        if (openedSource)
            source = EditorSceneManager.OpenScene(sourcePath, OpenSceneMode.Additive);

        GameObject sourceEnemy = FindOnlyEnemy(source, out string error);

        if (sourceEnemy == null)
        {
            if (openedSource)
                EditorSceneManager.CloseScene(source, true);

            return Fail(error);
        }

        GameObject copy = Object.Instantiate(sourceEnemy);
        SceneManager.MoveGameObjectToScene(copy, target);
        copy.name = GameObjectUtility.GetUniqueNameForSibling(null, sourceEnemy.name.Trim());

        Vector3 position = placeAt != null ? StandingPoint(placeAt) : fallbackPosition;
        position.z = sourceEnemy.transform.position.z;
        copy.transform.position = position + Vector3.up * PivotHeightAboveFeet(copy);

        Undo.RegisterCreatedObjectUndo(copy, "Copy Scene2 enemy");

        if (openedSource)
            EditorSceneManager.CloseScene(source, true);

        SceneManager.SetActiveScene(target);
        EditorSceneManager.MarkSceneDirty(target);
        Selection.activeGameObject = copy;

        string where = placeAt != null ? "at '" + placeAt.name + "'" : "in the middle of the Scene view";
        string message = "Done — added '" + copy.name + "' to '" + target.name + "' " + where +
                         ". The scene isn't saved yet: save it when you're happy (Ctrl/Cmd+Z undoes the copy).";
        Debug.Log("EnemyTransferTool: " + message, copy);
        return message;
    }

    // Where the enemy's feet go: the top-middle of a solid object (a platform),
    // otherwise the object's position (a marker or empty).
    static Vector3 StandingPoint(GameObject target)
    {
        Physics2D.SyncTransforms();

        foreach (Collider2D col in target.GetComponents<Collider2D>())
        {
            if (!col.isTrigger)
                return new Vector3(col.bounds.center.x, col.bounds.max.y, 0f);
        }

        return target.transform.position;
    }

    // How far the enemy's pivot sits above the bottom of its body collider, so
    // the copy can be placed by its feet rather than its pivot.
    static float PivotHeightAboveFeet(GameObject enemy)
    {
        Physics2D.SyncTransforms();

        foreach (Collider2D col in enemy.GetComponents<Collider2D>())
        {
            if (!col.isTrigger)
                return enemy.transform.position.y - col.bounds.min.y;
        }

        return 0f;
    }

    static Vector3 GetSceneViewCenter()
    {
        SceneView view = SceneView.lastActiveSceneView;
        return view != null ? view.pivot : Vector3.zero;
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
