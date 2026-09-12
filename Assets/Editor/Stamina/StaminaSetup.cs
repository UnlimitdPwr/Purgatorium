using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// One-shot editor helper for wiring up the stamina system. Adds PlayerStamina to
// the Player and builds a green stamina bar under PlayerHUD (mirroring
// PlayerHealthBar's Background + fill structure) in SampleScene, so neither has
// to be hand-built. Safe to re-run and safe to delete once the scene is committed.
public static class StaminaSetup
{
    const string ScenePath = "Assets/Scenes/SampleScene.unity";
    const string PlayerTag = "Player";
    const string HudName = "PlayerHUD";
    const string HealthBarName = "PlayerHealthBar";
    const string BarName = "PlayerStaminaBar";

    // Same 9-sliced/filled bar sprite the health bar uses, just recolored.
    const string BarSpriteGuid = "33dccf83b7f7f294992c97ebe1990464";

    static readonly Color BackgroundColor = new Color(0.21960786f, 0.21960786f, 0.21960786f, 1f);
    static readonly Color StaminaColor = new Color(0.2f, 0.8f, 0.2f, 1f);

    [MenuItem("Tools/Stamina/Add Stamina System To Scene")]
    public static void AddStaminaSystem()
    {
        Scene scene = EnsureSampleSceneOpen();

        if (!scene.IsValid())
        {
            Debug.LogWarning("StaminaSetup: could not open " + ScenePath);
            return;
        }

        PlayerStamina stamina = AddStaminaToPlayer();
        bool builtBar = BuildStaminaBar(stamina);

        if (stamina == null && !builtBar)
        {
            Debug.Log("StaminaSetup: nothing to do — already set up, or no Player found.");
            return;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("StaminaSetup: done — scene saved.");
    }

    // =========================
    // PLAYER COMPONENT
    // =========================

    static PlayerStamina AddStaminaToPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag(PlayerTag);

        if (player == null)
        {
            Debug.LogWarning("StaminaSetup: no GameObject tagged '" + PlayerTag + "' in the scene.");
            return null;
        }

        PlayerStamina existing = player.GetComponent<PlayerStamina>();

        if (existing != null)
        {
            Debug.Log("StaminaSetup: Player already has PlayerStamina.");
            return existing;
        }

        PlayerStamina added = Undo.AddComponent<PlayerStamina>(player);
        Debug.Log("StaminaSetup: added PlayerStamina to '" + player.name + "'", player);
        return added;
    }

    // =========================
    // UI
    // =========================

    static bool BuildStaminaBar(PlayerStamina stamina)
    {
        if (stamina == null)
            return false;

        if (GameObject.Find(BarName) != null)
        {
            Debug.Log("StaminaSetup: " + BarName + " already exists — no change.");
            return false;
        }

        GameObject hud = GameObject.Find(HudName);

        if (hud == null)
        {
            Debug.LogWarning("StaminaSetup: no '" + HudName + "' GameObject found — skipping UI.");
            return false;
        }

        int uiLayer = hud.layer;
        Vector2 barSize = new Vector2(300f, 24f);
        Vector2 anchoredPos = new Vector2(-320f, 210f);

        // Sit directly below the health bar if it's present, so the two line up.
        GameObject healthBar = GameObject.Find(HealthBarName);
        RectTransform healthRT = healthBar != null ? healthBar.GetComponent<RectTransform>() : null;

        if (healthRT != null)
        {
            const float gap = 8f;
            barSize = healthRT.sizeDelta;
            anchoredPos = healthRT.anchoredPosition + new Vector2(0f, -(barSize.y + gap));
        }

        GameObject barGO = new GameObject(BarName, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(barGO, "Add PlayerStaminaBar");
        barGO.layer = uiLayer;

        RectTransform barRT = barGO.GetComponent<RectTransform>();
        barRT.SetParent(hud.transform, false);
        barRT.anchorMin = new Vector2(0.5f, 0.5f);
        barRT.anchorMax = new Vector2(0.5f, 0.5f);
        barRT.pivot = new Vector2(0.5f, 0.5f);
        barRT.anchoredPosition = anchoredPos;
        barRT.sizeDelta = barSize;

        Image background = CreateStretchedImage("Background", barRT, uiLayer);
        background.color = BackgroundColor;

        Image fill = CreateStretchedImage("StaminaFill", barRT, uiLayer);
        fill.sprite = LoadBarSprite();
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = 0;
        fill.fillAmount = stamina.StaminaPercent;
        fill.color = StaminaColor;

        PlayerStaminaBar barScript = Undo.AddComponent<PlayerStaminaBar>(barGO);

        SerializedObject so = new SerializedObject(barScript);
        so.FindProperty("playerStamina").objectReferenceValue = stamina;
        so.FindProperty("staminaFill").objectReferenceValue = fill;
        so.ApplyModifiedPropertiesWithoutUndo();

        Debug.Log("StaminaSetup: built " + BarName + " under '" + hud.name + "'.", barGO);
        return true;
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
