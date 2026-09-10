using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// One-shot editor helper for wiring up the parry mechanic. Adds the
// EnemyKnockback component (introduced with feature/parry-mechanic) to every
// enemy in SampleScene so the scene doesn't have to be hand-edited. Safe to
// re-run and safe to delete once the scene is committed.
public static class ParrySetup
{
    const string ScenePath = "Assets/Scenes/SampleScene.unity";

    [MenuItem("Tools/Parry/Add EnemyKnockback To Scene Enemies")]
    public static void AddEnemyKnockback()
    {
        Scene scene = EnsureSampleSceneOpen();

        if (!scene.IsValid())
        {
            Debug.LogWarning("ParrySetup: could not open " + ScenePath);
            return;
        }

        EnemyController[] controllers =
            Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None);

        if (controllers.Length == 0)
        {
            Debug.LogWarning("ParrySetup: no EnemyController in the scene — nothing to do.");
            return;
        }

        int added = 0;

        foreach (EnemyController controller in controllers)
        {
            GameObject go = controller.gameObject;

            if (go.GetComponent<EnemyKnockback>() != null)
                continue;

            Undo.AddComponent<EnemyKnockback>(go);
            added++;

            Debug.Log("ParrySetup: added EnemyKnockback to '" + go.name + "'", go);
        }

        if (added == 0)
        {
            Debug.Log("ParrySetup: every enemy already has EnemyKnockback — no change.");
            return;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("ParrySetup: done — EnemyKnockback added to " +
                  added + " enemy object(s) and scene saved.");
    }

    static Scene EnsureSampleSceneOpen()
    {
        Scene active = SceneManager.GetActiveScene();

        if (active.path == ScenePath)
            return active;

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return default;

        return EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
    }
}
