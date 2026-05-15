using Tweenimator.Runtime.Attributes;
using UnityEditor;
using UnityEngine;

namespace Tweenimator.Editor.CustomEditors
{
    [CustomPropertyDrawer(typeof(InspectorReadOnlyAttribute))]
    public class InspectorReadOnlyPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            GUI.enabled = false; // Делаем поле доступным только для чтения
            EditorGUI.PropertyField(position, property, label);
            GUI.enabled = true; // Восстанавливаем обычное состояние
        }
    }
}
