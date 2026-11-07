using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Fixes Confirm / Cancel / Back on the bonfire Stats screen not responding
// to clicks. StatsPanel/StatsContainer (the translucent box behind the stat
// rows) stretches with the screen, so on most resolutions it covers those
// three buttons — and as the panel's last child it sat on top of them,
// catching every click with its background Image.
//
// This makes the box's background ignore clicks (the stat rows' own +/-
// buttons inside it still work) and moves the box behind the other panel
// contents so it no longer tints the buttons. Safe to re-run.
public static class BonfireStatsClickFix
{
    [MenuItem("Tools/Bonfire UI/Fix Stats Panel Buttons In Open Scene")]
    public static void FixOpenScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog(
                "Bonfire UI",
                "Stop Play Mode first — changes made during Play Mode are thrown away when it ends.",
                "OK");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        bool changed = false;

        foreach (BonfireStatsUI statsUI in Object.FindObjectsByType<BonfireStatsUI>(FindObjectsInactive.Include))
        {
            Transform container = statsUI.transform.Find("StatsContainer");

            if (container == null)
            {
                Debug.LogWarning("BonfireStatsClickFix: no StatsContainer under '" + statsUI.name + "'.", statsUI);
                continue;
            }

            Image background = container.GetComponent<Image>();

            if (background != null && background.raycastTarget)
            {
                Undo.RecordObject(background, "Stats box ignores clicks");
                background.raycastTarget = false;
                changed = true;
            }

            // Draw it first (behind everything else on the panel).
            if (container.GetSiblingIndex() != 0)
            {
                Undo.SetSiblingIndex(container, 0, "Move stats box behind buttons");
                changed = true;
            }

            Debug.Log("BonfireStatsClickFix: fixed '" + container.name + "' in " + scene.name, container);
        }

        if (!changed)
        {
            Debug.Log("BonfireStatsClickFix: " + scene.name + " already fixed — no change.");
            return;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }
}
