using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Editor helper that wires the enemy/world systems into the open scene
// (built for Scene2), so the scene never has to be hand-edited. Every step
// checks what's already there, so it's safe to re-run.
public static class LevelSystemsSetup
{
    // The optional enemy systems a fully set-up enemy has, in the order they're
    // added. EnemyPrefabTool uses the same list for the Enemy_Base prefab.
    public static readonly Type[] EnemySystems =
    {
        typeof(EnemyPatrol),
        typeof(EnemyJump),
        typeof(EnemyRetreat),
        typeof(EnemyJumpAttack),
        typeof(EnemyAttackToken),
        typeof(EnemyLoot),
        typeof(EnemyRespawn),
        typeof(EnemyStatusOnHit),
        typeof(EnemySearch),
        typeof(EnemyGroupAlert),
    };

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

        foreach (EnemyController enemy in Object.FindObjectsByType<EnemyController>(FindObjectsInactive.Include))
            changed |= AddMissingEnemySystems(enemy.gameObject);

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

    // Adds every system in EnemySystems that the enemy doesn't have yet.
    public static bool AddMissingEnemySystems(GameObject enemy)
    {
        bool changed = false;

        foreach (Type system in EnemySystems)
        {
            if (enemy.GetComponent(system) != null)
                continue;

            Undo.AddComponent(enemy, system);
            Debug.Log("LevelSystemsSetup: added " + system.Name + " to '" + enemy.name + "'", enemy);
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
}
