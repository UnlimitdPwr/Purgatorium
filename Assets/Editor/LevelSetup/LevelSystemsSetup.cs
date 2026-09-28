using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Editor helper that wires the enemy/world systems into the open scene
// (built for Scene2), so the scene never has to be hand-edited. Every step
// checks what's already there, so it's safe to re-run.
public static class LevelSystemsSetup
{
    const string SecondEnemyName = "Enemy 2";
    const float SecondEnemyOffsetX = 12f;

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
        changed |= AddSecondTestEnemy();

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

    // A copy of the scene's only enemy further along the ground, so patrols,
    // fights with more than one enemy, and respawning can be tested.
    static bool AddSecondTestEnemy()
    {
        EnemyController[] enemies = Object.FindObjectsByType<EnemyController>(FindObjectsInactive.Include);

        if (enemies.Length != 1 || GameObject.Find(SecondEnemyName) != null)
            return false;

        GameObject original = enemies[0].gameObject;
        Vector3 position = original.transform.position + new Vector3(SecondEnemyOffsetX, 0f, 0f);

        GameObject copy = Object.Instantiate(original, position, original.transform.rotation, original.transform.parent);
        copy.name = SecondEnemyName;
        Undo.RegisterCreatedObjectUndo(copy, "Add " + SecondEnemyName);

        Debug.Log("LevelSystemsSetup: added '" + SecondEnemyName + "' at x = " + position.x, copy);
        return true;
    }
}
