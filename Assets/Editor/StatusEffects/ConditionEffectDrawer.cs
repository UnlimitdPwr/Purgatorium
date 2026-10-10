using UnityEditor;
using UnityEngine;

// Inspector drawing for condition effects (see ConditionEffects.cs): each
// effect is one tick box with its name, and its settings only show up
// underneath while it's ticked. Display only — the values are ordinary
// serialized fields either way.
[CustomPropertyDrawer(typeof(ConditionEffect), true)]
public class ConditionEffectDrawer : PropertyDrawer
{
    const float Spacing = 2f;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float height = EditorGUIUtility.singleLineHeight;

        if (!property.FindPropertyRelative("enabled").boolValue)
            return height;

        foreach (SerializedProperty child in Settings(property))
            height += EditorGUI.GetPropertyHeight(child, true) + Spacing;

        return height;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        SerializedProperty enabled = property.FindPropertyRelative("enabled");

        Rect line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        enabled.boolValue = EditorGUI.ToggleLeft(line, label, enabled.boolValue, EditorStyles.boldLabel);

        if (enabled.boolValue)
        {
            EditorGUI.indentLevel++;
            float y = line.yMax + Spacing;

            foreach (SerializedProperty child in Settings(property))
            {
                float h = EditorGUI.GetPropertyHeight(child, true);
                EditorGUI.PropertyField(new Rect(position.x, y, position.width, h), child, true);
                y += h + Spacing;
            }

            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    // Every field of the effect except the on/off box itself.
    static System.Collections.Generic.IEnumerable<SerializedProperty> Settings(SerializedProperty property)
    {
        SerializedProperty child = property.Copy();
        SerializedProperty end = property.GetEndProperty();

        if (!child.NextVisible(true))
            yield break;

        while (!SerializedProperty.EqualContents(child, end))
        {
            if (child.name != "enabled")
                yield return child.Copy();

            if (!child.NextVisible(false))
                yield break;
        }
    }
}
