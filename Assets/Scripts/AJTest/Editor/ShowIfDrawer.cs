using System.Reflection;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(ShowIfAttribute))]
public class ShowIfDrawer : PropertyDrawer
{
    bool IsShown(SerializedProperty property)
    {
        var showIf = (ShowIfAttribute)attribute;
        string path = property.propertyPath;
        int dot = path.LastIndexOf('.');
        string prefix = dot >= 0 ? path.Substring(0, dot + 1) : "";

        // Every listed condition must pass ("a, !b" = a ticked AND b unticked).
        foreach (string part in showIf.condition.Split(','))
        {
            string name = part.Trim();
            bool want = showIf.showWhen;
            if (name.StartsWith("!")) { want = !want; name = name.Substring(1).Trim(); }
            SerializedProperty cond = property.serializedObject.FindProperty(prefix + name);
            if (cond == null || cond.propertyType != SerializedPropertyType.Boolean) continue;
            if (cond.boolValue != want) return false;
        }
        return true;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!IsShown(property)) return -EditorGUIUtility.standardVerticalSpacing;
        return EditorGUI.GetPropertyHeight(property, label, true);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (!IsShown(property)) return;

        // Unity only uses one drawer per field, so keep [Range] sliders working here.
        var range = fieldInfo.GetCustomAttribute<RangeAttribute>();
        if (range != null && property.propertyType == SerializedPropertyType.Float)
            EditorGUI.Slider(position, property, range.min, range.max, label);
        else if (range != null && property.propertyType == SerializedPropertyType.Integer)
            EditorGUI.IntSlider(position, property, (int)range.min, (int)range.max, label);
        else
            EditorGUI.PropertyField(position, property, label, true);
    }
}
