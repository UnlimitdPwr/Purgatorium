using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Editor helper for the status-effect system, so scenes are never hand-edited.
// Safe to re-run — every step checks what's already there:
//   1. Makes a settings asset for any StatusType that doesn't have one yet
//      (Assets/Status Code/Definitions). Existing assets are left alone.
//   2. Adds PlayerStatusEffects to the player and EnemyStatusEffects to every
//      enemy, and hands them every status asset.
//   3. Adds the buildup bars under the player's stamina bar.
public static class StatusEffectsSetup
{
    const string DefinitionsFolder = "Assets/Status Code/Definitions";

    // Gap between the bottom of the stamina bar and the first buildup bar.
    const float GapBelowStamina = 8f;

    [MenuItem("Tools/Status Effects/Set Up Status Effects In Open Scene")]
    public static void SetUpOpenScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog(
                "Status Effects",
                "Stop Play Mode first — changes made during Play Mode are thrown away when it ends.",
                "OK");
            return;
        }

        StatusEffectDefinition[] definitions = EnsureDefinitions();

        Scene scene = SceneManager.GetActiveScene();
        bool changed = false;

        foreach (PlayerHealth player in UnityEngine.Object.FindObjectsByType<PlayerHealth>(FindObjectsInactive.Include))
            changed |= SetUp<PlayerStatusEffects>(player.gameObject, definitions);

        foreach (EnemyController enemy in UnityEngine.Object.FindObjectsByType<EnemyController>(FindObjectsInactive.Include))
            changed |= SetUp<EnemyStatusEffects>(enemy.gameObject, definitions);

        foreach (PlayerHealthBar healthBar in UnityEngine.Object.FindObjectsByType<PlayerHealthBar>(FindObjectsInactive.Include))
            changed |= AddStatusBars(healthBar);

        if (!changed)
        {
            Debug.Log("StatusEffectsSetup: " + scene.name + " already set up — no change.");
            return;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("StatusEffectsSetup: done — " + scene.name + " saved.");
    }

    // =========================
    // DEFINITIONS
    // =========================

    // Starting look for each status. Anything not listed gets white and
    // its enum name — edit the asset afterwards.
    static readonly Dictionary<StatusType, Color> DefaultColors = new Dictionary<StatusType, Color>
    {
        { StatusType.Poison, new Color(0.45f, 0.85f, 0.2f) },
        { StatusType.Burn,   new Color(1f, 0.5f, 0.1f) },
        { StatusType.Bleed,  new Color(0.8f, 0.08f, 0.12f) },
        { StatusType.Curse,  new Color(0.6f, 0.25f, 0.85f) },
    };

    static StatusEffectDefinition[] EnsureDefinitions()
    {
        StatusEffectDefinition[] existing = StatusEffectsBase.FindAllDefinitions();
        HashSet<StatusType> covered = new HashSet<StatusType>();

        foreach (StatusEffectDefinition definition in existing)
            covered.Add(definition.type);

        bool created = false;

        foreach (StatusType type in Enum.GetValues(typeof(StatusType)))
        {
            if (covered.Contains(type))
                continue;

            if (!AssetDatabase.IsValidFolder(DefinitionsFolder))
                AssetDatabase.CreateFolder(Path.GetDirectoryName(DefinitionsFolder), Path.GetFileName(DefinitionsFolder));

            StatusEffectDefinition definition = ScriptableObject.CreateInstance<StatusEffectDefinition>();
            definition.type = type;
            definition.displayName = type.ToString();
            definition.barColor = DefaultColors.TryGetValue(type, out Color color) ? color : Color.white;

            // A full curse bar kills the player outright.
            if (type == StatusType.Curse)
                definition.onPlayer.instantDeath.enabled = true;

            string path = DefinitionsFolder + "/" + type + ".asset";
            AssetDatabase.CreateAsset(definition, path);
            Debug.Log("StatusEffectsSetup: created " + path, definition);
            created = true;
        }

        if (!created)
            return existing;

        AssetDatabase.SaveAssets();
        return StatusEffectsBase.FindAllDefinitions();
    }

    // =========================
    // CHARACTERS
    // =========================

    static bool SetUp<T>(GameObject target, StatusEffectDefinition[] definitions) where T : StatusEffectsBase
    {
        bool changed = false;

        T component = target.GetComponent<T>();

        if (component == null)
        {
            component = Undo.AddComponent<T>(target);
            Debug.Log("StatusEffectsSetup: added " + typeof(T).Name + " to '" + target.name + "'", target);
            changed = true;
        }

        SerializedObject so = new SerializedObject(component);
        SerializedProperty list = so.FindProperty("statusEffects");

        if (!SameDefinitions(list, definitions))
        {
            list.arraySize = definitions.Length;

            for (int i = 0; i < definitions.Length; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = definitions[i];

            so.ApplyModifiedProperties();
            Debug.Log("StatusEffectsSetup: gave '" + target.name + "' " + definitions.Length + " statuses", target);
            changed = true;
        }

        return changed;
    }

    static bool SameDefinitions(SerializedProperty list, StatusEffectDefinition[] definitions)
    {
        if (list.arraySize != definitions.Length)
            return false;

        for (int i = 0; i < definitions.Length; i++)
        {
            if (list.GetArrayElementAtIndex(i).objectReferenceValue != definitions[i])
                return false;
        }

        return true;
    }

    // =========================
    // HUD
    // =========================

    static bool AddStatusBars(PlayerHealthBar healthBar)
    {
        Transform hud = healthBar.transform.parent;

        if (hud == null || hud.GetComponentInChildren<PlayerStatusBars>(true) != null)
            return false;

        PlayerHealth player = (PlayerHealth)new SerializedObject(healthBar)
            .FindProperty("playerHealth").objectReferenceValue;

        PlayerStatusEffects status = player != null ? player.GetComponent<PlayerStatusEffects>() : null;

        if (status == null)
        {
            Debug.LogWarning("StatusEffectsSetup: '" + healthBar.name + "' has no player with " +
                             "PlayerStatusEffects — buildup bars not added.", healthBar);
            return false;
        }

        // Sit under the stamina bar if there is one, otherwise the health bar.
        PlayerStaminaBar staminaBar = hud.GetComponentInChildren<PlayerStaminaBar>(true);
        RectTransform above = staminaBar != null
            ? (RectTransform)staminaBar.transform
            : (RectTransform)healthBar.transform;

        GameObject go = new GameObject("PlayerStatusBars", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Add Player Status Bars");
        go.layer = above.gameObject.layer;

        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(hud, false);
        rect.SetSiblingIndex(above.GetSiblingIndex() + 1);
        rect.anchorMin = above.anchorMin;
        rect.anchorMax = above.anchorMax;
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = new Vector2(above.sizeDelta.x, 50f);

        // Left edge lined up with the bar above; top just below its bottom edge.
        float aboveLeft = above.anchoredPosition.x - above.pivot.x * above.sizeDelta.x;
        float aboveBottom = above.anchoredPosition.y - above.pivot.y * above.sizeDelta.y;
        rect.anchoredPosition = new Vector2(aboveLeft, aboveBottom - GapBelowStamina);

        PlayerStatusBars bars = go.AddComponent<PlayerStatusBars>();
        SerializedObject so = new SerializedObject(bars);
        so.FindProperty("playerStatus").objectReferenceValue = status;
        so.ApplyModifiedPropertiesWithoutUndo();

        Debug.Log("StatusEffectsSetup: added PlayerStatusBars under '" + above.name + "'", go);
        return true;
    }
}
