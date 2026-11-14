using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Tools > Shields > Convert Shield Items To ShieldData
//
// Shields made before the ShieldData blueprint existed are plain ItemData
// assets with equipmentType = Shield. This switches each of them to the
// ShieldData script IN PLACE: same file, same GUID, so every inventory,
// pickup and scene reference keeps pointing at it, and the name, description,
// sprites and stack size are kept. The new shield fields start at their
// defaults (which match the current no-shield parry/block values).
public static class ShieldDataConverter
{
    [MenuItem("Tools/Shields/Convert Shield Items To ShieldData")]
    public static void ConvertShieldItems()
    {
        MonoScript shieldScript = FindShieldDataScript();

        if (shieldScript == null)
        {
            Debug.LogError("ShieldDataConverter: couldn't find the ShieldData script.");
            return;
        }

        var converted = new List<string>();

        foreach (string guid in AssetDatabase.FindAssets("t:ItemData"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(path);

            if (item == null || item is ShieldData || item.equipmentType != EquipmentSlotType.Shield)
                continue;

            var serialized = new SerializedObject(item);
            serialized.FindProperty("m_Script").objectReferenceValue = shieldScript;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            converted.Add(path);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string message = converted.Count == 0
            ? "No plain-ItemData shields found — nothing to convert."
            : "Converted " + converted.Count + " shield(s) to ShieldData:\n\n" + string.Join("\n", converted);

        Debug.Log("ShieldDataConverter: " + message);
        EditorUtility.DisplayDialog("Convert Shields", message, "OK");
    }

    private static MonoScript FindShieldDataScript()
    {
        ShieldData temp = ScriptableObject.CreateInstance<ShieldData>();
        MonoScript script = MonoScript.FromScriptableObject(temp);
        Object.DestroyImmediate(temp);
        return script;
    }
}
