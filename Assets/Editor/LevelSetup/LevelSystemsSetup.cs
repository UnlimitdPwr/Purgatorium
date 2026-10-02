using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Editor helper that wires the enemy/world systems into the open scene
// (built for Scene2), so the scene never has to be hand-edited. Every step
// checks what's already there, so it's safe to re-run.
public static class LevelSystemsSetup
{
    [MenuItem("Tools/Level Setup/Wire Enemy Systems Into Open Scene")]
    public static void WireOpenScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog(
                "Level Setup",
                "Stop Play Mode first — changes made during Play Mode are thrown away when it ends.",
                "OK");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        bool changed = false;

        changed |= AddPatrolToEnemies();

        if (!changed)
        {
            Debug.Log("LevelSystemsSetup: " + scene.name + " already set up — no change.");
            return;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("LevelSystemsSetup: done — " + scene.name + " saved.");
    }

    // =========================
    // ENEMY AI
    // =========================

    static bool AddPatrolToEnemies()
    {
        bool changed = false;

        foreach (EnemyController enemy in Object.FindObjectsByType<EnemyController>(FindObjectsInactive.Include))
        {
            if (enemy.GetComponent<EnemyPatrol>() != null)
                continue;

            Undo.AddComponent<EnemyPatrol>(enemy.gameObject);
            Debug.Log("LevelSystemsSetup: added EnemyPatrol to '" + enemy.name + "'", enemy);
            changed = true;
        }

        return changed;
    }
}
