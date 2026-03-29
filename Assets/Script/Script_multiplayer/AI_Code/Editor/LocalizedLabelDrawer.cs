#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace DoAnGame.UI.Editor
{
    /// <summary>
    /// Tùy biến Inspector để hiển thị nhãn song ngữ theo LocalizedLabelAttribute.
    /// </summary>
    [CustomPropertyDrawer(typeof(LocalizedLabelAttribute))]
    public class LocalizedLabelDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var localized = (LocalizedLabelAttribute)attribute;
            var displayLabel = new GUIContent(string.IsNullOrEmpty(localized.Label) ? label.text : localized.Label, label.tooltip);
            EditorGUI.PropertyField(position, property, displayLabel, true);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
    }
}
#endif
