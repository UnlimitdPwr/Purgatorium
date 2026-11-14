using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

// Shield Inspector: the normal fields (the scaling ranks show as E-S
// dropdowns), plus a preview of what each rank is worth at a few stat levels,
// so designers can see the effect without entering Play mode.
[CustomEditor(typeof(ShieldData))]
public class ShieldDataEditor : Editor
{
    private static readonly int[] PreviewLevels = { 1, 10, 25, 40 };

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var shield = (ShieldData)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Scaling Preview", EditorStyles.boldLabel);

        var text = new StringBuilder();
        text.Append("Bonus at stat level:  ");
        foreach (int level in PreviewLevels)
            text.Append("Lv" + level + "    ");
        text.AppendLine();

        AppendRow(text, "Strength (block)", shield.strengthScaling);
        AppendRow(text, "Dexterity (parry)", shield.dexterityScaling);
        AppendRow(text, "Soul Dur. (durability + status)", shield.soulDurabilityScaling);

        float soulBonusAt40 = StatScaling.GetBonus(shield.soulDurabilityScaling, 40);
        text.AppendLine();
        text.Append("Max durability: " + shield.baseDurability.ToString("0") + " at Lv1, " +
                    (shield.baseDurability * (1f + soulBonusAt40)).ToString("0") + " at Lv40");

        EditorGUILayout.HelpBox(text.ToString(), MessageType.None);

        if (Resources.Load<StatScalingSettings>(StatScaling.ResourcePath) == null)
        {
            EditorGUILayout.HelpBox("No StatScalingSettings asset yet, so built-in defaults are used. " +
                                    "Tools > Shields > Create Stat Scaling Settings makes one to tune.",
                                    MessageType.Info);
        }
    }

    private static void AppendRow(StringBuilder text, string label, ScalingRank rank)
    {
        text.Append(label + " [" + rank + "]:  ");

        foreach (int level in PreviewLevels)
            text.Append("+" + (StatScaling.GetBonus(rank, level) * 100f).ToString("0") + "%   ");

        text.AppendLine();
    }

    // =========================
    // SETTINGS ASSET
    // =========================

    private const string SettingsFolder = "Assets/Resources";
    private const string SettingsPath = SettingsFolder + "/" + StatScaling.ResourcePath + ".asset";

    [MenuItem("Tools/Shields/Create Stat Scaling Settings")]
    public static void CreateStatScalingSettings()
    {
        var existing = AssetDatabase.LoadAssetAtPath<StatScalingSettings>(SettingsPath);

        if (existing == null)
        {
            if (!Directory.Exists(SettingsFolder))
                AssetDatabase.CreateFolder("Assets", "Resources");

            existing = CreateInstance<StatScalingSettings>();
            AssetDatabase.CreateAsset(existing, SettingsPath);
            AssetDatabase.SaveAssets();
            Debug.Log("Created " + SettingsPath);
        }

        Selection.activeObject = existing;
    }
}
