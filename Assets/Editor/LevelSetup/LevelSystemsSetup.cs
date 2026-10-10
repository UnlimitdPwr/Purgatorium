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
        changed |= AddToEnemies<EnemyJump>();
        changed |= AddToEnemies<EnemyRetreat>();
        changed |= AddToEnemies<EnemyJumpAttack>();
        changed |= AddToEnemies<EnemyAttackToken>();
        changed |= AddToEnemies<EnemyLoot>();
        changed |= AddToEnemies<EnemyRespawn>();
        changed |= AddToEnemies<EnemyStatusOnHit>();
        changed |= AddPlayerStatsToPlayers();

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

    // =========================
    // PLAYER
    // =========================

    // PlayerHealth and PlayerStamina read their max values from PlayerStats.
    // The script defaults match SampleScene's Player, so no values are set here.
    static bool AddPlayerStatsToPlayers()
    {
        bool changed = false;

        foreach (PlayerHealth player in Object.FindObjectsByType<PlayerHealth>(FindObjectsInactive.Include))
        {
            if (player.GetComponent<PlayerStats>() != null)
                continue;

            Undo.AddComponent<PlayerStats>(player.gameObject);
            Debug.Log("LevelSystemsSetup: added PlayerStats to '" + player.name + "'", player);
            changed = true;
        }

        return changed;
    }

    // Adds component T to every enemy that doesn't have it yet.
    static bool AddToEnemies<T>() where T : Component
    {
        bool changed = false;

        foreach (EnemyController enemy in Object.FindObjectsByType<EnemyController>(FindObjectsInactive.Include))
        {
            if (enemy.GetComponent<T>() != null)
                continue;

            Undo.AddComponent<T>(enemy.gameObject);
            Debug.Log("LevelSystemsSetup: added " + typeof(T).Name + " to '" + enemy.name + "'", enemy);
            changed = true;
        }

        return changed;
    }
}
