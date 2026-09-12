using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// One-shot editor helper for wiring up the dash mechanic. Adds the DashScript
// component (introduced with feature/dash) to the Player in SampleScene so the
// scene doesn't have to be hand-edited. Safe to re-run and safe to delete once
// the scene is committed.
public static class DashSetup
{
    const string ScenePath = "Assets/Scenes/SampleScene.unity";
    const string PlayerTag = "Player";

    [MenuItem("Tools/Dash/Add DashScript To Player")]
    public static void AddDashScript()
    {
        Scene scene = EnsureSampleSceneOpen();

        if (!scene.IsValid())
        {
            Debug.LogWarning("DashSetup: could not open " + ScenePath);
            return;
        }

        GameObject player = GameObject.FindGameObjectWithTag(PlayerTag);

        if (player == null)
        {
            Debug.LogWarning("DashSetup: no GameObject tagged '" + PlayerTag + "' in the scene.");
            return;
        }

        if (player.GetComponent<DashScript>() != null)
        {
            Debug.Log("DashSetup: Player already has DashScript — no change.");
            return;
        }

        Undo.AddComponent<DashScript>(player);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("DashSetup: done — DashScript added to '" + player.name + "' and scene saved.", player);
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
