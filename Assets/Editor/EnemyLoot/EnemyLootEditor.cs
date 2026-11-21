using UnityEditor;
using UnityEngine;

// EnemyLoot Inspector: essence, then an "Item Drops" toggle. With drops on it
// shows the drop list (item + % chance + amount), fills in the pickup prefab
// automatically, and sums up the odds.
[CustomEditor(typeof(EnemyLoot))]
[CanEditMultipleObjects]
public class EnemyLootEditor : Editor
{
    private const string PickupPrefabPath = "Assets/Prefabs/ItemPickup.prefab";

    private SerializedProperty essenceReward;
    private SerializedProperty itemDropsEnabled;
    private SerializedProperty itemDrops;
    private SerializedProperty pickupPrefab;
    private SerializedProperty dropPop;

    private void OnEnable()
    {
        essenceReward = serializedObject.FindProperty("essenceReward");
        itemDropsEnabled = serializedObject.FindProperty("itemDropsEnabled");
        itemDrops = serializedObject.FindProperty("itemDrops");
        pickupPrefab = serializedObject.FindProperty("pickupPrefab");
        dropPop = serializedObject.FindProperty("dropPop");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(essenceReward);

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(itemDropsEnabled, new GUIContent("Item Drops"));

        if (itemDropsEnabled.boolValue || itemDropsEnabled.hasMultipleDifferentValues)
        {
            // Turning drops on is when the pickup prefab is needed, so fill it in then.
            if (pickupPrefab.objectReferenceValue == null && !pickupPrefab.hasMultipleDifferentValues)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PickupPrefabPath);

                if (prefab != null)
                    pickupPrefab.objectReferenceValue = prefab.GetComponent<WorldItem>();
            }

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(itemDrops, new GUIContent("Drops"), true);
            EditorGUILayout.PropertyField(pickupPrefab);
            EditorGUILayout.PropertyField(dropPop);
            EditorGUI.indentLevel--;

            if (!serializedObject.isEditingMultipleObjects)
                DrawSummary((EnemyLoot)target);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private static void DrawSummary(EnemyLoot loot)
    {
        if (loot.itemDrops == null || loot.itemDrops.Count == 0)
        {
            EditorGUILayout.HelpBox("No drops listed yet — press + to add an item.", MessageType.Info);
            return;
        }

        float nothing = 1f;
        float expected = 0f;
        bool missingItem = false;

        foreach (EnemyItemDrop drop in loot.itemDrops)
        {
            if (drop == null || drop.item == null)
            {
                missingItem = true;
                continue;
            }

            float chance = Mathf.Clamp01(drop.dropChance / 100f);
            nothing *= 1f - chance;
            expected += chance;
        }

        string text = "Chance to drop nothing: " + (nothing * 100f).ToString("0.#") + "%\n" +
                      "Average number of item drops per kill: " + expected.ToString("0.##");

        EditorGUILayout.HelpBox(text, MessageType.None);

        if (missingItem)
            EditorGUILayout.HelpBox("A drop has no item set — it will be skipped.", MessageType.Warning);

        if (loot.pickupPrefab == null)
            EditorGUILayout.HelpBox("Pickup Prefab is empty — drops can't spawn. Assign " + PickupPrefabPath + ".",
                                    MessageType.Warning);
    }
}
