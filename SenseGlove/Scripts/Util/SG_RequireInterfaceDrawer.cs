#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(SG_RequireInterfaceAttribute))]
public sealed class SG_RequireInterfaceDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        SG_RequireInterfaceAttribute requireInterface = (SG_RequireInterfaceAttribute)attribute;

        EditorGUI.BeginProperty(position, label, property);
        EditorGUI.BeginChangeCheck();
        Object newValue = EditorGUI.ObjectField(position, label, property.objectReferenceValue, typeof(MonoBehaviour), true);
        if (EditorGUI.EndChangeCheck())
        {
            if (newValue == null)
                property.objectReferenceValue = null;
            else if (requireInterface.InterfaceType.IsAssignableFrom(newValue.GetType()))
                property.objectReferenceValue = newValue;
            else
            {
                Debug.LogError( $"'{newValue.name}' does not implement {requireInterface.InterfaceType.Name}. The reference has been cleared.", newValue);
                property.objectReferenceValue = null;
            }
        }
        EditorGUI.EndProperty();
    }
}
#endif