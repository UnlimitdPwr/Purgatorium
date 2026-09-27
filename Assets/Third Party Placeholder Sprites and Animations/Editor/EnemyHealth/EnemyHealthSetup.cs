using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// One-shot editor helper for wiring up the enemy dual health bar system. Adds
// EnemyHealth + EnemyDeath to every enemy in SampleScene, then builds a small
// world-space health bar canvas above each one's head (stacked Primary over
// Secondary), so the scene doesn't have to be hand-edited. Safe to re-run and
// safe to delete once the scene is committed.
public static class EnemyHealthSetup
{
    const string ScenePath = "Assets/Scenes/SampleScene.unity";
    const string CanvasName = "EnemyHealthCanvas";

    // Same bar sprite the player's health/stamina bars use, recolored per bar.
    const string BarSpriteGuid = "33dccf83b7f7f294992c97ebe1990464";

    static readonly Color BackgroundColor = new Color(0.21960786f, 0.21960786f, 0.21960786f, 1f);
    static readonly Color PrimaryColor = new Color(1f, 0.6f, 0.1f, 1f);
    static readonly Color SecondaryColor = new Color(0.7f, 0.2f, 0.9f, 1f);

    // World-space sizing, independent of any individual enemy's own transform
    // scale (compensated for per-enemy below) — deliberately small relative to
    // the player's 300x24 screen-space UI bars.
    const float CanvasWorldScale = 0.0035f;
    const float BarWidth = 240f;
    const float BarHeight = 26f;
    const float BarGap = 4f;
    const float WorldHeadOffset = 2.3f;

    [MenuItem("Tools/Enemy Health/Add Health Bars To Scene Enemies")]
    public static void AddEnemyHealthBars()
    {
        Scene scene = EnsureSampleSceneOpen();

        if (!scene.IsValid())
        {
            Debug.LogWarning("EnemyHealthSetup: could not open " + ScenePath);
            return;
        }

        EnemyController[] controllers =
            Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None);

        if (controllers.Length == 0)
        {
            Debug.LogWarning("EnemyHealthSetup: no EnemyController in the scene — nothing to do.");
            return;
        }

        bool changed = false;

        foreach (EnemyController controller in controllers)
        {
            changed |= SetupEnemy(controller.gameObject);
        }

        if (!changed)
        {
            Debug.Log("EnemyHealthSetup: every enemy already set up — no change.");
            return;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("EnemyHealthSetup: done — scene saved.");
    }

    static bool SetupEnemy(GameObject enemy)
    {
        bool changed = false;

        EnemyHealth health = enemy.GetComponent<EnemyHealth>();

        if (health == null)
        {
            health = Undo.AddComponent<EnemyHealth>(enemy);
            Debug.Log("EnemyHealthSetup: added EnemyHealth to '" + enemy.name + "'", enemy);
            changed = true;
        }

        if (enemy.GetComponent<EnemyDeath>() == null)
        {
            Undo.AddComponent<EnemyDeath>(enemy);
            Debug.Log("EnemyHealthSetup: added EnemyDeath to '" + enemy.name + "'", enemy);
            changed = true;
        }

        if (enemy.transform.Find(CanvasName) == null)
        {
            BuildHealthBarCanvas(enemy, health);
            Debug.Log("EnemyHealthSetup: built " + CanvasName + " above '" + enemy.name + "'", enemy);
            changed = true;
        }

        return changed;
    }

    // =========================
    // UI
    // =========================

    static void BuildHealthBarCanvas(GameObject enemy, EnemyHealth health)
    {
        Vector3 parentScale = enemy.transform.localScale;
        float scaleX = !Mathf.Approximately(parentScale.x, 0f) ? parentScale.x : 1f;
        float scaleY = !Mathf.Approximately(parentScale.y, 0f) ? parentScale.y : 1f;

        int uiLayer = enemy.layer;

        GameObject canvasGO = new GameObject(CanvasName, typeof(RectTransform), typeof(Canvas));
        Undo.RegisterCreatedObjectUndo(canvasGO, "Add " + CanvasName);
        canvasGO.layer = uiLayer;

        RectTransform canvasRT = canvasGO.GetComponent<RectTransform>();
        canvasRT.SetParent(enemy.transform, false);

        // Cancel out the enemy's own scale so the bar renders at a fixed,
        // enemy-scale-independent world size.
        canvasRT.localScale = new Vector3(CanvasWorldScale / scaleX, CanvasWorldScale / scaleY, 1f);
        canvasRT.localPosition = new Vector3(0f, WorldHeadOffset / scaleY, 0f);
        canvasRT.sizeDelta = new Vector2(BarWidth, BarHeight * 2f + BarGap);

        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;

        Image primaryFill = CreateBar(
            "PrimaryBar", canvasRT, uiLayer,
            new Vector2(0f, (BarHeight + BarGap) / 2f), PrimaryColor
        );

        Image secondaryFill = CreateBar(
            "SecondaryBar", canvasRT, uiLayer,
            new Vector2(0f, -(BarHeight + BarGap) / 2f), SecondaryColor
        );

        EnemyHealthBar barScript = Undo.AddComponent<EnemyHealthBar>(canvasGO);

        SerializedObject so = new SerializedObject(barScript);
        so.FindProperty("enemyHealth").objectReferenceValue = health;
        so.FindProperty("primaryFill").objectReferenceValue = primaryFill;
        so.FindProperty("secondaryFill").objectReferenceValue = secondaryFill;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // Builds one Background + Fill pair, sized (BarWidth x BarHeight), centered
    // at localAnchoredPosition within the canvas. Returns the Fill Image.
    static Image CreateBar(string name, RectTransform canvasRT, int layer, Vector2 anchoredPosition, Color fillColor)
    {
        GameObject barGO = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(barGO, "Add " + name);
        barGO.layer = layer;

        RectTransform barRT = barGO.GetComponent<RectTransform>();
        barRT.SetParent(canvasRT, false);
        barRT.anchorMin = new Vector2(0.5f, 0.5f);
        barRT.anchorMax = new Vector2(0.5f, 0.5f);
        barRT.pivot = new Vector2(0.5f, 0.5f);
        barRT.anchoredPosition = anchoredPosition;
        barRT.sizeDelta = new Vector2(BarWidth, BarHeight);

        Image background = CreateStretchedImage("Background", barRT, layer);
        background.color = BackgroundColor;

        Image fill = CreateStretchedImage("Fill", barRT, layer);
        fill.sprite = LoadBarSprite();
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = 0;
        fill.fillAmount = 0f; // both bars start empty
        fill.color = fillColor;

        return fill;
    }

    static Image CreateStretchedImage(string name, Transform parent, int layer)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Add " + name);
        go.layer = layer;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        return go.AddComponent<Image>();
    }

    static Sprite LoadBarSprite()
    {
        string path = AssetDatabase.GUIDToAssetPath(BarSpriteGuid);

        if (string.IsNullOrEmpty(path))
            return null;

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
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
